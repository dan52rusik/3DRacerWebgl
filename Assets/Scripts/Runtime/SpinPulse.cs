using UnityEngine;

namespace GlitchRacer
{
    public class SpinPulse : MonoBehaviour
    {
        private Vector3 baseScale;
        private float offset;

        private void Awake()
        {
            baseScale = transform.localScale;
            offset = Random.Range(0f, 10f);
        }

        private void Update()
        {
            transform.Rotate(0f, 90f * Time.deltaTime, 0f, Space.Self);
            transform.localScale = baseScale * (1f + (Mathf.Sin((Time.time + offset) * 6f) * 0.08f));
        }
    }
}
