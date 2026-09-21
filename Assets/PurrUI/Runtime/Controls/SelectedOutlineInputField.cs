using UnityEngine.Scripting.APIUpdating;
using UnityEngine;

namespace PurrNet.UI
{
    [ExecuteAlways]
    [MovedFrom(true, "PurrNet.UI.HeroUI", "PurrUI.HeroUI.Runtime", "SelectedOutlineInputField")]
    public class SelectedOutlineInputField : ThemeBinding
    {
        [SerializeField] private  TMPro.TMP_InputField _input;
        [SerializeField] private RectangleGraphic _graphic;
        [SerializeField] AnimationCurve _transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private Color _outlineColor = Color.white;
        [SerializeField] private bool _useThemeTransition;
        [SerializeField] private bool _useThemeFocusWidth;
        [SerializeField] private float _outlineWidth = 2f;
        [SerializeField] private float _outlineWidthNotSelected = 0f;
        [SerializeField] private float _transitionDuration = 0.2f;

        private float _timeSinceToggle;
        private bool _isSelected;

        public Color outlineColor
        {
            get => _outlineColor;
            set => _outlineColor = value;
        }

        public float outlineWidthNotSelected
        {
            get => _outlineWidthNotSelected;
            set => _outlineWidthNotSelected = value;
        }

        private bool _timingInitialized;
        private float _lastTransitionDuration;
        public bool useThemeTransition
        {
            get => _useThemeTransition;
            set { _useThemeTransition = value; RefreshTransitionDuration(); }
        }
        public bool useThemeFocusWidth { get => _useThemeFocusWidth; set => _useThemeFocusWidth = value; }
        public float transitionDuration => _useThemeTransition && palette ? palette.styles.inputTransitionDuration : Mathf.Max(0f, _transitionDuration);
        public float focusWidth => _useThemeFocusWidth && palette ? palette.styles.inputFocusWidth : _outlineWidth;

        private void Awake() => RefreshTransitionDuration();
        protected override void ApplyPalette() => RefreshTransitionDuration();
        private void RefreshTransitionDuration()
        {
            var duration = transitionDuration;
            if (!_timingInitialized)
            {
                _timingInitialized = true;
                _timeSinceToggle = duration;
            }
            else if (!Mathf.Approximately(duration, _lastTransitionDuration))
                _timeSinceToggle = ThemeAnimation.Retime(_timeSinceToggle, _lastTransitionDuration, duration);
            _lastTransitionDuration = duration;
        }

        protected override void Update()
        {
            base.Update();
            RefreshTransitionDuration();
            if (!Application.IsPlaying(gameObject) || !_input)
                return;
            UpdateVisuals(Time.deltaTime, _input.isFocused);
        }

        private void UpdateVisuals(float deltaTime, bool isCurrentlySelected)
        {
            RefreshTransitionDuration();

            if (_isSelected != isCurrentlySelected)
            {
                _isSelected = isCurrentlySelected;
                _timeSinceToggle = 0f;
            }

            var lerp = _lastTransitionDuration <= 0f ? 1f : Mathf.Clamp01(_timeSinceToggle / _lastTransitionDuration);
            if (_lastTransitionDuration > 0f && _transitionCurve != null) lerp = _transitionCurve.Evaluate(lerp);

            var targetWidth = _isSelected ? focusWidth : _outlineWidthNotSelected;
            var initialWidth = _isSelected ? _outlineWidthNotSelected : focusWidth;

            if (_graphic)
            {
                _graphic.outlineColor = _outlineColor;
                _graphic.outlineSize = Mathf.Lerp(initialWidth, targetWidth, lerp);
            }

            _timeSinceToggle += deltaTime;
        }
    }
}
