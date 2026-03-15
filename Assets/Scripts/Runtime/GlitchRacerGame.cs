using UnityEngine;
using YG;
#if PlayerStats_yg
using YG;
#endif

namespace GlitchRacer
{
    public class GlitchRacerGame : MonoBehaviour
    {
        public enum GlitchType
        {
            None,
            InvertControls,
            StaticNoise,
            DrunkVision,
            DrugsTrip
        }

        public enum SessionState
        {
            MainMenu,
            Playing,
            Shop,
            Settings,
            GameOver
        }

        [Header("Run Economy")]
        [SerializeField] private float maxRam = 100f;
        [SerializeField] private float ramDrainPerSecond = 5.5f;
        [SerializeField] private float startSpeed = 16f;
        [SerializeField] private float maxSpeed = 34f;
        [SerializeField] private float speedRampPerSecond = 0.65f;
        [SerializeField] private float distanceScoreFactor = 8f;
        [SerializeField] private float menuDemoSpeed = 18f;
        [SerializeField] private float chapterDistanceInterval = 320f;
        [SerializeField] private float chapterRushDuration = 5f;
        [SerializeField] private float chapterRushVisualSpeedMultiplier = 2.8f;

        private RunnerPlayer player;
        private TrackSegmentSpawner spawner;
        private GlitchCameraRig cameraRig;
        private GlitchRacerHud hud;
        private GlitchSynthEngine audioSynth;
        private GlitchRacerSaveData saveData;

        public float CurrentRam { get; private set; }
        public float MaxRam => maxRam;
        public float CurrentSpeed { get; private set; }
        public float VisualScrollSpeed => IsChapterRush ? CurrentSpeed * GetChapterRushSpeedFactor() : CurrentSpeed;
        public float Score { get; private set; }
        public float CurrentDistance { get; private set; }
        public float BestScore => saveData.bestScore;
        public float BestDistance => saveData.bestDistance;
        public float TotalDistance => saveData.totalDistance;
        public int Coins => saveData.coins;
        public int CollectedDataShards { get; private set; }
        public int LastRunCoinsReward { get; private set; }
        public bool HasUsedRevive { get; private set; }
        public SessionState State { get; private set; }
#if Localization_yg
        public string CurrentLanguage => GlitchRacerLocalization.NormalizeLanguage(YG2.lang);
#else
        public string CurrentLanguage => GlitchRacerLocalization.NormalizeLanguage(saveData?.languageCode ?? "en");
#endif
        public bool IsGameOver => State == SessionState.GameOver;
        public bool IsMenuVisible => State == SessionState.MainMenu || State == SessionState.Shop || State == SessionState.Settings;
        public bool IsInputEnabled => State == SessionState.Playing;
        public bool IsDemoMode => State != SessionState.Playing;
        public bool ControlsInverted => glitchTimer > 0f && activeGlitch == GlitchType.InvertControls;
        public bool HasStaticNoise => glitchTimer > 0f && activeGlitch == GlitchType.StaticNoise;
        public bool HasDrunkVision => glitchTimer > 0f && activeGlitch == GlitchType.DrunkVision;
        public bool HasDrugsTrip => glitchTimer > 0f && activeGlitch == GlitchType.DrugsTrip;
        public bool IsChapterRush => chapterRushTimer > 0f;
        public bool IsRamCritical => State == SessionState.Playing && CurrentRam < 30f;
        public float ChapterRushTimeRemaining => chapterRushTimer;
        public float GlitchTimeRemaining => glitchTimer;
        public GlitchType ActiveGlitch => glitchTimer > 0f ? activeGlitch : GlitchType.None;
        public string ActiveGlitchLabel => GlitchRacerLocalization.ActiveGlitchLabel(ActiveGlitch, CurrentLanguage);
        public float FuelDrainMultiplier => Mathf.Max(0.55f, 1f - (saveData.fuelUpgradeLevel * 0.08f));
        public float ScoreMultiplier => 1f + (saveData.scoreUpgradeLevel * 0.12f);
        public int FuelUpgradeLevel => saveData.fuelUpgradeLevel;
        public int ScoreUpgradeLevel => saveData.scoreUpgradeLevel;
        public bool MusicEnabled => saveData.musicEnabled;
        public bool SfxEnabled => saveData.sfxEnabled;
        public int FuelUpgradeCost => 120 + (saveData.fuelUpgradeLevel * 90);
        public int ScoreUpgradeCost => 140 + (saveData.scoreUpgradeLevel * 110);

        private float glitchTimer;
        private GlitchType activeGlitch;
        private float chapterRushTimer;
        private float nextChapterDistance;

