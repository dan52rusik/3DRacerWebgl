using System.Collections.Generic;
using UnityEngine;

namespace GlitchRacer
{
    [RequireComponent(typeof(AudioSource))]
    public class GlitchSynthEngine : MonoBehaviour
    {
        private GlitchRacerGame game;
        private float sampleRate;
        private double audioTime;

        private double enginePhase;
        private double musicNotesTime;
        private double leadNotesTime;
        private double bassOscPhase;
        private double leadOscPhase;
        private double pickupOscPhase;
        private double engineSubPhase;
        private double enginePulsePhase;
        private double damageTonePhase;
        private double arpOscPhase;
        private double gameOverPhase;
        private double gameOverSubPhase;
        private double mainMenuPadPhase;
        private double mainMenuPadSubPhase;
        private double mainMenuBellPhase;
        private double gameOverPadPhase;
        private double gameOverPadSubPhase;
        private double gameOverBellPhase;
        private double mainMenuTime;
        private double mainMenuBellTime;
        private double gameOverTime;
        private double gameOverBellTime;
        private double kickPhase;

        private float snareLowpass;
        private float targetEngineHz;
        private volatile float currentEngineHz;
        private volatile bool sfxEnabled;
        private volatile bool musicEnabled;
        private volatile bool isPlaying;
        private volatile bool isMenu;
        private volatile bool isGameOver;
        private volatile int activeGlitchLevel;
        private volatile float musicTension;
        private volatile float mainMenuMood;
        private volatile float gameOverMood;
        private volatile float currentRamNormalized;

        private readonly System.Random rng = new System.Random();
        private readonly Queue<int> sfxQueue = new Queue<int>();
        private readonly object sfxLock = new object();
        private readonly List<SfxState> activeSfx = new List<SfxState>();

        private float engineLowpass;
        private float engineRightLowpass;
        private float bassLowpass;
        private float bassRightLowpass;
        private float leadLowpass;
        private float leadRightLowpass;
        private float arpLowpass;
        private float arpRightLowpass;
        private float pickupLowpass;
        private float pickupRightLowpass;
        private float damageNoiseLowpass;
        private float gameOverLowpass;
        private float gameOverRightLowpass;
        private float menuPadLowpass;
        private float menuPadRightLowpass;
        private float menuBellLowpass;
        private float menuBellRightLowpass;
        private float lastMusicMix;
        private bool hasStateSnapshot;
        private GlitchRacerGame.SessionState lastSessionState;

        private class SfxState
        {
            public int type;
            public double duration;
            public double timeRemaining;
        }

        private readonly int[] kickPattern = { 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0 };
        private readonly int[] snarePattern = { 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0 };
        private readonly int[] bassline =
        {
            45, 45, 57, 45, 45, 45, 57, 45,
            41, 41, 53, 41, 41, 41, 53, 41,
            48, 48, 60, 48, 48, 48, 60, 48,
            40, 40, 52, 40, 40, 40, 52, 40
        };
        private readonly int[] leadline =
        {
            69, 0, 72, 0, 64, 0, 69, 0,
            65, 0, 69, 0, 60, 0, 65, 0,
            72, 0, 76, 0, 67, 0, 72, 0,
            64, 0, 67, 0, 59, 0, 64, 0
        };
        private readonly int[] arpline =
        {
            81, 84, 88, 84, 79, 84, 88, 84,
            79, 83, 86, 83, 78, 83, 86, 83
        };
        private readonly int[] menuBellLine = { 72, 76, 79, 76, 71, 74, 79, 74 };
        private readonly int[] gameOverPadline = { 57, 53, 48, 45 };
        private readonly int[] mainMenuPadline = { 60, 64, 67, 72 };
        private readonly int[] mainMenuBellLine = { 84, 88, 91, 88, 86, 89, 93, 89 };

        public void Configure(GlitchRacerGame gameManager) => game = gameManager;

        public void PlayPickupSound()
        {
            lock (sfxLock)
            {
                sfxQueue.Enqueue(1);
            }
        }

        public void PlayDamageSound()
        {
            lock (sfxLock)
            {
                sfxQueue.Enqueue(2);
            }
        }

        public void PlayGameOverSound()
        {
            lock (sfxLock)
            {
                sfxQueue.Enqueue(3);
            }
        }

