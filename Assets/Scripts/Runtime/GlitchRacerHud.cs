using UnityEngine;

namespace GlitchRacer
{
    public class GlitchRacerHud : MonoBehaviour
    {
        private GlitchRacerGame game;
        private Texture2D fillTexture;
        private GUIStyle labelStyle;
        private GUIStyle titleStyle;
        private GUIStyle centerStyle;
        private GUIStyle buttonStyle;
        private GUIStyle subStyle;
        private GUIStyle panelStyle;
        private GUIStyle tinyStyle;
        private GUIStyle heroStyle;
        private GUIStyle metricStyle;

        public void Configure(GlitchRacerGame gameManager)
        {
            game = gameManager;
        }

        private void Awake()
        {
            fillTexture = new Texture2D(1, 1);
            fillTexture.SetPixel(0, 0, Color.white);
            fillTexture.Apply();
        }

        private void OnGUI()
        {
            if (game == null)
            {
                return;
            }

            EnsureStyles();

            DrawBackdrop();

            if (game.State == GlitchRacerGame.SessionState.Playing || game.State == GlitchRacerGame.SessionState.GameOver)
            {
                DrawTopBar();
            }

            if (game.ControlsInverted)
            {
                GUI.color = new Color(0.8f, 0.2f, 1f, 0.18f);
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), fillTexture);
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

            if (game.ActiveGlitch != GlitchRacerGame.GlitchType.None)
            {
                GUI.Label(new Rect(24f, 104f, 700f, 30f), $"GLITCH ACTIVE {game.GlitchTimeRemaining:0.0}s  |  {game.ActiveGlitchLabel}", labelStyle);
            }

            if (game.State == GlitchRacerGame.SessionState.Playing)
            {
                GUI.Label(new Rect(24f, Screen.height - 52f, 900f, 30f), "A/D or Left/Right. Tap left/right side of the screen on mobile.", labelStyle);
            }

            if (game.State == GlitchRacerGame.SessionState.MainMenu)
            {
                DrawMainMenu();
            }
            else if (game.State == GlitchRacerGame.SessionState.Shop)
            {
                DrawShop();
            }
            else if (game.State == GlitchRacerGame.SessionState.Settings)
            {
                DrawSettings();
            }
            else if (game.IsGameOver)
            {
                DrawGameOver();
            }
        }

        private void DrawTopBar()
        {
            Rect header = new Rect(20f, 16f, Mathf.Min(720f, Screen.width - 40f), 128f);
            DrawPanelChrome(header, new Color(0.01f, 0.03f, 0.06f, 0.84f), new Color(0.08f, 0.95f, 1f, 0.5f), new Color(1f, 0.24f, 0.72f, 0.25f));

            GUI.Label(new Rect(header.x + 22f, header.y + 14f, 360f, 44f), "GLITCH RACER", heroStyle);
            GUI.Label(new Rect(header.x + 24f, header.y + 58f, 160f, 22f), "SCORE", tinyStyle);
            GUI.Label(new Rect(header.x + 24f, header.y + 76f, 180f, 30f), Mathf.RoundToInt(game.Score).ToString("N0"), metricStyle);
            GUI.Label(new Rect(header.x + 188f, header.y + 58f, 160f, 22f), "DISTANCE", tinyStyle);
            GUI.Label(new Rect(header.x + 188f, header.y + 76f, 180f, 30f), $"{Mathf.FloorToInt(game.CurrentDistance)} m", metricStyle);

            Rect ramPanel = new Rect(header.x + 348f, header.y + 56f, 238f, 46f);
            DrawSoftCard(ramPanel, new Color(1f, 1f, 1f, 0.06f));
            GUI.Label(new Rect(ramPanel.x + 14f, ramPanel.y + 4f, 120f, 18f), "RAM STABILITY", tinyStyle);
            Rect barRect = new Rect(ramPanel.x + 14f, ramPanel.y + 22f, 170f, 12f);
            GUI.color = new Color(1f, 1f, 1f, 0.16f);
            GUI.DrawTexture(barRect, fillTexture);
            GUI.color = new Color(0.26f, 1f, 0.45f);
            GUI.DrawTexture(new Rect(barRect.x, barRect.y, barRect.width * Mathf.Clamp01(game.CurrentRam / 100f), barRect.height), fillTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(ramPanel.x + 192f, ramPanel.y + 16f, 80f, 20f), $"{Mathf.CeilToInt(game.CurrentRam)}%", labelStyle);

            Rect coinsCard = new Rect(Screen.width - 220f, 20f, 192f, 56f);
            DrawPanelChrome(coinsCard, new Color(0.02f, 0.04f, 0.07f, 0.86f), new Color(1f, 0.8f, 0.2f, 0.45f), new Color(1f, 1f, 1f, 0.06f));
            GUI.Label(new Rect(coinsCard.x + 16f, coinsCard.y + 8f, 120f, 16f), "WALLET", tinyStyle);
            GUI.Label(new Rect(coinsCard.x + 16f, coinsCard.y + 24f, 156f, 26f), $"{game.Coins:N0}", metricStyle);
        }

