using System.Collections.Generic;
using UnityEngine;

namespace GlitchRacer
{
    public class TrackSegmentSpawner : MonoBehaviour
    {
        [SerializeField] private float segmentLength = 30f;
        [SerializeField] private int visibleSegments = 7;
        [SerializeField] private float laneOffset = 4f;
        [Header("Material Templates")]
        [SerializeField] private Material solidTemplate;
        [SerializeField] private Material glowTemplate;

        private readonly List<GameObject> activeSegments = new();
        private readonly Dictionary<Color, Material> solidMaterialPool = new();
        private Material cachedGlowMaterial;
        private Texture2D generatedGlowTexture;
        private Texture2D companyLogoTexture;
        private Material companyLogoMaterial;
        private GlitchRacerGame game;
        private float nextSpawnZ;
        private int segmentIndex;
        private bool lastChapterRushState;
        private int lastIntegrityTier = -1;

        public void Configure(GlitchRacerGame gameManager)
        {
            game = gameManager;
        }

        private void Start()
        {
            // FIX: убран безусловный ResetTrack() из Start().
            // Bootstrap вызывает Configure(game) -> EnterMainMenu() -> spawner.ResetTrack().
            // Если Start() тоже вызывал ResetTrack() - первый кадр перестраивал сегменты дважды.
            // Оставляем запасной вариант только если Bootstrap не сработал (тестовая сцена).
            if (game == null)
            {
                game = FindFirstObjectByType<GlitchRacerGame>();
                ResetTrack();
            }
        }

        private void Update()
        {
            if (game == null || game.IsGameOver)
            {
                return;
            }

            EnsureSegmentPool();
            if (activeSegments.Count == 0)
            {
                return;
            }

            bool chapterRushActive = game.IsChapterRush;
            if (chapterRushActive != lastChapterRushState)
            {
                RefreshSegmentEntities(chapterRushActive);
                lastChapterRushState = chapterRushActive;
            }

            int integrityTier = GetIntegrityTier();
            if (integrityTier != lastIntegrityTier)
            {
                ApplyTrackIntegrityState(integrityTier);
                lastIntegrityTier = integrityTier;
            }

            float movement = game.VisualScrollSpeed * Time.deltaTime;
            for (int i = 0; i < activeSegments.Count; i++)
            {
                activeSegments[i].transform.position += Vector3.back * movement;
            }

            nextSpawnZ -= movement;

            // Улучшенная проверка: используем цикл, если пропустили несколько сегментов из-за лага
            while (activeSegments.Count > 0 && activeSegments[0].transform.position.z < -segmentLength * 1.5f)
            {
                GameObject segment = activeSegments[0];
                activeSegments.RemoveAt(0);

                // Ставим сегмент ровно в конец очереди, используя текущий nextSpawnZ
                segment.transform.position = new Vector3(0f, 0f, nextSpawnZ);

                try
                {
                    ClearSegmentEntities(segment.transform);
                    if (!chapterRushActive)
                    {
                        PopulateSegment(segment.transform, segmentIndex);
                    }

                    ApplySegmentIntegrity(segment.transform, lastIntegrityTier, segmentIndex);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[TrackSegmentSpawner] Error updating segment {segmentIndex}: {ex}");
                }

                activeSegments.Add(segment);

                nextSpawnZ += segmentLength;
                segmentIndex++;
            }
        }

