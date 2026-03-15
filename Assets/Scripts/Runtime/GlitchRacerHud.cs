using UnityEngine;
using UnityEngine.UI;
using YG;

namespace GlitchRacer
{
    public class GlitchRacerHud : MonoBehaviour
    {
        private GlitchRacerGame game;
        private GameObject canvasRoot;
        private Text scoreText;
        private Text scoreLabelText;
        private Text distanceText;
        private Text distanceLabelText;
        private Text ramText;
        private Text ramLabelText;
        private Text coinsText;
        private Text coinsLabelText;
        private Text glitchText;
        private Text brandTitleText;
        private Image ramFill;
        private Texture2D fillTexture;
        private RectTransform hudSafeAreaRoot;
        private GUIStyle labelStyle;
        private GUIStyle titleStyle;
        private GUIStyle centerStyle;
        private GUIStyle buttonStyle;
        private GUIStyle subStyle;
        private GUIStyle panelStyle;
        private GUIStyle tinyStyle;
        private GUIStyle heroStyle;
        private GUIStyle metricStyle;
        private GUIStyle upgradeTitleStyle;
        private GUIStyle upgradeBodyStyle;
        private Font runtimeUiFont;

        private float uiScale = 1f;
        private float vWidth => Screen.width / uiScale;
        private float vHeight => Screen.height / uiScale;
        private Rect lastSafeArea = new Rect(-1f, -1f, -1f, -1f);
        private Vector2Int lastScreenSize = new Vector2Int(-1, -1);

        public void Configure(GlitchRacerGame gameManager)
        {
            game = gameManager;
            BuildCanvasHud();
        }

        public void RefreshLocalization()
        {
            RefreshStaticHudTexts();
        }

        private void Awake()
        {
            fillTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            fillTexture.SetPixel(0, 0, Color.white);
            fillTexture.Apply();
            fillTexture.hideFlags = HideFlags.HideAndDontSave;
            runtimeUiFont = Resources.Load<Font>("Fonts/Roboto-Regular");
        }

        private void OnDestroy()
        {
            if (fillTexture != null)
            {
                Destroy(fillTexture);
                fillTexture = null;
            }
        }

        private void OnGUI()
        {
            if (game == null)
            {
                return;
            }

            uiScale = Mathf.Max(0.5f, Mathf.Min(Screen.width / 1280f, Screen.height / 720f));
            GUI.matrix = Matrix4x4.Scale(new Vector3(uiScale, uiScale, 1f));

            EnsureStyles();
            Rect safeRect = GetSafeAreaVirtualRect();

            DrawBackdrop();

            if (game.ControlsInverted)
            {
                GUI.color = new Color(0.8f, 0.2f, 1f, 0.18f);
                GUI.DrawTexture(new Rect(0f, 0f, vWidth, vHeight), fillTexture);
                GUI.color = Color.white;
            }

            if (game.HasDrunkVision)
            {
                DrawDrunkOverlay();
            }

            if (game.HasDrugsTrip)
            {
                DrawDrugsOverlay();
            }

            if (game.HasStaticNoise)
            {
                DrawStaticNoise();
            }

            if (game.IsRamCritical)
            {
                DrawCriticalRamOverlay();
            }

            if (game.State == GlitchRacerGame.SessionState.Playing)
            {
                DrawControlHint(safeRect);
            }

            if (game.State == GlitchRacerGame.SessionState.MainMenu)
            {
                DrawMainMenu(safeRect);
            }
            else if (game.State == GlitchRacerGame.SessionState.Shop)
            {
                DrawShop(safeRect);
            }
            else if (game.State == GlitchRacerGame.SessionState.Settings)
            {
                DrawSettings(safeRect);
            }
            else if (game.IsGameOver)
            {
                DrawGameOver(safeRect);
            }
        }