        private void DrawBackdrop()
        {
            if (!game.IsMenuVisible)
            {
                return;
            }

            GUI.color = new Color(0.01f, 0.01f, 0.03f, 0.6f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), fillTexture);
            GUI.color = new Color(0.05f, 0.55f, 0.7f, 0.05f);
            GUI.DrawTexture(new Rect(-120f, 0f, Screen.width * 0.36f, Screen.height), fillTexture);
            GUI.color = new Color(1f, 0.22f, 0.7f, 0.05f);
            GUI.DrawTexture(new Rect(Screen.width * 0.72f, 0f, Screen.width * 0.34f, Screen.height), fillTexture);
            GUI.color = Color.white;
        }

        private void DrawStaticNoise()
        {
            GUI.color = new Color(1f, 1f, 1f, 0.06f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), fillTexture);

            int seed = Mathf.FloorToInt(Time.time * 60f);
            Random.InitState(seed);
            for (int i = 0; i < 110; i++)
            {
                float width = Random.Range(10f, 84f);
                float height = Random.Range(4f, 20f);
                float x = Random.Range(0f, Screen.width - width);
                float y = Random.Range(0f, Screen.height - height);
                float alpha = Random.Range(0.06f, 0.22f);

                GUI.color = new Color(Random.value, Random.value, Random.value, alpha);
                GUI.DrawTexture(new Rect(x, y, width, height), fillTexture);
            }

            for (int i = 0; i < 8; i++)
            {
                float bandY = Random.Range(0f, Screen.height);
                float bandHeight = Random.Range(8f, 26f);
                GUI.color = new Color(1f, 1f, 1f, Random.Range(0.05f, 0.12f));
                GUI.DrawTexture(new Rect(0f, bandY, Screen.width, bandHeight), fillTexture);
            }

            GUI.color = Color.white;
        }

        private void DrawDrunkOverlay()
        {
            float sway = Mathf.Sin(Time.time * 2.5f) * 24f;
            GUI.color = new Color(0.2f, 0.9f, 0.95f, 0.08f);
            GUI.DrawTexture(new Rect(-60f + sway, 0f, Screen.width * 0.4f, Screen.height), fillTexture);
            GUI.color = new Color(1f, 0.2f, 0.7f, 0.08f);
            GUI.DrawTexture(new Rect(Screen.width * 0.62f - sway, 0f, Screen.width * 0.42f, Screen.height), fillTexture);

            for (int i = 0; i < 4; i++)
            {
                float waveY = Screen.height * (0.18f + i * 0.2f) + Mathf.Sin(Time.time * (2f + i)) * 18f;
                GUI.color = new Color(1f, 1f, 1f, 0.05f);
                GUI.DrawTexture(new Rect(0f, waveY, Screen.width, 10f), fillTexture);
            }

            GUI.color = Color.white;
        }

        private void DrawDrugsOverlay()
        {
            float pulse = (Mathf.Sin(Time.time * 3.2f) + 1f) * 0.5f;
            float drift = Mathf.Sin(Time.time * 1.7f) * 42f;

            GUI.color = new Color(1f, 0.1f, 0.7f, 0.11f + pulse * 0.06f);
            GUI.DrawTexture(new Rect(-90f + drift, -10f, Screen.width * 0.52f, Screen.height + 20f), fillTexture);

            GUI.color = new Color(0.12f, 1f, 0.86f, 0.1f + (1f - pulse) * 0.07f);
            GUI.DrawTexture(new Rect(Screen.width * 0.52f - drift, -10f, Screen.width * 0.56f, Screen.height + 20f), fillTexture);

            for (int i = 0; i < 6; i++)
            {
                float bandHeight = 10f + Mathf.Sin(Time.time * (2.5f + i * 0.3f)) * 6f;
                float y = Screen.height * (0.1f + i * 0.14f) + Mathf.Cos(Time.time * (1.8f + i)) * 22f;
                GUI.color = new Color(i % 2 == 0 ? 1f : 0.2f, i % 2 == 0 ? 0.2f : 1f, 0.95f, 0.06f);
                GUI.DrawTexture(new Rect(-20f, y, Screen.width + 40f, bandHeight), fillTexture);
            }

            for (int i = 0; i < 12; i++)
            {
                float size = 24f + Mathf.PingPong(Time.time * (20f + i), 36f);
                float x = Mathf.Repeat((i * 97f) + Time.time * (18f + i * 4f), Screen.width + 120f) - 60f;
                float y = Screen.height * (0.08f + (i % 6) * 0.14f) + Mathf.Sin(Time.time * (1.3f + i)) * 18f;
                GUI.color = new Color(i % 3 == 0 ? 1f : 0.2f, i % 3 == 1 ? 1f : 0.25f, 1f, 0.05f);
                GUI.DrawTexture(new Rect(x, y, size, size * 0.26f), fillTexture);
            }

            GUI.color = new Color(1f, 1f, 1f, 0.05f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), fillTexture);
            GUI.color = Color.white;
        }

        private void DrawMainMenu()
        {
            Rect panel = new Rect(42f, 42f, Mathf.Min(560f, Screen.width * 0.44f), Screen.height - 84f);
            DrawPanelChrome(panel, new Color(0.01f, 0.02f, 0.04f, 0.92f), new Color(0.08f, 0.95f, 1f, 0.45f), new Color(1f, 0.24f, 0.72f, 0.2f));
            float buttonY = Mathf.Min(panel.y + 390f, panel.yMax - 188f);

            GUI.Label(new Rect(panel.x + 28f, panel.y + 22f, panel.width - 56f, 20f), "BROKEN DATA ABYSS // VIRUS RUNNER", tinyStyle);
            GUI.Label(new Rect(panel.x + 28f, panel.y + 38f, panel.width - 56f, 50f), "GLITCH RACER", heroStyle);
            GUI.Label(new Rect(panel.x + 28f, panel.y + 92f, panel.width - 56f, 78f), "Virus-car dives through a broken data abyss. Stack score, survive memory collapse, and convert the run into hard coins for permanent upgrades.", subStyle);

            DrawStatChip(new Rect(panel.x + 28f, panel.y + 180f, panel.width - 56f, 42f), "WALLET", $"{game.Coins:N0} coins");
            DrawStatChip(new Rect(panel.x + 28f, panel.y + 228f, panel.width - 56f, 42f), "BEST SCORE", Mathf.RoundToInt(game.BestScore).ToString("N0"));
            DrawStatChip(new Rect(panel.x + 28f, panel.y + 276f, panel.width - 56f, 42f), "BEST DISTANCE", $"{Mathf.RoundToInt(game.BestDistance):N0} m");
            DrawStatChip(new Rect(panel.x + 28f, panel.y + 324f, panel.width - 56f, 42f), "TOTAL DISTANCE", $"{Mathf.RoundToInt(game.TotalDistance):N0} m");

            if (DrawActionButton(new Rect(panel.x + 28f, buttonY, panel.width - 56f, 50f), "Start Run", true))
            {
                game.StartGame();
            }

            if (DrawActionButton(new Rect(panel.x + 28f, buttonY + 58f, panel.width - 56f, 46f), "Shop / Upgrades"))
            {
                game.OpenShop();
            }

            if (DrawActionButton(new Rect(panel.x + 28f, buttonY + 112f, panel.width - 56f, 46f), "Settings"))
            {
                game.OpenSettings();
            }

            Rect formula = new Rect(panel.x + 28f, panel.yMax - 86f, panel.width - 56f, 54f);
            DrawSoftCard(formula, new Color(1f, 1f, 1f, 0.05f));
            GUI.Label(new Rect(formula.x + 14f, formula.y + 8f, formula.width - 28f, 16f), "RUN PAYOUT", tinyStyle);
            GUI.Label(new Rect(formula.x + 14f, formula.y + 22f, formula.width - 28f, 24f), "3.5 x shards + 0.03 x score + 0.12 x meters", subStyle);
        }

        private void DrawShop()
        {
            Rect panel = new Rect(Screen.width * 0.5f - 280f, 88f, 560f, 380f);
            DrawPanel(panel, "Shop");

            DrawUpgradeCard(
                new Rect(panel.x + 20f, panel.y + 72f, panel.width - 40f, 104f),
                $"Fuel Efficiency Lv.{game.FuelUpgradeLevel}",
                $"Reduces RAM drain by 8% per level.\nCurrent drain multiplier: x{game.FuelDrainMultiplier:0.00}",
                $"Buy {game.FuelUpgradeCost}",
                out Rect fuelButtonRect);
            if (GUI.Button(fuelButtonRect, GUIContent.none, GUIStyle.none))
            {
                game.TryBuyFuelUpgrade();
            }

            DrawUpgradeCard(
                new Rect(panel.x + 20f, panel.y + 190f, panel.width - 40f, 104f),
                $"Score Booster Lv.{game.ScoreUpgradeLevel}",
                $"Boosts all score gains by 12% per level.\nCurrent score multiplier: x{game.ScoreMultiplier:0.00}",
                $"Buy {game.ScoreUpgradeCost}",
                out Rect scoreButtonRect);
            if (GUI.Button(scoreButtonRect, GUIContent.none, GUIStyle.none))
            {
                game.TryBuyScoreUpgrade();
            }

            if (DrawActionButton(new Rect(panel.x + 20f, panel.y + panel.height - 62f, panel.width - 40f, 42f), "Back"))
            {
                game.CloseOverlayToMenu();
            }
        }

        private void DrawSettings()
        {
            Rect panel = new Rect(Screen.width * 0.5f - 240f, 110f, 480f, 300f);
            DrawPanel(panel, "Settings");

            if (DrawActionButton(new Rect(panel.x + 24f, panel.y + 78f, panel.width - 48f, 48f), $"Music: {(game.MusicEnabled ? "On" : "Off")}"))
            {
                game.ToggleMusic();
            }

            if (DrawActionButton(new Rect(panel.x + 24f, panel.y + 140f, panel.width - 48f, 48f), $"SFX: {(game.SfxEnabled ? "On" : "Off")}"))
            {
                game.ToggleSfx();
            }

            GUI.Label(new Rect(panel.x + 24f, panel.y + 204f, panel.width - 48f, 42f), "Progress is saved automatically on every run end and purchase.", subStyle);

            if (DrawActionButton(new Rect(panel.x + 24f, panel.y + panel.height - 58f, panel.width - 48f, 40f), "Back"))
            {
                game.CloseOverlayToMenu();
            }
        }

        private void DrawGameOver()
        {
            Rect panel = new Rect(Screen.width * 0.5f - 260f, Screen.height * 0.5f - 170f, 520f, 340f);
            DrawPanel(panel, "System Failure");

            GUI.Label(new Rect(panel.x + 24f, panel.y + 76f, panel.width - 48f, 30f), $"Score: {Mathf.RoundToInt(game.Score):N0}", labelStyle);
            GUI.Label(new Rect(panel.x + 24f, panel.y + 108f, panel.width - 48f, 30f), $"Distance: {Mathf.RoundToInt(game.CurrentDistance):N0} m", labelStyle);
            GUI.Label(new Rect(panel.x + 24f, panel.y + 140f, panel.width - 48f, 30f), $"Data shards: {game.CollectedDataShards:N0}", labelStyle);
            GUI.Label(new Rect(panel.x + 24f, panel.y + 172f, panel.width - 48f, 30f), $"Coins earned: +{game.LastRunCoinsReward:N0}", labelStyle);
            GUI.Label(new Rect(panel.x + 24f, panel.y + 214f, panel.width - 48f, 54f), "Leaderboard metric: run distance in meters. Use this when sending results to Yandex leaderboards.", subStyle);

            if (DrawActionButton(new Rect(panel.x + 24f, panel.y + panel.height - 64f, (panel.width - 60f) * 0.5f, 42f), "Run Again", true))
            {
                game.StartGame();
            }

            if (DrawActionButton(new Rect(panel.center.x + 6f, panel.y + panel.height - 64f, (panel.width - 60f) * 0.5f, 42f), "Main Menu"))
            {
                game.EnterMainMenu();
            }
        }

        private void DrawPanel(Rect panel, string title)
        {
            DrawPanelChrome(panel, new Color(0.01f, 0.02f, 0.04f, 0.95f), new Color(0.08f, 0.95f, 1f, 0.42f), new Color(1f, 0.24f, 0.72f, 0.16f));
            GUI.Label(new Rect(panel.x + 24f, panel.y + 22f, panel.width - 48f, 40f), title, centerStyle);
        }

        private void DrawPanelChrome(Rect rect, Color bg, Color leftAccent, Color rightAccent)
        {
            GUI.color = bg;
            GUI.Box(rect, GUIContent.none, panelStyle);
            GUI.color = leftAccent;
            GUI.DrawTexture(new Rect(rect.x, rect.y, 4f, rect.height), fillTexture);
            GUI.color = rightAccent;
            GUI.DrawTexture(new Rect(rect.xMax - 4f, rect.y, 4f, rect.height), fillTexture);
            GUI.color = new Color(1f, 1f, 1f, 0.06f);
            GUI.DrawTexture(new Rect(rect.x + 10f, rect.y + 10f, rect.width - 20f, rect.height - 20f), fillTexture);
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
            DrawSoftCard(rect, new Color(1f, 1f, 1f, 0.05f));
            GUI.Label(new Rect(rect.x + 14f, rect.y + 5f, 140f, 14f), label, tinyStyle);
            GUI.Label(new Rect(rect.x + 14f, rect.y + 16f, rect.width - 28f, 20f), value, metricStyle);
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

        private void DrawUpgradeCard(Rect rect, string title, string description, string priceText, out Rect buttonRect)
        {
            DrawSoftCard(rect, new Color(1f, 1f, 1f, 0.06f));
            GUI.Label(new Rect(rect.x + 16f, rect.y + 10f, rect.width - 200f, 24f), title, labelStyle);
            GUI.Label(new Rect(rect.x + 16f, rect.y + 38f, rect.width - 200f, 48f), description, subStyle);
            buttonRect = new Rect(rect.x + rect.width - 170f, rect.y + 26f, 150f, 48f);
            bool hovered = buttonRect.Contains(Event.current.mousePosition);
            GUI.color = hovered ? new Color(1f, 0.8f, 0.2f, 0.92f) : new Color(0.9f, 0.68f, 0.12f, 0.82f);
            GUI.DrawTexture(buttonRect, fillTexture);
            GUI.color = new Color(0f, 0f, 0f, 0.22f);
            GUI.DrawTexture(new Rect(buttonRect.x, buttonRect.yMax - 3f, buttonRect.width, 3f), fillTexture);
            GUI.color = Color.white;
            GUI.Label(buttonRect, priceText, centerStyle);
        }

        private void EnsureStyles()
        {
            if (labelStyle != null)
            {
                return;
            }

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold
            };
            labelStyle.normal.textColor = Color.white;

            titleStyle = new GUIStyle(labelStyle)
            {
                fontSize = 34
            };

            heroStyle = new GUIStyle(labelStyle)
            {
                fontSize = 42
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
                fontSize = 17,
                wordWrap = true
            };
            subStyle.normal.textColor = new Color(0.82f, 0.88f, 0.94f);

            tinyStyle = new GUIStyle(labelStyle)
            {
                fontSize = 11
            };
            tinyStyle.normal.textColor = new Color(0.56f, 0.76f, 0.88f);

            metricStyle = new GUIStyle(labelStyle)
            {
                fontSize = 18
            };

            panelStyle = new GUIStyle(GUI.skin.box);
            panelStyle.normal.background = null;
            panelStyle.border = new RectOffset(0, 0, 0, 0);
        }
    }
}
