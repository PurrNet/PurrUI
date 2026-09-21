using UnityEngine.Scripting.APIUpdating;
using TMPro;
using UnityEngine;

namespace PurrNet.UI
{
    /// <summary>Required-field feedback with palette-based text and outline colors.</summary>
    [ExecuteAlways]
    [MovedFrom(true, "PurrNet.UI.HeroUI", "PurrUI.HeroUI.Runtime", "ThemedInputFieldMandatory")]
    public sealed class ThemedInputFieldMandatory : ThemeBinding
    {
        [SerializeField] private TMP_InputField _inputField;
        [SerializeField] private SelectedOutlineInputField _graphic;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private TMP_Text _subLabel;
        [SerializeField] private AnimationCurve _transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private float _transitionDuration = 0.2f;
        [Space]
        [SerializeField] private float _minOutline;
        [SerializeField] private float _minOutlineError = 1f;
        [SerializeField] private ColorInfo _textColor = new() { enabled = true, color = ColorType.Surface, contrast = true };
        [SerializeField] private ColorInfo _errorTextColor = new() { enabled = true, color = ColorType.Danger };
        [SerializeField] private ColorInfo _outlineColor = new() { enabled = true, color = ColorType.Accent };
        [SerializeField] private ColorInfo _errorOutlineColor = new() { enabled = true, color = ColorType.Danger };
        [SerializeField] private ColorInfo _requiredMarkerColor = new() { enabled = true, color = ColorType.Danger };
        [Tooltip("Optional label text with {0} as the required marker's palette color, e.g. Username <color=#{0}>*</color>.")]
        [SerializeField] private string _labelFormat;

        private bool _wasModifiedOnce;
        private bool _shouldError;
        private float _elapsed;
        private float _fromOutlineWidth;

        private void Awake() => _elapsed = _transitionDuration;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (Application.IsPlaying(gameObject) && _inputField)
                _inputField.onValueChanged.AddListener(OnValueUpdated);
        }

        protected override void OnDisable()
        {
            if (_inputField)
                _inputField.onValueChanged.RemoveListener(OnValueUpdated);
            ApplyColors(false);
            base.OnDisable();
        }

        protected override void ApplyPalette() => ApplyColors(_shouldError);

        private void ApplyColors(bool error)
        {
            var textColor = error ? _errorTextColor : _textColor;
            ThemeColors.Set(_label, textColor);
            ThemeColors.Set(_subLabel, textColor);

            if (_label && palette && _requiredMarkerColor.enabled && !string.IsNullOrEmpty(_labelFormat))
            {
                var marker = ColorUtility.ToHtmlStringRGBA(_requiredMarkerColor.GetColor(palette));
                var text = _labelFormat.Replace("{0}", marker);
                if (_label.text != text)
                    _label.text = text;
            }

            if (!_graphic)
                return;
            var outlineColor = error ? _errorOutlineColor : _outlineColor;
            if (_graphic.TryGetComponent<ThemedInputOutline>(out var themed))
                themed.SetColor(outlineColor);
            else if (palette && outlineColor.enabled)
                _graphic.outlineColor = outlineColor.GetColor(palette);
        }

        protected override void Update()
        {
            base.Update();
            if (Application.IsPlaying(gameObject))
                UpdateOutline();
        }

        private void UpdateOutline()
        {
            if (!_graphic)
                return;

            _elapsed += Time.deltaTime;
            var progress = _transitionDuration <= 0f ? 1f : Mathf.Clamp01(_elapsed / _transitionDuration);
            var targetWidth = _shouldError ? _minOutlineError : _minOutline;
            _graphic.outlineWidthNotSelected = Mathf.Lerp(_fromOutlineWidth, targetWidth,
                _transitionCurve.Evaluate(progress));
        }

        private void OnValueUpdated(string value)
        {
            bool empty = string.IsNullOrEmpty(value);
            if (!empty)
                _wasModifiedOnce = true;
            bool shouldError = _wasModifiedOnce && empty;
            if (shouldError == _shouldError)
                return;

            _shouldError = shouldError;
            _fromOutlineWidth = _graphic ? _graphic.outlineWidthNotSelected : _minOutline;
            _elapsed = 0f;
            ApplyPalette();
        }
    }
}