        // FIX: Configure теперь НЕ вызывает EnterMainMenu().
        // Bootstrap сам вызовет EnterMainMenu() после Configure(), когда все ссылки уже заданы.
        public void Configure(RunnerPlayer playerController, TrackSegmentSpawner trackSpawner, GlitchCameraRig rig, GlitchRacerHud gameHud, GlitchSynthEngine gameSynth)
        {
            player = playerController;
            spawner = trackSpawner;
            cameraRig = rig;
            hud = gameHud;
            audioSynth = gameSynth;
        }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            saveData = GlitchRacerSaveSystem.Load();
            InitializeLanguage();
#if PlayerStats_yg
            YG2.onGetSDKData += HandleCloudSaveLoaded;
            YG2.onRewardAdv += OnRewardAdv;
#endif
#if Localization_yg
            YG2.onSwitchLang += HandleLanguageSwitched;
#endif
            // FIX: EnterMainMenu() убран отсюда. Раньше вызывался здесь когда player/spawner/rig/hud
            // ещё не были назначены через Configure() — все вызовы внутри (player?.ResetRunner() и т.д.)
            // уходили в никуда. Bootstrap вызывает EnterMainMenu() явно после Configure().
        }

        private void OnDestroy()
        {
#if PlayerStats_yg
            YG2.onGetSDKData -= HandleCloudSaveLoaded;
            YG2.onRewardAdv -= OnRewardAdv;
#endif
#if Localization_yg
            YG2.onSwitchLang -= HandleLanguageSwitched;
#endif
        }

        private void OnRewardAdv(string id)
        {
            if (id == "Revive")
            {
                ReviveFromAd();
            }
        }

        private void Update()
        {
            SimulateRun();
        }

        public void AddScore(float amount)
        {
            if (State != SessionState.Playing || IsChapterRush)
            {
                return;
            }

            Score += amount * ScoreMultiplier;
        }

        public void AddRam(float amount)
        {
            if (State != SessionState.Playing || IsChapterRush)
            {
                return;
            }

            CurrentRam = Mathf.Clamp(CurrentRam + amount, 0f, maxRam);
            audioSynth?.PlayPickupSound();
        }

        public void HitObstacle(float ramDamage)
        {
            if (State != SessionState.Playing)
            {
                return;
            }

            CurrentRam = Mathf.Max(0f, CurrentRam - ramDamage);
            cameraRig?.Punch();
            audioSynth?.PlayDamageSound();

            if (CurrentRam <= 0f)
            {
                EndRun();
            }
        }

        private void ReviveFromAd()
        {
            if (State != SessionState.GameOver)
            {
                return;
            }

            State = SessionState.Playing;
            HasUsedRevive = true;
            CurrentRam = maxRam;
            glitchTimer = 0f;
            activeGlitch = GlitchType.None;
            player?.SetControlMode(true);
            spawner?.ClearImminentObstacles();
        }

        public void TriggerGlitch(float duration, float bonusScore, GlitchType glitchType)
        {
            if (State != SessionState.Playing)
            {
                return;
            }

            glitchTimer = Mathf.Max(glitchTimer, duration);
            activeGlitch = glitchType;
            AddScore(bonusScore);
            AddRam(8f);
            cameraRig?.Punch();
            audioSynth?.PlayDamageSound(); // plays a cool crunchy noise good for glitches
        }

        public void CollectDataShard(float value)
        {
            if (State != SessionState.Playing)
            {
                return;
            }

            CollectedDataShards++;
            AddScore(value);
            audioSynth?.PlayPickupSound();
        }

        public void StartGame()
        {
            State = SessionState.Playing;
            HasUsedRevive = false;
            ResetRunRuntime();
            player?.SetControlMode(true);
            spawner?.ResetTrack();
            player?.ResetRunner();
        }

        public void EnterMainMenu()
        {
            State = SessionState.MainMenu;
            LastRunCoinsReward = 0;
            HasUsedRevive = false;
            ResetRunRuntime();
            player?.SetControlMode(false);
            spawner?.ResetTrack();
            player?.ResetRunner();
        }

        public void OpenShop()
        {
            State = SessionState.Shop;
        }

        public void OpenSettings()
        {
            State = SessionState.Settings;
        }

        public void CloseOverlayToMenu()
        {
            State = SessionState.MainMenu;
        }

        public void ToggleMusic()
        {
            saveData.musicEnabled = !saveData.musicEnabled;
            SaveProgress();
        }

        public void ToggleSfx()
        {
            saveData.sfxEnabled = !saveData.sfxEnabled;
            SaveProgress();
        }

