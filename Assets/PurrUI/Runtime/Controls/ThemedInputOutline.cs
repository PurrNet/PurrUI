using UnityEngine.Scripting.APIUpdating;
using TMPro;
using UnityEngine;

namespace PurrNet.UI
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SelectedOutlineInputField))]
    [MovedFrom(true, "PurrNet.UI.HeroUI", "PurrUI.HeroUI.Runtime", "ThemedInputOutline")]
    public sealed class ThemedInputOutline : ThemeBinding
    {
        [SerializeField] private SelectedOutlineInputField _outline;
        [SerializeField] private RectangleGraphic _graphic;
        [SerializeField] private ColorInfo _color = new() { enabled = true, color = ColorType.Accent };
        [SerializeField] private TMP_InputField _input;
        [SerializeField] private ColorInfo _selectionColor = new() { enabled = true, color = ColorType.Accent };
        [SerializeField] private ColorTone _selectionTone;
        [SerializeField, Range(0f, 1f)] private float _selectionOpacity = 0.35f;

        private void Reset() => _outline = GetComponent<SelectedOutlineInputField>();

        public void SetColor(ColorInfo color)
        {
            _color = color;
            ApplyPalette();
        }

        protected override void ApplyPalette()
        {
            if (!_outline)
                _outline = GetComponent<SelectedOutlineInputField>();
            if (_outline && palette && _color.enabled)
            {
                _outline.outlineColor = _color.GetColor(palette);
                if (!Application.isPlaying && _graphic)
                    _graphic.outlineColor = _outline.outlineColor;
            }
            if (_input && palette && _selectionColor.enabled)
            {
                var selection = _selectionTone.Apply(_selectionColor, palette);
                selection.a *= _selectionOpacity;
                _input.selectionColor = selection;
            }
        }
    }
}
