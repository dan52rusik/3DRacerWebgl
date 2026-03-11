using System.Collections.Generic;
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

        public int CurrentLane => currentLane;
        public float LaneOffset => laneOffset;

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
        private Transform leftRearAnchor;
        private Transform rightRearAnchor;
        private Material fxMaterial;
        private Vector3 lastPosition;
        private float burstCooldown;
        private LineRenderer leftTrailLine;
        private LineRenderer rightTrailLine;
        private readonly List<Vector3> leftTrailPoints = new();
        private readonly List<Vector3> rightTrailPoints = new();

        private const float TrailSampleDistance = 0.08f;
        private const float MaxTrailLength = 5.5f;

        public void Configure(GlitchRacerGame gameManager)
        {
            game = gameManager;
        }

        private void Awake()
        {
            fxMaterial = new Material(Shader.Find("Sprites/Default"));

            leftSparks = CreateSparkEmitter("LeftSparks", new Vector3(-0.5f, -0.34f, -0.95f), new Color(0.08f, 0.95f, 1f));
            rightSparks = CreateSparkEmitter("RightSparks", new Vector3(0.5f, -0.34f, -0.95f), new Color(1f, 0.28f, 0.72f));

            leftRearAnchor = CreateAnchor("LeftRearAnchor", new Vector3(-0.32f, -0.5f, -0.18f));
            rightRearAnchor = CreateAnchor("RightRearAnchor", new Vector3(0.32f, -0.5f, -0.18f));

            leftTrailLine = CreateTrailLine("LeftTrailLine", new Color(0.08f, 0.95f, 1f));
            rightTrailLine = CreateTrailLine("RightTrailLine", new Color(1f, 0.28f, 0.72f));

            lastPosition = transform.position;
            ResetTrailPoints();
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

            float speedFactor = Mathf.InverseLerp(10f, 34f, game.VisualScrollSpeed);
            float lateralFactor = Mathf.Clamp01(Mathf.Abs(velocity.x) / 8f);
            float glitchFactor = game.ControlsInverted ? 1f : 0f;
            float chapterFactor = game.IsChapterRush ? 1f : 0f;
            float chaos = Mathf.Clamp01(speedFactor + (lateralFactor * 0.7f) + (glitchFactor * 0.65f) + (chapterFactor * 0.8f));

            bool active = game.State == GlitchRacerGame.SessionState.Playing || game.IsMenuVisible;
            UpdateSparkEmitter(leftSparks, active, chaos, lateralFactor, glitchFactor);
            UpdateSparkEmitter(rightSparks, active, chaos, lateralFactor, glitchFactor);
            UpdateTrailLine(leftTrailLine, leftTrailPoints, leftRearAnchor, active, chaos, lateralFactor, glitchFactor, new Color(0.08f, 0.95f, 1f));
            UpdateTrailLine(rightTrailLine, rightTrailPoints, rightRearAnchor, active, chaos, lateralFactor, glitchFactor, new Color(1f, 0.28f, 0.72f));

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
            emission.rateOverTime = active ? Mathf.Lerp(3f, 18f, chaos) : 0f;

            var main = system.main;
            main.startLifetime = Mathf.Lerp(0.08f, 0.16f, chaos);
            main.startSpeed = Mathf.Lerp(0.4f, 1.5f, chaos);
            main.startSize = Mathf.Lerp(0.03f, 0.06f, chaos);

            var shape = system.shape;
            shape.rotation = new Vector3(0f, 0f, Mathf.Lerp(12f, 36f, lateralFactor + glitchFactor * 0.3f));
        }

        private void UpdateTrailLine(LineRenderer line, List<Vector3> points, Transform frontAnchor, bool active, float chaos, float lateralFactor, float glitchFactor, Color baseColor)
        {
            if (line == null || frontAnchor == null)
            {
                return;
            }

            float movement = game.VisualScrollSpeed * Time.deltaTime;
            for (int i = 0; i < points.Count; i++)
            {
                points[i] += Vector3.back * movement;
            }

            Vector3 frontPoint = frontAnchor.position;
            if (!active)
            {
                points.Clear();
            }

            if (points.Count == 0)
            {
                points.Add(frontPoint);
                points.Add(frontPoint + Vector3.back * 0.06f);
            }
            else
            {
                points[0] = frontPoint;
                if (points.Count == 1 || Vector3.Distance(points[1], frontPoint) > TrailSampleDistance)
                {
                    points.Insert(1, frontPoint);
                }
            }

            float traveled = 0f;
            for (int i = 1; i < points.Count; i++)
            {
                traveled += Vector3.Distance(points[i - 1], points[i]);
                if (traveled > MaxTrailLength)
                {
                    points.RemoveRange(i, points.Count - i);
                    break;
                }
            }

            line.enabled = active;
            line.widthMultiplier = Mathf.Lerp(0.16f, 0.24f, Mathf.Clamp01(chaos + lateralFactor * 0.5f));
            line.positionCount = points.Count;
            line.SetPositions(points.ToArray());

            Gradient gradient = new();
            Color head = Color.Lerp(baseColor, Color.white, 0.2f);
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(head, 0f),
                    new GradientColorKey(baseColor, 0.4f),
                    new GradientColorKey(baseColor, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.95f, 0f),
                    new GradientAlphaKey(0.6f, 0.35f),
                    new GradientAlphaKey(0f, 1f)
                });
            line.colorGradient = gradient;
        }

        private Transform CreateAnchor(string anchorName, Vector3 localPosition)
        {
            GameObject anchor = new(anchorName);
            anchor.transform.SetParent(transform, false);
            anchor.transform.localPosition = localPosition;
            return anchor.transform;
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
            main.startLifetime = 0.1f;
            main.startSpeed = 0.8f;
            main.startSize = 0.04f;
            main.startColor = color;
            main.maxParticles = 60;

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
            velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0.02f, 0.16f);
            velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.9f, -0.35f);

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

        private LineRenderer CreateTrailLine(string trailName, Color color)
        {
            GameObject lineObject = new(trailName);
            if (transform.parent != null)
            {
                lineObject.transform.SetParent(transform.parent, true);
            }

            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.material = new Material(fxMaterial);
            line.useWorldSpace = true;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch;
            line.numCornerVertices = 2;
            line.numCapVertices = 2;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.startColor = color;
            line.endColor = color;
            return line;
        }

        private void ResetTrailPoints()
        {
            Vector3 leftPoint = leftRearAnchor.position;
            Vector3 rightPoint = rightRearAnchor.position;
            leftTrailPoints.Clear();
            rightTrailPoints.Clear();
            leftTrailPoints.Add(leftPoint);
            leftTrailPoints.Add(leftPoint + Vector3.back * 0.06f);
            rightTrailPoints.Add(rightPoint);
            rightTrailPoints.Add(rightPoint + Vector3.back * 0.06f);
        }
    }
}