        private void Start()
        {
            sampleRate = AudioSettings.outputSampleRate;
            AudioSource src = GetComponent<AudioSource>();
            src.playOnAwake = true;
            src.loop = true;
            src.spatialBlend = 0f;
            src.volume = 1f;
            src.clip = AudioClip.Create("SynthDriver", 1, 1, (int)sampleRate, false);
            src.Play();
        }

        private void Update()
        {
            if (game == null)
            {
                return;
            }

            if (!hasStateSnapshot || lastSessionState != game.State)
            {
                HandleStateChanged(game.State);
                lastSessionState = game.State;
                hasStateSnapshot = true;
            }

            sfxEnabled = game.SfxEnabled;
            musicEnabled = game.MusicEnabled;
            isPlaying = game.State == GlitchRacerGame.SessionState.Playing;
            isMenu = game.IsMenuVisible;
            isGameOver = game.State == GlitchRacerGame.SessionState.GameOver;
            activeGlitchLevel = game.ActiveGlitch != GlitchRacerGame.GlitchType.None ? 1 : 0;
            currentRamNormalized = Mathf.Clamp01(game.CurrentRam / Mathf.Max(1f, game.MaxRam));

            float speedTension = Mathf.InverseLerp(16f, 34f, game.CurrentSpeed);
            float ramTension = 1f - currentRamNormalized;
            float glitchTension = activeGlitchLevel > 0 ? 0.18f : 0f;
            float rushTension = game.IsChapterRush ? 0.24f : 0f;
            float targetTension = Mathf.Clamp01(speedTension * 0.45f + ramTension * 0.37f + glitchTension + rushTension);

            musicTension = Mathf.Lerp(musicTension, targetTension, Time.deltaTime * 2.2f);
            mainMenuMood = Mathf.Lerp(mainMenuMood, isMenu ? 1f : 0f, Time.deltaTime * 1.8f);
            gameOverMood = Mathf.Lerp(gameOverMood, isGameOver ? 1f : 0f, Time.deltaTime * 1.8f);

            targetEngineHz = isPlaying ? 35f + (game.VisualScrollSpeed * 1.5f) : 24f;
            currentEngineHz = Mathf.Lerp(currentEngineHz, targetEngineHz, Time.deltaTime * 3f);
        }

        private void HandleStateChanged(GlitchRacerGame.SessionState newState)
        {
            if (newState == GlitchRacerGame.SessionState.MainMenu ||
                newState == GlitchRacerGame.SessionState.Shop ||
                newState == GlitchRacerGame.SessionState.Settings)
            {
                mainMenuTime = 0.0;
                mainMenuBellTime = 0.0;
                mainMenuPadPhase = 0.0;
                mainMenuPadSubPhase = 0.0;
                mainMenuBellPhase = 0.0;
            }

            if (newState == GlitchRacerGame.SessionState.GameOver)
            {
                gameOverTime = 0.0;
                gameOverBellTime = 0.0;
                gameOverPadPhase = 0.0;
                gameOverPadSubPhase = 0.0;
                gameOverBellPhase = 0.0;
            }
        }

        private float GetNoteFreq(int midi) => 440f * Mathf.Pow(2f, (midi - 69f) / 12f);

        private static float PulseWave(double phase, float width) => phase % 1.0 < width ? 1f : -1f;

        private static float TriangleWave(double phase)
        {
            float x = (float)phase * 4f;
            return x < 2f ? x - 1f : 3f - x;
        }

        private static float SoftClip(float value) => value / (1f + Mathf.Abs(value));

