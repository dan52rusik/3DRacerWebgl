using System.Collections.Generic;
using UnityEngine;

namespace GlitchRacer
{
    public class TrackSegmentSpawner : MonoBehaviour
    {
        [SerializeField] private float segmentLength = 30f;
        [SerializeField] private int visibleSegments = 7;
        [SerializeField] private float laneOffset = 4f;

        private readonly List<GameObject> activeSegments = new();
        private GlitchRacerGame game;
        private float nextSpawnZ;
        private int segmentIndex;

        public void Configure(GlitchRacerGame gameManager)
        {
            game = gameManager;
        }

        private void Start()
        {
            if (game == null)
            {
                game = FindFirstObjectByType<GlitchRacerGame>();
            }

            ResetTrack();
        }

        private void Update()
        {
            if (game == null || game.IsGameOver)
            {
                return;
            }

            float movement = game.CurrentSpeed * Time.deltaTime;
            for (int i = 0; i < activeSegments.Count; i++)
            {
                activeSegments[i].transform.position += Vector3.back * movement;
            }

            if (activeSegments.Count > 0 && activeSegments[0].transform.position.z < -segmentLength * 1.5f)
            {
                Destroy(activeSegments[0]);
                activeSegments.RemoveAt(0);
                SpawnSegment();
            }
        }

        public void ResetTrack()
        {
            for (int i = 0; i < activeSegments.Count; i++)
            {
                if (activeSegments[i] != null)
                {
                    Destroy(activeSegments[i]);
                }
            }

            activeSegments.Clear();
            nextSpawnZ = 0f;
            segmentIndex = 0;

            for (int i = 0; i < visibleSegments; i++)
            {
                SpawnSegment();
            }
        }

        private void SpawnSegment()
        {
            GameObject segmentRoot = new($"TrackSegment_{segmentIndex}");
            segmentRoot.transform.SetParent(transform, false);
            segmentRoot.transform.position = new Vector3(0f, 0f, nextSpawnZ);

            CreateTrackVisual(segmentRoot.transform, segmentIndex);
            PopulateSegment(segmentRoot.transform, segmentIndex);

            activeSegments.Add(segmentRoot);
            nextSpawnZ += segmentLength;
            segmentIndex++;
        }

        private void CreateTrackVisual(Transform parent, int currentSegmentIndex)
        {
            for (int lane = 0; lane < 3; lane++)
            {
                CreateLaneIslands(parent, lane, currentSegmentIndex);
            }

            CreateRiftGlow(parent);
            CreateVoidScenery(parent, currentSegmentIndex);
        }

        private void CreateLaneIslands(Transform parent, int lane, int currentSegmentIndex)
        {
            float laneX = (lane - 1) * laneOffset;
            Color laneColor = lane switch
            {
                0 => new Color(0.09f, 0.2f, 0.42f),
                1 => new Color(0.12f, 0.15f, 0.38f),
                _ => new Color(0.16f, 0.12f, 0.35f)
            };

            Color glowColor = lane switch
            {
                0 => new Color(0.08f, 0.95f, 1f),
                1 => new Color(0.36f, 0.66f, 1f),
                _ => new Color(1f, 0.23f, 0.78f)
            };

            for (int pad = 0; pad < 5; pad++)
            {
                float padZ = (-segmentLength * 0.5f) + 3.2f + (pad * 6f);
                float padY = -0.55f + Mathf.Sin((currentSegmentIndex * 0.8f) + (lane * 0.7f) + pad) * 0.18f;

                GameObject island = GameObject.CreatePrimitive(PrimitiveType.Cube);
                island.name = $"Lane_{lane}_Island_{pad}";
                island.transform.SetParent(parent, false);
                island.transform.localPosition = new Vector3(laneX, padY, padZ);
                island.transform.localScale = new Vector3(3.15f, 0.7f, 4.1f);
                ApplyColor(island, laneColor);
                RemoveCollider(island);

                GameObject underGlow = GameObject.CreatePrimitive(PrimitiveType.Cube);
                underGlow.name = $"Lane_{lane}_Glow_{pad}";
                underGlow.transform.SetParent(island.transform, false);
                underGlow.transform.localPosition = new Vector3(0f, -0.42f, 0f);
                underGlow.transform.localScale = new Vector3(1.02f, 0.12f, 1.04f);
                ApplyColor(underGlow, glowColor);
                RemoveCollider(underGlow);

                GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.name = $"Lane_{lane}_Shard_{pad}";
                shard.transform.SetParent(island.transform, false);
                shard.transform.localPosition = new Vector3(Random.Range(-1.1f, 1.1f), 0.65f, Random.Range(-1.4f, 1.4f));
                shard.transform.localRotation = Quaternion.Euler(Random.Range(-15f, 15f), Random.Range(0f, 45f), Random.Range(-22f, 22f));
                shard.transform.localScale = new Vector3(0.18f, Random.Range(0.35f, 0.9f), 0.18f);
                ApplyColor(shard, Color.Lerp(laneColor, glowColor, 0.65f));
                RemoveCollider(shard);
            }
        }

        private void CreateRiftGlow(Transform parent)
        {
            GameObject rift = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rift.name = "RiftGlow";
            rift.transform.SetParent(parent, false);
            rift.transform.localPosition = new Vector3(0f, -5.6f, 0f);
            rift.transform.localScale = new Vector3(24f, 0.15f, segmentLength * 1.2f);
            ApplyColor(rift, new Color(0.1f, 0.02f, 0.2f));
            RemoveCollider(rift);
        }