        public void ResetTrack()
        {
            RebuildSegmentPoolIfNeeded();

            nextSpawnZ = 0f;
            segmentIndex = 0;
            lastChapterRushState = game != null && game.IsChapterRush;
            lastIntegrityTier = GetIntegrityTier();

            for (int i = 0; i < activeSegments.Count; i++)
            {
                GameObject segment = activeSegments[i];
                segment.transform.position = new Vector3(0f, 0f, nextSpawnZ);

                try
                {
                    ClearSegmentEntities(segment.transform);
                    if (!lastChapterRushState)
                    {
                        PopulateSegment(segment.transform, segmentIndex);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[TrackSegmentSpawner] Error populated reset track segment {segmentIndex}: {ex}");
                }

                nextSpawnZ += segmentLength;
                segmentIndex++;
            }

            ApplyTrackIntegrityState(lastIntegrityTier);
        }

        public void ClearImminentObstacles()
        {
            for (int i = 0; i < activeSegments.Count; i++)
            {
                Transform segment = activeSegments[i].transform;
                if (segment.position.z > -15f && segment.position.z < segmentLength * 2f)
                {
                    TrackEntity[] entities = segment.GetComponentsInChildren<TrackEntity>();
                    for (int j = 0; j < entities.Length; j++)
                    {
                        if (entities[j].Type == TrackEntityType.Obstacle || entities[j].Type == TrackEntityType.Glitch)
                        {
                            Destroy(entities[j].gameObject);
                        }
                    }
                }
            }
        }

        private void ClearSegmentEntities(Transform segmentRoot)
        {
            TrackEntity[] entities = segmentRoot.GetComponentsInChildren<TrackEntity>();
            for (int i = 0; i < entities.Length; i++)
            {
                Destroy(entities[i].gameObject);
            }
        }

        private void RefreshSegmentEntities(bool chapterRushActive)
        {
            for (int i = 0; i < activeSegments.Count; i++)
            {
                if (activeSegments[i] == null)
                {
                    continue;
                }

                Transform segment = activeSegments[i].transform;
                try
                {
                    ClearSegmentEntities(segment);
                    if (!chapterRushActive)
                    {
                        PopulateSegment(segment, Mathf.Max(0, segmentIndex - activeSegments.Count + i));
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[TrackSegmentSpawner] Error in RefreshSegmentEntities: {ex}");
                }
            }
        }

        private void CreateTrackVisual(Transform parent, int currentSegmentIndex)
        {
            CreateVoidFog(parent, currentSegmentIndex);
            CreateBridgeDeck(parent, currentSegmentIndex);
            CreateBridgeSupports(parent, currentSegmentIndex);
            CreateServerAbyss(parent, currentSegmentIndex);

            bool isIntroSegment = currentSegmentIndex == 0;
            if (!isIntroSegment)
            {
                CreateAbyssRifts(parent, currentSegmentIndex);
                CreateAbyssCore(parent, currentSegmentIndex);
                CreateScanArches(parent, currentSegmentIndex);
            }
        }

        private void CreateVoidFog(Transform parent, int currentSegmentIndex)
        {
        }

        private void CreateBridgeDeck(Transform parent, int currentSegmentIndex)
        {
            Color panelColor = new(0.13f, 0.16f, 0.22f);
            Color centerAccent = new(0.7f, 0.9f, 1f);

            GameObject deckSurface = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deckSurface.name = "DeckSurface";
            deckSurface.transform.SetParent(parent, false);
            deckSurface.transform.localPosition = new Vector3(0f, 0.085f, 0f);
            deckSurface.transform.localScale = new Vector3(9.36f, 0.022f, segmentLength * 0.995f);
            ApplyColor(deckSurface, panelColor);
            RemoveCollider(deckSurface);

            float gapLength = 1.46f;
            float currentZ = -segmentLength * 0.5f;

            for (int i = 0; i < 5; i++)
            {
                float markerZ = -segmentLength * 0.5f + 3f + i * 6f;
                float gapStartZ = markerZ - gapLength * 0.5f;

                float solidLength = gapStartZ - currentZ;
                if (solidLength > 0.01f)
                {
                    CreateDeckSlice(parent, $"SolidBase_{i}", currentZ + solidLength * 0.5f, solidLength);
                }

                CreateDeckSlice(parent, $"GapBase_{i}", markerZ, gapLength);
                currentZ = markerZ + gapLength * 0.5f;
            }

            float finalSolidLength = (segmentLength * 0.5f) - currentZ;
            if (finalSolidLength > 0.01f)
            {
                CreateDeckSlice(parent, "SolidBase_5", currentZ + finalSolidLength * 0.5f, finalSolidLength);
            }

            for (int strip = 0; strip < 2; strip++)
            {
                float x = strip == 0 ? -2f : 2f;
                Color riftAccent = strip == 0 ? new Color(0.08f, 0.95f, 1f) : new Color(1f, 0.28f, 0.78f);
                for (int spark = 0; spark < 4; spark++)
                {
                    float sparkZ = -segmentLength * 0.34f + spark * 5.8f;
                    GameObject riftSpark = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    riftSpark.name = $"LaneRiftSpark_{strip}_{spark}";
                    riftSpark.transform.SetParent(parent, false);
                    riftSpark.transform.localPosition = new Vector3(x, 0.09f, sparkZ);
                    riftSpark.transform.localRotation = Quaternion.Euler(0f, 0f, spark % 2 == 0 ? 28f : -28f);
                    riftSpark.transform.localScale = new Vector3(0.34f, 0.03f, 0.08f);
                    ApplyColor(riftSpark, Color.Lerp(riftAccent, Color.white, 0.28f));
                    RemoveCollider(riftSpark);
                }
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

                CreateGlowBox(parent, $"CorruptionGapGlow_{marker}", new Vector3(0f, 0.09f, z), new Vector3(5.8f, 0.22f, 1.26f), new Color(1f, 0.22f, 0.4f, 0.08f));
                Transform gapGlow = parent.Find($"CorruptionGapGlow_{marker}");
                if (gapGlow != null)
                {
                    gapGlow.gameObject.SetActive(false);
                }

                CreateVisualPart(parent, $"CorruptionGapLipL_{marker}", PrimitiveType.Cube, new Vector3(-2.35f, 0.085f, z + 0.12f), new Vector3(0f, 0f, -12f), new Vector3(4.15f, 0.05f, 0.22f), new Color(0.15f, 0.16f, 0.22f));
                SetChildActive(parent, $"CorruptionGapLipL_{marker}", false);
                CreateVisualPart(parent, $"CorruptionGapLipR_{marker}", PrimitiveType.Cube, new Vector3(2.35f, 0.085f, z - 0.12f), new Vector3(0f, 0f, 12f), new Vector3(4.15f, 0.05f, 0.22f), new Color(0.15f, 0.16f, 0.22f));
                SetChildActive(parent, $"CorruptionGapLipR_{marker}", false);

                CreateVisualPart(parent, $"CorruptionGapShardA_{marker}", PrimitiveType.Cube, new Vector3(-1.6f, 0.1f, z - 0.28f), new Vector3(0f, 0f, 28f), new Vector3(0.72f, 0.035f, 0.14f), new Color(1f, 0.3f, 0.5f));
                SetChildActive(parent, $"CorruptionGapShardA_{marker}", false);
                CreateVisualPart(parent, $"CorruptionGapShardB_{marker}", PrimitiveType.Cube, new Vector3(1.85f, 0.1f, z + 0.22f), new Vector3(0f, 0f, -24f), new Vector3(0.66f, 0.035f, 0.14f), new Color(1f, 0.3f, 0.5f));
                SetChildActive(parent, $"CorruptionGapShardB_{marker}", false);
            }
        }

        private int GetIntegrityTier()
        {
            if (game == null || game.IsMenuVisible)
            {
                return 0;
            }

            float ram = game.CurrentRam;
            if (ram >= 80f)
            {
                return 0;
            }

            if (ram >= 55f)
            {
                return 1;
            }

            if (ram >= 30f)
            {
                return 2;
            }

            return 3;
        }

        private void ApplyTrackIntegrityState(int integrityTier)
        {
            for (int segment = 0; segment < activeSegments.Count; segment++)
            {
                if (activeSegments[segment] == null)
                {
                    continue;
                }

                ApplySegmentIntegrity(activeSegments[segment].transform, integrityTier, segment);
            }
        }

        private void EnsureSegmentPool()
        {
            for (int i = activeSegments.Count - 1; i >= 0; i--)
            {
                if (activeSegments[i] == null)
                {
                    activeSegments.RemoveAt(i);
                }
            }

            RebuildSegmentPoolIfNeeded();
        }

        private void RebuildSegmentPoolIfNeeded()
        {
            if (activeSegments.Count > 0)
            {
                return;
            }

            CleanupSceneSegments();

            for (int i = 0; i < visibleSegments + 1; i++)
            {
                GameObject segmentRoot = new($"TrackSegment_{i}");
                segmentRoot.transform.SetParent(transform, false);
                try
                {
                    CreateTrackVisual(segmentRoot.transform, i);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[TrackSegmentSpawner] Error creating visual for segment {i}: {ex}");
                }
                activeSegments.Add(segmentRoot);
            }
        }

        private void CleanupSceneSegments()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (!child.name.StartsWith("TrackSegment_"))
                {
                    continue;
                }

#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(child.gameObject);
                    continue;
                }
#endif
                Destroy(child.gameObject);
            }
        }

