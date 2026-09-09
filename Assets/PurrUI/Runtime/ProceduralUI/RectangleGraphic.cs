using UnityEngine;
using UnityEngine.UI;

namespace PurrNet.UI
{
#if UNITY_6000_0_OR_NEWER
    [AddComponentMenu("UI (Canvas)/PurrUI/Rectangle Graphic")]
#else
    [AddComponentMenu("UI/PurrUI/Rectangle Graphic")]
#endif
    public class RectangleGraphic : SignedDistanceFieldGraphic
    {
        [Tooltip("Custom uses this graphic's local corners. Other roles use the nearest palette's default styles.")]
        [SerializeField] RoundnessRole _roundnessRole;
        [SerializeField] bool _useMaxRoundness;
        [SerializeField] bool _uniformRoundness;
        [SerializeField] Vector4 _roundnessInPixels;

        private IPaletteProvider _paletteProvider;
        private bool _providerDirty;
        private bool _deferredStylesDirty;

        public RoundnessRole roundnessRole
        {
            get => _roundnessRole;
            set
            {
                if (_roundnessRole == value) return;
                _roundnessRole = value;
                ResolveProvider();
                SetVerticesDirty();
            }
        }

        public Vector4 resolvedRoundness => GetRoundness();

        public bool useMaxRoundness
        {
            get => _useMaxRoundness;
            set { if (_useMaxRoundness == value) return; _useMaxRoundness = value; SetVerticesDirty(); }
        }

        public bool uniformRoundness
        {
            get => _uniformRoundness;
            set { if (_uniformRoundness == value) return; _uniformRoundness = value; SetVerticesDirty(); }
        }

        public Vector4 roundnessInPixels
        {
            get => _roundnessInPixels;
            set { if (_roundnessInPixels == value) return; _roundnessInPixels = value; SetVerticesDirty(); }
        }

        protected override void OnEnable()
        {
            ResolveProvider();
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            if (_paletteProvider != null)
                _paletteProvider.onColorChange -= OnStylesChanged;
            _paletteProvider = null;
            base.OnDisable();
        }

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();
            ResolveProvider();
        }

        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();
            ResolveProvider();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            // Validation can run while Unity loads objects; resolve hierarchy changes on the main loop.
            _providerDirty = true;
            base.OnValidate();
        }
#endif

        private bool ProviderMissing => _paletteProvider == null ||
            (_paletteProvider is Object providerObject && !providerObject);

        private void Update()
        {
            if (_providerDirty || (_roundnessRole != RoundnessRole.Custom && ProviderMissing))
                ResolveProvider();
            if (_deferredStylesDirty && !CanvasUpdateRegistry.IsRebuildingGraphics())
            {
                _deferredStylesDirty = false;
                SetVerticesDirty();
            }
        }

        private void ResolveProvider() => ResolveProviderInternal(true);

        private void ResolveProviderInternal(bool markDirty)
        {
            var provider = isActiveAndEnabled && _roundnessRole != RoundnessRole.Custom
                ? GetComponentInParent<IPaletteProvider>(true) : null;
            _providerDirty = false;
            if (ReferenceEquals(provider, _paletteProvider))
                return;
            if (_paletteProvider != null)
                _paletteProvider.onColorChange -= OnStylesChanged;
            _paletteProvider = provider;
            if (_paletteProvider != null)
                _paletteProvider.onColorChange += OnStylesChanged;
            if (markDirty)
                OnStylesChanged();
            else
                _deferredStylesDirty = true;
        }

        private void OnStylesChanged()
        {
            // Palette notifications may arrive while a canvas is already generating geometry.
            _deferredStylesDirty = true;
            if (CanvasUpdateRegistry.IsRebuildingGraphics())
                return;
            _deferredStylesDirty = false;
            SetVerticesDirty();
        }

        private bool TryGetThemeRoundness(out CornerRoundness roundness)
        {
            roundness = default;
            if (_roundnessRole == RoundnessRole.Custom)
                return false;
            if (isActiveAndEnabled && (_providerDirty || ProviderMissing))
                ResolveProviderInternal(false);
            // Queries on inactive prefab contents may resolve a theme without subscribing to it.
            var provider = isActiveAndEnabled ? _paletteProvider : GetComponentInParent<IPaletteProvider>(true);
            var theme = provider?.palette;
            if (!theme)
                return false;

            var styles = theme.styles;
            switch (_roundnessRole)
            {
                case RoundnessRole.Button: roundness = styles.buttonRoundness; return true;
                case RoundnessRole.Panel: roundness = styles.panelRoundness; return true;
                case RoundnessRole.Input: roundness = styles.inputRoundness; return true;
                case RoundnessRole.Item: roundness = styles.itemRoundness; return true;
                default: return false;
            }
        }

        protected override Vector4 GetRoundness()
        {
            return ResolveRoundness(rectTransform.rect.width, rectTransform.rect.height);
        }

        /// <summary>Resolves corners for a given size, including expanded geometry such as a glow.</summary>
        public Vector4 ResolveRoundness(float width, float height)
        {
            if (TryGetThemeRoundness(out var roundness))
                return roundness.Resolve(width, height);

            // Preserve authored custom corners exactly, including asymmetric and fully rounded shapes.
            float maxRoundness = Mathf.Min(width, height) * 0.5f;
            if (_useMaxRoundness)
                return new Vector4(maxRoundness, maxRoundness, maxRoundness, maxRoundness);
            if (_uniformRoundness)
            {
                float r = _roundnessInPixels.x;
                return new Vector4(r, r, r, r);
            }
            return _roundnessInPixels;
        }
    }
}