        public void ToggleLanguage()
        {
            SetLanguage(CurrentLanguage == "ru" ? "en" : "ru");
        }

        public bool TryBuyFuelUpgrade()
        {
            if (saveData.coins < FuelUpgradeCost)
            {
                return false;
            }

            saveData.coins -= FuelUpgradeCost;
            saveData.fuelUpgradeLevel++;
            SaveProgress();
            return true;
        }

        public bool TryBuyScoreUpgrade()
        {
            if (saveData.coins < ScoreUpgradeCost)
            {
                return false;
            }

            saveData.coins -= ScoreUpgradeCost;
            saveData.scoreUpgradeLevel++;
            SaveProgress();
            return true;
        }

        public int CalculateCoinsReward()
        {
            float shardValue = CollectedDataShards * 3.5f;
            float scoreValue = Score * 0.03f;
            float distanceValue = CurrentDistance * 0.12f;
            return Mathf.Max(0, Mathf.RoundToInt(shardValue + scoreValue + distanceValue));
        }

        private void ResetRunRuntime()
        {
            CurrentRam = maxRam;
            CurrentSpeed = startSpeed;
            Score = 0f;
            CurrentDistance = 0f;
            CollectedDataShards = 0;
            glitchTimer = 0f;
            activeGlitch = GlitchType.None;
            chapterRushTimer = 0f;
            nextChapterDistance = chapterDistanceInterval;
        }

        private void SimulateRun()
        {
            CurrentRam = Mathf.Clamp(CurrentRam, 0f, maxRam);

            bool runActive = State == SessionState.Playing || IsMenuVisible;
            if (!runActive)
            {
                return;
            }

            float targetSpeed = State == SessionState.Playing
                ? Mathf.MoveTowards(CurrentSpeed, maxSpeed, speedRampPerSecond * Time.deltaTime)
                : menuDemoSpeed;

            CurrentSpeed = targetSpeed;

            if (State != SessionState.Playing || !IsChapterRush)
            {
                CurrentDistance += CurrentSpeed * Time.deltaTime;
            }

            if (State == SessionState.Playing)
            {
                if (!IsChapterRush && CurrentDistance >= nextChapterDistance)
                {
                    TriggerChapterRush();
                }

                if (!IsChapterRush)
                {
                    CurrentRam = Mathf.Max(0f, CurrentRam - (ramDrainPerSecond * FuelDrainMultiplier * Time.deltaTime));
                    Score += CurrentSpeed * distanceScoreFactor * ScoreMultiplier * Time.deltaTime;
                }

                if (CurrentRam <= 0f)
                {
                    EndRun();
                }
            }
            else
            {
                CurrentRam = maxRam;
            }

            if (glitchTimer > 0f)
            {
                glitchTimer = Mathf.Max(0f, glitchTimer - Time.deltaTime);
                if (glitchTimer <= 0f)
                {
                    activeGlitch = GlitchType.None;
                }
            }

            if (chapterRushTimer > 0f)
            {
                chapterRushTimer = Mathf.Max(0f, chapterRushTimer - Time.deltaTime);
            }
        }

        private void EndRun()
        {
            State = SessionState.GameOver;
            audioSynth?.PlayGameOverSound();
            LastRunCoinsReward = CalculateCoinsReward();
            saveData.coins += LastRunCoinsReward;
            saveData.bestScore = Mathf.Max(saveData.bestScore, Score);
            saveData.bestDistance = Mathf.Max(saveData.bestDistance, CurrentDistance);
            saveData.totalDistance += CurrentDistance;
            SaveProgress();

            YG2.InterstitialAdvShow();
        }

        private void SaveProgress()
        {
            GlitchRacerSaveSystem.Save(saveData);
        }

        private void InitializeLanguage()
        {
#if !Localization_yg
            if (string.IsNullOrEmpty(saveData?.languageCode))
            {
                if (saveData != null) saveData.languageCode = "en";
            }
#endif
        }

        private void SetLanguage(string language)
        {
            string normalized = GlitchRacerLocalization.NormalizeLanguage(language);
#if Localization_yg
            if (YG2.lang != normalized)
            {
                YG2.SwitchLanguage(normalized);
            }
#else
            HandleLanguageSwitched(normalized);
#endif
        }

        private void HandleLanguageSwitched(string language)
        {
#if !Localization_yg
            string normalized = GlitchRacerLocalization.NormalizeLanguage(language);
            if (saveData != null)
            {
                saveData.languageCode = normalized;
                SaveProgress();
            }
#endif
            hud?.RefreshLocalization();
        }

        private void TriggerChapterRush()
        {
            chapterRushTimer = chapterRushDuration;
            nextChapterDistance += chapterDistanceInterval;
            cameraRig?.Punch();
        }