        private void ApplySegmentIntegrity(Transform segmentRoot, int integrityTier, int segmentOrder)
        {
            bool useContinuousSurface = integrityTier == 0;
            SetChildActive(segmentRoot, "DeckSurface", useContinuousSurface);

            for (int marker = 0; marker < 5; marker++)
            {
                bool showGap = integrityTier > 0 && ShouldBreakMarker(integrityTier, segmentOrder, marker);
                bool showSegmentPanel = !useContinuousSurface && !showGap;
                SetChildActive(segmentRoot, $"DeckPanel_{marker}", showSegmentPanel);
                SetChildActive(segmentRoot, $"CenterLine_{marker}", showSegmentPanel);
                SetChildActive(segmentRoot, $"PanelGlow_{marker}", showSegmentPanel);
                SetChildActive(segmentRoot, $"GapBase_{marker}", !showGap);
                SetChildActive(segmentRoot, $"CorruptionGapGlow_{marker}", showGap);
                SetChildActive(segmentRoot, $"CorruptionGapLipL_{marker}", showGap);
                SetChildActive(segmentRoot, $"CorruptionGapLipR_{marker}", showGap);
                SetChildActive(segmentRoot, $"CorruptionGapShardA_{marker}", showGap);
                SetChildActive(segmentRoot, $"CorruptionGapShardB_{marker}", showGap);
            }

        }

        private bool ShouldBreakMarker(int integrityTier, int segmentOrder, int marker)
        {
            int hash = (segmentOrder * 7 + marker * 3) % 5;
            return integrityTier switch
            {
                1 => hash == 1,
                2 => hash == 1 || hash == 3,
                3 => hash != 4,
                _ => false
            };
        }

        private void SetChildActive(Transform parent, string childName, bool isActive)
        {
            Transform child = parent.Find(childName);
            if (child != null && child.gameObject.activeSelf != isActive)
            {
                child.gameObject.SetActive(isActive);
            }
        }

        private void CreateDeckSlice(Transform parent, string name, float zCenter, float length)
        {
            GameObject sliceRoot = new(name);
            sliceRoot.transform.SetParent(parent, false);
            sliceRoot.transform.localPosition = new Vector3(0f, 0f, zCenter);

            Color hullColor = new(0.08f, 0.1f, 0.14f);
            Color deckGlowColor = new(0.11f, 0.14f, 0.2f);
            Color underColor = new(0.04f, 0.05f, 0.08f);
            Color slotColor = new(0.01f, 0.01f, 0.03f);
            Color riftCoreColor = new(0.85f, 0.95f, 1f);
            Color seamColor = new(0.21f, 0.25f, 0.34f);

            CreateVisualPart(sliceRoot.transform, "BridgeDeck", PrimitiveType.Cube, new Vector3(0f, 0.02f, 0f), Vector3.zero, new Vector3(10.2f, 0.16f, length), hullColor);
            CreateVisualPart(sliceRoot.transform, "DeckGlow", PrimitiveType.Cube, new Vector3(0f, -0.04f, 0f), Vector3.zero, new Vector3(7.4f, 0.02f, length), deckGlowColor);
            CreateVisualPart(sliceRoot.transform, "Undercarriage", PrimitiveType.Cube, new Vector3(0f, -0.38f, 0f), Vector3.zero, new Vector3(8.8f, 0.26f, length), underColor);

            for (int strip = 0; strip < 2; strip++)
            {
                float x = strip == 0 ? -2f : 2f;
                CreateVisualPart(sliceRoot.transform, $"LaneRiftSlot_{strip}", PrimitiveType.Cube, new Vector3(x, -0.02f, 0f), Vector3.zero, new Vector3(0.78f, 0.12f, length), slotColor);
                CreateVisualPart(sliceRoot.transform, $"LaneRiftCore_{strip}", PrimitiveType.Cube, new Vector3(x, 0.01f, 0f), Vector3.zero, new Vector3(0.12f, 0.03f, length), riftCoreColor);
                CreateVisualPart(sliceRoot.transform, $"LaneSeam_{strip}", PrimitiveType.Cube, new Vector3(x, 0.06f, 0f), Vector3.zero, new Vector3(0.14f, 0.02f, length), seamColor);

                Color riftAccent = strip == 0 ? new Color(0.08f, 0.95f, 1f) : new Color(1f, 0.28f, 0.78f);
                CreateGlowBox(sliceRoot.transform, $"LaneRiftGlow_{strip}", new Vector3(x, 0.04f, 0f), new Vector3(0.42f, 0.2f, length), new Color(riftAccent.r, riftAccent.g, riftAccent.b, 0.18f));
            }

            CreateBridgeSide(sliceRoot.transform, -4.8f, new Color(0.08f, 0.95f, 1f), "Left", length);
            CreateBridgeSide(sliceRoot.transform, 4.8f, new Color(1f, 0.32f, 0.76f), "Right", length);
        }

