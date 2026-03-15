using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GlitchRacer
{
    public class GlitchSynthEngine : MonoBehaviour
    {
        private GlitchRacerGame game;
        private int sampleRate = 44100;
        
        private AudioSource musicSource;
        private AudioSource menuSource;
        private AudioSource engineSource;
        private AudioSource sfxSource;

        private AudioClip runMusicClip;
        private AudioClip menuMusicClip;
        private AudioClip engineClip;
        private AudioClip pickupClip;
        private AudioClip damageClip;
        private AudioClip crashClip;

        private float targetEnginePitch = 1f;
        private float currentEnginePitch = 1f;
        private volatile bool sfxEnabled;
        private volatile bool musicEnabled;
        
        private readonly System.Random rng = new System.Random();

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
        private readonly int[] mainMenuPadline = { 60, 64, 67, 72 };
        private readonly int[] mainMenuBellLine = { 84, 88, 91, 88, 86, 89, 93, 89 };

        public void Configure(GlitchRacerGame gameManager) => game = gameManager;

        public void PlayPickupSound()
        {
            if (sfxEnabled && sfxSource != null && pickupClip != null)
                sfxSource.PlayOneShot(pickupClip, 0.45f);
        }

        public void PlayDamageSound()
        {
            if (sfxEnabled && sfxSource != null && damageClip != null)
                sfxSource.PlayOneShot(damageClip, 0.65f);
        }

        public void PlayGameOverSound()
        {
            if (sfxEnabled && sfxSource != null && crashClip != null)
                sfxSource.PlayOneShot(crashClip, 0.9f);
        }

        private void Awake()
        {
            sampleRate = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 44100;
            
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;

            menuSource = gameObject.AddComponent<AudioSource>();
            menuSource.loop = true;
            menuSource.spatialBlend = 0f;

            engineSource = gameObject.AddComponent<AudioSource>();
            engineSource.loop = true;
            engineSource.spatialBlend = 0f;
            
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.spatialBlend = 0f;

            // Generate Audio Clips synchronously on boot (WebGL safe!)
            runMusicClip = GenerateRunMusic(16f); // 16 second loop
            menuMusicClip = GenerateMenuMusic(16f);
            engineClip = GenerateEngineTone(1f);
            pickupClip = GenerateSfxPickup(0.18f);
            damageClip = GenerateSfxDamage(0.35f);
            crashClip = GenerateSfxCrash(1.8f);

            musicSource.clip = runMusicClip;
            menuSource.clip = menuMusicClip;
            engineSource.clip = engineClip;

            engineSource.Play();
            musicSource.Play();
            menuSource.Play();
        }

        private void Update()
        {
            if (game == null) return;
            
            sfxEnabled = game.SfxEnabled;
            musicEnabled = game.MusicEnabled;
            
            bool isPlaying = game.State == GlitchRacerGame.SessionState.Playing;
            bool isMenuOrOver = game.State == GlitchRacerGame.SessionState.MainMenu || 
                                game.State == GlitchRacerGame.SessionState.Settings || 
                                game.State == GlitchRacerGame.SessionState.Shop ||
                                game.State == GlitchRacerGame.SessionState.GameOver;

            musicSource.volume = Mathf.Lerp(musicSource.volume, (musicEnabled && isPlaying) ? 0.35f : 0f, Time.deltaTime * 6f);
            menuSource.volume = Mathf.Lerp(menuSource.volume, (musicEnabled && isMenuOrOver) ? 0.45f : 0f, Time.deltaTime * 3f);
            
            targetEnginePitch = isPlaying ? 1f + (game.VisualScrollSpeed * 0.02f) : 0.6f;
            currentEnginePitch = Mathf.Lerp(currentEnginePitch, targetEnginePitch, Time.deltaTime * 3f);
            
            engineSource.pitch = currentEnginePitch;
            
            float targetEngineVol = (sfxEnabled && isPlaying) ? 0.25f : (sfxEnabled && isMenuOrOver ? 0.08f : 0f);
            engineSource.volume = Mathf.Lerp(engineSource.volume, targetEngineVol, Time.deltaTime * 4f);
        }

        private static float GetNoteFreq(int midi) => 440f * Mathf.Pow(2f, (midi - 69f) / 12f);
        private static float PulseWave(double phase, float width) => phase % 1.0 < width ? 1f : -1f;
        private static float TriangleWave(double phase) { float x = (float)(phase % 1.0) * 4f; return x < 2f ? x - 1f : 3f - x; }
        private static float SoftClip(float value) => value / (1f + Mathf.Abs(value));
        private static float OnePoleLowPass(ref float state, float input, float cutoffHz, double dt)
        {
            float alpha = 1f - Mathf.Exp(-(float)(6.283185307179586 * cutoffHz * dt));
            state += (input - state) * alpha;
            return state;
        }

        private AudioClip GenerateRunMusic(float duration)
        {
            int samples = Mathf.CeilToInt(duration * sampleRate);
            float[] data = new float[samples * 2];
            double dt = 1.0 / sampleRate;

            double musicNotesTime = 0;
            double bassOscPhase = 0;
            double leadNotesTime = 0;
            double leadOscPhase = 0;
            double arpOscPhase = 0;
            double kickPhase = 0;
            float snareLowpass = 0;
            float bassLowpass = 0, leadLowpass = 0, arpLowpass = 0;

            for (int i = 0; i < samples; i++)
            {
                double time = i * dt;
                float left = 0, right = 0;
                float kickMix = 0, snareMix = 0;

                double bassRate = 8.0;
                musicNotesTime += bassRate * dt;
                int step16 = Mathf.FloorToInt((float)musicNotesTime * 2f) % 16;
                int step32 = Mathf.FloorToInt((float)musicNotesTime) % 32;
                float stepTime = (float)(musicNotesTime % 1.0);
                float step16Time = (float)((musicNotesTime * 2.0) % 1.0);

                if (kickPattern[step16] > 0)
                {
                    float kEnv = (1f - step16Time) * (1f - step16Time);
                    float kPitchEnv = Mathf.Pow(Mathf.Clamp01(1f - step16Time / 0.12f), 5f);
                    float kFreq = Mathf.Lerp(45f, 380f, kPitchEnv);
                    kickPhase = (kickPhase + kFreq * dt) % 1.0;
                    kickMix = SoftClip(Mathf.Sin((float)kickPhase * Mathf.PI * 2f)) * kEnv * 0.9f;
                }

                if (snarePattern[step16] > 0)
                {
                    float sEnv = (1f - step16Time) * (1f - step16Time);
                    float noise = (float)rng.NextDouble() * 2f - 1f;
                    float fNoise = OnePoleLowPass(ref snareLowpass, noise, 4500f, dt);
                    snareMix = (noise - fNoise) * 0.5f * sEnv * 0.35f;
                }

                float bassEnv = Mathf.Clamp01(stepTime / 0.05f) * Mathf.Clamp01(1f - stepTime);
                float bassFreq = GetNoteFreq(bassline[step32]);
                bassOscPhase = (bassOscPhase + bassFreq * dt) % 1.0;
                float fm = Mathf.Sin((float)bassOscPhase * Mathf.PI * 2f) * 1.5f;
                float bassRaw = Mathf.Sin((float)(bassOscPhase + fm / 6.28) * Mathf.PI * 2f) * bassEnv;
                float bassFilt = OnePoleLowPass(ref bassLowpass, bassRaw, 600f, dt) * 0.18f;

                double leadRate = 4.0;
                leadNotesTime += leadRate * dt;
                float leadEnv = Mathf.Clamp01((float)(leadNotesTime % 1.0) / 0.08f) * Mathf.Clamp01(1f - (float)(leadNotesTime % 1.0));
                int leadStep = Mathf.FloorToInt((float)leadNotesTime) % leadline.Length;
                float leadFilt = 0;
                
                if (leadline[leadStep] > 0)
                {
                    leadOscPhase = (leadOscPhase + GetNoteFreq(leadline[leadStep]) * dt) % 1.0;
                    float leadRaw = TriangleWave(leadOscPhase) * leadEnv;
                    leadFilt = OnePoleLowPass(ref leadLowpass, leadRaw, 2000f, dt) * 0.08f;
                }

                double arpRate = 12.0;
                double arpTime = time * arpRate;
                float arpEnv = Mathf.Clamp01(1f - (float)(arpTime % 1.0));
                arpOscPhase = (arpOscPhase + GetNoteFreq(arpline[Mathf.FloorToInt((float)arpTime) % arpline.Length]) * dt) % 1.0;
                float arpFilt = OnePoleLowPass(ref arpLowpass, PulseWave(arpOscPhase, 0.4f) * arpEnv, 2400f, dt) * 0.06f;

                left = kickMix + snareMix + bassFilt + leadFilt + arpFilt;
                right = kickMix + snareMix + bassFilt + leadFilt + arpFilt;

                data[i * 2] = SoftClip(left);
                data[i * 2 + 1] = SoftClip(right);
            }

            AudioClip clip = AudioClip.Create("RunMusic", samples, 2, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateMenuMusic(float duration)
        {
            int samples = Mathf.CeilToInt(duration * sampleRate);
            float[] data = new float[samples * 2];
            double dt = 1.0 / sampleRate;

            double timePad = 0, timeBell = 0;
            double padPhase1 = 0, padPhase2 = 0, bellPhase = 0;
            float padLP = 0, bellLP = 0;

            for (int i = 0; i < samples; i++)
            {
                float left = 0, right = 0;
                timePad += 0.34 * dt;
                timeBell += 0.72 * dt;

                float padEnv = Mathf.SmoothStep(1f, 0.6f, (float)(timePad % 1.0)) * 0.08f;
                float padFreq = GetNoteFreq(mainMenuPadline[Mathf.FloorToInt((float)timePad) % mainMenuPadline.Length]);
                padPhase1 = (padPhase1 + padFreq * dt) % 1.0;
                padPhase2 = (padPhase2 + (padFreq * 0.5f) * dt) % 1.0;
                
                float padRaw = (Mathf.Sin((float)padPhase1 * Mathf.PI * 2f) * 0.65f + 
                                Mathf.Sin((float)padPhase2 * Mathf.PI * 2f) * 0.35f) * padEnv;
                float padF = OnePoleLowPass(ref padLP, padRaw, 1200f, dt);

                float bellEnv = Mathf.Pow(1f - (float)(timeBell % 1.0), 2f) * 0.04f;
                bellPhase = (bellPhase + GetNoteFreq(mainMenuBellLine[Mathf.FloorToInt((float)timeBell) % mainMenuBellLine.Length]) * dt) % 1.0;
                float bellRaw = Mathf.Sin((float)bellPhase * Mathf.PI * 2f) * bellEnv;
                float bellF = OnePoleLowPass(ref bellLP, bellRaw, 2400f, dt);

                left = padF + bellF;
                right = padF + bellF;
                data[i * 2] = left;
                data[i * 2 + 1] = right;
            }

            AudioClip clip = AudioClip.Create("MenuMusic", samples, 2, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateEngineTone(float duration)
        {
            int samples = Mathf.CeilToInt(duration * sampleRate);
            float[] data = new float[samples * 2];
            double dt = 1.0 / sampleRate;
            double phase1 = 0, phase2 = 0;
            float lp = 0;
            float baseHz = 40f;

            for (int i = 0; i < samples; i++)
            {
                phase1 = (phase1 + baseHz * dt) % 1.0;
                phase2 = (phase2 + baseHz * 0.5f * dt) % 1.0;
                float raw = Mathf.Sin((float)phase1 * Mathf.PI * 2f) * 0.6f + 
                            PulseWave(phase2, 0.4f) * 0.2f;
                
                float val = OnePoleLowPass(ref lp, raw, 220f, dt) * 0.15f;
                data[i * 2] = val;
                data[i * 2 + 1] = val;
            }

            AudioClip clip = AudioClip.Create("EngineLoop", samples, 2, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateSfxPickup(float duration)
        {
            int samples = Mathf.CeilToInt(duration * sampleRate);
            float[] data = new float[samples];
            double dt = 1.0 / sampleRate;
            double phase = 0;

            for (int i = 0; i < samples; i++)
            {
                float p = (float)i / samples;
                phase = (phase + Mathf.Lerp(430f, 620f, p) * dt) % 1.0;
                float env = (1f - p) * (1f - p);
                data[i] = Mathf.Sin((float)phase * Mathf.PI * 2f) * env * 0.25f;
            }

            AudioClip clip = AudioClip.Create("SfxPickup", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateSfxDamage(float duration)
        {
            int samples = Mathf.CeilToInt(duration * sampleRate);
            float[] data = new float[samples];
            double dt = 1.0 / sampleRate;
            float lp = 0;
            double tone = 0;

            for (int i = 0; i < samples; i++)
            {
                float p = (float)i / samples;
                float env = (1f - p) * (1f - p);
                tone = (tone + Mathf.Lerp(180f, 70f, p) * dt) % 1.0;
                
                float noise = (float)rng.NextDouble() * 2f - 1f;
                float filtered = OnePoleLowPass(ref lp, noise, 900f, dt);
                
                data[i] = (Mathf.Sin((float)tone * Mathf.PI * 2f) * 0.15f + filtered * 0.35f) * env;
            }

            AudioClip clip = AudioClip.Create("SfxDamage", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateSfxCrash(float duration)
        {
            int samples = Mathf.CeilToInt(duration * sampleRate);
            float[] data = new float[samples];
            double dt = 1.0 / sampleRate;
            double phase1 = 0, phase2 = 0;
            float lp = 0;

            for (int i = 0; i < samples; i++)
            {
                float p = (float)i / samples;
                float env = (1f - p) * (1f - p);
                float leadFreq = Mathf.Lerp(240f, 54f, p * p);
                float subFreq = Mathf.Lerp(120f, 34f, p * p * p);
                
                phase1 = (phase1 + leadFreq * dt) % 1.0;
                phase2 = (phase2 + subFreq * dt) % 1.0;

                float noise = (float)rng.NextDouble() * 2f - 1f;
                float raw = Mathf.Sin((float)phase1 * Mathf.PI * 2f) * 0.5f + 
                            Mathf.Sin((float)phase2 * Mathf.PI * 2f) * 0.35f +
                            noise * 0.15f;
                            
                data[i] = OnePoleLowPass(ref lp, raw, Mathf.Lerp(1800f, 140f, p * p), dt) * env * 0.7f;
            }

            AudioClip clip = AudioClip.Create("SfxCrash", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
