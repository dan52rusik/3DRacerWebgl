using UnityEngine;
using UnityEngine.InputSystem;

namespace GlitchRacer
{
    public class RunnerPlayer : MonoBehaviour
    {
        [SerializeField] private float laneOffset = 4f;
        [SerializeField] private float laneSwitchSpeed = 10f;
        [SerializeField] private float tiltAmount = 15f;

        private GlitchRacerGame game;
        private int currentLane = 1;
        private float currentVelocity;
        private bool manualControl;
        private float autoLaneTimer;

        public void Configure(GlitchRacerGame gameManager)
        {
            game = gameManager;
        }

        public void SetControlMode(bool isManual)
        {
            manualControl = isManual;
        }

        public void ResetRunner()
        {
            currentLane = 1;
            currentVelocity = 0f;
            autoLaneTimer = 0.6f;
            transform.position = new Vector3(0f, transform.position.y, 0f);
            transform.rotation = Quaternion.identity;
        }

        private void Update()
        {
            if (game == null || game.IsGameOver)
            {
                return;
            }

            if (manualControl && game.IsInputEnabled)
            {
                int input = ReadLaneInput();
                if (game.ControlsInverted)
                {
                    input *= -1;
                }

                if (input != 0)
                {
                    currentLane = Mathf.Clamp(currentLane + input, 0, 2);
                }
            }
            else
            {
                UpdateAutoPilot();
            }

            float targetX = (currentLane - 1) * laneOffset;
            float nextX = Mathf.SmoothDamp(transform.position.x, targetX, ref currentVelocity, 1f / laneSwitchSpeed);
            transform.position = new Vector3(nextX, transform.position.y, 0f);

            float tilt = Mathf.Clamp((targetX - transform.position.x) * tiltAmount, -tiltAmount, tiltAmount);
            transform.rotation = Quaternion.Euler(0f, 0f, tilt);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (game == null)
            {
                return;
            }

            TrackEntity entity = other.GetComponent<TrackEntity>();
            if (entity != null)
            {
                entity.Consume(game);
            }
        }

        private void UpdateAutoPilot()
        {
            autoLaneTimer -= Time.deltaTime;
            if (autoLaneTimer > 0f)
            {
                return;
            }

            autoLaneTimer = Random.Range(0.85f, 1.8f);
            currentLane = Random.Range(0, 3);
        }

        private static int ReadLaneInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)
                {
                    return -1;
                }

