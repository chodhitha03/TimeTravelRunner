using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TimeTravellerRunner
{
    /// <summary>
    /// Clean, bright, vibrant post-processing inspired by Subway Surfers.
    /// Provides punchy daytime saturation, filmic tone, and subtle highlight sheen
    /// without any blinding glare, washed-out fog, or harsh bloom.
    /// </summary>
    public sealed class CyberPostProcessing : MonoBehaviour
    {
        private Volume volume;
        private VolumeProfile profile;

        public static CyberPostProcessing Ensure(Transform parent)
        {
            if (parent == null) return null;
            CyberPostProcessing existing = parent.GetComponentInChildren<CyberPostProcessing>();
            if (existing != null) return existing;

            GameObject go = new GameObject("SUBWAY POST PROCESSING / URP Volume");
            go.transform.SetParent(parent, false);
            CyberPostProcessing cpp = go.AddComponent<CyberPostProcessing>();
            cpp.Initialize();
            return cpp;
        }

        private void Initialize()
        {
            volume = gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 100f;

            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "SubwaySurfers_CleanProfile";

            // 1. Bloom: subtle specular sheen only on coins/headlights (NO blinding glare)
            Bloom bloom = profile.Add<Bloom>(true);
            bloom.active = true;
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 1.05f;
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 0.45f;
            bloom.scatter.overrideState = true;
            bloom.scatter.value = 0.45f;
            bloom.tint.overrideState = true;
            bloom.tint.value = Color.white;

            // 2. Tonemapping: crisp highlight preservation
            Tonemapping tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.active = true;
            tonemapping.mode.overrideState = true;
            tonemapping.mode.value = TonemappingMode.Neutral;

            // 3. Color Adjustments: cheerful, rich, punchy daytime cartoon colors
            ColorAdjustments colorAdj = profile.Add<ColorAdjustments>(true);
            colorAdj.active = true;
            colorAdj.postExposure.overrideState = true;
            colorAdj.postExposure.value = 0.08f;
            colorAdj.contrast.overrideState = true;
            colorAdj.contrast.value = 14f;
            colorAdj.saturation.overrideState = true;
            colorAdj.saturation.value = 22f;

            // 4. Subtle Vignette: gentle depth without dark corners
            Vignette vignette = profile.Add<Vignette>(true);
            vignette.active = true;
            vignette.intensity.overrideState = true;
            vignette.intensity.value = 0.15f;
            vignette.smoothness.overrideState = true;
            vignette.smoothness.value = 0.35f;

            volume.profile = profile;
        }

        private void OnDestroy()
        {
            if (profile != null) Destroy(profile);
        }
    }
}
