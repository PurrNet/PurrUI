using UnityEngine;

namespace PurrNet.UI
{
    internal static class ThemeAnimation
    {
        // Keep normalized progress when the theme changes a running transition.
        internal static float Retime(float elapsed, float previousDuration, float duration)
        {
            if (duration <= 0f) return 0f;
            if (previousDuration <= 0f) return duration;
            return Mathf.Clamp01(elapsed / previousDuration) * duration;
        }
    }
}