        private void HandleCloudSaveLoaded()
        {
            if (State == SessionState.Playing)
            {
                return;
            }

            saveData = GlitchRacerSaveSystem.Load();
            InitializeLanguage();
            hud?.RefreshLocalization();
        }

        private float GetChapterRushSpeedFactor()
        {
            float normalized = 1f - Mathf.Clamp01(chapterRushTimer / Mathf.Max(0.01f, chapterRushDuration));
            float envelope = Mathf.Sin(normalized * Mathf.PI);
            return Mathf.Lerp(1f, chapterRushVisualSpeedMultiplier, envelope);
        }
    }

    [System.Serializable]
    public class GlitchRacerSaveData
    {
        public int coins;
        public float bestScore;
        public float bestDistance;
        public float totalDistance;
        public int fuelUpgradeLevel;
        public int scoreUpgradeLevel;
        public bool musicEnabled = true;
        public bool sfxEnabled = true;
        public string languageCode;
    }

    public static class GlitchRacerSaveSystem
    {
        private const string SaveKey = "glitch_racer_save_v1";
#if PlayerStats_yg
        private const string InitKey = "gr_save_initialized";
        private const string CoinsKey = "gr_coins";
        private const string BestScoreKey = "gr_best_score";
        private const string BestDistanceKey = "gr_best_distance";
        private const string TotalDistanceKey = "gr_total_distance";
        private const string FuelUpgradeKey = "gr_fuel_upgrade";
        private const string ScoreUpgradeKey = "gr_score_upgrade";
        private const string MusicEnabledKey = "gr_music_enabled";
        private const string SfxEnabledKey = "gr_sfx_enabled";
        private const string LanguageKey = "gr_language";
#endif

        public static GlitchRacerSaveData Load()
        {
#if PlayerStats_yg
            if (YG2.isSDKEnabled && YG2.GetState(InitKey) == 1)
            {
                return FromYG2Stats();
            }
#endif

            if (!PlayerPrefs.HasKey(SaveKey))
            {
                return new GlitchRacerSaveData();
            }

            string json = PlayerPrefs.GetString(SaveKey, string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                return new GlitchRacerSaveData();
            }

            try
            {
                return JsonUtility.FromJson<GlitchRacerSaveData>(json) ?? new GlitchRacerSaveData();
            }
            catch
            {
                return new GlitchRacerSaveData();
            }
        }

        public static void Save(GlitchRacerSaveData data)
        {
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();

#if PlayerStats_yg
            if (YG2.isSDKEnabled)
            {
                ApplyToYG2Stats(data);
            }
#endif
        }

#if PlayerStats_yg
        private static GlitchRacerSaveData FromYG2Stats()
        {
            return new GlitchRacerSaveData
            {
                coins = YG2.GetState(CoinsKey),
                bestScore = YG2.GetState(BestScoreKey),
                bestDistance = YG2.GetState(BestDistanceKey),
                totalDistance = YG2.GetState(TotalDistanceKey),
                fuelUpgradeLevel = YG2.GetState(FuelUpgradeKey),
                scoreUpgradeLevel = YG2.GetState(ScoreUpgradeKey),
                musicEnabled = YG2.GetState(MusicEnabledKey) != 0,
                sfxEnabled = YG2.GetState(SfxEnabledKey) != 0,
                languageCode = YG2.GetState(LanguageKey) == 1 ? "ru" : "en"
            };
        }

        private static void ApplyToYG2Stats(GlitchRacerSaveData data)
        {
            var stats = YG2.GetAllStats() != null
                ? new System.Collections.Generic.Dictionary<string, int>(YG2.GetAllStats())
                : new System.Collections.Generic.Dictionary<string, int>();

            stats[InitKey] = 1;
            stats[CoinsKey] = data.coins;
            stats[BestScoreKey] = Mathf.RoundToInt(data.bestScore);
            stats[BestDistanceKey] = Mathf.RoundToInt(data.bestDistance);
            stats[TotalDistanceKey] = Mathf.RoundToInt(data.totalDistance);
            stats[FuelUpgradeKey] = data.fuelUpgradeLevel;
            stats[ScoreUpgradeKey] = data.scoreUpgradeLevel;
            stats[MusicEnabledKey] = data.musicEnabled ? 1 : 0;
            stats[SfxEnabledKey] = data.sfxEnabled ? 1 : 0;
            stats[LanguageKey] = GlitchRacerLocalization.NormalizeLanguage(data.languageCode) == "ru" ? 1 : 0;

            YG2.SetAllStats(stats);
        }
#endif
    }
}
