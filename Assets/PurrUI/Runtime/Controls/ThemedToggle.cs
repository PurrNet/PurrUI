using UnityEngine.Scripting.APIUpdating;
using UnityEngine;

namespace PurrNet.UI
{
    /// <summary>Themes a toggle without changing its input, audio or knob animation.</summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ToggleElement))]
    [MovedFrom(true, "PurrNet.UI.HeroUI", "PurrUI.HeroUI.Runtime", "ThemedToggle")]
    public sealed class ThemedToggle : ThemeBinding
    {
        [SerializeField] private ToggleElement _toggle;
        [SerializeField] private RectangleGraphic _background;
        [SerializeField] private ColorInfo _onColor = new() { enabled = true, color = ColorType.Accent };
        [SerializeField] private ColorInfo _offColor = new() { enabled = true, color = ColorType.Surface };
        [SerializeField] private ColorTone _onTone;
        [SerializeField] private ColorTone _offTone = new()
        {
            shade = new ColorInfo { enabled = true, color = ColorType.Surface, contrast = true },
            blend = 0.065f
        };
        [SerializeField, Range(0f, 1f)] private float _offOpacity = 1f;
        [SerializeField] private bool _useThemeTransition;
        [SerializeField] private float _transitionDuration = 0.2f;
        [SerializeField] private AnimationCurve _transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private Color _fromColor;
        private Color _targetColor;
        private Color _currentColor;
        private float _elapsed;
        private bool _initialized;
        private bool _lastValue;
        private float _lastTransitionDuration;
        public bool useThemeTransition
        {
            get => _useThemeTransition;
            set { _useThemeTransition = value; RefreshTransitionDuration(); }
        }
        public float transitionDuration => _useThemeTransition && palette ? palette.styles.toggleTransitionDuration : Mathf.Max(0f, _transitionDuration);
        private void RefreshTransitionDuration()
        {
            var duration = transitionDuration;
            if (_initialized && !Mathf.Approximately(duration, _lastTransitionDuration))
                _elapsed = ThemeAnimation.Retime(_elapsed, _lastTransitionDuration, duration);
            _lastTransitionDuration = duration;
        }

        private void Reset() => _toggle = GetComponent<ToggleElement>();

        protected override void OnEnable()
        {
            if (!_toggle)
                _toggle = GetComponent<ToggleElement>();
            _initialized = false;
            base.OnEnable();
        }

        protected override void ApplyPalette()
        {
            RefreshTransitionDuration();
            if (!_toggle || !_background || !palette)
                return;

            var value = _toggle.value;
            var info = value ? _onColor : _offColor;
            if (!info.enabled)
                return;

            var tone = value ? _onTone : _offTone;
            var target = tone.Apply(info, palette);
            if (!value)
                target.a *= _offOpacity;
            // A style-only update should keep a toggle transition at the same point.
            if (_initialized && value == _lastValue && target == _targetColor)
                return;
            _lastValue = value;
            _targetColor = target;
            _fromColor = _initialized ? _currentColor : _targetColor;
            _currentColor = _fromColor;
            _elapsed = 0f;
            _initialized = true;
        }

        private void LateUpdate()
        {
            RefreshTransitionDuration();
            if (!_toggle || !_background || !palette)
                return;
            if (!_initialized || _lastValue != _toggle.value)
                ApplyPalette();
            if (!_initialized || !(_toggle.value ? _onColor : _offColor).enabled)
                return;

            _elapsed += Time.deltaTime;
            bool instant = !Application.isPlaying || _lastTransitionDuration <= 0f;
            var progress = instant ? 1f : Mathf.Clamp01(_elapsed / _lastTransitionDuration);
            if (!instant && _transitionCurve != null)
                progress = _transitionCurve.Evaluate(progress);
            _currentColor = Color.Lerp(_fromColor, _targetColor, progress);
            // ToggleElement writes a master tint in Update. Keep that tint neutral
            // and use the fill slot so its legacy RGB cannot multiply the palette.
            _background.color = Color.white;
            _background.graphicColor = _currentColor;
        }
    }
}
