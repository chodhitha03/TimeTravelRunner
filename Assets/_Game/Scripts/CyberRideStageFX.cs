using UnityEngine;

namespace TimeTravellerRunner
{
    /// <summary>Shadow gives a visual ground reference during flight; the expanding
    /// touchdown ring shows where the character actually lands.</summary>
    public sealed class CyberRideStageFX
    {
        public readonly GameObject Root;
        private readonly Transform shadow;
        private readonly Transform touchdown;
        private float pulseTime;
        private const float PulseDuration = .55f;

        public CyberRideStageFX(Transform parent)
        {
            Transform stage = new GameObject("CYBER / ride altitude and landing feedback").transform;
            stage.SetParent(parent, false);
            Root = stage.gameObject;
            GameObject shadowObject = RunnerArt.Ring("Gadget height / ground reference", stage,
                Vector3.zero, .84f, .08f, "#296A8B", false);
            shadow = shadowObject.transform;
            shadow.localRotation = Quaternion.Euler(90f, 0f, 0f);
            GameObject pulse = RunnerArt.Ring("Touchdown / expanding contact ring", stage,
                Vector3.zero, .85f, .095f, "#5FEFFF", true);
            touchdown = pulse.transform;
            touchdown.localRotation = Quaternion.Euler(90f, 0f, 0f);
            touchdown.gameObject.SetActive(false);
        }

        public void Tick(float x, float z, float lift, bool landed, float dt)
        {
            shadow.position = new Vector3(x, .075f, z);
            shadow.localScale = Vector3.one * Mathf.Lerp(.9f, 1.9f,
                Mathf.Clamp01(lift / 5.2f));
            shadow.gameObject.SetActive(lift > .12f);

            if (landed) pulseTime = PulseDuration;
            if (pulseTime > 0f)
            {
                pulseTime = Mathf.Max(0f, pulseTime - Mathf.Max(0f, dt));
                touchdown.position = new Vector3(x, .09f, z);
                touchdown.localScale = Vector3.one *
                    Mathf.Lerp(.65f, 2.1f, 1f - pulseTime / PulseDuration);
                touchdown.gameObject.SetActive(true);
            }
            else touchdown.gameObject.SetActive(false);
        }
    }
}
