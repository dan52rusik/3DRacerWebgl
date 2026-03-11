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

        private bool consumed;

        public void Setup(TrackEntityType type, float value, float duration = 5f)
        {
            entityType = type;
            amount = value;
            glitchDuration = duration;
        }

        public void Consume(GlitchRacerGame game)
        {
            if (consumed || game == null || game.IsGameOver)
            {
                return;
            }

            consumed = true;

            switch (entityType)
            {
                case TrackEntityType.Score:
                    game.AddScore(amount);
                    break;
                case TrackEntityType.Ram:
                    game.AddRam(amount);
                    break;
                case TrackEntityType.Glitch:
                    game.TriggerGlitch(glitchDuration, amount);
                    break;
                case TrackEntityType.Obstacle:
                    game.HitObstacle(amount);
                    break;
            }

            Destroy(gameObject);
        }
    }
}
