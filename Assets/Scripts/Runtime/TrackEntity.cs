using UnityEngine;

namespace GlitchRacer
{
    public enum TrackEntityType
    {
        Score,
        Ram,
        Glitch,
        Obstacle
    }

    public class TrackEntity : MonoBehaviour
    {
        [SerializeField] private TrackEntityType entityType;
        [SerializeField] private float amount = 10f;
        [SerializeField] private float glitchDuration = 5f;
        [SerializeField] private GlitchRacerGame.GlitchType glitchType = GlitchRacerGame.GlitchType.InvertControls;

        private bool consumed;

        public void Setup(TrackEntityType type, float value, float duration = 5f, GlitchRacerGame.GlitchType appliedGlitch = GlitchRacerGame.GlitchType.InvertControls)
        {
            entityType = type;
            amount = value;
            glitchDuration = duration;
            glitchType = appliedGlitch;
        }

        public void Consume(GlitchRacerGame game)
        {
            if (consumed || game == null || game.State != GlitchRacerGame.SessionState.Playing)
            {
                return;
            }

            consumed = true;

            switch (entityType)
            {
                case TrackEntityType.Score:
                    game.CollectDataShard(amount);
                    break;
                case TrackEntityType.Ram:
                    game.AddRam(amount);
                    break;
                case TrackEntityType.Glitch:
                    game.TriggerGlitch(glitchDuration, amount, glitchType);
                    break;
                case TrackEntityType.Obstacle:
                    game.HitObstacle(amount);
                    break;
            }

            Destroy(gameObject);
        }
    }
}
