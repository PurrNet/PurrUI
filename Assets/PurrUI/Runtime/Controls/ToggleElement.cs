using UnityEngine.Scripting.APIUpdating;
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PurrNet.UI
{
    [ExecuteInEditMode]
    [MovedFrom(true, "PurrNet.UI.HeroUI", "PurrUI.HeroUI.Runtime", "ToggleElement")]
    public class ToggleElement : ThemeBinding, IPointerClickHandler
    {
        [SerializeField] private bool _value = false;
        [Space]
        [SerializeField] RectangleGraphic _background;
        [SerializeField] RectangleGraphic _nob;
        [Space]
        [SerializeField] AnimationCurve _transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] Color _backgroundOn = Color.white;
        [SerializeField] Color _backgroundOff = Color.gray;
        [SerializeField] Vector2 _leftnobPosition = new Vector2(-0.5f, 0);
        [SerializeField] Vector2 _rightNobPosition = new Vector2(0.5f, 0);
        [SerializeField] private bool _useThemeTransition;
        [SerializeField] float _transitionDuration = 0.2f;
        [Space]
        [SerializeField] private AudioClip[] _enableSound, _disableSound;

        public event Action<bool> onToggled;
        private float _timeSinceToggle = 0f;

#if UNITY_EDITOR
        private bool _lastValue = false;
#endif

        private bool _timingInitialized;
        private float _lastTransitionDuration;
        public bool useThemeTransition
        {
            get => _useThemeTransition;
            set { _useThemeTransition = value; RefreshTransitionDuration(); }
        }
        public float transitionDuration => _useThemeTransition && palette ? palette.styles.toggleTransitionDuration : Mathf.Max(0f, _transitionDuration);

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

        public bool value
        {
            get => _value;
            set
            {
                if (_value == value) return;
                _value = value;
                OnValueChanged();
            }
        }

        private void OnValueChanged()
        {
#if UNITY_EDITOR
            _lastValue = _value;
#endif
            _timeSinceToggle = 0f;
            onToggled?.Invoke(_value);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (!Application.isPlaying && _value != _lastValue)
            {
                OnValueChanged();
            }
        }
#endif

        public void Toggle()
        {
            value = !value;
        }

        protected override void Update()
        {
            base.Update();
            UpdateVisuals(Time.deltaTime);
        }

        private void UpdateVisuals(float deltaTime)
        {
            RefreshTransitionDuration();
            if (!_background || !_nob)
                return;

            float lerp = _lastTransitionDuration <= 0f ? 1f : Mathf.Clamp01(_timeSinceToggle / _lastTransitionDuration);
            if (_lastTransitionDuration > 0f && _transitionCurve != null) lerp = _transitionCurve.Evaluate(lerp);

            var targetColor = _value ? _backgroundOn : _backgroundOff;
            var fromColor = _value ? _backgroundOff : _backgroundOn;
            var targetPosition = _value ? _rightNobPosition : _leftnobPosition;
            var fromPosition = _value ? _leftnobPosition : _rightNobPosition;

            _background.color = Color.Lerp(fromColor, targetColor, lerp);
            _nob.transform.localPosition = Vector3.Lerp(fromPosition, targetPosition, lerp);

            if (_timeSinceToggle <= _lastTransitionDuration)
                _timeSinceToggle += deltaTime;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Toggle();
            Sounds2D.Play(new AudioSession(_value ? _enableSound : _disableSound).WithPitch(1, 0.1f));
        }
    }
}