                if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)
                {
                    return 1;
                }
            }

            Pointer pointer = Pointer.current;
            if (pointer != null && pointer.press.wasPressedThisFrame)
            {
                return pointer.position.ReadValue().x < Screen.width * 0.5f ? -1 : 1;
            }

            return 0;
        }
    }

    public class VirusCarEffects : MonoBehaviour
    {
        private GlitchRacerGame game;
        private ParticleSystem leftSparks;
        private ParticleSystem rightSparks;
        private TrailRenderer leftTrail;
        private TrailRenderer rightTrail;
        private Material fxMaterial;
        private Vector3 lastPosition;
        private float burstCooldown;

        public void Configure(GlitchRacerGame gameManager)
        {
            game = gameManager;
        }

        private void Awake()
        {
            fxMaterial = new Material(Shader.Find("Sprites/Default"));

            leftSparks = CreateSparkEmitter("LeftSparks", new Vector3(-0.58f, -0.42f, -1.08f), new Color(0.08f, 0.95f, 1f));
            rightSparks = CreateSparkEmitter("RightSparks", new Vector3(0.58f, -0.42f, -1.08f), new Color(1f, 0.28f, 0.72f));

            leftTrail = CreateTrail("LeftTrail", new Vector3(-0.74f, -0.12f, -1.24f), new Color(0.08f, 0.95f, 1f));
            rightTrail = CreateTrail("RightTrail", new Vector3(0.74f, -0.12f, -1.24f), new Color(1f, 0.28f, 0.72f));

            lastPosition = transform.position;
        }

        private void Update()
        {
            if (game == null)
            {
                return;
            }

            float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
            Vector3 velocity = (transform.position - lastPosition) / deltaTime;
            lastPosition = transform.position;

            float speedFactor = Mathf.InverseLerp(10f, 34f, game.CurrentSpeed);
            float lateralFactor = Mathf.Clamp01(Mathf.Abs(velocity.x) / 8f);
            float glitchFactor = game.ControlsInverted ? 1f : 0f;
            float chaos = Mathf.Clamp01(speedFactor + (lateralFactor * 0.7f) + (glitchFactor * 0.65f));

            bool active = game.State == GlitchRacerGame.SessionState.Playing || game.IsMenuVisible;
            UpdateSparkEmitter(leftSparks, active, chaos, lateralFactor, glitchFactor);
            UpdateSparkEmitter(rightSparks, active, chaos, lateralFactor, glitchFactor);
            UpdateTrail(leftTrail, active, chaos);
            UpdateTrail(rightTrail, active, chaos);

            burstCooldown -= Time.deltaTime;
            if (active && burstCooldown <= 0f && (lateralFactor > 0.45f || glitchFactor > 0.5f))
            {
                int burstCount = glitchFactor > 0.5f ? 18 : 10;
                leftSparks.Emit(burstCount);
                rightSparks.Emit(burstCount);
                burstCooldown = glitchFactor > 0.5f ? 0.08f : 0.16f;
            }
        }

        private void UpdateSparkEmitter(ParticleSystem system, bool active, float chaos, float lateralFactor, float glitchFactor)
        {
            var emission = system.emission;
            emission.enabled = active;
            emission.rateOverTime = active ? Mathf.Lerp(6f, 42f, chaos) : 0f;

            var main = system.main;
            main.startLifetime = Mathf.Lerp(0.14f, 0.28f, chaos);
            main.startSpeed = Mathf.Lerp(1.2f, 4.6f, chaos);
            main.startSize = Mathf.Lerp(0.05f, 0.11f, chaos);

            var shape = system.shape;
            shape.rotation = new Vector3(0f, 0f, Mathf.Lerp(12f, 36f, lateralFactor + glitchFactor * 0.3f));
        }

        private void UpdateTrail(TrailRenderer trail, bool active, float chaos)
        {
            trail.emitting = active;
            trail.time = Mathf.Lerp(0.08f, 0.24f, chaos);
            trail.startWidth = Mathf.Lerp(0.08f, 0.18f, chaos);
            trail.endWidth = 0.01f;
        }

        private ParticleSystem CreateSparkEmitter(string effectName, Vector3 localPosition, Color color)
        {
            GameObject effectObject = new(effectName);
            effectObject.transform.SetParent(transform, false);
            effectObject.transform.localPosition = localPosition;
            effectObject.transform.localRotation = Quaternion.Euler(20f, 180f, 0f);

            ParticleSystem system = effectObject.AddComponent<ParticleSystem>();
            var main = system.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 0.16f;
            main.startSpeed = 1.6f;
            main.startSize = 0.06f;
            main.startColor = color;
            main.maxParticles = 120;

            var emission = system.emission;
            emission.enabled = true;
            emission.rateOverTime = 10f;

            var shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 18f;
            shape.radius = 0.02f;

            var velocityOverLifetime = system.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
            velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0.15f, 0.65f);
            velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(-2.6f, -1.1f);

            var colorOverLifetime = system.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(color, 0f),
                    new GradientColorKey(Color.white, 0.45f),
                    new GradientColorKey(color, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.7f, 0.55f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = gradient;

            ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.material = fxMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortMode = ParticleSystemSortMode.Distance;

            return system;
        }

        private TrailRenderer CreateTrail(string trailName, Vector3 localPosition, Color color)
        {
            GameObject trailObject = new(trailName);
            trailObject.transform.SetParent(transform, false);
            trailObject.transform.localPosition = localPosition;

            TrailRenderer trail = trailObject.AddComponent<TrailRenderer>();
            trail.material = fxMaterial;
            trail.time = 0.12f;
            trail.startWidth = 0.1f;
            trail.endWidth = 0.01f;
            trail.alignment = LineAlignment.View;
            trail.minVertexDistance = 0.02f;
            trail.numCornerVertices = 2;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;

            Gradient gradient = new();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(color, 0f),
                    new GradientColorKey(Color.white, 0.3f),
                    new GradientColorKey(color, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.85f, 0f),
                    new GradientAlphaKey(0.2f, 0.65f),
                    new GradientAlphaKey(0f, 1f)
                });
            trail.colorGradient = gradient;

            return trail;
        }
    }
}