        private void BuildCanvasHud()
        {
            CleanupExistingCanvasHud();

            if (canvasRoot != null)
            {
                return;
            }

            canvasRoot = new GameObject("GlitchCanvasHud");
            canvasRoot.transform.SetParent(transform, false);

            Canvas canvas = canvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasRoot.AddComponent<GraphicRaycaster>();
            hudSafeAreaRoot = CreateSafeAreaRoot(canvasRoot.transform);
            ApplySafeAreaToCanvas();

            CreateHudCard("BrandCard", new Vector2(24f, -24f), new Vector2(340f, 70f), new Color(0.01f, 0.03f, 0.06f, 0.78f), out RectTransform brandRect);
            CreateAccent(brandRect, new Vector2(3f, -3f), new Vector2(3f, 64f), new Color(0.08f, 0.95f, 1f, 0.78f));
            CreateCanvasText(brandRect, "BrandTitle", new Vector2(22f, -8f), new Vector2(280f, 44f), GlitchRacerLocalization.GameTitle(game.CurrentLanguage), 32, TextAnchor.UpperLeft, out brandTitleText);

            CreateHudCard("ScoreCard", new Vector2(24f, -104f), new Vector2(180f, 70f), new Color(0.01f, 0.03f, 0.06f, 0.74f), out RectTransform scoreRect);
            CreateCanvasText(scoreRect, "ScoreLabel", new Vector2(14f, -8f), new Vector2(120f, 16f), GlitchRacerLocalization.ScoreLabel(game.CurrentLanguage), 16, TextAnchor.UpperLeft, out scoreLabelText);
            CreateCanvasText(scoreRect, "ScoreValue", new Vector2(14f, -26f), new Vector2(146f, 28f), "0", 30, TextAnchor.UpperLeft, out scoreText);

            CreateHudCard("DistanceCard", new Vector2(214f, -104f), new Vector2(180f, 70f), new Color(0.01f, 0.03f, 0.06f, 0.74f), out RectTransform distanceRect);
            CreateCanvasText(distanceRect, "DistanceLabel", new Vector2(14f, -8f), new Vector2(120f, 16f), GlitchRacerLocalization.DistanceLabel(game.CurrentLanguage), 16, TextAnchor.UpperLeft, out distanceLabelText);
            CreateCanvasText(distanceRect, "DistanceValue", new Vector2(14f, -26f), new Vector2(146f, 28f), "0 m", 30, TextAnchor.UpperLeft, out distanceText);

            CreateHudCard("RamCard", new Vector2(414f, -24f), new Vector2(380f, 60f), new Color(0.01f, 0.03f, 0.06f, 0.74f), out RectTransform ramRect);
            CreateCanvasText(ramRect, "RamLabel", new Vector2(14f, -6f), new Vector2(180f, 16f), GlitchRacerLocalization.RamStability(game.CurrentLanguage), 16, TextAnchor.UpperLeft, out ramLabelText);
            CreateRamBar(ramRect);

            CreateHudCard("CoinsCard", new Vector2(-24f, -24f), new Vector2(210f, 60f), new Color(0.02f, 0.04f, 0.07f, 0.8f), out RectTransform coinsRect, true);
            CreateAccent(coinsRect, new Vector2(0f, -3f), new Vector2(3f, 54f), new Color(1f, 0.8f, 0.2f, 0.82f));
            CreateCanvasText(coinsRect, "CoinsLabel", new Vector2(16f, -8f), new Vector2(120f, 16f), GlitchRacerLocalization.WalletLabel(game.CurrentLanguage), 16, TextAnchor.UpperLeft, out coinsLabelText);
            CreateCanvasText(coinsRect, "CoinsValue", new Vector2(16f, -24f), new Vector2(150f, 24f), "0", 28, TextAnchor.UpperLeft, out coinsText);

            CreateHudCard("GlitchCard", new Vector2(414f, -92f), new Vector2(380f, 40f), new Color(0.18f, 0.05f, 0.24f, 0.74f), out RectTransform glitchRect);
            glitchRect.gameObject.SetActive(false);
            CreateCanvasText(glitchRect, "GlitchValue", new Vector2(12f, -8f), new Vector2(352f, 20f), string.Empty, 16, TextAnchor.UpperLeft, out glitchText);
        }

        private void CleanupExistingCanvasHud()
        {
            canvasRoot = null;
            scoreText = null;
            distanceText = null;
            ramText = null;
            coinsText = null;
            glitchText = null;
            ramFill = null;
            scoreLabelText = null;
            distanceLabelText = null;
            ramLabelText = null;
            coinsLabelText = null;
            brandTitleText = null;
            hudSafeAreaRoot = null;
            lastSafeArea = new Rect(-1f, -1f, -1f, -1f);
            lastScreenSize = new Vector2Int(-1, -1);

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child.name != "GlitchCanvasHud")
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

        private void CreateRamBar(RectTransform parent)
        {
            GameObject bg = new GameObject("RamBarBackground", typeof(Image));
            bg.transform.SetParent(parent, false);
            Image bgImage = bg.GetComponent<Image>();
            bgImage.color = new Color(1f, 1f, 1f, 0.16f);
            RectTransform bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 1f);
            bgRect.anchorMax = new Vector2(0f, 1f);
            bgRect.pivot = new Vector2(0f, 1f);
            bgRect.anchoredPosition = new Vector2(14f, -26f);
            bgRect.sizeDelta = new Vector2(270f, 12f);

            GameObject fill = new GameObject("RamBarFill", typeof(Image));
            fill.transform.SetParent(bg.transform, false);
            ramFill = fill.GetComponent<Image>();
            ramFill.color = new Color(0.26f, 1f, 0.45f);
            RectTransform fillRect = ramFill.rectTransform;
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = new Vector2(0f, 0f);
            fillRect.offsetMax = new Vector2(0f, 0f);

            CreateCanvasText(parent, "RamValue", new Vector2(300f, -15f), new Vector2(64f, 22f), "100%", 22, TextAnchor.UpperRight, out ramText);
        }

