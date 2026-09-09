using UnityEngine.Scripting.APIUpdating;
using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace PurrNet.UI
{
    [ExecuteAlways]
    [MovedFrom(true, "PurrNet.UI.HeroUI", "PurrUI.HeroUI.Runtime", "ButtonElement")]
    public class ButtonElement : ThemeBinding, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [SerializeField] private RectangleGraphic _graphic;
        [SerializeField] private bool _interactable = true;
        [Space]
        [SerializeField] AnimationCurve _transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private ColorInfo _normalColor = new() { enabled = true, color = ColorType.Accent };
        [SerializeField] private ColorInfo _hoverColor = new() { enabled = true, color = ColorType.Accent };
        [SerializeField] private ColorTone _normalTone;
        [SerializeField] private ColorTone _hoverTone;
        [SerializeField, Range(0f, 1f)] private float _normalOpacity = 1f;
        [SerializeField, Range(0f, 1f)] private float _hoverOpacity = 1f;
        [SerializeField, Min(0f)] private float _normalBrightness = 1f;
        [SerializeField, Min(0f)] private float _hoverBrightness = 1.12f;
        [SerializeField] private bool _useThemeTransition;
        [SerializeField] private bool _useThemeDisabledOpacity;
        [SerializeField] private bool _useThemePressedScale;
        [SerializeField] private float _disabledAlpha = 0.5f;
        [SerializeField] private float _scaleNormal = 1f;
        [SerializeField] private float _scaleClicked = 1.1f;
        [SerializeField, Min(0f)] private float _transitionDuration = 0.2f;
        [Space]
        [SerializeField] private AudioClip[] _clickSounds;

        [UsedImplicitly]
        public UnityEvent onClickUnity;

        public event Action onClick;

        public Color backgroundNormal => _resolvedNormal;
        public Color backgroundHover => _resolvedHover;

        private Color _resolvedNormal;
        private Color _resolvedHover;
        private bool _initialized;
        private float _lastTransitionDuration;

        public bool useThemeTransition
        {
            get => _useThemeTransition;
            set { _useThemeTransition = value; RefreshTransitionDuration(); }
        }
        public bool useThemeDisabledOpacity { get => _useThemeDisabledOpacity; set => _useThemeDisabledOpacity = value; }
        public bool useThemePressedScale { get => _useThemePressedScale; set => _useThemePressedScale = value; }
        public float transitionDuration => _useThemeTransition && palette ? palette.styles.buttonTransitionDuration : Mathf.Max(0f, _transitionDuration);
        public float disabledOpacity => _useThemeDisabledOpacity && palette ? palette.styles.buttonDisabledOpacity : _disabledAlpha;
        public float pressedScale => _useThemePressedScale && palette ? palette.styles.buttonPressedScale : _scaleClicked;

        private void RefreshTransitionDuration()
        {
            EnsureInitialized();
            var duration = transitionDuration;
            if (Mathf.Approximately(duration, _lastTransitionDuration)) return;
            _backgroundTimer = ThemeAnimation.Retime(_backgroundTimer, _lastTransitionDuration, duration);
            _interactableTimer = ThemeAnimation.Retime(_interactableTimer, _lastTransitionDuration, duration);
            _pressTimer = ThemeAnimation.Retime(_pressTimer, _lastTransitionDuration, duration);
            _lastTransitionDuration = duration;
        }

        public void SetColors(ColorInfo normal, ColorInfo hover,
            ColorTone? normalTone = null, ColorTone? hoverTone = null)
        {
            _normalColor = normal;
            _hoverColor = hover;
            _normalTone = normalTone.GetValueOrDefault();
            _hoverTone = hoverTone.GetValueOrDefault();
            // A dynamic state may use a different role than the prefab originally used.
            _normalBrightness = 1f;
            _hoverBrightness = normalTone.HasValue || hoverTone.HasValue ? 1f : 1.12f;
            ApplyPalette();
        }

        protected override void ApplyPalette()
        {
            RefreshTransitionDuration();
            if (!palette)
                return;

            if (_normalColor.enabled)
                _resolvedNormal = ResolveColor(_normalColor, _normalTone, _normalBrightness, _normalOpacity);
            if (_hoverColor.enabled)
                _resolvedHover = ResolveColor(_hoverColor, _hoverTone, _hoverBrightness, _hoverOpacity);

            // Editor previews show the normal fill without animating scale or interaction.
            if (!Application.IsPlaying(gameObject) && _graphic && _normalColor.enabled)
                _graphic.graphicColor = _resolvedNormal;
        }

        private Color ResolveColor(ColorInfo info, ColorTone tone, float brightness, float opacity)
        {
            var color = tone.Apply(info, palette);
            color.r = Mathf.Clamp01(color.r * brightness);
            color.g = Mathf.Clamp01(color.g * brightness);
            color.b = Mathf.Clamp01(color.b * brightness);
            color.a *= opacity;
            return color;
        }

        private float _interactableTimer = 0f;
        private float _backgroundTimer = 0f;
        private float _pressTimer = 0f;
        private bool _isHovering = false;
        private bool _isPressing = false;

        private Transform _trs;

        private bool _parentGroupInteractable = true;

        public bool isInteractable => _parentGroupInteractable && _interactable;

        public bool interactable
        {
            get => _interactable;
            set
            {
                var old = isInteractable;
                _interactable = value;
                var @new = isInteractable;

                if (old != @new)
                    InteractableChanged();
            }
        }

        private readonly List<CanvasGroup> _canvasGroupCache = new ();

        protected void OnCanvasGroupChanged()
        {
            var old = isInteractable;
            _parentGroupInteractable = ParentGroupAllowsInteraction();
            var @new = isInteractable;
            if (old != @new)
                InteractableChanged();
        }

        bool ParentGroupAllowsInteraction()
        {
            var t = transform;
            while (t)
            {
                t.GetComponents(_canvasGroupCache);
                for (var i = 0; i < _canvasGroupCache.Count; i++)
                {
                    if (_canvasGroupCache[i].enabled && !_canvasGroupCache[i].interactable)
                        return false;

                    if (_canvasGroupCache[i].ignoreParentGroups)
                        return true;
                }

                t = t.parent;
            }

            return true;
        }

        private void Reset() => _graphic = GetComponent<RectangleGraphic>();

        private void Awake() => EnsureInitialized();

        private void EnsureInitialized()
        {
            _trs = transform;
            if (_initialized)
                return;

            _initialized = true;
            _lastTransitionDuration = transitionDuration;
            _backgroundTimer = _lastTransitionDuration;
            _interactableTimer = _lastTransitionDuration;
            _pressTimer = _lastTransitionDuration;
            // Keep the authored fill, including ghost-button alpha, until a palette resolves.
            _resolvedNormal = _resolvedHover = _graphic ? _graphic.graphicColor : Color.white;
        }

        protected override void OnEnable()
        {
            EnsureInitialized();
            _parentGroupInteractable = ParentGroupAllowsInteraction();
            base.OnEnable();
        }

        private void InteractableChanged()
        {
            _interactableTimer = 0f;
        }

        protected override void Update()
        {
            base.Update();
            if (!Application.IsPlaying(gameObject))
                return;

            UpdateVisuals(Time.deltaTime);
        }

        private void UpdateVisuals(float deltaTime)
        {
            RefreshTransitionDuration();
            float colorLerp = TransitionProgress(_backgroundTimer);
            float pressLerp = TransitionProgress(_pressTimer);
            float interactableLerp = TransitionProgress(_interactableTimer);

            bool canInteract = isInteractable;
            bool hoveringOrPressed = canInteract && (_isHovering || _isPressing);
            bool pressed = canInteract && _isPressing;

            var targetColor = hoveringOrPressed ? _resolvedHover : _resolvedNormal;
            var initialColor = hoveringOrPressed ? _resolvedNormal : _resolvedHover;

            var targetScale = pressed ? pressedScale : _scaleNormal;
            var initialScale = pressed ? _scaleNormal : pressedScale;

            var targetAlphaMult = canInteract ? 1f : disabledOpacity;
            var initialAlphaMult = canInteract ? disabledOpacity : 1f;

            var color = Color.Lerp(initialColor, targetColor, colorLerp);
            color.a *= Mathf.Lerp(initialAlphaMult, targetAlphaMult, interactableLerp);

            if (_graphic)
                _graphic.graphicColor = color;
            _trs.localScale = Vector3.Lerp(
                new Vector3(initialScale, initialScale, 1),
                new Vector3(targetScale, targetScale, 1), pressLerp);

            _backgroundTimer += deltaTime;
            _interactableTimer += deltaTime;
            _pressTimer += deltaTime;
        }

        private float TransitionProgress(float elapsed)
        {
            if (_lastTransitionDuration <= 0f)
                return 1f;
            var progress = Mathf.Clamp01(elapsed / _lastTransitionDuration);
            return _transitionCurve != null ? _transitionCurve.Evaluate(progress) : progress;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!Application.IsPlaying(gameObject))
                return;

            _isHovering = true;
            if (!_isPressing && isInteractable)
                _backgroundTimer = 0f;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!Application.IsPlaying(gameObject))
                return;

            _isHovering = false;
            if (!_isPressing && isInteractable)
                _backgroundTimer = 0f;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!Application.IsPlaying(gameObject))
                return;

            if (isInteractable)
            {
                _isPressing = true;
                _pressTimer = 0f;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!Application.IsPlaying(gameObject))
                return;

            _isPressing = false;
            if (!_isHovering && isInteractable)
                _backgroundTimer = 0f;

            if (isInteractable)
                _pressTimer = 0f;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!Application.IsPlaying(gameObject))
                return;

            if (!isInteractable)
                return;

            onClickUnity?.Invoke();
            onClick?.Invoke();
            Sounds2D.Play(new AudioSession(_clickSounds).WithPitch(1, 0.1f));
        }
    }
}