        private void CreateBridgeSide(Transform parent, float sideX, Color accent, string sideName, float length)
        {
            GameObject sideWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sideWall.name = $"{sideName}Wall";
            sideWall.transform.SetParent(parent, false);
            sideWall.transform.localPosition = new Vector3(sideX, 0.28f, 0f);
            sideWall.transform.localScale = new Vector3(0.32f, 0.55f, length);
            ApplyColor(sideWall, new Color(0.08f, 0.1f, 0.14f));
            RemoveCollider(sideWall);

            GameObject topRail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            topRail.name = $"{sideName}Rail";
            topRail.transform.SetParent(parent, false);
            topRail.transform.localPosition = new Vector3(sideX, 0.66f, 0f);
            topRail.transform.localScale = new Vector3(0.12f, 0.16f, length);
            ApplyColor(topRail, Color.Lerp(accent, Color.white, 0.15f));
            RemoveCollider(topRail);

            GameObject edgeGlow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            edgeGlow.name = $"{sideName}Glow";
            edgeGlow.transform.SetParent(parent, false);
            edgeGlow.transform.localPosition = new Vector3(sideX * 0.985f, 0.52f, 0f);
            edgeGlow.transform.localScale = new Vector3(0.06f, 0.62f, length * 1.01f);
            ApplyColor(edgeGlow, accent);
            RemoveCollider(edgeGlow);

            CreateGlowBox(parent, $"{sideName}FakeGlow", new Vector3(sideX * 0.94f, 0.54f, 0f), new Vector3(0.28f, 0.44f, length * 1.02f), new Color(accent.r, accent.g, accent.b, 0.16f));
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

        private void CreateAbyssRifts(Transform parent, int currentSegmentIndex)
        {
            int riftCount = currentSegmentIndex % 2 == 0 ? 2 : 1;
            for (int i = 0; i < riftCount; i++)
            {
                float z = -segmentLength * 0.28f + i * 11f + Random.Range(-1.2f, 1.2f);
                float x = i % 2 == 0 ? -1.4f : 1.8f;
                float y = -5.4f - Random.Range(0f, 1.1f);

                GameObject shell = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shell.name = $"AbyssRiftShell_{i}";
                shell.transform.SetParent(parent, false);
                shell.transform.localPosition = new Vector3(x, y, z);
                shell.transform.localRotation = Quaternion.Euler(0f, Random.Range(-20f, 20f), Random.Range(-8f, 8f));
                shell.transform.localScale = new Vector3(2.4f, 0.2f, 6.6f);
                ApplyColor(shell, new Color(0.02f, 0.02f, 0.05f));
                RemoveCollider(shell);

                GameObject core = GameObject.CreatePrimitive(PrimitiveType.Cube);
                core.name = $"AbyssRiftCore_{i}";
                core.transform.SetParent(parent, false);
                core.transform.localPosition = new Vector3(x, y + 0.08f, z);
                core.transform.localRotation = shell.transform.localRotation;
                core.transform.localScale = new Vector3(0.38f, 0.05f, 5.8f);
                ApplyColor(core, new Color(0.92f, 0.98f, 1f));
                RemoveCollider(core);

                Color riftColor = i % 2 == 0 ? new Color(0.08f, 0.95f, 1f) : new Color(1f, 0.28f, 0.78f);
                CreateGlowBox(parent, $"AbyssRiftGlow_{i}", new Vector3(x, y + 0.18f, z), new Vector3(2.6f, 0.45f, 6.8f), new Color(riftColor.r, riftColor.g, riftColor.b, 0.18f));

                for (int spark = 0; spark < 4; spark++)
                {
                    float sparkOffset = -2.2f + spark * 1.45f;
                    GameObject burst = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    burst.name = $"AbyssRiftBurst_{i}_{spark}";
                    burst.transform.SetParent(parent, false);
                    burst.transform.localPosition = new Vector3(x + Random.Range(-0.35f, 0.35f), y + 0.24f, z + sparkOffset);
                    burst.transform.localRotation = Quaternion.Euler(Random.Range(-20f, 20f), Random.Range(0f, 180f), Random.Range(-30f, 30f));
                    burst.transform.localScale = new Vector3(0.22f, 0.04f, 0.82f);
                    ApplyColor(burst, Color.Lerp(riftColor, Color.white, 0.25f));
                    RemoveCollider(burst);
                }
            }
        }

        private void CreateAbyssCore(Transform parent, int currentSegmentIndex)
        {
            float pulseOffset = (currentSegmentIndex % 2 == 0) ? 0.4f : -0.4f;

            GameObject voidDisc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            voidDisc.name = "AbyssCoreDisc";
            voidDisc.transform.SetParent(parent, false);
            voidDisc.transform.localPosition = new Vector3(pulseOffset, -9.6f, 0f);
            voidDisc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            voidDisc.transform.localScale = new Vector3(8.5f, 0.18f, 8.5f);
            ApplyColor(voidDisc, new Color(0.01f, 0.01f, 0.02f));
            RemoveCollider(voidDisc);

            GameObject voidInner = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            voidInner.name = "AbyssCoreInner";
            voidInner.transform.SetParent(parent, false);
            voidInner.transform.localPosition = new Vector3(pulseOffset, -9.48f, 0f);
            voidInner.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            voidInner.transform.localScale = new Vector3(5.2f, 0.08f, 5.2f);
            ApplyColor(voidInner, new Color(0f, 0f, 0f));
            RemoveCollider(voidInner);

            CreateGlowBox(parent, "AbyssCoreRing", new Vector3(pulseOffset, -9.2f, 0f), new Vector3(10.8f, 1f, 10.8f), new Color(0.08f, 0.95f, 1f, 0.12f));
            CreateGlowBox(parent, "AbyssCoreRingHot", new Vector3(pulseOffset, -9.1f, 0f), new Vector3(7.4f, 1f, 7.4f), new Color(1f, 0.3f, 0.78f, 0.08f));

            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f + (currentSegmentIndex % 2 == 0 ? 15f : -15f);
                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;

                GameObject fracture = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fracture.name = $"AbyssCoreFracture_{i}";
                fracture.transform.SetParent(parent, false);
                fracture.transform.localPosition = new Vector3(pulseOffset, -8.7f, 0f) + dir * 2.6f;
                fracture.transform.localRotation = Quaternion.Euler(0f, angle, 22f);
                fracture.transform.localScale = new Vector3(0.22f, 0.05f, 3.4f);
                ApplyColor(fracture, i % 2 == 0 ? new Color(0.08f, 0.95f, 1f) : new Color(1f, 0.3f, 0.78f));
                RemoveCollider(fracture);
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
            CreateGlowBox(archRoot.transform, "ScanArchHalo", new Vector3(0f, 5.1f, 0f), new Vector3(9.8f, 0.28f, 0.42f), new Color(0.3f, 0.85f, 1f, 0.12f));

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
            signCore.transform.localPosition = new Vector3(0f, 0f, 0f);
            signCore.transform.localScale = new Vector3(0.88f, 0.74f, 0.72f);
            ApplyColor(signCore, new Color(0.04f, 0.05f, 0.08f));
            RemoveCollider(signCore);
            
            GameObject logoCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            logoCube.name = "ArchLogo3D";
            logoCube.transform.SetParent(archRoot.transform, false);
            logoCube.transform.localPosition = new Vector3(0f, 5.64f, -0.25f);
            logoCube.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
            logoCube.transform.localScale = new Vector3(3.6f, 0.7f, 0.4f);
            ApplyCompanyLogo(logoCube);
            RemoveCollider(logoCube);

            CreateGlowBox(archRoot.transform, "ArchBillboardGlow", new Vector3(0f, 5.64f, 0f), new Vector3(4.4f, 1.1f, 0.22f), new Color(0.24f, 0.88f, 1f, 0.16f));

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
            int safeLane = Random.Range(0, 3);

            for (float z = 4f; z < segmentLength - 4f; z += 6f)
            {
                if (currentSegmentIndex < 2)
                {
                    CreateScoreTriplet(parent, safeLane, z);
                    continue;
                }

                int pattern = Random.Range(0, 100);
                if (pattern < 20)
                {
                    CreateObstacleWall(parent, z, safeLane);
                }
                else if (pattern < 42)
                {
                    CreateFuelPocket(parent, z, safeLane);
                }
                else if (pattern < 54)
                {
                    CreateGlitchPickup(parent, z, safeLane);
                }
                else
                {
                    CreateScoreTriplet(parent, safeLane, z);
                }
            }
        }

        private void CreateObstacleWall(Transform parent, float z, int safeLane)
        {
            int blockedLane = (safeLane + Random.Range(1, 3)) % 3;
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

        private void CreateFuelPocket(Transform parent, float z, int safeLane)
        {
            int fuelLane = safeLane;
            CreateEntity(parent, TrackEntityType.Ram, 24f, fuelLane, z, PrimitiveType.Cylinder, new Vector3(1.2f, 0.5f, 1.2f), new Color(0.26f, 1f, 0.45f));

            int scoreLane = (fuelLane + 1 + Random.Range(0, 2)) % 3;
            CreateScoreTriplet(parent, scoreLane, z);
        }

        private void CreateGlitchPickup(Transform parent, float z, int safeLane)
        {
            int glitchLane = safeLane;
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

                if (lane != ((safeLane + 1) % 3))
                {
                    CreateEntity(parent, TrackEntityType.Obstacle, 18f, lane, z + 0.5f, PrimitiveType.Cube, new Vector3(1.8f, 1.8f, 1.8f), new Color(1f, 0.3f, 0.3f));
                }
                else
                {
                    CreateEntity(parent, TrackEntityType.Score, 18f, lane, z + 0.4f, PrimitiveType.Sphere, new Vector3(1f, 1f, 1f), new Color(1f, 0.87f, 0.2f));
                }
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

            Collider collider = entity.GetComponent<Collider>();
            if (collider == null)
            {
                collider = entity.AddComponent<BoxCollider>();
            }
            collider.isTrigger = true;

            TrackEntity trackEntity = entity.AddComponent<TrackEntity>();
            trackEntity.Setup(type, amount, glitchDuration, glitchType);

            Renderer rootRenderer = entity.GetComponent<Renderer>();
            if (type == TrackEntityType.Score || type == TrackEntityType.Ram || type == TrackEntityType.Glitch)
            {
                if (rootRenderer != null)
                {
                    rootRenderer.enabled = false;
                }

                BuildPickupVisual(entity.transform, type, color, glitchType);
            }
            else if (type == TrackEntityType.Obstacle)
            {
                if (rootRenderer != null)
                {
                    rootRenderer.enabled = false;
                }

                BuildObstacleVisual(entity.transform, color);
            }
            else
            {
                ApplyColor(entity, color);
            }

            if (type == TrackEntityType.Score)
            {
                CreateGlowBox(entity.transform, "ScoreGlow", Vector3.zero, new Vector3(scale.x * 1.8f, scale.y * 1.8f, scale.z * 1.8f), new Color(1f, 0.84f, 0.2f, 0.12f));
            }
            else if (type == TrackEntityType.Ram)
            {
                CreateGlowBox(entity.transform, "RamGlow", Vector3.zero, new Vector3(scale.x * 1.5f, scale.y * 1.2f, scale.z * 1.5f), new Color(0.26f, 1f, 0.45f, 0.14f));
            }
            else if (type == TrackEntityType.Glitch)
            {
                CreateGlowBox(entity.transform, "GlitchGlow", Vector3.zero, new Vector3(scale.x * 1.8f, scale.y * 1.8f, scale.z * 1.8f), new Color(color.r, color.g, color.b, 0.18f));
            }
            else if (type == TrackEntityType.Obstacle)
            {
                CreateGlowBox(entity.transform, "ObstacleGlow", new Vector3(0f, 0.15f, 0f), new Vector3(scale.x * 1.4f, scale.y * 1.2f, scale.z * 1.2f), new Color(1f, 0.22f, 0.4f, 0.14f));
            }
        }

        private void BuildPickupVisual(Transform parent, TrackEntityType type, Color color, GlitchRacerGame.GlitchType glitchType)
        {
            GameObject visualRoot = new($"{type}Visual");
            visualRoot.transform.SetParent(parent, false);
            visualRoot.transform.localPosition = Vector3.zero;
            visualRoot.transform.localRotation = Quaternion.identity;
            visualRoot.transform.localScale = Vector3.one;
            visualRoot.AddComponent<SpinPulse>();

            switch (type)
            {
                case TrackEntityType.Score:
                    BuildScoreShard(visualRoot.transform, color);
                    break;
                case TrackEntityType.Ram:
                    BuildRamCell(visualRoot.transform, color);
                    break;
                case TrackEntityType.Glitch:
                    BuildGlitchArtifact(visualRoot.transform, color, glitchType);
                    break;
            }
        }

        private void BuildScoreShard(Transform parent, Color color)
        {
            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Cube);
            core.name = "ShardCore";
            core.transform.SetParent(parent, false);
            core.transform.localPosition = Vector3.zero;
            core.transform.localRotation = Quaternion.Euler(45f, 45f, 0f);
            core.transform.localScale = new Vector3(0.66f, 0.66f, 0.66f);
            ApplyColor(core, color);
            RemoveCollider(core);

            CreateVisualPart(parent, "ShardWingTop", PrimitiveType.Cube, new Vector3(0f, 0.42f, 0f), new Vector3(25f, 0f, 45f), new Vector3(0.18f, 0.52f, 0.38f), Color.Lerp(color, Color.white, 0.2f));
            CreateVisualPart(parent, "ShardWingBottom", PrimitiveType.Cube, new Vector3(0f, -0.42f, 0f), new Vector3(-25f, 0f, 45f), new Vector3(0.18f, 0.52f, 0.38f), Color.Lerp(color, Color.white, 0.1f));
            CreateVisualPart(parent, "ShardRing", PrimitiveType.Cylinder, new Vector3(0f, 0f, 0f), new Vector3(90f, 0f, 0f), new Vector3(0.68f, 0.06f, 0.68f), new Color(1f, 0.96f, 0.78f));
        }

        private void BuildRamCell(Transform parent, Color color)
        {
            CreateVisualPart(parent, "RamBody", PrimitiveType.Cube, Vector3.zero, Vector3.zero, new Vector3(0.82f, 0.92f, 0.56f), new Color(0.08f, 0.18f, 0.12f));
            CreateVisualPart(parent, "RamCore", PrimitiveType.Cube, new Vector3(0f, 0f, 0.02f), Vector3.zero, new Vector3(0.54f, 0.7f, 0.28f), color);
            CreateVisualPart(parent, "RamCapTop", PrimitiveType.Cube, new Vector3(0f, 0.56f, 0f), Vector3.zero, new Vector3(0.28f, 0.16f, 0.32f), new Color(0.82f, 0.92f, 1f));
            CreateVisualPart(parent, "RamCapBottom", PrimitiveType.Cube, new Vector3(0f, -0.56f, 0f), Vector3.zero, new Vector3(0.28f, 0.16f, 0.32f), new Color(0.82f, 0.92f, 1f));
            CreateVisualPart(parent, "RamSideL", PrimitiveType.Cube, new Vector3(-0.42f, 0f, 0f), Vector3.zero, new Vector3(0.08f, 0.72f, 0.34f), Color.Lerp(color, Color.white, 0.18f));
            CreateVisualPart(parent, "RamSideR", PrimitiveType.Cube, new Vector3(0.42f, 0f, 0f), Vector3.zero, new Vector3(0.08f, 0.72f, 0.34f), Color.Lerp(color, Color.white, 0.18f));
        }

        private void BuildGlitchArtifact(Transform parent, Color color, GlitchRacerGame.GlitchType glitchType)
        {
            CreateVisualPart(parent, "GlitchCore", PrimitiveType.Cube, Vector3.zero, new Vector3(25f, 45f, 15f), new Vector3(0.76f, 0.76f, 0.76f), color);
            CreateVisualPart(parent, "GlitchInner", PrimitiveType.Cube, new Vector3(0f, 0f, 0f), new Vector3(-15f, 25f, 55f), new Vector3(0.34f, 0.34f, 0.34f), new Color(0.96f, 0.98f, 1f));

            Color accent = glitchType switch
            {
                GlitchRacerGame.GlitchType.StaticNoise => new Color(1f, 0.96f, 0.62f),
                GlitchRacerGame.GlitchType.DrunkVision => new Color(0.5f, 1f, 0.96f),
                GlitchRacerGame.GlitchType.DrugsTrip => new Color(1f, 0.58f, 0.98f),
                _ => new Color(0.86f, 0.68f, 1f)
            };

            CreateVisualPart(parent, "GlitchSlatA", PrimitiveType.Cube, new Vector3(0f, 0.62f, 0f), new Vector3(0f, 45f, 0f), new Vector3(0.94f, 0.08f, 0.18f), accent);
            CreateVisualPart(parent, "GlitchSlatB", PrimitiveType.Cube, new Vector3(0f, -0.62f, 0f), new Vector3(0f, -45f, 0f), new Vector3(0.94f, 0.08f, 0.18f), accent);
            CreateVisualPart(parent, "GlitchSlatC", PrimitiveType.Cube, new Vector3(0.62f, 0f, 0f), new Vector3(45f, 0f, 0f), new Vector3(0.18f, 0.94f, 0.08f), accent);
        }

        private void BuildObstacleVisual(Transform parent, Color color)
        {
            Color faceColor = Color.Lerp(color, Color.white, 0.04f);
            Color edgeColor = new Color(0.54f, 0.03f, 0.12f);
            Color glowColor = new Color(1f, 0.22f, 0.36f, 0.2f);

            CreateVisualPart(parent, "ErrorShadow", PrimitiveType.Cube, new Vector3(0f, -0.34f, -0.12f), Vector3.zero, new Vector3(1.62f, 0.08f, 0.24f), new Color(0.18f, 0.03f, 0.07f));
            CreateGlowBox(parent, "ErrorWordGlow", new Vector3(0f, 0.02f, 0.06f), new Vector3(1.75f, 1.1f, 1f), glowColor);

            float[] centers = { -0.62f, -0.31f, 0f, 0.31f, 0.62f };
            float width = 0.22f;
            float height = 0.78f;
            float thickness = 0.06f;
            float depth = 0.2f;
            float y = 0.06f;

            BuildLetterE(parent, "ErrorE", centers[0], y, 0f, depth, width, height, thickness, faceColor, edgeColor);
            BuildLetterR(parent, "ErrorR1", centers[1], y, 0f, depth, width, height, thickness, faceColor, edgeColor);
            BuildLetterR(parent, "ErrorR2", centers[2], y, 0f, depth, width, height, thickness, faceColor, edgeColor);
            BuildLetterO(parent, "ErrorO", centers[3], y, 0f, depth, width, height, thickness, faceColor, edgeColor);
            BuildLetterR(parent, "ErrorR3", centers[4], y, 0f, depth, width, height, thickness, faceColor, edgeColor);
        }

        private void BuildLetterE(Transform parent, string prefix, float xCenter, float yCenter, float z, float depth, float width, float height, float thickness, Color faceColor, Color edgeColor)
        {
            CreateLetterBody(parent, $"{prefix}_Stem", new Vector3(xCenter - width * 0.5f + thickness * 0.5f, yCenter, z), new Vector3(thickness, height, depth), faceColor, edgeColor);
            CreateLetterBody(parent, $"{prefix}_Top", new Vector3(xCenter, yCenter + height * 0.5f - thickness * 0.5f, z), new Vector3(width, thickness, depth), faceColor, edgeColor);
            CreateLetterBody(parent, $"{prefix}_Mid", new Vector3(xCenter - width * 0.08f, yCenter, z), new Vector3(width * 0.82f, thickness, depth), faceColor, edgeColor);
            CreateLetterBody(parent, $"{prefix}_Bottom", new Vector3(xCenter, yCenter - height * 0.5f + thickness * 0.5f, z), new Vector3(width, thickness, depth), faceColor, edgeColor);
        }

        private void BuildLetterO(Transform parent, string prefix, float xCenter, float yCenter, float z, float depth, float width, float height, float thickness, Color faceColor, Color edgeColor)
        {
            CreateLetterBody(parent, $"{prefix}_Top", new Vector3(xCenter, yCenter + height * 0.5f - thickness * 0.5f, z), new Vector3(width, thickness, depth), faceColor, edgeColor);
            CreateLetterBody(parent, $"{prefix}_Bottom", new Vector3(xCenter, yCenter - height * 0.5f + thickness * 0.5f, z), new Vector3(width, thickness, depth), faceColor, edgeColor);
            CreateLetterBody(parent, $"{prefix}_Left", new Vector3(xCenter - width * 0.5f + thickness * 0.5f, yCenter, z), new Vector3(thickness, height, depth), faceColor, edgeColor);
            CreateLetterBody(parent, $"{prefix}_Right", new Vector3(xCenter + width * 0.5f - thickness * 0.5f, yCenter, z), new Vector3(thickness, height, depth), faceColor, edgeColor);
        }

        private void BuildLetterR(Transform parent, string prefix, float xCenter, float yCenter, float z, float depth, float width, float height, float thickness, Color faceColor, Color edgeColor)
        {
            CreateLetterBody(parent, $"{prefix}_Stem", new Vector3(xCenter - width * 0.5f + thickness * 0.5f, yCenter, z), new Vector3(thickness, height, depth), faceColor, edgeColor);
            CreateLetterBody(parent, $"{prefix}_Top", new Vector3(xCenter, yCenter + height * 0.5f - thickness * 0.5f, z), new Vector3(width, thickness, depth), faceColor, edgeColor);
            CreateLetterBody(parent, $"{prefix}_Mid", new Vector3(xCenter, yCenter, z), new Vector3(width * 0.9f, thickness, depth), faceColor, edgeColor);
            CreateLetterBody(parent, $"{prefix}_RightUpper", new Vector3(xCenter + width * 0.5f - thickness * 0.5f, yCenter + height * 0.25f, z), new Vector3(thickness, height * 0.5f, depth), faceColor, edgeColor);
            CreateVisualPart(parent, $"{prefix}_LegShadow", PrimitiveType.Cube, new Vector3(xCenter + width * 0.06f, yCenter - height * 0.28f, z - 0.04f), new Vector3(0f, 0f, -36f), new Vector3(thickness * 1.1f, height * 0.62f, depth), edgeColor);
            CreateVisualPart(parent, $"{prefix}_Leg", PrimitiveType.Cube, new Vector3(xCenter + width * 0.06f, yCenter - height * 0.28f, z + 0.04f), new Vector3(0f, 0f, -36f), new Vector3(thickness, height * 0.62f, depth), faceColor);
        }

        private void CreateLetterBody(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Color faceColor, Color edgeColor)
        {
            Vector3 shadowScale = new(localScale.x * 1.06f, localScale.y * 1.06f, localScale.z);
            CreateVisualPart(parent, $"{name}_Shadow", PrimitiveType.Cube, localPosition + new Vector3(0f, 0f, -0.05f), Vector3.zero, shadowScale, edgeColor);
            CreateVisualPart(parent, name, PrimitiveType.Cube, localPosition + new Vector3(0f, 0f, 0.04f), Vector3.zero, localScale, faceColor);
        }

        private void CreateVisualPart(Transform parent, string name, PrimitiveType primitiveType, Vector3 localPosition, Vector3 localRotationEuler, Vector3 localScale, Color color)
        {
            GameObject part = GameObject.CreatePrimitive(primitiveType);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = Quaternion.Euler(localRotationEuler);
            part.transform.localScale = localScale;
            ApplyColor(part, color);
            RemoveCollider(part);
        }

        private void CreateGlowBox(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Color color)
        {
            GameObject glow = GameObject.CreatePrimitive(PrimitiveType.Quad);
            glow.name = name;
            glow.transform.SetParent(parent, false);
            glow.transform.localPosition = localPosition;

            Vector3 quadScale;
            Quaternion quadRotation;
            bool zDominant = localScale.z > localScale.x * 1.5f && localScale.z > localScale.y * 1.5f;
            bool xDominant = localScale.x > localScale.y * 1.5f;

            if (zDominant && localScale.x < localScale.y)
            {
                quadScale = new Vector3(
                    Mathf.Max(0.1f, localScale.z),
                    Mathf.Max(0.1f, localScale.y),
                    1f);
                quadRotation = Quaternion.Euler(0f, 90f, 0f);
            }
            else if (zDominant || xDominant)
            {
                quadScale = new Vector3(
                    Mathf.Max(0.1f, localScale.x),
                    Mathf.Max(0.1f, localScale.z),
                    1f);
                quadRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            else
            {
                quadScale = new Vector3(
                    Mathf.Max(0.1f, localScale.x),
                    Mathf.Max(0.1f, localScale.y),
                    1f);
                quadRotation = Quaternion.Euler(0f, 180f, 0f);
            }

            glow.transform.localScale = quadScale;
            glow.transform.localRotation = quadRotation;
            RemoveCollider(glow);

            Renderer renderer = glow.GetComponent<Renderer>();
            Material glowMaterial = GetGlowMaterial();
            if (glowMaterial != null)
            {
                renderer.sharedMaterial = glowMaterial;
                renderer.material.color = color;
            }

            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            glow.layer = parent.gameObject.layer;
        }

        private Material GetGlowMaterial()
        {
            if (cachedGlowMaterial != null)
            {
                return cachedGlowMaterial;
            }

            if (glowTemplate != null)
            {
                cachedGlowMaterial = new Material(glowTemplate);
            }
            else
            {
                // Fallback если забыли назначить в инспекторе
                Shader shader = FindRuntimeShader(
                    "Universal Render Pipeline/Particles/Unlit",
                    "Universal Render Pipeline/Unlit",
                    "Unlit/Color",
                    "Sprites/Default");
                if (shader == null)
                {
                    Debug.LogError("TrackSegmentSpawner: glow shader not found. Add a fallback shader to Always Included Shaders.");
                    return null;
                }

                cachedGlowMaterial = new Material(shader);
            }

            cachedGlowMaterial.renderQueue = 3000;

            if (cachedGlowMaterial.HasProperty("_Surface"))
            {
                cachedGlowMaterial.SetFloat("_Surface", 1f);
            }

            if (cachedGlowMaterial.HasProperty("_Blend"))
            {
                cachedGlowMaterial.SetFloat("_Blend", 0f);
            }

            if (cachedGlowMaterial.HasProperty("_SrcBlend"))
            {
                cachedGlowMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            }

            if (cachedGlowMaterial.HasProperty("_DstBlend"))
            {
                cachedGlowMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            }

            if (cachedGlowMaterial.HasProperty("_ZWrite"))
            {
                cachedGlowMaterial.SetInt("_ZWrite", 0);
            }

            cachedGlowMaterial.renderQueue = 3000;
            Texture2D glowTex = GetOrCreateGlowTexture();
            cachedGlowMaterial.mainTexture = glowTex;

            if (cachedGlowMaterial.HasProperty("_BaseMap"))
            {
                cachedGlowMaterial.SetTexture("_BaseMap", glowTex);
            }

            if (cachedGlowMaterial.HasProperty("_MainTex"))
            {
                cachedGlowMaterial.SetTexture("_MainTex", glowTex);
            }

            return cachedGlowMaterial;
        }

        private Texture2D GetOrCreateGlowTexture()
        {
            if (generatedGlowTexture != null)
            {
                return generatedGlowTexture;
            }

            const int size = 64;
            generatedGlowTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            generatedGlowTexture.wrapMode = TextureWrapMode.Clamp;
            generatedGlowTexture.filterMode = FilterMode.Bilinear;
            generatedGlowTexture.hideFlags = HideFlags.HideAndDontSave;

            Vector2 center = new(size * 0.5f, size * 0.5f);
            float radius = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(1f - (distance / radius));
                    alpha *= alpha;
                    generatedGlowTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            generatedGlowTexture.Apply();
            return generatedGlowTexture;
        }

        // FIX: ApplyColor теперь использует пул - один Material на уникальный цвет.
        // Раньше каждый вызов делал new Material(), что при построении сегмента
        // (100+ объектов) давало 100+ аллокаций за раз -> GC pause -> jitter.
        private void ApplyColor(GameObject target, Color color)
        {
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            Material material = GetOrCreateSolidMaterial(color);
            if (material != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        private Material GetOrCreateSolidMaterial(Color color)
        {
            if (solidMaterialPool.TryGetValue(color, out Material existing))
            {
                return existing;
            }

            Material mat;
            if (solidTemplate != null)
            {
                mat = new Material(solidTemplate);
            }
            else
            {
                Shader shader = FindRuntimeShader(
                    "Universal Render Pipeline/Unlit",
                    "Unlit/Color",
                    "Sprites/Default");
                if (shader == null)
                {
                    Debug.LogError("TrackSegmentSpawner: solid shader not found. Add a fallback shader to Always Included Shaders.");
                    return null;
                }

                mat = new Material(shader);
            }
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", color);
            }

            if (mat.HasProperty("_Color"))
            {
                mat.SetColor("_Color", color);
            }

            solidMaterialPool[color] = mat;
            return mat;
        }

        private void ApplyCompanyLogo(GameObject target)
        {
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            Material mat = GetCompanyLogoMaterial();
            if (mat != null)
            {
                renderer.sharedMaterial = mat;
            }
        }

        private Material GetCompanyLogoMaterial()
        {
            if (companyLogoMaterial != null)
            {
                return companyLogoMaterial;
            }

            if (companyLogoTexture == null)
            {
                companyLogoTexture = Resources.Load<Texture2D>("logo overheat");
            }

            Shader shader = FindRuntimeShader(
                "Universal Render Pipeline/Unlit",
                "Unlit/Color",
                "Sprites/Default");

            if (shader == null)
            {
                return null;
            }

            companyLogoMaterial = new Material(shader);
            if (companyLogoTexture != null)
            {
                if (companyLogoMaterial.HasProperty("_BaseMap"))
                {
                    companyLogoMaterial.SetTexture("_BaseMap", companyLogoTexture);
                    companyLogoMaterial.SetColor("_BaseColor", Color.white);
                }
                
                if (companyLogoMaterial.HasProperty("_MainTex"))
                {
                    companyLogoMaterial.SetTexture("_MainTex", companyLogoTexture);
                    companyLogoMaterial.SetColor("_Color", Color.white);
                }
            }

            return companyLogoMaterial;
        }

        private void OnDestroy()
        {
            if (generatedGlowTexture != null)
            {
                Destroy(generatedGlowTexture);
                generatedGlowTexture = null;
            }

            if (cachedGlowMaterial != null)
            {
                Destroy(cachedGlowMaterial);
                cachedGlowMaterial = null;
            }

            if (companyLogoMaterial != null)
            {
                Destroy(companyLogoMaterial);
                companyLogoMaterial = null;
            }

            // companyLogoTexture is a project asset loaded from Resources, so we don't Destroy() it manually.

            foreach (Material material in solidMaterialPool.Values)
            {
                if (material != null)
                {
                    Destroy(material);
                }
            }

            solidMaterialPool.Clear();
        }

        private static Shader FindRuntimeShader(params string[] shaderNames)
        {
            for (int i = 0; i < shaderNames.Length; i++)
            {
                Shader shader = Shader.Find(shaderNames[i]);
                if (shader != null)
                {
                    return shader;
                }
            }

            return null;
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