        private void CreateVoidScenery(Transform parent, int currentSegmentIndex)
        {
            for (int i = 0; i < 6; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float x = Random.Range(10f, 18f) * side;
                float y = Random.Range(-1.5f, 7f);
                float z = Random.Range(-segmentLength * 0.45f, segmentLength * 0.45f);

                GameObject monolith = GameObject.CreatePrimitive(PrimitiveType.Cube);
                monolith.name = $"VoidMonolith_{i}";
                monolith.transform.SetParent(parent, false);
                monolith.transform.localPosition = new Vector3(x, y, z);
                monolith.transform.localRotation = Quaternion.Euler(Random.Range(-12f, 12f), Random.Range(10f, 40f), Random.Range(-18f, 18f));
                monolith.transform.localScale = new Vector3(Random.Range(0.5f, 1.2f), Random.Range(4f, 10f), Random.Range(0.5f, 1.4f));
                ApplyColor(monolith, i % 3 == 0 ? new Color(0.08f, 0.85f, 0.95f) : new Color(0.18f, 0.12f, 0.28f));
                RemoveCollider(monolith);
            }

            if (currentSegmentIndex % 2 == 0)
            {
                GameObject arch = GameObject.CreatePrimitive(PrimitiveType.Cube);
                arch.name = "ScanlineArch";
                arch.transform.SetParent(parent, false);
                arch.transform.localPosition = new Vector3(0f, 4.4f, 0f);
                arch.transform.localRotation = Quaternion.Euler(0f, 0f, 6f);
                arch.transform.localScale = new Vector3(18f, 0.12f, 1.6f);
                ApplyColor(arch, new Color(0.12f, 0.55f, 1f));
                RemoveCollider(arch);
            }
        }

        private void PopulateSegment(Transform parent, int currentSegmentIndex)
        {
            for (float z = 4f; z < segmentLength - 4f; z += 6f)
            {
                if (currentSegmentIndex < 2)
                {
                    CreateScoreTriplet(parent, Random.Range(0, 3), z);
                    continue;
                }

                int pattern = Random.Range(0, 100);
                if (pattern < 20)
                {
                    CreateObstacleWall(parent, z);
                }
                else if (pattern < 42)
                {
                    CreateFuelPocket(parent, z);
                }
                else if (pattern < 54)
                {
                    CreateGlitchPickup(parent, z);
                }
                else
                {
                    CreateScoreTriplet(parent, Random.Range(0, 3), z);
                }
            }
        }

        private void CreateObstacleWall(Transform parent, float z)
        {
            int blockedLane = Random.Range(0, 3);
            for (int lane = 0; lane < 3; lane++)
            {
                if (lane == blockedLane)
                {
                    continue;
                }

                CreateEntity(parent, TrackEntityType.Score, 18f, lane, z, PrimitiveType.Sphere, new Vector3(1f, 1f, 1f), new Color(1f, 0.87f, 0.2f));
            }

            CreateEntity(parent, TrackEntityType.Obstacle, 28f, blockedLane, z + 0.4f, PrimitiveType.Cube, new Vector3(2.3f, 2.3f, 2.3f), new Color(1f, 0.16f, 0.38f));
        }

        private void CreateFuelPocket(Transform parent, float z)
        {
            int fuelLane = Random.Range(0, 3);
            CreateEntity(parent, TrackEntityType.Ram, 24f, fuelLane, z, PrimitiveType.Cylinder, new Vector3(1.2f, 0.5f, 1.2f), new Color(0.26f, 1f, 0.45f));

            int scoreLane = (fuelLane + Random.Range(1, 3)) % 3;
            CreateScoreTriplet(parent, scoreLane, z);
        }

        private void CreateGlitchPickup(Transform parent, float z)
        {
            int glitchLane = Random.Range(0, 3);
            CreateEntity(parent, TrackEntityType.Glitch, 120f, glitchLane, z, PrimitiveType.Cube, new Vector3(1.4f, 1.4f, 1.4f), new Color(0.7f, 0.2f, 1f), 5f, 45f);

            for (int lane = 0; lane < 3; lane++)
            {
                if (lane == glitchLane)
                {
                    continue;
                }

                CreateEntity(parent, TrackEntityType.Obstacle, 18f, lane, z + 0.5f, PrimitiveType.Cube, new Vector3(1.8f, 1.8f, 1.8f), new Color(1f, 0.3f, 0.3f));
            }
        }

        private void CreateScoreTriplet(Transform parent, int lane, float z)
        {
            for (int i = 0; i < 3; i++)
            {
                CreateEntity(parent, TrackEntityType.Score, 14f, lane, z + (i * 1.6f), PrimitiveType.Sphere, new Vector3(0.9f, 0.9f, 0.9f), new Color(1f, 0.87f, 0.2f));
            }
        }

        private void CreateEntity(Transform parent, TrackEntityType type, float amount, int lane, float z, PrimitiveType primitiveType, Vector3 scale, Color color, float glitchDuration = 5f, float yRotation = 0f)
        {
            GameObject entity = GameObject.CreatePrimitive(primitiveType);
            entity.transform.SetParent(parent, false);
            entity.transform.localPosition = new Vector3((lane - 1) * laneOffset, 1.05f, z - (segmentLength * 0.5f));
            entity.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
            entity.transform.localScale = scale;
            ApplyColor(entity, color);

            Collider collider = entity.GetComponent<Collider>();
            collider.isTrigger = true;

            TrackEntity trackEntity = entity.AddComponent<TrackEntity>();
            trackEntity.Setup(type, amount, glitchDuration);

            entity.AddComponent<SpinPulse>();
        }

        private static void ApplyColor(GameObject target, Color color)
        {
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            renderer.material.color = color;
        }

        private static void RemoveCollider(GameObject target)
        {
            Collider collider = target.GetComponent<Collider>();
            if (collider == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(collider);
                return;
            }
#endif

            Object.Destroy(collider);
        }
    }
}