        private void CreateHudCard(string name, Vector2 anchoredPosition, Vector2 size, Color color, out RectTransform rect, bool rightAligned = false)
        {
            GameObject card = new GameObject(name, typeof(Image));
            card.transform.SetParent(hudSafeAreaRoot != null ? hudSafeAreaRoot : canvasRoot.transform, false);
            Image image = card.GetComponent<Image>();
            image.color = color;

            rect = card.GetComponent<RectTransform>();
            if (rightAligned)
            {
                rect.anchorMin = new Vector2(1f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 1f);
            }
            else
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
            }

            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private void CreateAccent(RectTransform parent, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            GameObject accent = new GameObject("Accent", typeof(Image));
            accent.transform.SetParent(parent, false);
            Image image = accent.GetComponent<Image>();
            image.color = color;
            RectTransform rect = accent.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private void CreateCanvasText(RectTransform parent, string name, Vector2 anchoredPosition, Vector2 size, string initialText, int fontSize, TextAnchor alignment, out Text text)
        {
            GameObject textObj = new GameObject(name, typeof(Text));
            textObj.transform.SetParent(parent, false);
            text = textObj.GetComponent<Text>();
            text.font = runtimeUiFont != null
                ? runtimeUiFont
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = initialText;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            RectTransform rect = text.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private void Update()
        {
            if (game == null || canvasRoot == null)
            {
                return;
            }

            ApplySafeAreaToCanvas();

            bool showHud = game.State == GlitchRacerGame.SessionState.Playing || game.State == GlitchRacerGame.SessionState.GameOver;
            canvasRoot.SetActive(showHud);

            if (!showHud)
            {
                return;
            }

            if (scoreText != null)
            {
                scoreText.text = Mathf.RoundToInt(game.Score).ToString("N0");
            }

            if (distanceText != null)
            {
                distanceText.text = GlitchRacerLocalization.Meters(Mathf.FloorToInt(game.CurrentDistance), game.CurrentLanguage);
            }

            if (coinsText != null)
            {
                coinsText.text = game.Coins.ToString("N0");
            }

            if (ramFill != null)
            {
                float maxRam = Mathf.Max(1f, game.MaxRam);
                float ramPercentage = Mathf.Clamp01(game.CurrentRam / maxRam);
                ramFill.rectTransform.anchorMax = new Vector2(ramPercentage, 1f);
            }

            if (ramText != null)
            {
                float maxRam = Mathf.Max(1f, game.MaxRam);
                float ramPercent = Mathf.Clamp(game.CurrentRam / maxRam * 100f, 0f, 100f);
                ramText.text = $"{Mathf.CeilToInt(ramPercent)}%";
            }

            if (glitchText != null && glitchText.transform.parent != null)
            {
                bool showGlitch = game.ActiveGlitch != GlitchRacerGame.GlitchType.None;
                glitchText.transform.parent.gameObject.SetActive(showGlitch);
                if (showGlitch)
                {
                    glitchText.text = GlitchRacerLocalization.GlitchTimer(game.GlitchTimeRemaining, game.ActiveGlitchLabel, game.CurrentLanguage);
                }
            }
        }

        private void RefreshStaticHudTexts()
        {
            if (game == null)
            {
                return;
            }

            string language = game.CurrentLanguage;
            if (scoreLabelText != null) scoreLabelText.text = GlitchRacerLocalization.ScoreLabel(language);
            if (distanceLabelText != null) distanceLabelText.text = GlitchRacerLocalization.DistanceLabel(language);
            if (ramLabelText != null) ramLabelText.text = GlitchRacerLocalization.RamStability(language);
            if (coinsLabelText != null) coinsLabelText.text = GlitchRacerLocalization.WalletLabel(language);
            if (brandTitleText != null) brandTitleText.text = GlitchRacerLocalization.GameTitle(language);
        }

        private void DrawBackdrop()
        {
            if (!game.IsMenuVisible)
            {
                return;
            }

            GUI.color = new Color(0.01f, 0.01f, 0.03f, 0.5f);
            GUI.DrawTexture(new Rect(0f, 0f, vWidth, vHeight), fillTexture);
            GUI.color = new Color(0f, 0f, 0f, 0.18f);
            GUI.DrawTexture(new Rect(0f, 0f, vWidth * 0.18f, vHeight), fillTexture);
            GUI.DrawTexture(new Rect(vWidth * 0.82f, 0f, vWidth * 0.18f, vHeight), fillTexture);
            GUI.color = Color.white;
        }

        private void DrawStaticNoise()
        {
            GUI.color = new Color(1f, 1f, 1f, 0.06f);
            GUI.DrawTexture(new Rect(0f, 0f, vWidth, vHeight), fillTexture);

            int seed = Mathf.FloorToInt(Time.time * 60f);
            Random.InitState(seed);
            for (int i = 0; i < 110; i++)
            {
                float width = Random.Range(10f, 84f);
                float height = Random.Range(4f, 20f);
                float x = Random.Range(0f, vWidth - width);
                float y = Random.Range(0f, vHeight - height);
                float alpha = Random.Range(0.06f, 0.22f);

                GUI.color = new Color(Random.value, Random.value, Random.value, alpha);
                GUI.DrawTexture(new Rect(x, y, width, height), fillTexture);
            }

            for (int i = 0; i < 8; i++)
            {
                float bandY = Random.Range(0f, vHeight);
                float bandHeight = Random.Range(8f, 26f);
                GUI.color = new Color(1f, 1f, 1f, Random.Range(0.05f, 0.12f));
                GUI.DrawTexture(new Rect(0f, bandY, vWidth, bandHeight), fillTexture);
            }

            GUI.color = Color.white;
        }

        private void DrawDrunkOverlay()
        {
            float sway = Mathf.Sin(Time.time * 2.5f) * 18f;
            GUI.color = new Color(0.2f, 0.9f, 0.95f, 0.035f);
            GUI.DrawTexture(new Rect(-24f + sway, 0f, vWidth * 0.18f, vHeight), fillTexture);
            GUI.color = new Color(1f, 0.2f, 0.7f, 0.035f);
            GUI.DrawTexture(new Rect(vWidth * 0.82f - sway, 0f, vWidth * 0.2f, vHeight), fillTexture);

            for (int i = 0; i < 4; i++)
            {
                float waveY = vHeight * (0.18f + i * 0.2f) + Mathf.Sin(Time.time * (2f + i)) * 18f;
                GUI.color = new Color(1f, 1f, 1f, 0.035f);
                GUI.DrawTexture(new Rect(0f, waveY, vWidth, 6f), fillTexture);
            }

            GUI.color = Color.white;
        }

        private void DrawCriticalRamOverlay()
        {
            float pulse = (Mathf.Sin(Time.time * 12f) + 1f) * 0.5f;
            GUI.color = new Color(1f, 0f, 0.15f, 0.08f + pulse * 0.15f);
            GUI.DrawTexture(new Rect(0f, 0f, vWidth, vHeight), fillTexture);
            GUI.color = Color.white;

            if (Mathf.FloorToInt(Time.time * 8f) % 2 == 0)
            {
                GUI.color = new Color(1f, 0.15f, 0.25f, 0.95f);
                GUI.Label(new Rect(0f, vHeight * 0.28f, vWidth, 60f), GlitchRacerLocalization.CriticalInstability(game.CurrentLanguage), centerStyle);
                GUI.color = Color.white;
            }
        }

        private void DrawTouchControlZones()
        {
        }

        private void DrawControlHint(Rect safeRect)
        {
            if (RunnerPlayer.UseTouchControls)
            {
                return;
            }

            string hint = GlitchRacerLocalization.ControlHint(RunnerPlayer.UseTouchControls, game.CurrentLanguage);
            GUI.Label(new Rect(safeRect.x + 24f, safeRect.yMax - 52f, Mathf.Min(900f, safeRect.width - 48f), 30f), hint, labelStyle);
        }

        private void DrawDrugsOverlay()
        {
            float pulse = (Mathf.Sin(Time.time * 3.2f) + 1f) * 0.5f;
            float drift = Mathf.Sin(Time.time * 1.7f) * 28f;

            GUI.color = new Color(1f, 0.1f, 0.7f, 0.06f + pulse * 0.03f);
            GUI.DrawTexture(new Rect(-48f + drift, -10f, vWidth * 0.28f, vHeight + 20f), fillTexture);

            GUI.color = new Color(0.12f, 1f, 0.86f, 0.06f + (1f - pulse) * 0.03f);
            GUI.DrawTexture(new Rect(vWidth * 0.72f - drift, -10f, vWidth * 0.32f, vHeight + 20f), fillTexture);

            for (int i = 0; i < 6; i++)
            {
                float bandHeight = 6f + Mathf.Sin(Time.time * (2.5f + i * 0.3f)) * 4f;
                float y = vHeight * (0.1f + i * 0.14f) + Mathf.Cos(Time.time * (1.8f + i)) * 22f;
                GUI.color = new Color(i % 2 == 0 ? 1f : 0.2f, i % 2 == 0 ? 0.2f : 1f, 0.95f, 0.03f);
                GUI.DrawTexture(new Rect(-20f, y, vWidth + 40f, bandHeight), fillTexture);
            }

            for (int i = 0; i < 12; i++)
            {
                float size = 24f + Mathf.PingPong(Time.time * (20f + i), 36f);
                float x = Mathf.Repeat((i * 97f) + Time.time * (18f + i * 4f), vWidth + 120f) - 60f;
                float y = vHeight * (0.08f + (i % 6) * 0.14f) + Mathf.Sin(Time.time * (1.3f + i)) * 18f;
                GUI.color = new Color(i % 3 == 0 ? 1f : 0.2f, i % 3 == 1 ? 1f : 0.25f, 1f, 0.025f);
                GUI.DrawTexture(new Rect(x, y, size, size * 0.26f), fillTexture);
            }

            GUI.color = new Color(1f, 1f, 1f, 0.025f);
            GUI.DrawTexture(new Rect(0f, 0f, vWidth, vHeight), fillTexture);
            GUI.color = Color.white;
        }

        private void DrawMainMenu(Rect safeRect)
        {
            float panelWidth = Mathf.Min(520f, safeRect.width * 0.95f);
            Rect panel = new Rect(safeRect.center.x - panelWidth * 0.5f, safeRect.y + 24f, panelWidth, Mathf.Max(260f, safeRect.height - 48f));
            DrawPanelChrome(panel, new Color(0.01f, 0.02f, 0.04f, 0.88f), new Color(0.08f, 0.95f, 1f, 0.26f), new Color(1f, 0.24f, 0.72f, 0.1f));
            bool compact = panel.height < 620f;
            float chipHeight = compact ? 38f : 44f;
            float chipStep = compact ? 42f : 50f;
            float buttonHeightPrimary = compact ? 48f : 54f;
            float buttonHeightSecondary = 42f;
            float buttonGap = compact ? 10f : 12f;
            float buttonSectionHeight = buttonHeightPrimary + buttonGap + buttonHeightSecondary + buttonGap + buttonHeightSecondary;

            string language = game.CurrentLanguage;
            float contentY = panel.y + 22f;

            float taglineH = tinyStyle.CalcHeight(new GUIContent(GlitchRacerLocalization.BrandTagline(language)), panel.width - 56f);
            GUI.Label(new Rect(panel.x + 28f, contentY, panel.width - 56f, taglineH), GlitchRacerLocalization.BrandTagline(language), tinyStyle);
            contentY += taglineH + (compact ? 4f : 6f);

            float titleH = heroStyle.CalcHeight(new GUIContent(GlitchRacerLocalization.GameTitle(language)), panel.width - 56f);
            GUI.Label(new Rect(panel.x + 28f, contentY, panel.width - 56f, titleH), GlitchRacerLocalization.GameTitle(language), heroStyle);
            contentY += titleH + (compact ? 8f : 12f);

            float descH = subStyle.CalcHeight(new GUIContent(GlitchRacerLocalization.MainMenuDescription(language)), panel.width - 56f);
            GUI.Label(new Rect(panel.x + 28f, contentY, panel.width - 56f, descH), GlitchRacerLocalization.MainMenuDescription(language), subStyle);
            contentY += descH + (compact ? 14f : 24f);

            DrawStatChip(new Rect(panel.x + 28f, contentY, panel.width - 56f, chipHeight), GlitchRacerLocalization.WalletLabel(language), GlitchRacerLocalization.WalletValue(game.Coins, language));
            contentY += chipStep;
            DrawStatChip(new Rect(panel.x + 28f, contentY, panel.width - 56f, chipHeight), GlitchRacerLocalization.BestScore(language), Mathf.RoundToInt(game.BestScore).ToString("N0"));
            contentY += chipStep;
            DrawStatChip(new Rect(panel.x + 28f, contentY, panel.width - 56f, chipHeight), GlitchRacerLocalization.BestDistance(language), GlitchRacerLocalization.Meters(Mathf.RoundToInt(game.BestDistance), language));
            contentY += chipStep;
            DrawStatChip(new Rect(panel.x + 28f, contentY, panel.width - 56f, chipHeight), GlitchRacerLocalization.TotalDistance(language), GlitchRacerLocalization.Meters(Mathf.RoundToInt(game.TotalDistance), language));

            float formulaH1 = tinyStyle.CalcHeight(new GUIContent(GlitchRacerLocalization.RunPayout(language)), panel.width - 56f);
            float formulaH2 = tinyStyle.CalcHeight(new GUIContent(GlitchRacerLocalization.PayoutFormula(language)), panel.width - 56f);
            float formulaBoxH = formulaH1 + formulaH2 + 12f;

            float formulaTop = panel.yMax - formulaBoxH - 14f;
            float buttonY = formulaTop - buttonSectionHeight - (compact ? 10f : 16f);
            buttonY = Mathf.Max(contentY + (compact ? 14f : 24f), buttonY);
            bool showFormula = buttonY + buttonSectionHeight + (compact ? 10f : 16f) <= formulaTop;

            if (DrawActionButton(new Rect(panel.x + 28f, buttonY, panel.width - 56f, buttonHeightPrimary), GlitchRacerLocalization.StartRun(language), true))
            {
                game.StartGame();
            }

            if (DrawActionButton(new Rect(panel.x + 28f, buttonY + buttonHeightPrimary + buttonGap, panel.width - 56f, buttonHeightSecondary), GlitchRacerLocalization.ShopUpgrades(language)))
            {
                game.OpenShop();
            }

            if (DrawActionButton(new Rect(panel.x + 28f, buttonY + buttonHeightPrimary + buttonGap + buttonHeightSecondary + buttonGap, panel.width - 56f, buttonHeightSecondary), GlitchRacerLocalization.Settings(language)))
            {
                game.OpenSettings();
            }

            if (showFormula)
            {
                Rect formula = new Rect(panel.x + 28f, formulaTop, panel.width - 56f, formulaBoxH);
                DrawSoftCard(formula, new Color(1f, 1f, 1f, 0.04f));
                GUI.Label(new Rect(formula.x + 14f, formula.y + 6f, formula.width - 28f, formulaH1), GlitchRacerLocalization.RunPayout(language), tinyStyle);
                GUI.Label(new Rect(formula.x + 14f, formula.y + 6f + formulaH1 + 2f, formula.width - 28f, formulaH2), GlitchRacerLocalization.PayoutFormula(language), tinyStyle);
            }
        }

        private void DrawShop(Rect safeRect)
        {
            float shopWidth = Mathf.Min(560f, safeRect.width * 0.95f);
            float shopHeight = Mathf.Min(620f, safeRect.height * 0.84f);
            Rect panel = new Rect(safeRect.center.x - shopWidth * 0.5f, safeRect.center.y - shopHeight * 0.5f, shopWidth, shopHeight);
            string language = game.CurrentLanguage;
            DrawPanel(panel, GlitchRacerLocalization.ShopTitle(language));

            float contentY = panel.y + 76f;

            contentY += DrawDynamicUpgradeCard(new Rect(panel.x + 12f, contentY, panel.width - 24f, 0f),
                GlitchRacerLocalization.FuelUpgradeTitle(game.FuelUpgradeLevel, language),
                GlitchRacerLocalization.FuelUpgradeBody(game.FuelDrainMultiplier, language),
                GlitchRacerLocalization.Buy(game.FuelUpgradeCost, language),
                out Rect fuelButtonRect);
            if (GUI.Button(fuelButtonRect, GUIContent.none, GUIStyle.none)) game.TryBuyFuelUpgrade();

            contentY += 12f;

            contentY += DrawDynamicUpgradeCard(new Rect(panel.x + 12f, contentY, panel.width - 24f, 0f),
                GlitchRacerLocalization.ScoreUpgradeTitle(game.ScoreUpgradeLevel, language),
                GlitchRacerLocalization.ScoreUpgradeBody(game.ScoreMultiplier, language),
                GlitchRacerLocalization.Buy(game.ScoreUpgradeCost, language),
                out Rect scoreButtonRect);
            if (GUI.Button(scoreButtonRect, GUIContent.none, GUIStyle.none)) game.TryBuyScoreUpgrade();

            if (DrawActionButton(new Rect(panel.x + 20f, panel.yMax - 56f, panel.width - 40f, 40f), GlitchRacerLocalization.Back(language)))
            {
                game.CloseOverlayToMenu();
            }
        }

        private void DrawSettings(Rect safeRect)
        {
            string language = game.CurrentLanguage;
            float panelWidth = Mathf.Min(480f, safeRect.width * 0.95f);
            float panelHeight = Mathf.Min(400f, safeRect.height * 0.8f);
            Rect panel = new Rect(safeRect.center.x - panelWidth * 0.5f, safeRect.center.y - panelHeight * 0.5f, panelWidth, panelHeight);
            DrawPanel(panel, GlitchRacerLocalization.Settings(language));

            float contentY = panel.y + 78f;

            if (DrawActionButton(new Rect(panel.x + 24f, contentY, panel.width - 48f, 48f), GlitchRacerLocalization.MusicButton(game.MusicEnabled, language))) game.ToggleMusic();
            contentY += 58f;

            if (DrawActionButton(new Rect(panel.x + 24f, contentY, panel.width - 48f, 48f), GlitchRacerLocalization.SfxButton(game.SfxEnabled, language))) game.ToggleSfx();
            contentY += 58f;

            if (DrawActionButton(new Rect(panel.x + 24f, contentY, panel.width - 48f, 48f), GlitchRacerLocalization.LanguageButton(game.CurrentLanguage, language))) game.ToggleLanguage();
            contentY += 58f;

            float descH = subStyle.CalcHeight(new GUIContent(GlitchRacerLocalization.ProgressSaved(language)), panel.width - 48f);
            GUI.Label(new Rect(panel.x + 24f, contentY, panel.width - 48f, descH), GlitchRacerLocalization.ProgressSaved(language), subStyle);

            if (DrawActionButton(new Rect(panel.x + 24f, panel.yMax - 58f, panel.width - 48f, 40f), GlitchRacerLocalization.Back(language)))
            {
                game.CloseOverlayToMenu();
            }
        }

        private void DrawGameOver(Rect safeRect)
        {
            string language = game.CurrentLanguage;
            float panelWidth = Mathf.Min(520f, safeRect.width * 0.95f);
            float panelHeight = Mathf.Min(400f, safeRect.height * 0.9f);
            Rect panel = new Rect(safeRect.center.x - panelWidth * 0.5f, safeRect.center.y - panelHeight * 0.5f, panelWidth, panelHeight);
            DrawPanel(panel, GlitchRacerLocalization.SystemFailure(language));

            float contentY = panel.y + 76f;
            float lh = 30f; // line height
            
            GUI.Label(new Rect(panel.x + 24f, contentY, panel.width - 48f, lh), GlitchRacerLocalization.ScoreLine(Mathf.RoundToInt(game.Score), language), labelStyle);
            contentY += lh;
            GUI.Label(new Rect(panel.x + 24f, contentY, panel.width - 48f, lh), GlitchRacerLocalization.DistanceLine(Mathf.RoundToInt(game.CurrentDistance), language), labelStyle);
            contentY += lh;
            GUI.Label(new Rect(panel.x + 24f, contentY, panel.width - 48f, lh), GlitchRacerLocalization.DataShardsLine(game.CollectedDataShards, language), labelStyle);
            contentY += lh;
            GUI.Label(new Rect(panel.x + 24f, contentY, panel.width - 48f, lh), GlitchRacerLocalization.CoinsEarnedLine(game.LastRunCoinsReward, language), labelStyle);
            contentY += lh + 8f;

            float descH = subStyle.CalcHeight(new GUIContent(GlitchRacerLocalization.LeaderboardMetric(language)), panel.width - 48f);
            GUI.Label(new Rect(panel.x + 24f, contentY, panel.width - 48f, descH), GlitchRacerLocalization.LeaderboardMetric(language), subStyle);

            float buttonY = panel.y + panel.height - 108f;

            if (!game.HasUsedRevive)
            {
                if (DrawActionButton(new Rect(panel.x + 24f, buttonY, panel.width - 48f, 42f), GlitchRacerLocalization.ReviveWatchAd(language), true))
                {
#if RewardedAdv_yg
                    YG2.RewardedAdvShow("Revive");
#endif
                }
            }

            if (DrawActionButton(new Rect(panel.x + 24f, buttonY + 50f, (panel.width - 60f) * 0.5f, 42f), GlitchRacerLocalization.RunAgain(language)))
            {
                game.StartGame();
            }

            if (DrawActionButton(new Rect(panel.center.x + 6f, buttonY + 50f, (panel.width - 60f) * 0.5f, 42f), GlitchRacerLocalization.MainMenu(language)))
            {
                game.EnterMainMenu();
            }
        }

        private void DrawPanel(Rect panel, string title)
        {
            DrawPanelChrome(panel, new Color(0.01f, 0.02f, 0.04f, 0.97f), new Color(0.08f, 0.95f, 1f, 0.42f), new Color(1f, 0.24f, 0.72f, 0.16f));
            GUI.Label(new Rect(panel.x + 24f, panel.y + 22f, panel.width - 48f, 40f), title, centerStyle);
        }

        private void DrawPanelChrome(Rect rect, Color bg, Color leftAccent, Color rightAccent)
        {
            GUI.color = bg;
            GUI.Box(rect, GUIContent.none, panelStyle);
            GUI.color = leftAccent;
            GUI.DrawTexture(new Rect(rect.x, rect.y, 3f, rect.height), fillTexture);
            GUI.color = rightAccent;
            GUI.DrawTexture(new Rect(rect.xMax - 2f, rect.y, 2f, rect.height), fillTexture);
            GUI.color = new Color(1f, 1f, 1f, 0.04f);
            GUI.DrawTexture(new Rect(rect.x + 14f, rect.y + 14f, rect.width - 28f, rect.height - 28f), fillTexture);
            GUI.color = Color.white;
        }

        private void DrawSoftCard(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, fillTexture);
            GUI.color = new Color(1f, 1f, 1f, 0.08f);
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1f), fillTexture);
            GUI.color = Color.white;
        }

        private void DrawStatChip(Rect rect, string label, string value)
        {
            DrawSoftCard(rect, new Color(1f, 1f, 1f, 0.045f));
            GUI.Label(new Rect(rect.x + 14f, rect.y + 4f, rect.width - 28f, 16f), label, tinyStyle);
            GUI.Label(new Rect(rect.x + 14f, rect.y + 18f, rect.width - 28f, rect.height - 18f), value, upgradeTitleStyle);
        }

        private bool DrawActionButton(Rect rect, string text, bool primary = false)
        {
            bool hovered = rect.Contains(Event.current.mousePosition);
            Color bg = primary
                ? (hovered ? new Color(0.08f, 0.72f, 0.9f, 0.92f) : new Color(0.05f, 0.56f, 0.74f, 0.88f))
                : (hovered ? new Color(1f, 1f, 1f, 0.16f) : new Color(1f, 1f, 1f, 0.08f));
            Color accent = primary ? new Color(1f, 1f, 1f, 0.22f) : new Color(0.08f, 0.95f, 1f, 0.35f);

            GUI.color = bg;
            GUI.DrawTexture(rect, fillTexture);
            GUI.color = accent;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 2f), fillTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), fillTexture);
            GUI.color = Color.white;
            GUI.Label(rect, text, centerStyle);
            return GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }

        private float DrawDynamicUpgradeCard(Rect rect, string title, string description, string priceText, out Rect buttonRect)
        {
            float paddingX = 14f;
            float buttonW = Mathf.Min(152f, rect.width * 0.45f);
            float textWidth = rect.width - buttonW - paddingX * 2.5f;

            float titleH = upgradeTitleStyle.CalcHeight(new GUIContent(title), textWidth);
            float bodyH = upgradeBodyStyle.CalcHeight(new GUIContent(description), textWidth);
            float totalHeight = Mathf.Max(95f, 14f + titleH + 6f + bodyH + 16f);
            rect.height = totalHeight;

            GUI.color = new Color(0.08f, 0.1f, 0.14f, 0.88f);
            GUI.DrawTexture(rect, fillTexture);
            GUI.color = new Color(1f, 1f, 1f, 0.06f);
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1f), fillTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), fillTexture);
            GUI.color = Color.white;

            GUI.Label(new Rect(rect.x + paddingX, rect.y + 14f, textWidth, titleH), title, upgradeTitleStyle);
            GUI.Label(new Rect(rect.x + paddingX, rect.y + 14f + titleH + 6f, textWidth, bodyH), description, upgradeBodyStyle);
            
            buttonRect = new Rect(rect.x + rect.width - buttonW - paddingX, rect.y + totalHeight * 0.5f - 21f, buttonW, 42f);
            bool hovered = buttonRect.Contains(Event.current.mousePosition);
            GUI.color = hovered ? new Color(1f, 0.8f, 0.2f, 0.92f) : new Color(0.9f, 0.68f, 0.12f, 0.82f);
            GUI.DrawTexture(buttonRect, fillTexture);
            GUI.color = new Color(0f, 0f, 0f, 0.22f);
            GUI.DrawTexture(new Rect(buttonRect.x, buttonRect.yMax - 3f, buttonRect.width, 3f), fillTexture);
            GUI.color = Color.white;

            Rect coinRect = new Rect(buttonRect.x + 10f, buttonRect.y + 10f, 18f, 18f);
            GUI.color = new Color(1f, 0.95f, 0.65f, 0.95f);
            GUI.DrawTexture(coinRect, fillTexture);
            GUI.color = new Color(0.72f, 0.52f, 0.06f, 0.9f);
            GUI.DrawTexture(new Rect(coinRect.x + 3f, coinRect.y + 3f, 12f, 12f), fillTexture);
            GUI.color = Color.white;

            GUI.Label(new Rect(buttonRect.x + 32f, buttonRect.y + 7f, buttonRect.width - 34f, 26f), priceText, upgradeTitleStyle);

            return totalHeight;
        }

        private void EnsureStyles()
        {
            if (labelStyle != null)
            {
                return;
            }

            Font customFont = Resources.Load<Font>("Fonts/Roboto-Regular");

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                font = customFont
            };
            labelStyle.normal.textColor = Color.white;

            titleStyle = new GUIStyle(labelStyle)
            {
                fontSize = 34
            };

            heroStyle = new GUIStyle(labelStyle)
            {
                fontSize = 30
            };

            centerStyle = new GUIStyle(labelStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 24
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold
            };
            buttonStyle.normal.background = null;
            buttonStyle.hover.background = null;
            buttonStyle.active.background = null;
            buttonStyle.normal.textColor = Color.white;

            subStyle = new GUIStyle(labelStyle)
            {
                fontSize = 15,
                wordWrap = true
            };
            subStyle.normal.textColor = new Color(0.82f, 0.88f, 0.94f);

            tinyStyle = new GUIStyle(labelStyle)
            {
                fontSize = 10
            };
            tinyStyle.normal.textColor = new Color(0.56f, 0.76f, 0.88f);

            metricStyle = new GUIStyle(labelStyle)
            {
                fontSize = 17
            };

            upgradeTitleStyle = new GUIStyle(labelStyle)
            {
                fontSize = 14
            };

            upgradeBodyStyle = new GUIStyle(subStyle)
            {
                fontSize = 11
            };

            panelStyle = new GUIStyle(GUI.skin.box);
            panelStyle.normal.background = null;
            panelStyle.border = new RectOffset(0, 0, 0, 0);
        }

        private RectTransform CreateSafeAreaRoot(Transform parent)
        {
            GameObject root = new GameObject("HudSafeArea");
            root.transform.SetParent(parent, false);
            RectTransform rect = root.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private void ApplySafeAreaToCanvas()
        {
            if (hudSafeAreaRoot == null)
            {
                return;
            }

            Rect safeArea = Screen.safeArea;
            Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
            if (safeArea == lastSafeArea && screenSize == lastScreenSize)
            {
                return;
            }

            lastSafeArea = safeArea;
            lastScreenSize = screenSize;

            float width = Mathf.Max(1f, Screen.width);
            float height = Mathf.Max(1f, Screen.height);
            Vector2 anchorMin = new Vector2(safeArea.xMin / width, safeArea.yMin / height);
            Vector2 anchorMax = new Vector2(safeArea.xMax / width, safeArea.yMax / height);

            hudSafeAreaRoot.anchorMin = anchorMin;
            hudSafeAreaRoot.anchorMax = anchorMax;
            hudSafeAreaRoot.offsetMin = Vector2.zero;
            hudSafeAreaRoot.offsetMax = Vector2.zero;
        }

        private Rect GetSafeAreaVirtualRect()
        {
            Rect safeArea = Screen.safeArea;
            return new Rect(
                safeArea.x / uiScale,
                safeArea.y / uiScale,
                safeArea.width / uiScale,
                safeArea.height / uiScale);
        }
    }
}
