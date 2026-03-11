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
            CreateVoidFog(parent, currentSegmentIndex);
            CreateBridgeDeck(parent, currentSegmentIndex);
            CreateBridgeSupports(parent, currentSegmentIndex);
            CreateServerAbyss(parent, currentSegmentIndex);
            CreateScanArches(parent, currentSegmentIndex);
        }

        private void CreateVoidFog(Transform parent, int currentSegmentIndex)
        {
            GameObject haze = GameObject.CreatePrimitive(PrimitiveType.Cube);
            haze.name = "VoidFog";
            haze.transform.SetParent(parent, false);
            haze.transform.localPosition = new Vector3(0f, -8.5f, 0f);
            haze.transform.localScale = new Vector3(34f, 0.2f, segmentLength * 1.35f);
            ApplyColor(haze, Color.Lerp(new Color(0.03f, 0.01f, 0.08f), new Color(0.02f, 0.09f, 0.14f), (currentSegmentIndex % 2) * 0.35f));
            RemoveCollider(haze);

            GameObject riftCore = GameObject.CreatePrimitive(PrimitiveType.Cube);
            riftCore.name = "RiftCore";
            riftCore.transform.SetParent(parent, false);
            riftCore.transform.localPosition = new Vector3(0f, -12.5f, 0f);
            riftCore.transform.localScale = new Vector3(16f, 0.08f, segmentLength * 0.9f);
            ApplyColor(riftCore, new Color(0.02f, 0.45f, 0.7f));
            RemoveCollider(riftCore);
        }

        private void CreateBridgeDeck(Transform parent, int currentSegmentIndex)
        {
            Color hullColor = new(0.08f, 0.1f, 0.14f);
            Color panelColor = new(0.13f, 0.16f, 0.22f);
            Color leftAccent = new(0.08f, 0.95f, 1f);
            Color rightAccent = new(1f, 0.32f, 0.76f);
            Color centerAccent = new(0.7f, 0.9f, 1f);

            GameObject deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deck.name = "BridgeDeck";
            deck.transform.SetParent(parent, false);
            deck.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            deck.transform.localScale = new Vector3(10.2f, 0.16f, segmentLength);
            ApplyColor(deck, hullColor);
            RemoveCollider(deck);

            GameObject undercarriage = GameObject.CreatePrimitive(PrimitiveType.Cube);
            undercarriage.name = "Undercarriage";
            undercarriage.transform.SetParent(parent, false);
            undercarriage.transform.localPosition = new Vector3(0f, -0.38f, 0f);
            undercarriage.transform.localScale = new Vector3(8.8f, 0.26f, segmentLength);
            ApplyColor(undercarriage, new Color(0.04f, 0.05f, 0.08f));
            RemoveCollider(undercarriage);

            for (int strip = 0; strip < 2; strip++)
            {
                float x = strip == 0 ? -2f : 2f;
                GameObject seam = GameObject.CreatePrimitive(PrimitiveType.Cube);
                seam.name = $"LaneSeam_{strip}";
                seam.transform.SetParent(parent, false);
                seam.transform.localPosition = new Vector3(x, 0.06f, 0f);
                seam.transform.localScale = new Vector3(0.14f, 0.02f, segmentLength);
                ApplyColor(seam, new Color(0.21f, 0.25f, 0.34f));
                RemoveCollider(seam);
            }

            for (int marker = 0; marker < 5; marker++)
            {
                float z = -segmentLength * 0.5f + 3f + marker * 6f;

                GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                panel.name = $"DeckPanel_{marker}";
                panel.transform.SetParent(parent, false);
                panel.transform.localPosition = new Vector3(0f, 0.065f, z);
                panel.transform.localScale = new Vector3(9.3f, 0.02f, 0.95f);
                ApplyColor(panel, panelColor);
                RemoveCollider(panel);

                GameObject centerLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
                centerLine.name = $"CenterLine_{marker}";
                centerLine.transform.SetParent(parent, false);
                centerLine.transform.localPosition = new Vector3(0f, 0.085f, z);
                centerLine.transform.localScale = new Vector3(0.42f, 0.015f, 0.65f);
                ApplyColor(centerLine, centerAccent);
                RemoveCollider(centerLine);
            }

            CreateBridgeSide(parent, -4.8f, leftAccent, "Left");
            CreateBridgeSide(parent, 4.8f, rightAccent, "Right");
        }

        private void CreateBridgeSide(Transform parent, float sideX, Color accent, string sideName)
        {
            GameObject sideWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sideWall.name = $"{sideName}Wall";
            sideWall.transform.SetParent(parent, false);
            sideWall.transform.localPosition = new Vector3(sideX, 0.28f, 0f);
            sideWall.transform.localScale = new Vector3(0.32f, 0.55f, segmentLength);
            ApplyColor(sideWall, new Color(0.08f, 0.1f, 0.14f));
            RemoveCollider(sideWall);

            GameObject topRail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            topRail.name = $"{sideName}Rail";
            topRail.transform.SetParent(parent, false);
            topRail.transform.localPosition = new Vector3(sideX, 0.66f, 0f);
            topRail.transform.localScale = new Vector3(0.12f, 0.16f, segmentLength);
            ApplyColor(topRail, Color.Lerp(accent, Color.white, 0.15f));
            RemoveCollider(topRail);

            GameObject edgeGlow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            edgeGlow.name = $"{sideName}Glow";
            edgeGlow.transform.SetParent(parent, false);
            edgeGlow.transform.localPosition = new Vector3(sideX * 0.985f, 0.52f, 0f);
            edgeGlow.transform.localScale = new Vector3(0.06f, 0.62f, segmentLength * 1.01f);
            ApplyColor(edgeGlow, accent);
            RemoveCollider(edgeGlow);
        }

        private void CreateBridgeSupports(Transform parent, int currentSegmentIndex)
        {
            for (int i = 0; i < 4; i++)
            {
                float z = -segmentLength * 0.5f + 4f + i * 8f;

                GameObject crossbar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                crossbar.name = $"Crossbar_{i}";
                crossbar.transform.SetParent(parent, false);
                crossbar.transform.localPosition = new Vector3(0f, -0.36f, z);
                crossbar.transform.localScale = new Vector3(9.7f, 0.05f, 0.18f);
                ApplyColor(crossbar, new Color(0.05f, 0.07f, 0.11f));
                RemoveCollider(crossbar);

                for (int side = 0; side < 2; side++)
                {
                    float x = side == 0 ? -5.2f : 5.2f;
                    GameObject sideRail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    sideRail.name = $"SideRail_{i}_{side}";
                    sideRail.transform.SetParent(parent, false);
                    sideRail.transform.localPosition = new Vector3(x, 0.18f, z);
                    sideRail.transform.localScale = new Vector3(0.08f, 0.44f, 0.18f);
                    ApplyColor(sideRail, new Color(0.1f, 0.14f, 0.2f));
                    RemoveCollider(sideRail);

                    GameObject sideAccent = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    sideAccent.name = $"SideAccent_{i}_{side}";
                    sideAccent.transform.SetParent(parent, false);
                    sideAccent.transform.localPosition = new Vector3(x, 0.22f, z);
                    sideAccent.transform.localScale = new Vector3(0.03f, 0.34f, 0.06f);
                    ApplyColor(sideAccent, side == 0 ? new Color(0.08f, 0.95f, 1f) : new Color(1f, 0.24f, 0.7f));
                    RemoveCollider(sideAccent);

                    GameObject diagonal = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    diagonal.name = $"Diagonal_{i}_{side}";
                    diagonal.transform.SetParent(parent, false);
                    diagonal.transform.localPosition = new Vector3(x * 0.7f, -3.2f, z);
                    diagonal.transform.localRotation = Quaternion.Euler(0f, 0f, side == 0 ? 22f : -22f);
                    diagonal.transform.localScale = new Vector3(0.06f, 6.6f, 0.12f);
                    ApplyColor(diagonal, new Color(0.04f, 0.05f, 0.09f));
                    RemoveCollider(diagonal);
                }
            }
        }

        private void CreateServerAbyss(Transform parent, int currentSegmentIndex)
        {
            for (int i = 0; i < 10; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float x = Random.Range(8f, 16f) * side;
                float y = Random.Range(-16f, -4f);
                float z = Random.Range(-segmentLength * 0.45f, segmentLength * 0.45f);

                GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tower.name = $"ServerTower_{i}";
                tower.transform.SetParent(parent, false);
                tower.transform.localPosition = new Vector3(x, y, z);
                tower.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 45f), 0f);
                tower.transform.localScale = new Vector3(Random.Range(1.1f, 2.6f), Random.Range(8f, 22f), Random.Range(1.1f, 2.6f));
                ApplyColor(tower, i % 3 == 0 ? new Color(0.05f, 0.1f, 0.2f) : new Color(0.09f, 0.06f, 0.14f));
                RemoveCollider(tower);

                GameObject strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                strip.name = $"ServerLight_{i}";
                strip.transform.SetParent(tower.transform, false);
                strip.transform.localPosition = new Vector3(0f, 0f, tower.transform.localScale.z * 0.51f);
                strip.transform.localScale = new Vector3(0.12f, 0.96f, 0.08f);
                ApplyColor(strip, i % 2 == 0 ? new Color(0.08f, 0.95f, 1f) : new Color(1f, 0.24f, 0.72f));
                RemoveCollider(strip);

                if ((i + currentSegmentIndex) % 3 == 0)
                {
                    GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    shaft.name = $"DataShaft_{i}";
                    shaft.transform.SetParent(parent, false);
                    shaft.transform.localPosition = new Vector3(x * 0.75f, -8f, z);
                    shaft.transform.localScale = new Vector3(0.45f, 18f, 0.45f);
                    ApplyColor(shaft, new Color(0.05f, 0.42f, 0.66f));
                    RemoveCollider(shaft);
                }
            }
        }

        private void CreateScanArches(Transform parent, int currentSegmentIndex)
        {
            if (currentSegmentIndex % 2 != 0)
            {
                return;
            }

            float z = Random.Range(-2f, 2f);
            GameObject topBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            topBar.name = "ScanArchTop";
            topBar.transform.SetParent(parent, false);
            topBar.transform.localPosition = new Vector3(0f, 4.6f, z);
            topBar.transform.localScale = new Vector3(14.5f, 0.16f, 0.6f);
            ApplyColor(topBar, new Color(0.14f, 0.7f, 1f));
            RemoveCollider(topBar);

            for (int side = 0; side < 2; side++)
            {
                float x = side == 0 ? -7.2f : 7.2f;
                GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pillar.name = $"ScanArchPillar_{side}";
                pillar.transform.SetParent(parent, false);
                pillar.transform.localPosition = new Vector3(x, 2.2f, z);
                pillar.transform.localScale = new Vector3(0.22f, 4.7f, 0.4f);
                ApplyColor(pillar, side == 0 ? new Color(0.08f, 0.95f, 1f) : new Color(1f, 0.28f, 0.78f));
                RemoveCollider(pillar);
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
