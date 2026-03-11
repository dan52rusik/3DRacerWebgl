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

            CreateTrackVisual(segmentRoot.transform);
            PopulateSegment(segmentRoot.transform, segmentIndex);

            activeSegments.Add(segmentRoot);
            nextSpawnZ += segmentLength;
            segmentIndex++;
        }

        private void CreateTrackVisual(Transform parent)
        {
            GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
            road.name = "Road";
            road.transform.SetParent(parent, false);
            road.transform.localPosition = new Vector3(0f, -0.6f, 0f);
            road.transform.localScale = new Vector3(16f, 1f, segmentLength);
            ApplyColor(road, new Color(0.06f, 0.07f, 0.12f));
            RemoveCollider(road);

            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? -7f : 7f;
                GameObject rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rail.name = $"Rail_{i}";
                rail.transform.SetParent(parent, false);
                rail.transform.localPosition = new Vector3(x, 0.35f, 0f);
                rail.transform.localScale = new Vector3(0.2f, 1.1f, segmentLength);
                ApplyColor(rail, new Color(0f, 1f, 0.93f));
                RemoveCollider(rail);
            }

            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? -2f : 2f;
                GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stripe.name = $"LaneStripe_{i}";
                stripe.transform.SetParent(parent, false);
                stripe.transform.localPosition = new Vector3(x, -0.05f, 0f);
                stripe.transform.localScale = new Vector3(0.12f, 0.05f, segmentLength);
                ApplyColor(stripe, new Color(0.18f, 0.5f, 1f));
                RemoveCollider(stripe);
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
