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
                DrawMainMenu();
                DrawShop();
            }
            else if (game.State == GlitchRacerGame.SessionState.Settings)
            {
                DrawMainMenu();
                DrawSettings();
            }
            else if (game.IsGameOver)
            {
                DrawGameOver();
            }
        }

        private void DrawTopBar()
        {
            GUI.Label(new Rect(24f, 20f, 300f, 40f), "GLITCH RACER", titleStyle);
            GUI.Label(new Rect(24f, 64f, 320f, 30f), $"Score: {Mathf.RoundToInt(game.Score):N0}", labelStyle);
            GUI.Label(new Rect(24f, 98f, 320f, 30f), $"Distance: {Mathf.FloorToInt(game.CurrentDistance)} m", labelStyle);

            Rect barRect = new(250f, 100f, 260f, 24f);
            GUI.color = new Color(1f, 1f, 1f, 0.2f);
            GUI.DrawTexture(barRect, fillTexture);
            GUI.color = new Color(0.26f, 1f, 0.45f);
            GUI.DrawTexture(new Rect(barRect.x, barRect.y, barRect.width * Mathf.Clamp01(game.CurrentRam / 100f), barRect.height), fillTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(520f, 98f, 180f, 30f), $"RAM {Mathf.CeilToInt(game.CurrentRam)}%", labelStyle);
            GUI.Label(new Rect(Screen.width - 260f, 20f, 220f, 30f), $"Coins: {game.Coins:N0}", labelStyle);
        }

        private void DrawBackdrop()
        {
            if (!game.IsMenuVisible)
            {
                return;
            }

            GUI.color = new Color(0.01f, 0.01f, 0.03f, 0.3f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), fillTexture);
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

        private void DrawMainMenu()
        {
            Rect panel = new Rect(42f, 42f, Mathf.Min(520f, Screen.width * 0.42f), Screen.height - 84f);
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.Box(panel, GUIContent.none, panelStyle);
            GUI.color = Color.white;

            GUI.Label(new Rect(panel.x + 28f, panel.y + 28f, panel.width - 56f, 44f), "GLITCH RACER", titleStyle);
            GUI.Label(new Rect(panel.x + 28f, panel.y + 82f, panel.width - 56f, 88f), "Virus-car dives through a broken data abyss. Collect shards, survive glitches, and turn score into coins for stronger runs.", subStyle);

            GUI.Label(new Rect(panel.x + 28f, panel.y + 170f, panel.width - 56f, 30f), $"Wallet: {game.Coins:N0} coins", labelStyle);
            GUI.Label(new Rect(panel.x + 28f, panel.y + 204f, panel.width - 56f, 30f), $"Best score: {Mathf.RoundToInt(game.BestScore):N0}", labelStyle);
            GUI.Label(new Rect(panel.x + 28f, panel.y + 238f, panel.width - 56f, 30f), $"Best distance: {Mathf.RoundToInt(game.BestDistance):N0} m", labelStyle);
            GUI.Label(new Rect(panel.x + 28f, panel.y + 272f, panel.width - 56f, 30f), $"Total distance: {Mathf.RoundToInt(game.TotalDistance):N0} m", labelStyle);

            if (GUI.Button(new Rect(panel.x + 28f, panel.y + 334f, panel.width - 56f, 52f), "Start Run", buttonStyle))
            {
                game.StartGame();
            }

            if (GUI.Button(new Rect(panel.x + 28f, panel.y + 398f, panel.width - 56f, 52f), "Shop / Upgrades", buttonStyle))
            {
                game.OpenShop();
            }

            if (GUI.Button(new Rect(panel.x + 28f, panel.y + 462f, panel.width - 56f, 52f), "Settings", buttonStyle))
            {
                game.OpenSettings();
            }

            GUI.Label(new Rect(panel.x + 28f, panel.yMax - 120f, panel.width - 56f, 90f),
                $"Coin formula\n3.5 x shards + 0.03 x score + 0.12 x meters\nLeaderboard distance metric: meters traveled this run",
                subStyle);
        }

        private void DrawShop()
        {
            Rect panel = new Rect(Screen.width * 0.5f - 260f, 90f, 520f, 360f);
            DrawPanel(panel, "Shop");

            GUI.Label(new Rect(panel.x + 24f, panel.y + 72f, panel.width - 48f, 30f), $"Fuel Efficiency Lv.{game.FuelUpgradeLevel}", labelStyle);
            GUI.Label(new Rect(panel.x + 24f, panel.y + 104f, panel.width - 48f, 54f), $"Reduces RAM drain by 8% per level.\nCurrent drain multiplier: x{game.FuelDrainMultiplier:0.00}", subStyle);
            if (GUI.Button(new Rect(panel.x + panel.width - 184f, panel.y + 84f, 160f, 44f), $"Buy {game.FuelUpgradeCost}", buttonStyle))
            {
                game.TryBuyFuelUpgrade();
            }

            GUI.Label(new Rect(panel.x + 24f, panel.y + 186f, panel.width - 48f, 30f), $"Score Booster Lv.{game.ScoreUpgradeLevel}", labelStyle);
            GUI.Label(new Rect(panel.x + 24f, panel.y + 218f, panel.width - 48f, 54f), $"Boosts all score gains by 12% per level.\nCurrent score multiplier: x{game.ScoreMultiplier:0.00}", subStyle);
            if (GUI.Button(new Rect(panel.x + panel.width - 184f, panel.y + 198f, 160f, 44f), $"Buy {game.ScoreUpgradeCost}", buttonStyle))
            {
                game.TryBuyScoreUpgrade();
            }

            if (GUI.Button(new Rect(panel.x + 24f, panel.y + panel.height - 62f, panel.width - 48f, 42f), "Back", buttonStyle))
            {
                game.CloseOverlayToMenu();
            }
        }

        private void DrawSettings()
        {
            Rect panel = new Rect(Screen.width * 0.5f - 240f, 110f, 480f, 300f);
            DrawPanel(panel, "Settings");

            if (GUI.Button(new Rect(panel.x + 24f, panel.y + 78f, panel.width - 48f, 48f), $"Music: {(game.MusicEnabled ? "On" : "Off")}", buttonStyle))
            {
                game.ToggleMusic();
            }

            if (GUI.Button(new Rect(panel.x + 24f, panel.y + 140f, panel.width - 48f, 48f), $"SFX: {(game.SfxEnabled ? "On" : "Off")}", buttonStyle))
            {
                game.ToggleSfx();
            }

            GUI.Label(new Rect(panel.x + 24f, panel.y + 204f, panel.width - 48f, 42f), "Progress is saved automatically on every run end and purchase.", subStyle);

            if (GUI.Button(new Rect(panel.x + 24f, panel.y + panel.height - 58f, panel.width - 48f, 40f), "Back", buttonStyle))
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

            if (GUI.Button(new Rect(panel.x + 24f, panel.y + panel.height - 64f, (panel.width - 60f) * 0.5f, 42f), "Run Again", buttonStyle))
            {
                game.StartGame();
            }

            if (GUI.Button(new Rect(panel.center.x + 6f, panel.y + panel.height - 64f, (panel.width - 60f) * 0.5f, 42f), "Main Menu", buttonStyle))
            {
                game.EnterMainMenu();
            }
        }

        private void DrawPanel(Rect panel, string title)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.78f);
            GUI.Box(panel, GUIContent.none, panelStyle);
            GUI.color = Color.white;
            GUI.Label(new Rect(panel.x + 24f, panel.y + 22f, panel.width - 48f, 40f), title, centerStyle);
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

            centerStyle = new GUIStyle(labelStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 28
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold
            };

            subStyle = new GUIStyle(labelStyle)
            {
                fontSize = 18,
                wordWrap = true
            };

            panelStyle = new GUIStyle(GUI.skin.box);
        }
    }
}
