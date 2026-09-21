using System;
using UnityEngine;

namespace PurrNet.UI
{
    public enum RoundnessRole
    {
        Custom = 0,
        Button = 1,
        Panel = 2,
        Input = 3,
        Item = 4
    }

    /// <summary>Uniform corners in pixels, or fully rounded corners that follow the element's size.</summary>
    [Serializable]
    public struct CornerRoundness
    {
        [Tooltip("Use half the smaller dimension, keeping pills and circles rounded when resized.")]
        public bool full;
        [Min(0f)] public float pixels;

        public static CornerRoundness Pixels(float pixels) => new CornerRoundness
        {
            pixels = ThemeStyles.NonNegative(pixels)
        };

        public static CornerRoundness Full => new CornerRoundness { full = true };

        public Vector4 Resolve(float width, float height)
        {
            float maximum = ThemeStyles.NonNegative(Mathf.Min(width, height)) * 0.5f;
            float radius = full ? maximum : ThemeStyles.NonNegative(pixels);
            return new Vector4(radius, radius, radius, radius);
        }
    }

    /// <summary>Shared dimensions and interaction defaults for elements that opt into their theme.</summary>
    [Serializable]
    public struct ThemeStyles
    {
        [Header("Roundness")]
        public CornerRoundness buttonRoundness;
        public CornerRoundness panelRoundness;
        public CornerRoundness inputRoundness;
        public CornerRoundness itemRoundness;

        [Header("Buttons")]
        [Min(0f), Tooltip("Seconds used by button hover, press and disabled transitions.")]
        public float buttonTransitionDuration;
        [Range(0f, 1f), Tooltip("Opacity multiplier for disabled buttons; transparent fills stay transparent.")]
        public float buttonDisabledOpacity;
        [Min(0f), Tooltip("Button scale while pressed; 1 leaves its size unchanged.")]
        public float buttonPressedScale;

        [Header("Toggles")]
        [Min(0f), Tooltip("Seconds used by toggle color and knob transitions.")]
        public float toggleTransitionDuration;

        [Header("Inputs")]
        [Min(0f), Tooltip("Seconds used by input focus transitions.")]
        public float inputTransitionDuration;
        [Min(0f), Tooltip("Outline width in pixels while an input is focused.")]
        public float inputFocusWidth;

        public static ThemeStyles Default => new ThemeStyles
        {
            buttonRoundness = CornerRoundness.Full,
            panelRoundness = CornerRoundness.Pixels(16f),
            inputRoundness = CornerRoundness.Pixels(10f),
            itemRoundness = CornerRoundness.Pixels(16f),
            buttonTransitionDuration = 0.2f,
            buttonDisabledOpacity = 0.5f,
            buttonPressedScale = 0.99f,
            toggleTransitionDuration = 0.1f,
            inputTransitionDuration = 0.15f,
            inputFocusWidth = 2f
        };

        /// <summary>Returns a copy with finite, nonnegative dimensions and valid opacity.</summary>
        public ThemeStyles Sanitize()
        {
            var value = this;
            value.buttonRoundness.pixels = NonNegative(value.buttonRoundness.pixels);
            value.panelRoundness.pixels = NonNegative(value.panelRoundness.pixels);
            value.inputRoundness.pixels = NonNegative(value.inputRoundness.pixels);
            value.itemRoundness.pixels = NonNegative(value.itemRoundness.pixels);
            value.buttonTransitionDuration = NonNegative(value.buttonTransitionDuration);
            value.buttonDisabledOpacity = Mathf.Clamp01(NonNegative(value.buttonDisabledOpacity));
            value.buttonPressedScale = NonNegative(value.buttonPressedScale);
            value.toggleTransitionDuration = NonNegative(value.toggleTransitionDuration);
            value.inputTransitionDuration = NonNegative(value.inputTransitionDuration);
            value.inputFocusWidth = NonNegative(value.inputFocusWidth);
            return value;
        }

        internal static float NonNegative(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Max(0f, value);
        }
    }
}