        private static float OnePoleLowPass(ref float state, float input, float cutoffHz, double dt)
        {
            float alpha = 1f - Mathf.Exp(-(float)(6.283185307179586 * cutoffHz * dt));
            state += (input - state) * alpha;
            return state;
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            double dt = 1.0 / sampleRate;

            lock (sfxLock)
            {
                while (sfxQueue.Count > 0)
                {
                    int type = sfxQueue.Dequeue();
                    double duration = type == 1 ? 0.14 : (type == 3 ? 1.8 : 0.5);
                    activeSfx.Add(new SfxState { type = type, duration = duration, timeRemaining = duration });
                }
            }

            for (int i = 0; i < data.Length; i += channels)
            {
                audioTime += dt;
                float left = 0f;
                float right = 0f;
                float kickDucking = 0f;

                if (isPlaying)
                {
                    float glitchAmount = activeGlitchLevel > 0 ? 1f : 0f;
                    float tension = musicTension;

                    float pitchWobble = 1f;
                    if (currentRamNormalized < 0.3f)
                    {
                        float severity = (0.3f - currentRamNormalized) * 3.33f;
                        pitchWobble = 1f - (severity * 0.3f) + Mathf.Sin((float)audioTime * 6f) * (0.06f * severity);
                    }

                    float kickMix = 0f;
                    float snareMix = 0f;

                    if (musicEnabled)
                    {
                        double bassRate = Mathf.Lerp(6.8f, 8.6f, tension) * pitchWobble;
                        musicNotesTime += bassRate * dt;
                        int step16 = Mathf.FloorToInt((float)musicNotesTime) % 16;
                        int step32 = Mathf.FloorToInt((float)musicNotesTime) % 32;
                        float stepTime = (float)(musicNotesTime % 1.0);

                        if (kickPattern[step16] > 0)
                        {
                            float kDecay = 1f - Mathf.Clamp01(stepTime / 0.5f);
                            float kEnv = kDecay * kDecay;
                            float kPitchEnv = Mathf.Pow(1f - Mathf.Clamp01(stepTime / 0.12f), 5f);
                            float kFreq = Mathf.Lerp(42f, 380f, kPitchEnv);
                            kickPhase = (kickPhase + kFreq * dt * pitchWobble) % 1.0;
                            float kickCore = SoftClip(Mathf.Sin((float)kickPhase * Mathf.PI * 2f) * 2.5f);
                            float click = stepTime < 0.015f ? ((float)rng.NextDouble() * 2f - 1f) * 0.2f : 0f;
                            kickMix = (kickCore * kEnv + click) * 0.9f;
                            kickDucking = kEnv * 0.85f;
                        }

                        if (snarePattern[step16] > 0)
                        {
                            float sDecay = 1f - Mathf.Clamp01(stepTime / 0.35f);
                            float sEnv = sDecay * sDecay;
                            float noise = (float)rng.NextDouble() * 2f - 1f;
                            float filteredNoise = OnePoleLowPass(ref snareLowpass, noise, 4500f, dt);
                            float hpNoise = noise - filteredNoise;
                            float sTone = Mathf.Sin((float)((audioTime * 180f * pitchWobble) % 1.0) * Mathf.PI * 2f) * 0.2f;
                            snareMix = (hpNoise * 0.5f + sTone) * sEnv * 0.45f;
                        }

                        float bassEnv = Mathf.Clamp01(stepTime / 0.06f) * (1f - Mathf.Clamp01((stepTime - 0.18f) / 0.82f));
                        float bassFreq = GetNoteFreq(bassline[step32]) * pitchWobble;
                        if (activeGlitchLevel > 0 && rng.NextDouble() > 0.992)
                        {
                            bassFreq *= 0.5f;
                        }

                        bassOscPhase = (bassOscPhase + bassFreq * dt) % 1.0;
                        float fmIndex = Mathf.Lerp(0.8f, 3.5f, tension);
                        float modulator = Mathf.Sin((float)bassOscPhase * Mathf.PI * 2f);
                        float bassRaw = Mathf.Sin((float)(bassOscPhase + modulator * (fmIndex / 6.28f)) * Mathf.PI * 2f) * bassEnv;

                        float bassCutoff = Mathf.Lerp(250f, 600f, tension);
                        float bassLeft = OnePoleLowPass(ref bassLowpass, bassRaw, bassCutoff, dt) * 0.15f;
                        float bassRight = OnePoleLowPass(ref bassRightLowpass, bassRaw, bassCutoff * 0.94f, dt) * 0.15f;
                        bassLeft *= 1f - kickDucking;
                        bassRight *= 1f - kickDucking;

                        double leadRate = Mathf.Lerp(3.35f, 5.4f, tension) * pitchWobble;
                        leadNotesTime += leadRate * dt;
                        float leadStepTime = (float)(leadNotesTime % 1.0);
                        float leadEnv = Mathf.Clamp01(leadStepTime / 0.08f) * (1f - Mathf.Clamp01((leadStepTime - 0.22f) / 0.78f));
                        int leadStep = Mathf.FloorToInt((float)leadNotesTime) % leadline.Length;

                        float leadLeft = 0f;
                        float leadRight = 0f;
                        if (leadline[leadStep] > 0)
                        {
                            float leadFreq = GetNoteFreq(leadline[leadStep]) * pitchWobble;
                            leadOscPhase = (leadOscPhase + leadFreq * dt) % 1.0;
                            double detunedPhase = (leadOscPhase + dt * leadFreq * 0.0125f) % 1.0;
                            float leadRaw = (TriangleWave(leadOscPhase) * 0.42f +
                                Mathf.Sin((float)leadOscPhase * Mathf.PI * 2f) * 0.36f +
                                Mathf.Sin((float)detunedPhase * Mathf.PI * 2f) * 0.22f) * leadEnv;
                            float leadCutoff = Mathf.Lerp(1200f, 2200f, tension) * (glitchAmount > 0f ? 0.78f : 1f);
                            leadLeft = OnePoleLowPass(ref leadLowpass, leadRaw, leadCutoff, dt) * 0.045f;
                            leadRight = OnePoleLowPass(ref leadRightLowpass, leadRaw, leadCutoff * 0.92f, dt) * 0.05f;
                        }

                        float arpLeft = 0f;
                        float arpRight = 0f;
                        if (tension > 0.18f)
                        {
                            double arpRate = Mathf.Lerp(0f, 10.5f, Mathf.SmoothStep(0f, 1f, tension));
                            double arpTime = leadNotesTime * (arpRate / System.Math.Max(0.001, leadRate));
                            int arpStep = Mathf.FloorToInt((float)arpTime) % arpline.Length;
                            float arpEnv = (1f - (float)(arpTime % 1.0)) * Mathf.Lerp(0.14f, 0.42f, tension);
                            float arpFreq = GetNoteFreq(arpline[arpStep]) * pitchWobble;
                            arpOscPhase = (arpOscPhase + arpFreq * dt) % 1.0;
                            float arpRaw = (PulseWave(arpOscPhase, 0.3f) * 0.55f +
                                Mathf.Sin((float)arpOscPhase * Mathf.PI * 2f) * 0.45f) * arpEnv;
                            float arpCutoff = Mathf.Lerp(1300f, 2800f, tension);
                            arpLeft = OnePoleLowPass(ref arpLowpass, arpRaw, arpCutoff, dt) * 0.035f;
                            arpRight = OnePoleLowPass(ref arpRightLowpass, arpRaw, arpCutoff * 0.9f, dt) * 0.04f;
                        }

                        lastMusicMix = Mathf.Lerp(lastMusicMix, bassLeft + leadLeft + arpLeft + kickMix, 0.02f);
                        left += bassLeft + leadLeft + arpLeft + kickMix + snareMix;
                        right += bassRight + leadRight + arpRight + kickMix + snareMix;
                    }

                    if (sfxEnabled)
                    {
                        enginePhase = (enginePhase + currentEngineHz * dt) % 1.0;
                        engineSubPhase = (engineSubPhase + (currentEngineHz * 0.5f) * dt) % 1.0;
                        enginePulsePhase = (enginePulsePhase + (currentEngineHz * 1.01f) * dt) % 1.0;

                        float engineRaw =
                            Mathf.Sin((float)enginePhase * Mathf.PI * 2f) * 0.55f +
                            Mathf.Sin((float)engineSubPhase * Mathf.PI * 2f) * 0.35f +
                            PulseWave(enginePulsePhase, 0.38f) * 0.1f +
                            ((float)rng.NextDouble() * 2f - 1f) * (0.015f + glitchAmount * 0.04f);

                        float cutoff = Mathf.Lerp(140f, 260f, Mathf.InverseLerp(24f, 90f, currentEngineHz));
                        left += OnePoleLowPass(ref engineLowpass, engineRaw, cutoff, dt) * 0.11f * (1f - kickDucking);
                        right += OnePoleLowPass(ref engineRightLowpass, engineRaw, cutoff * 0.94f, dt) * 0.11f * (1f - kickDucking);
                    }
                }

                if (musicEnabled && !isPlaying && mainMenuMood > 0.01f)
                {
                    mainMenuTime += 0.34 * dt;
                    int padStep = Mathf.FloorToInt((float)mainMenuTime) % mainMenuPadline.Length;
                    float padEnv = Mathf.SmoothStep(1f, 0.58f, (float)(mainMenuTime % 1.0)) * Mathf.Lerp(0.045f, 0.075f, mainMenuMood);
                    float padFreq = GetNoteFreq(mainMenuPadline[padStep]);
                    mainMenuPadPhase = (mainMenuPadPhase + padFreq * dt) % 1.0;
                    mainMenuPadSubPhase = (mainMenuPadSubPhase + (padFreq * 0.5f) * dt) % 1.0;

                    float padRaw =
                        (Mathf.Sin((float)mainMenuPadPhase * Mathf.PI * 2f) * 0.65f +
                        Mathf.Sin((float)mainMenuPadSubPhase * Mathf.PI * 2f) * 0.35f) * padEnv;

                    float padCutoff = Mathf.Lerp(900f, 1450f, mainMenuMood);
                    left += OnePoleLowPass(ref menuPadLowpass, padRaw, padCutoff, dt) * 0.95f;
                    right += OnePoleLowPass(ref menuPadRightLowpass, padRaw, padCutoff * 0.92f, dt) * 1.05f;

                    mainMenuBellTime += 0.72 * dt;
                    int bellStep = Mathf.FloorToInt((float)mainMenuBellTime) % mainMenuBellLine.Length;
                    float bellEnv = 1f - (float)(mainMenuBellTime % 1.0);
                    bellEnv *= bellEnv;
                    bellEnv *= Mathf.Lerp(0.012f, 0.028f, mainMenuMood);
                    float bellFreq = GetNoteFreq(mainMenuBellLine[bellStep]);
                    mainMenuBellPhase = (mainMenuBellPhase + bellFreq * dt) % 1.0;

                    float bellRaw =
                        (Mathf.Sin((float)mainMenuBellPhase * Mathf.PI * 2f) +
                        Mathf.Sin((float)mainMenuBellPhase * Mathf.PI * 4f) * 0.22f) * bellEnv;

                    left += OnePoleLowPass(ref menuBellLowpass, bellRaw, 2400f, dt) * 0.92f;
                    right += OnePoleLowPass(ref menuBellRightLowpass, bellRaw, 2160f, dt) * 1.08f;
                }

                if (musicEnabled && !isPlaying && gameOverMood > 0.01f)
                {
                    gameOverTime += 0.18 * dt;
                    int padStep = Mathf.FloorToInt((float)gameOverTime) % gameOverPadline.Length;
                    float padEnv = Mathf.SmoothStep(1f, 0.35f, (float)(gameOverTime % 1.0)) * Mathf.Lerp(0.07f, 0.12f, gameOverMood);
                    float padFreq = GetNoteFreq(gameOverPadline[padStep]);
                    gameOverPadPhase = (gameOverPadPhase + padFreq * dt) % 1.0;
                    gameOverPadSubPhase = (gameOverPadSubPhase + (padFreq * 0.5f) * dt) % 1.0;

                    float padRaw =
                        (Mathf.Sin((float)gameOverPadPhase * Mathf.PI * 2f) * 0.65f +
                        Mathf.Sin((float)gameOverPadSubPhase * Mathf.PI * 2f) * 0.35f) * padEnv;

                    float padCutoff = Mathf.Lerp(420f, 300f, gameOverMood);
                    left += OnePoleLowPass(ref menuPadLowpass, padRaw, padCutoff, dt) * 0.95f;
                    right += OnePoleLowPass(ref menuPadRightLowpass, padRaw, padCutoff * 0.92f, dt) * 1.05f;

                    gameOverBellTime += 0.48 * dt;
                    int bellStep = Mathf.FloorToInt((float)gameOverBellTime) % menuBellLine.Length;
                    float bellEnv = 1f - (float)(gameOverBellTime % 1.0);
                    bellEnv *= bellEnv;
                    bellEnv *= Mathf.Lerp(0.018f, 0.04f, gameOverMood);
                    float bellFreq = GetNoteFreq(menuBellLine[bellStep]) * 0.5f;
                    gameOverBellPhase = (gameOverBellPhase + bellFreq * dt) % 1.0;

                    float bellRaw =
                        (Mathf.Sin((float)gameOverBellPhase * Mathf.PI * 2f) +
                        Mathf.Sin((float)gameOverBellPhase * Mathf.PI * 4f) * 0.22f) * bellEnv;

                    left += OnePoleLowPass(ref menuBellLowpass, bellRaw, 900f, dt) * 0.92f;
                    right += OnePoleLowPass(ref menuBellRightLowpass, bellRaw, 810f, dt) * 1.08f;
                }

                if (sfxEnabled)
                {
                    for (int j = activeSfx.Count - 1; j >= 0; j--)
                    {
                        SfxState sfx = activeSfx[j];
                        sfx.timeRemaining -= dt;
                        if (sfx.timeRemaining <= 0)
                        {
                            activeSfx.RemoveAt(j);
                            continue;
                        }

                        float p = 1f - (float)(sfx.timeRemaining / sfx.duration);
                        float sfxWave = 0f;

                        if (sfx.type == 1)
                        {
                            pickupOscPhase = (pickupOscPhase + Mathf.Lerp(620f, 430f, p) * dt) % 1.0;
                            float env = (1f - p) * (1f - p);
                            float rawPickup =
                                (Mathf.Sin((float)pickupOscPhase * Mathf.PI * 2f) * 0.9f +
                                Mathf.Sin((float)pickupOscPhase * Mathf.PI * 4f) * 0.18f) * 0.075f * env;
                            left += OnePoleLowPass(ref pickupLowpass, rawPickup, 700f, dt) * 0.98f;
                            right += OnePoleLowPass(ref pickupRightLowpass, rawPickup, 660f, dt) * 1.02f;
                            continue;
                        }

                        if (sfx.type == 2)
                        {
                            damageTonePhase = (damageTonePhase + Mathf.Lerp(180f, 70f, p) * dt) % 1.0;
                            sfxWave =
                                Mathf.Sin((float)damageTonePhase * Mathf.PI * 2f) * 0.07f * (1f - p) +
                                OnePoleLowPass(ref damageNoiseLowpass, (float)(rng.NextDouble() * 2.0 - 1.0), 900f, dt) * 0.12f * (1f - p) * (1f - p);
                        }
                        else if (sfx.type == 3)
                        {
                            float t = p;
                            float env = 1f - t;
                            env *= env;

                            float leadFreq = Mathf.Lerp(240f, 54f, t * t);
                            float subFreq = Mathf.Lerp(120f, 34f, t * t * t);
                            gameOverPhase = (gameOverPhase + leadFreq * dt) % 1.0;
                            gameOverSubPhase = (gameOverSubPhase + subFreq * dt) % 1.0;

                            float leadTone = Mathf.Sin((float)gameOverPhase * Mathf.PI * 2f);
                            float subTone = Mathf.Sin((float)gameOverSubPhase * Mathf.PI * 2f) * 0.7f;
                            float shimmer = Mathf.Sin((float)gameOverPhase * Mathf.PI * 4f) * 0.18f;
                            float noise = ((float)rng.NextDouble() * 2f - 1f) * 0.05f * (1f - t);

                            float rawGameOver = (leadTone * 0.62f + subTone * 0.28f + shimmer * 0.1f) * 0.22f + noise;
                            float sweepCutoff = Mathf.Lerp(1800f, 140f, t * t);
                            float leftGameOver = OnePoleLowPass(ref gameOverLowpass, rawGameOver, sweepCutoff, dt) * env;
                            float rightGameOver = OnePoleLowPass(ref gameOverRightLowpass, rawGameOver, sweepCutoff * 0.9f, dt) * env;

                            left += leftGameOver * 1.12f;
                            right += rightGameOver * 1.05f;
                            continue;
                        }

                        left += sfxWave;
                        right += sfxWave;
                    }
                }

                float duck = 1f - Mathf.Clamp01(Mathf.Abs(lastMusicMix) * 0.6f);
                if (sfxEnabled && activeSfx.Count > 0)
                {
                    left *= Mathf.Lerp(0.82f, 1f, duck);
                    right *= Mathf.Lerp(0.82f, 1f, duck);
                }

                data[i] = SoftClip(left * 1.1f);
                if (channels > 1)
                {
                    data[i + 1] = SoftClip(right * 1.1f);
                }
            }
        }
    }
}
