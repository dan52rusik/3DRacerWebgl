using UnityEngine;

namespace GlitchRacer
{
    public class GlitchCameraRig : MonoBehaviour
    {
        [SerializeField] private Vector3 followOffset = new(0f, 4.4f, -7.4f);
        [SerializeField] private float followLerp = 8f;

        private GlitchRacerGame game;
        private Transform target;
        private float punch;
        private float baseFieldOfView;
        private Camera cachedCamera;

        public void Configure(GlitchRacerGame gameManager, Transform followTarget)
        {
            game = gameManager;
            target = followTarget;
        }

        private void Awake()
        {
            cachedCamera = GetComponent<Camera>();
            if (cachedCamera != null)
            {
                baseFieldOfView = cachedCamera.fieldOfView;
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 offset = followOffset;
            if (game != null && game.IsMenuVisible)
            {
                offset += new Vector3(2.8f, 0.4f, -2.2f);
            }
            else if (game != null && game.IsChapterRush)
            {
                offset += new Vector3(0f, 0.7f, -2.8f);
            }

            transform.position = Vector3.Lerp(transform.position, target.position + offset, followLerp * Time.deltaTime);
            Vector3 lookTarget = target.position + Vector3.up * 0.55f;
            if (game != null && game.IsChapterRush)
            {
                lookTarget += new Vector3(
                    Mathf.Sin(Time.time * 1.8f) * 0.35f,
                    Mathf.Cos(Time.time * 2.1f) * 0.2f,
                    0f);
                transform.position += new Vector3(
                    Mathf.Sin(Time.time * 8.5f) * 0.08f,
                    Mathf.Sin(Time.time * 5.8f) * 0.05f,
                    0f);
            }
            else if (game != null && game.HasDrugsTrip)
            {
                lookTarget += new Vector3(
                    Mathf.Sin(Time.time * 1.4f) * 1.95f,
                    Mathf.Cos(Time.time * 3.8f) * 0.9f,
                    Mathf.Sin(Time.time * 0.9f) * 0.35f);
                transform.position += new Vector3(
                    Mathf.Sin(Time.time * 2.8f) * 0.42f,
                    Mathf.Sin(Time.time * 4.4f) * 0.24f,
                    Mathf.Cos(Time.time * 1.8f) * 0.18f);
            }
            else if (game != null && game.HasDrunkVision)
            {
                lookTarget += new Vector3(
                    Mathf.Sin(Time.time * 1.9f) * 1.15f,
                    Mathf.Cos(Time.time * 2.6f) * 0.5f,
                    0f);
                transform.position += new Vector3(
                    Mathf.Sin(Time.time * 2.2f) * 0.2f,
                    Mathf.Sin(Time.time * 3.6f) * 0.12f,
                    0f);
            }

            transform.LookAt(lookTarget);

            float roll = 0f;
            if (game != null && game.ControlsInverted)
            {
                roll = Mathf.Sin(Time.time * 14f) * 18f;
            }
            else if (game != null && game.IsChapterRush)
            {
                roll = Mathf.Sin(Time.time * 6.8f) * 5f;
            }
            else if (game != null && game.HasDrugsTrip)
            {
                roll = Mathf.Sin(Time.time * 3.1f) * 16f + Mathf.Cos(Time.time * 1.7f) * 11f;
            }
            else if (game != null && game.HasDrunkVision)
            {
                roll = Mathf.Sin(Time.time * 2.4f) * 10f + Mathf.Cos(Time.time * 1.35f) * 6f;
            }
            else if (game != null && game.HasStaticNoise)
            {
                roll = Mathf.Sin(Time.time * 30f) * 1.8f;
            }

            if (punch > 0f)
            {
                roll += Mathf.Sin(Time.time * 45f) * 8f * punch;
                punch = Mathf.MoveTowards(punch, 0f, Time.deltaTime * 3f);
            }

            transform.rotation *= Quaternion.Euler(0f, 0f, roll);

            if (cachedCamera != null)
            {
                float fovTarget = baseFieldOfView + ((game != null && game.ControlsInverted) ? 10f : 0f) + ((game != null && game.IsMenuVisible) ? 6f : 0f);
                if (game != null && game.IsChapterRush)
                {
                    fovTarget += 18f + Mathf.Sin(Time.time * 5.5f) * 2.4f;
                }
                else if (game != null && game.HasDrugsTrip)
                {
                    fovTarget += 13f + Mathf.Sin(Time.time * 2.7f) * 6f + Mathf.Cos(Time.time * 1.5f) * 2.5f;
                }
                else if (game != null && game.HasDrunkVision)
                {
                    fovTarget += 8f + Mathf.Sin(Time.time * 2.1f) * 3.5f;
                }
                else if (game != null && game.HasStaticNoise)
                {
                    fovTarget += Mathf.Sin(Time.time * 18f) * 0.8f;
                }

                cachedCamera.fieldOfView = Mathf.Lerp(cachedCamera.fieldOfView, fovTarget, Time.deltaTime * 5f);
                if (game != null && game.ControlsInverted)
                {
                    cachedCamera.backgroundColor = Color.Lerp(new Color(0.06f, 0.04f, 0.12f), new Color(0.1f, 0.24f, 0.18f), (Mathf.Sin(Time.time * 11f) + 1f) * 0.5f);
                }
                else if (game != null && game.IsChapterRush)
                {
                    cachedCamera.backgroundColor = Color.Lerp(
                        new Color(0.01f, 0.02f, 0.08f),
                        new Color(0.02f, 0.06f, 0.14f),
                        (Mathf.Sin(Time.time * 3.8f) + 1f) * 0.5f);
                }
                else if (game != null && game.HasDrugsTrip)
                {
                    cachedCamera.backgroundColor = Color.Lerp(
                        new Color(0.12f, 0.04f, 0.16f),
                        new Color(0.02f, 0.24f, 0.19f),
                        (Mathf.Sin(Time.time * 4.2f) + 1f) * 0.5f);
                }
                else if (game != null && game.HasDrunkVision)
                {
                    cachedCamera.backgroundColor = Color.Lerp(new Color(0.05f, 0.04f, 0.08f), new Color(0.08f, 0.08f, 0.15f), (Mathf.Sin(Time.time * 2.7f) + 1f) * 0.5f);
                }
                else if (game != null && game.HasStaticNoise)
                {
                    cachedCamera.backgroundColor = Color.Lerp(new Color(0.03f, 0.04f, 0.08f), new Color(0.07f, 0.07f, 0.07f), (Mathf.Sin(Time.time * 24f) + 1f) * 0.5f);
                }
                else
                {
                    cachedCamera.backgroundColor = new Color(0.03f, 0.04f, 0.08f);
                }
            }
        }

        public void Punch()
        {
            punch = 1f;
        }
    }
}
