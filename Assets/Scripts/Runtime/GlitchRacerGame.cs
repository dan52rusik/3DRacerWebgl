using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace GlitchRacer
{
    public class GlitchRacerGame : MonoBehaviour
    {
        [Header("Run Economy")]
        [SerializeField] private float maxRam = 100f;
        [SerializeField] private float ramDrainPerSecond = 5.5f;
        [SerializeField] private float startSpeed = 16f;
        [SerializeField] private float maxSpeed = 34f;
        [SerializeField] private float speedRampPerSecond = 0.65f;
        [SerializeField] private float distanceScoreFactor = 8f;

        private RunnerPlayer player;
        private TrackSegmentSpawner spawner;
        private GlitchCameraRig cameraRig;
        private GlitchRacerHud hud;

        public float CurrentRam { get; private set; }
        public float CurrentSpeed { get; private set; }
        public float Score { get; private set; }
        public float BestScore { get; private set; }
        public bool IsGameOver { get; private set; }
        public bool ControlsInverted => glitchTimer > 0f;
        public float GlitchTimeRemaining => glitchTimer;

        private float glitchTimer;

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
            ResetRun();
        }

        private void Update()
        {
            if (IsGameOver)
            {
                Keyboard keyboard = Keyboard.current;
                if (keyboard != null && (keyboard.rKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
                {
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                }

                return;
            }

            CurrentSpeed = Mathf.MoveTowards(CurrentSpeed, maxSpeed, speedRampPerSecond * Time.deltaTime);
            CurrentRam = Mathf.Max(0f, CurrentRam - ramDrainPerSecond * Time.deltaTime);
            Score += CurrentSpeed * distanceScoreFactor * Time.deltaTime;

            if (glitchTimer > 0f)
            {
                glitchTimer = Mathf.Max(0f, glitchTimer - Time.deltaTime);
            }

            if (CurrentRam <= 0f)
            {
                EndRun();
            }
        }

        public void AddScore(float amount)
        {
            if (IsGameOver)
            {
                return;
            }

            Score += amount;
        }

        public void AddRam(float amount)
        {
            if (IsGameOver)
            {
                return;
            }

            CurrentRam = Mathf.Clamp(CurrentRam + amount, 0f, maxRam);
        }

        public void HitObstacle(float ramDamage)
        {
            if (IsGameOver)
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

        public void TriggerGlitch(float duration, float bonusScore)
        {
            if (IsGameOver)
            {
                return;
            }

            glitchTimer = Mathf.Max(glitchTimer, duration);
            AddScore(bonusScore);
            AddRam(8f);
        }

        private void ResetRun()
        {
            CurrentRam = maxRam;
            CurrentSpeed = startSpeed;
            Score = 0f;
            glitchTimer = 0f;
            IsGameOver = false;
        }

        private void EndRun()
        {
            IsGameOver = true;
            BestScore = Mathf.Max(BestScore, Score);
        }
    }
}
