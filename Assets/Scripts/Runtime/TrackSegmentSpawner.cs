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

            GameObject deckGlow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deckGlow.name = "DeckGlow";
            deckGlow.transform.SetParent(parent, false);
            deckGlow.transform.localPosition = new Vector3(0f, -0.04f, 0f);
            deckGlow.transform.localScale = new Vector3(7.4f, 0.02f, segmentLength * 1.02f);
            ApplyColor(deckGlow, new Color(0.11f, 0.14f, 0.2f));
            RemoveCollider(deckGlow);

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

                GameObject panelGlow = GameObject.CreatePrimitive(PrimitiveType.Cube);
                panelGlow.name = $"PanelGlow_{marker}";
                panelGlow.transform.SetParent(parent, false);
                panelGlow.transform.localPosition = new Vector3(0f, 0.04f, z);
                panelGlow.transform.localScale = new Vector3(8.6f, 0.01f, 0.34f);
                ApplyColor(panelGlow, new Color(0.16f, 0.2f, 0.3f));
                RemoveCollider(panelGlow);
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
            for (int i = 0; i < 3; i++)
            {
                float z = -segmentLength * 0.5f + 5f + i * 10f;

                GameObject crossbar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                crossbar.name = $"Crossbar_{i}";
                crossbar.transform.SetParent(parent, false);
                crossbar.transform.localPosition = new Vector3(0f, -0.42f, z);
                crossbar.transform.localScale = new Vector3(8.2f, 0.05f, 0.14f);
                ApplyColor(crossbar, new Color(0.05f, 0.07f, 0.11f));
                RemoveCollider(crossbar);

                for (int side = 0; side < 2; side++)
                {
                    float x = side == 0 ? -3.8f : 3.8f;

                    GameObject pylon = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    pylon.name = $"Pylon_{i}_{side}";
                    pylon.transform.SetParent(parent, false);
                    pylon.transform.localPosition = new Vector3(x, -3.9f, z);
                    pylon.transform.localScale = new Vector3(0.26f, 7.2f, 0.26f);
                    ApplyColor(pylon, new Color(0.05f, 0.06f, 0.1f));
                    RemoveCollider(pylon);

                    GameObject pylonAccent = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    pylonAccent.name = $"PylonAccent_{i}_{side}";
                    pylonAccent.transform.SetParent(parent, false);
                    pylonAccent.transform.localPosition = new Vector3(x, -1.2f, z);
                    pylonAccent.transform.localScale = new Vector3(0.04f, 1.6f, 0.04f);
                    ApplyColor(pylonAccent, side == 0 ? new Color(0.08f, 0.95f, 1f) : new Color(1f, 0.24f, 0.7f));
                    RemoveCollider(pylonAccent);
                }
            }
        }

        private void CreateServerAbyss(Transform parent, int currentSegmentIndex)
        {
            for (int i = 0; i < 5; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float x = Random.Range(6.3f, 8.8f) * side;
                float y = Random.Range(2.2f, 4.2f);
                float z = Random.Range(-segmentLength * 0.45f, segmentLength * 0.45f);

                GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tower.name = $"ServerTower_{i}";
                tower.transform.SetParent(parent, false);
                tower.transform.localPosition = new Vector3(x, y, z);
                tower.transform.localRotation = Quaternion.identity;
                tower.transform.localScale = new Vector3(Random.Range(0.9f, 1.4f), Random.Range(5.6f, 8.8f), Random.Range(1.1f, 1.7f));
                ApplyColor(tower, new Color(0.05f, 0.06f, 0.09f));
                RemoveCollider(tower);

                AddServerMassing(parent, tower.transform.position, tower.transform.localScale, i);
                AddWindows(parent, tower.transform.position, tower.transform.localScale, i);
                AddTowerAccent(parent, tower.transform.position, tower.transform.localScale, i);

                if ((i + currentSegmentIndex) % 2 == 0)
                {
                    GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    shaft.name = $"DataShaft_{i}";
                    shaft.transform.SetParent(parent, false);
                    shaft.transform.localPosition = new Vector3(x * 0.94f, 1.2f, z);
                    shaft.transform.localScale = new Vector3(0.14f, 2.6f, 0.14f);
                    ApplyColor(shaft, new Color(0.06f, 0.22f, 0.34f));
                    RemoveCollider(shaft);
                }
            }
        }

        private void AddServerMassing(Transform parent, Vector3 towerPosition, Vector3 towerScale, int seed)
        {
            Color shellColor = seed % 2 == 0 ? new Color(0.1f, 0.13f, 0.18f) : new Color(0.12f, 0.09f, 0.16f);
            float faceDirection = Mathf.Sign(-towerPosition.x);
            float faceX = towerPosition.x + faceDirection * (towerScale.x * 0.5f + 0.03f);

            GameObject coreFace = GameObject.CreatePrimitive(PrimitiveType.Cube);
            coreFace.name = $"ServerCoreFace_{seed}";
            coreFace.transform.SetParent(parent, false);
            coreFace.transform.position = new Vector3(faceX, towerPosition.y, towerPosition.z);
            coreFace.transform.localScale = new Vector3(0.06f, towerScale.y * 0.9f, towerScale.z * 0.76f);
            ApplyColor(coreFace, shellColor);
            RemoveCollider(coreFace);

            int bayCount = Mathf.Clamp(Mathf.RoundToInt(towerScale.y * 0.8f), 6, 10);
            float bayStep = towerScale.y * 0.78f / bayCount;
            for (int bay = 0; bay < bayCount; bay++)
            {
                float bayY = towerPosition.y - towerScale.y * 0.35f + bayStep * (bay + 0.5f);

                GameObject bayPanel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bayPanel.name = $"RackBay_{seed}_{bay}";
                bayPanel.transform.SetParent(parent, false);
                bayPanel.transform.position = new Vector3(faceX + faceDirection * 0.02f, bayY, towerPosition.z);
                bayPanel.transform.localScale = new Vector3(0.025f, bayStep * 0.72f, towerScale.z * 0.62f);
                ApplyColor(bayPanel, new Color(0.05f, 0.06f, 0.09f));
                RemoveCollider(bayPanel);
            }

            GameObject topCap = GameObject.CreatePrimitive(PrimitiveType.Cube);
            topCap.name = $"ServerTopCap_{seed}";
            topCap.transform.SetParent(parent, false);
            topCap.transform.position = new Vector3(towerPosition.x, towerPosition.y + towerScale.y * 0.47f, towerPosition.z);
            topCap.transform.localScale = new Vector3(1.16f, 0.14f, towerScale.z * 0.78f);
            ApplyColor(topCap, new Color(0.15f, 0.18f, 0.24f));
            RemoveCollider(topCap);
        }

        private void AddWindows(Transform parent, Vector3 towerPosition, Vector3 towerScale, int seed)
        {
            int rows = Mathf.Clamp(Mathf.RoundToInt(towerScale.y * 0.85f), 6, 11);
            int columns = 2;
            Color windowColor = seed % 2 == 0 ? new Color(0.09f, 0.95f, 1f) : new Color(1f, 0.34f, 0.82f);
            float faceDirection = Mathf.Sign(-towerPosition.x);
            float faceX = towerPosition.x + faceDirection * (towerScale.x * 0.5f + 0.05f);
            float rowStep = towerScale.y * 0.72f / rows;

            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    if (Random.value < 0.2f)
                    {
                        continue;
                    }

                    GameObject window = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    window.name = $"Window_{row}_{column}";
                    window.transform.SetParent(parent, false);

                    float x = faceX;
                    float y = towerPosition.y - (towerScale.y * 0.32f) + rowStep * (row + 0.5f);
                    float z = towerPosition.z + (column == 0 ? -0.22f : 0.22f);
                    window.transform.position = new Vector3(x, y, z);
                    window.transform.localScale = new Vector3(0.03f, 0.09f, 0.07f);
                    ApplyColor(window, Color.Lerp(windowColor, Color.white, Random.Range(0.05f, 0.2f)));
                    RemoveCollider(window);
                }
            }
        }

        private void AddTowerAccent(Transform parent, Vector3 towerPosition, Vector3 towerScale, int seed)
        {
            Color accentColor = seed % 2 == 0 ? new Color(0.08f, 0.95f, 1f) : new Color(1f, 0.24f, 0.72f);
            float faceDirection = Mathf.Sign(-towerPosition.x);
            float faceX = towerPosition.x + faceDirection * (towerScale.x * 0.5f + 0.06f);

            for (int stripIndex = 0; stripIndex < 2; stripIndex++)
            {
                float y = towerPosition.y - towerScale.y * 0.18f + stripIndex * towerScale.y * 0.34f;

                GameObject strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                strip.name = $"ServerLight_{seed}_{stripIndex}";
                strip.transform.SetParent(parent, false);
                strip.transform.position = new Vector3(faceX, y, towerPosition.z);
                strip.transform.localScale = new Vector3(0.03f, 0.04f, towerScale.z * 0.7f);
                ApplyColor(strip, accentColor);
                RemoveCollider(strip);
            }
        }

        private void CreateScanArches(Transform parent, int currentSegmentIndex)
        {
            if (currentSegmentIndex % 2 != 0)
            {
                return;
            }

            float z = Random.Range(-2f, 2f);
            Color leftAccent = new(0.08f, 0.7f, 0.92f);
            Color rightAccent = new(0.88f, 0.28f, 0.68f);
            Color frameColor = new(0.09f, 0.11f, 0.16f);

            GameObject archRoot = new("ScanArch");
            archRoot.transform.SetParent(parent, false);
            archRoot.transform.localPosition = new Vector3(0f, 0f, z);

            GameObject topBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            topBar.name = "ScanArchTop";
            topBar.transform.SetParent(archRoot.transform, false);
            topBar.transform.localPosition = new Vector3(0f, 5.1f, 0f);
            topBar.transform.localScale = new Vector3(10.9f, 0.2f, 0.48f);
            ApplyColor(topBar, frameColor);
            RemoveCollider(topBar);

            for (int side = 0; side < 2; side++)
            {
                float x = side == 0 ? -6.1f : 6.1f;
                Color accent = side == 0 ? leftAccent : rightAccent;

                GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pillar.name = $"ScanArchPillar_{side}";
                pillar.transform.SetParent(archRoot.transform, false);
                pillar.transform.localPosition = new Vector3(x, 2.55f, 0f);
                pillar.transform.localScale = new Vector3(0.42f, 5.1f, 0.52f);
                ApplyColor(pillar, frameColor);
                RemoveCollider(pillar);

                GameObject innerGlow = GameObject.CreatePrimitive(PrimitiveType.Cube);
                innerGlow.name = $"ScanArchGlow_{side}";
                innerGlow.transform.SetParent(archRoot.transform, false);
                innerGlow.transform.localPosition = new Vector3(x + (side == 0 ? 0.14f : -0.14f), 2.75f, 0f);
                innerGlow.transform.localScale = new Vector3(0.06f, 4.4f, 0.12f);
                ApplyColor(innerGlow, accent);
                RemoveCollider(innerGlow);

                GameObject foot = GameObject.CreatePrimitive(PrimitiveType.Cube);
                foot.name = $"ScanArchFoot_{side}";
                foot.transform.SetParent(archRoot.transform, false);
                foot.transform.localPosition = new Vector3(x, 0.15f, 0f);
                foot.transform.localScale = new Vector3(0.76f, 0.22f, 0.9f);
                ApplyColor(foot, new Color(0.07f, 0.09f, 0.14f));
                RemoveCollider(foot);
            }

            GameObject innerTopGlow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            innerTopGlow.name = "ScanArchTopGlow";
            innerTopGlow.transform.SetParent(archRoot.transform, false);
            innerTopGlow.transform.localPosition = new Vector3(0f, 5.1f, 0f);
            innerTopGlow.transform.localScale = new Vector3(9.6f, 0.06f, 0.12f);
            ApplyColor(innerTopGlow, new Color(0.68f, 0.88f, 1f));
            RemoveCollider(innerTopGlow);

            GameObject signBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            signBody.name = "ArchBillboard";
            signBody.transform.SetParent(archRoot.transform, false);
            signBody.transform.localPosition = new Vector3(0f, 5.64f, 0f);
            signBody.transform.localScale = new Vector3(3.8f, 0.78f, 0.16f);
            ApplyColor(signBody, new Color(0.08f, 0.1f, 0.14f));
            RemoveCollider(signBody);

            GameObject signBorder = GameObject.CreatePrimitive(PrimitiveType.Cube);
            signBorder.name = "ArchBillboardBorder";
            signBorder.transform.SetParent(signBody.transform, false);
            signBorder.transform.localPosition = Vector3.zero;
            signBorder.transform.localScale = new Vector3(1.04f, 1.08f, 0.9f);
            ApplyColor(signBorder, new Color(0.11f, 0.8f, 0.98f));
            RemoveCollider(signBorder);

            GameObject signCore = GameObject.CreatePrimitive(PrimitiveType.Cube);
            signCore.name = "ArchBillboardCore";
            signCore.transform.SetParent(signBody.transform, false);
            signCore.transform.localPosition = new Vector3(0f, 0f, -0.02f);
            signCore.transform.localScale = new Vector3(0.88f, 0.74f, 0.72f);
            ApplyColor(signCore, new Color(0.78f, 0.92f, 1f));
            RemoveCollider(signCore);

            GameObject leftBracket = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftBracket.name = "ArchBillboardBracketL";
            leftBracket.transform.SetParent(archRoot.transform, false);
            leftBracket.transform.localPosition = new Vector3(-1.35f, 5.38f, 0f);
            leftBracket.transform.localRotation = Quaternion.Euler(0f, 0f, 24f);
            leftBracket.transform.localScale = new Vector3(0.16f, 0.58f, 0.12f);
            ApplyColor(leftBracket, frameColor);
            RemoveCollider(leftBracket);

            GameObject rightBracket = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightBracket.name = "ArchBillboardBracketR";
            rightBracket.transform.SetParent(archRoot.transform, false);
            rightBracket.transform.localPosition = new Vector3(1.35f, 5.38f, 0f);
            rightBracket.transform.localRotation = Quaternion.Euler(0f, 0f, -24f);
            rightBracket.transform.localScale = new Vector3(0.16f, 0.58f, 0.12f);
            ApplyColor(rightBracket, frameColor);
            RemoveCollider(rightBracket);
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
            GlitchRacerGame.GlitchType glitchType = (GlitchRacerGame.GlitchType)Random.Range(1, 5);
            Color glitchColor = glitchType switch
            {
                GlitchRacerGame.GlitchType.InvertControls => new Color(0.7f, 0.2f, 1f),
                GlitchRacerGame.GlitchType.StaticNoise => new Color(1f, 0.94f, 0.38f),
                GlitchRacerGame.GlitchType.DrunkVision => new Color(0.3f, 1f, 0.95f),
                GlitchRacerGame.GlitchType.DrugsTrip => new Color(1f, 0.34f, 0.92f),
                _ => new Color(0.7f, 0.2f, 1f)
            };

            float duration = 15f;
            CreateEntity(parent, TrackEntityType.Glitch, 120f, glitchLane, z, PrimitiveType.Cube, new Vector3(1.4f, 1.4f, 1.4f), glitchColor, duration, 45f, glitchType);

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

        private void CreateEntity(Transform parent, TrackEntityType type, float amount, int lane, float z, PrimitiveType primitiveType, Vector3 scale, Color color, float glitchDuration = 5f, float yRotation = 0f, GlitchRacerGame.GlitchType glitchType = GlitchRacerGame.GlitchType.InvertControls)
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
            trackEntity.Setup(type, amount, glitchDuration, glitchType);

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
