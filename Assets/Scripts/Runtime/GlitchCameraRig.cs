using UnityEngine;

namespace GlitchRacer
{
    public class GlitchCameraRig : MonoBehaviour
    {
        [SerializeField] private Vector3 followOffset = new(0f, 5.1f, -8.6f);
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
                offset += new Vector3(2.3f, 0.6f, -1.8f);
            }

            transform.position = Vector3.Lerp(transform.position, target.position + offset, followLerp * Time.deltaTime);
            transform.LookAt(target.position + Vector3.up * 0.9f);

            float roll = 0f;
            if (game != null && game.ControlsInverted)
            {
                roll = Mathf.Sin(Time.time * 14f) * 18f;
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
                cachedCamera.fieldOfView = Mathf.Lerp(cachedCamera.fieldOfView, fovTarget, Time.deltaTime * 5f);
                cachedCamera.backgroundColor = game != null && game.ControlsInverted
                    ? Color.Lerp(new Color(0.03f, 0.02f, 0.08f), new Color(0.08f, 0.2f, 0.16f), (Mathf.Sin(Time.time * 11f) + 1f) * 0.5f)
                    : new Color(0.01f, 0.02f, 0.05f);
            }
        }

        public void Punch()
        {
            punch = 1f;
        }
    }
}
