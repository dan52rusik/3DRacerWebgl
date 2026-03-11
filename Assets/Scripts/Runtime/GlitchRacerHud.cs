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

            DrawTopBar();

            if (game.ControlsInverted)
            {
                GUI.color = new Color(0.8f, 0.2f, 1f, 0.18f);
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), fillTexture);
                GUI.color = Color.white;
                GUI.Label(new Rect(24f, 104f, 600f, 30f), $"GLITCH ACTIVE {game.GlitchTimeRemaining:0.0}s  |  controls inverted", labelStyle);
            }

            GUI.Label(new Rect(24f, Screen.height - 52f, 900f, 30f), "A/D or Left/Right. Tap left/right side of the screen on mobile.", labelStyle);

            if (game.IsGameOver)
            {
                GUI.color = new Color(0f, 0f, 0f, 0.72f);
                GUI.DrawTexture(new Rect(Screen.width * 0.5f - 230f, Screen.height * 0.5f - 120f, 460f, 240f), fillTexture);
                GUI.color = Color.white;

                GUI.Label(new Rect(Screen.width * 0.5f - 170f, Screen.height * 0.5f - 82f, 340f, 40f), "SYSTEM FAILURE", centerStyle);
                GUI.Label(new Rect(Screen.width * 0.5f - 170f, Screen.height * 0.5f - 24f, 340f, 40f), $"Score: {Mathf.RoundToInt(game.Score):N0}", centerStyle);
                GUI.Label(new Rect(Screen.width * 0.5f - 170f, Screen.height * 0.5f + 26f, 340f, 40f), "Press R or Space", centerStyle);
            }
        }

        private void DrawTopBar()
        {
            GUI.Label(new Rect(24f, 20f, 300f, 40f), "GLITCH RACER", titleStyle);
            GUI.Label(new Rect(24f, 64f, 320f, 30f), $"Score: {Mathf.RoundToInt(game.Score):N0}", labelStyle);

            Rect barRect = new(250f, 66f, 260f, 24f);
            GUI.color = new Color(1f, 1f, 1f, 0.2f);
            GUI.DrawTexture(barRect, fillTexture);
            GUI.color = new Color(0.26f, 1f, 0.45f);
            GUI.DrawTexture(new Rect(barRect.x, barRect.y, barRect.width * Mathf.Clamp01(game.CurrentRam / 100f), barRect.height), fillTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(520f, 64f, 180f, 30f), $"RAM {Mathf.CeilToInt(game.CurrentRam)}%", labelStyle);
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
        }
    }
}
