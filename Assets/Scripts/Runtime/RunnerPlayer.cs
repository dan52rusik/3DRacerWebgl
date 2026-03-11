using UnityEngine;
using UnityEngine.InputSystem;

namespace GlitchRacer
{
    public class RunnerPlayer : MonoBehaviour
    {
        [SerializeField] private float laneOffset = 4f;
        [SerializeField] private float laneSwitchSpeed = 10f;
        [SerializeField] private float tiltAmount = 15f;

        private GlitchRacerGame game;
        private int currentLane = 1;
        private float currentVelocity;

        public void Configure(GlitchRacerGame gameManager)
        {
            game = gameManager;
        }

        private void Update()
        {
            if (game == null || game.IsGameOver)
            {
                return;
            }

            int input = ReadLaneInput();
            if (game.ControlsInverted)
            {
                input *= -1;
            }

            if (input != 0)
            {
                currentLane = Mathf.Clamp(currentLane + input, 0, 2);
            }

            float targetX = (currentLane - 1) * laneOffset;
            float nextX = Mathf.SmoothDamp(transform.position.x, targetX, ref currentVelocity, 1f / laneSwitchSpeed);
            transform.position = new Vector3(nextX, transform.position.y, 0f);

            float tilt = Mathf.Clamp((targetX - transform.position.x) * tiltAmount, -tiltAmount, tiltAmount);
            transform.rotation = Quaternion.Euler(0f, 0f, tilt);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (game == null)
            {
                return;
            }

            TrackEntity entity = other.GetComponent<TrackEntity>();
            if (entity != null)
            {
                entity.Consume(game);
            }
        }

        private static int ReadLaneInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)
                {
                    return -1;
                }

                if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)
                {
                    return 1;
                }
            }

            Pointer pointer = Pointer.current;
            if (pointer != null && pointer.press.wasPressedThisFrame)
            {
                return pointer.position.ReadValue().x < Screen.width * 0.5f ? -1 : 1;
            }

            return 0;
        }
    }
}
