using UnityEngine;

namespace TimeTravellerRunner
{
    /// <summary>Lightweight procedural animation for holograms, props, coins and flying cars.</summary>
    public sealed class CyberMotion : MonoBehaviour
    {
        public enum MotionType { Bob, Spin, Fly, Sway }
        private MotionType motion;
        private float amplitude, speed, phase;
        private Vector3 origin;
        private Quaternion originalRotation;

        public CyberMotion Configure(MotionType type, float movement, float frequency)
        {
            motion = type;
            amplitude = movement;
            speed = frequency;
            origin = transform.localPosition;
            originalRotation = transform.localRotation;
            phase = (transform.position.z * .137f + transform.position.x * .23f);
            return this;
        }

        private void Update()
        {
            float t = Time.time;
            if (motion == MotionType.Spin)
                transform.localRotation = originalRotation * Quaternion.Euler(0, t * speed, 0);
            else if (motion == MotionType.Fly)
                transform.localPosition = origin + new Vector3(Mathf.Sin(t * speed + phase) * amplitude,
                    Mathf.Cos(t * speed * .57f + phase) * .28f, Mathf.Cos(t * .42f + phase) * 2.5f);
            else if (motion == MotionType.Sway)
                transform.localPosition = origin + Vector3.right * Mathf.Sin(t * speed + phase) * amplitude;
            else
                transform.localPosition = origin + Vector3.up * (Mathf.Sin(t * speed + phase) * amplitude);
        }
    }
}
