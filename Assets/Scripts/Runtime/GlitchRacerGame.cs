using UnityEngine;

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
        private GlitchRacerSaveData saveData;

        public float CurrentRam { get; private set; }
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
        public SessionState State { get; private set; }
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
        public string ActiveGlitchLabel => ActiveGlitch switch
        {
            GlitchType.InvertControls => "controls inverted",
            GlitchType.StaticNoise => "signal noise",
            GlitchType.DrunkVision => "vision drifting",
            GlitchType.DrugsTrip => "drugs trip",
            _ => "stable"
        };
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

        public void Configure(RunnerPlayer playerController, TrackSegmentSpawner trackSpawner, GlitchCameraRig rig, GlitchRacerHud gameHud)
        {
            player = playerController;
            spawner = trackSpawner;
            cameraRig = rig;
            hud = gameHud;
        }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            saveData = GlitchRacerSaveSystem.Load();
            EnterMainMenu();
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
        }

        public void HitObstacle(float ramDamage)
        {
            if (State != SessionState.Playing)
            {
                return;
            }

            CurrentRam = Mathf.Max(0f, CurrentRam - ramDamage);
            cameraRig?.Punch();

            if (CurrentRam <= 0f)
            {
                EndRun();
            }
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
        }

        public void CollectDataShard(float value)
        {
            if (State != SessionState.Playing)
            {
                return;
            }

            CollectedDataShards++;
            AddScore(value);
        }

        public void StartGame()
        {
            State = SessionState.Playing;
            ResetRunRuntime();
            player?.SetControlMode(true);
            spawner?.ResetTrack();
            player?.ResetRunner();
        }

        public void EnterMainMenu()
        {
            State = SessionState.MainMenu;
            LastRunCoinsReward = 0;
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
            LastRunCoinsReward = CalculateCoinsReward();
            saveData.coins += LastRunCoinsReward;
            saveData.bestScore = Mathf.Max(saveData.bestScore, Score);
            saveData.bestDistance = Mathf.Max(saveData.bestDistance, CurrentDistance);
            saveData.totalDistance += CurrentDistance;
            SaveProgress();
        }

        private void SaveProgress()
        {
            GlitchRacerSaveSystem.Save(saveData);
        }

        private void TriggerChapterRush()
        {
            chapterRushTimer = chapterRushDuration;
            nextChapterDistance += chapterDistanceInterval;
            cameraRig?.Punch();
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
    }

    public static class GlitchRacerSaveSystem
    {
        private const string SaveKey = "glitch_racer_save_v1";

        public static GlitchRacerSaveData Load()
        {
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
        }
    }
}
