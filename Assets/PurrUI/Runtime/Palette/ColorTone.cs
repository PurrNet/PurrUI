using System;
using UnityEngine;

namespace PurrNet.UI
{
    /// <summary>A shade between two palette roles, independent of visual opacity.</summary>
    [Serializable]
    public struct ColorTone
    {
        public ColorInfo shade;
        [Range(0f, 1f)] public float blend;

        public Color Apply(ColorInfo source, ColorPalette palette)
        {
            var color = source.GetColor(palette);
            return blend <= 0f || !shade.enabled ? color : Color.Lerp(color, shade.GetColor(palette), blend);
        }
    }
}
