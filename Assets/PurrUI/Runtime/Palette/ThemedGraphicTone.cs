using UnityEngine;
using UnityEngine.UI;

namespace PurrNet.UI
{
    /// <summary>Preserves an authored shade using two theme roles in one graphic slot.</summary>
    [ExecuteAlways]
    public sealed class ThemedGraphicTone : ThemeBinding
    {
        [SerializeField] private Graphic _graphic;
        [SerializeField, Min(0)] private int _slot;
        [SerializeField] private ColorInfo _color = new() { enabled = true, color = ColorType.Surface };
        [SerializeField] private ColorTone _tone;
        [SerializeField, Range(0f, 1f)] private float _brightness = 1f;
        [SerializeField, Range(0f, 1f)] private float _opacity = 1f;

        private void Reset() => _graphic = GetComponent<Graphic>();

        protected override void ApplyPalette()
        {
            if (!_graphic)
                _graphic = GetComponent<Graphic>();
            if (!_graphic || !palette || !_color.enabled)
                return;

            var color = _tone.Apply(_color, palette);
            color.r *= _brightness;
            color.g *= _brightness;
            color.b *= _brightness;
            color.a *= _opacity;
            if (_graphic is IColored colored)
            {
                if (_slot < (colored.keys?.Length ?? 0))
                    colored.SetColor(_slot, color);
            }
            else if (_slot == 0)
            {
                _graphic.color = color;
            }
        }
    }
}
