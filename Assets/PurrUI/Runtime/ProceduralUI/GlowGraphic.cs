using UnityEngine;
using UnityEngine.UI;

namespace PurrNet.UI
{
#if UNITY_6000_0_OR_NEWER
    [AddComponentMenu("UI (Canvas)/PurrUI/Glow Graphic")]
#else
    [AddComponentMenu("UI/PurrUI/Glow Graphic")]
#endif
    public class GlowGraphic : SignedDistanceFieldGraphic, ILayoutIgnorer
    {
        const string GLOW_SHADER_NAME = "Hidden/PurrUI/GlowRenderer";

        [SerializeField, HideInInspector] Shader _glowShader;

        [SerializeField] RectangleGraphic _source;
        [SerializeField] float _extraSize;

        [SerializeField] bool _useMaxRoundness;
        [SerializeField] bool _uniformRoundness;
        [SerializeField] Vector4 _roundnessInPixels;

        [SerializeField] Color _glowColor = Color.white;
        [SerializeField] GradientType _glowGradientType;
        [SerializeField] Color _glowGradientColor = Color.white;
        [SerializeField, Range(2, 20)] int _glowGradientQuality = 8;
        [SerializeField] RadialMode _glowRadialMode;
        [SerializeField, Min(0.01f)] float _glowGradientSize = 1f;
        [SerializeField] Gradient _glowGradient;
        [SerializeField, Range(0f, 360f)] float _glowGradientAngle;

        [SerializeField, Min(0f)] float _spread;
        [SerializeField, Min(0f)] float _blur = 10f;
        [SerializeField, Min(0.01f)] float _power = 1f;

        [SerializeField, HideInInspector] GlowModifier _owner;

        internal GlowModifier owner
        {
            get => _owner;
            set => _owner = value;
        }

        static Material _glowMaterial;

        public override Material defaultMaterial
        {
            get
            {
                if (_glowMaterial == null && _glowShader != null)
                    _glowMaterial = new Material(_glowShader) { hideFlags = HideFlags.HideAndDontSave };
                return _glowMaterial;
            }
        }

        public RectangleGraphic source
        {
            get => _source;
            set { if (_source == value) { TrackSource(); return; } _source = value; TrackSource(); SetVerticesDirty(); }
        }

        public float extraSize
        {
            get => _extraSize;
            set { if (Mathf.Approximately(_extraSize, value)) return; _extraSize = value; SetVerticesDirty(); }
        }

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

        public Color glowColor
        {
            get => _glowColor;
            set { if (_glowColor == value) return; _glowColor = value; SetVerticesDirty(); }
        }

        public GradientType glowGradientType
        {
            get => _glowGradientType;
            set { if (_glowGradientType == value) return; _glowGradientType = value; SetVerticesDirty(); }
        }

        public Color glowGradientColor
        {
            get => _glowGradientColor;
            set { if (_glowGradientColor == value) return; _glowGradientColor = value; SetVerticesDirty(); }
        }

        public int glowGradientQuality
        {
            get => _glowGradientQuality;
            set { value = Mathf.Clamp(value, 2, 20); if (_glowGradientQuality == value) return; _glowGradientQuality = value; SetVerticesDirty(); }
        }

        public RadialMode glowRadialMode
        {
            get => _glowRadialMode;
            set { if (_glowRadialMode == value) return; _glowRadialMode = value; SetVerticesDirty(); }
        }

        public float glowGradientSize
        {
            get => _glowGradientSize;
            set { value = Mathf.Max(0.01f, value); if (Mathf.Approximately(_glowGradientSize, value)) return; _glowGradientSize = value; SetVerticesDirty(); }
        }

        public Gradient glowGradient
        {
            get => _glowGradient;
            set { _glowGradient = value; SetVerticesDirty(); }
        }

        public float glowGradientAngle
        {
            get => _glowGradientAngle;
            set { if (Mathf.Approximately(_glowGradientAngle, value)) return; _glowGradientAngle = value; SetVerticesDirty(); }
        }

        public float spread
        {
            get => _spread;
            set { value = Mathf.Max(0f, value); if (Mathf.Approximately(_spread, value)) return; _spread = value; SetVerticesDirty(); }
        }

        public float blur
        {
            get => _blur;
            set { value = Mathf.Max(0f, value); if (Mathf.Approximately(_blur, value)) return; _blur = value; SetVerticesDirty(); }
        }

        public float power
        {
            get => _power;
            set { value = Mathf.Max(0.01f, value); if (Mathf.Approximately(_power, value)) return; _power = value; SetVerticesDirty(); }
        }

        RectangleGraphic _trackedSource;

        void TrackSource()
        {
            if (_trackedSource == _source)
                return;

            if (_trackedSource)
                _trackedSource.UnregisterDirtyVerticesCallback(OnSourceChanged);

            _trackedSource = _source;

            if (_trackedSource)
                _trackedSource.RegisterDirtyVerticesCallback(OnSourceChanged);
        }

        void OnSourceChanged()
        {
            SetVerticesDirty();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            TrackSource();
        }

        protected override void OnDisable()
        {
            if (_trackedSource)
                _trackedSource.UnregisterDirtyVerticesCallback(OnSourceChanged);
            _trackedSource = null;
            base.OnDisable();
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            _glowShader = Shader.Find(GLOW_SHADER_NAME);
        }

        protected override void OnValidate()
        {
            if (!_glowShader)
                _glowShader = Shader.Find(GLOW_SHADER_NAME);
            base.OnValidate();
            TrackSource();
        }
#endif

        protected override Vector4 GetRoundness()
        {
            if (_source)
            {
                float sw = _source.rectTransform.rect.width + _extraSize * 2f;
                float sh = _source.rectTransform.rect.height + _extraSize * 2f;
                return _source.ResolveRoundness(sw, sh);
            }

            float width = rectTransform.rect.width;
            float height = rectTransform.rect.height;
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

        void GetEffectiveDimensions(out float width, out float height)
        {
            if (_source)
            {
                width = _source.rectTransform.rect.width + _extraSize * 2f;
                height = _source.rectTransform.rect.height + _extraSize * 2f;
            }
            else
            {
                width = rectTransform.rect.width + _extraSize * 2f;
                height = rectTransform.rect.height + _extraSize * 2f;
            }
        }

        void GetEffectiveFrame(out bool nf, out float fw, out FramePlacement pl)
        {
            if (_source)
            {
                nf = _source.noFill;
                fw = _source.frameWidth;
                pl = _source.framePlacement;
            }
            else
            {
                nf = noFill;
                fw = frameWidth;
                pl = framePlacement;
            }
        }

        void GetFrameEncoding(out float encodedFrame, out float outwardExtension)
        {
            GetEffectiveFrame(out bool nf, out float fw, out FramePlacement pl);
            if (!nf)
            {
                encodedFrame = 0f;
                outwardExtension = 0f;
                return;
            }
            float fwr = Mathf.Round(fw);
            encodedFrame = -(1f + (int)pl * 4096f + fwr);
            if (pl == FramePlacement.Center) outwardExtension = fwr * 0.5f;
            else if (pl == FramePlacement.Outside) outwardExtension = fwr;
            else outwardExtension = 0f;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            float rectW = rectTransform.rect.width;
            float rectH = rectTransform.rect.height;

            if (rectW <= 0f || rectH <= 0f)
                return;

            GetEffectiveDimensions(out float width, out float height);

            if (_glowGradientType == GradientType.Radial ||
                _glowGradientType == GradientType.Gradient)
            {
                PopulateGlowSubdivided(vh, width, height);
                return;
            }

            PopulateGlowQuad(vh, width, height);
        }

        void AddGlowVert(VertexHelper vh, Color col, Vector3 pos, Vector4 uv0, Vector4 roundness, float encodedFrame)
        {
            var vertex = UIVertex.simpleVert;
            vertex.color = col;
            vertex.position = pos;
            vertex.uv0 = uv0;
            vertex.uv1 = roundness;
            vertex.uv2 = new Vector4(_spread, _blur, _power, encodedFrame);
            vh.AddVert(vertex);
        }

        void PopulateGlowQuad(VertexHelper vh, float width, float height)
        {
            GetFrameEncoding(out float encodedFrame, out float frameExt);
            float margin = _spread + _blur + frameExt + 1f;

            float rectW = rectTransform.rect.width;
            float rectH = rectTransform.rect.height;

            float offX = (rectW - width) * 0.5f;
            float offY = (rectH - height) * 0.5f;

            var pivot = new Vector3(
                rectTransform.pivot.x * rectW,
                rectTransform.pivot.y * rectH, 0);

            var roundness = GetRoundness();

            Color colorA = _glowColor * color;
            Color colorB = _glowGradientType != GradientType.None
                ? _glowGradientColor * color
                : colorA;

            Color c0, c1, c2, c3;
            switch (_glowGradientType)
            {
                case GradientType.Vertical:
                    c0 = colorB; c1 = colorA; c2 = colorA; c3 = colorB;
                    break;
                case GradientType.Horizontal:
                    c0 = colorA; c1 = colorA; c2 = colorB; c3 = colorB;
                    break;
                default:
                    c0 = c1 = c2 = c3 = colorA;
                    break;
            }

            AddGlowVert(vh, c0, new Vector3(offX - margin, offY - margin) - pivot,
                new Vector4(0, 0, width, height), roundness, encodedFrame);
            AddGlowVert(vh, c1, new Vector3(offX - margin, offY + height + margin) - pivot,
                new Vector4(0, 1, width, height), roundness, encodedFrame);
            AddGlowVert(vh, c2, new Vector3(offX + width + margin, offY + height + margin) - pivot,
                new Vector4(1, 1, width, height), roundness, encodedFrame);
            AddGlowVert(vh, c3, new Vector3(offX + width + margin, offY - margin) - pivot,
                new Vector4(1, 0, width, height), roundness, encodedFrame);

            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }

        void PopulateGlowSubdivided(VertexHelper vh, float width, float height)
        {
            int n = Mathf.Clamp(_glowGradientQuality, 2, 20);
            GetFrameEncoding(out float encodedFrame, out float frameExt);
            float margin = _spread + _blur + frameExt + 1f;

            float rectW = rectTransform.rect.width;
            float rectH = rectTransform.rect.height;

            float offX = (rectW - width) * 0.5f;
            float offY = (rectH - height) * 0.5f;

            var pivot = new Vector3(
                rectTransform.pivot.x * rectW,
                rectTransform.pivot.y * rectH, 0);

            var roundness = GetRoundness();

            Color masterColor = color;
            Color colorA = _glowColor * masterColor;
            Color colorB = _glowGradientColor * masterColor;

            float angleRad = _glowGradientAngle * Mathf.Deg2Rad;
            var gradDir = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));

            // Hoist invariants out of the loop
            var glowParams = new Vector4(_spread, _blur, _power, encodedFrame);
            float posMinX = offX - margin;
            float posRangeX = width + margin * 2f;
            float posMinY = offY - margin;
            float posRangeY = height + margin * 2f;

            var vertex = UIVertex.simpleVert;
            vertex.uv1 = roundness;
            vertex.uv2 = glowParams;

            int cols = n + 1;
            for (int y = 0; y <= n; y++)
            {
                float fy = (float)y / n;
                float posY = posMinY + posRangeY * fy - pivot.y;

                for (int x = 0; x <= n; x++)
                {
                    float fx = (float)x / n;

                    vertex.position = new Vector3(posMinX + posRangeX * fx - pivot.x, posY, 0);
                    vertex.uv0 = new Vector4(fx, fy, width, height);

                    switch (_glowGradientType)
                    {
                        case GradientType.Radial:
                        {
                            float t;
                            if (_glowRadialMode == RadialMode.Ellipse)
                            {
                                float dx = fx - 0.5f;
                                float dy = fy - 0.5f;
                                t = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                            }
                            else
                            {
                                float refDim = _glowRadialMode == RadialMode.CircleCover
                                    ? Mathf.Max(width, height)
                                    : Mathf.Min(width, height);
                                float dx = (fx - 0.5f) * width;
                                float dy = (fy - 0.5f) * height;
                                t = Mathf.Sqrt(dx * dx + dy * dy) / (refDim * 0.5f);
                            }
                            vertex.color = Color.Lerp(colorA, colorB, Mathf.Clamp01(t / _glowGradientSize));
                            break;
                        }
                        case GradientType.Gradient:
                        {
                            float t = Mathf.Clamp01(
                                (fx - 0.5f) * gradDir.x + (fy - 0.5f) * gradDir.y + 0.5f);
                            vertex.color = (_glowGradient != null ? _glowGradient.Evaluate(t) : Color.white)
                                  * masterColor;
                            break;
                        }
                        default:
                            vertex.color = colorA;
                            break;
                    }

                    vh.AddVert(vertex);
                }
            }

            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    int i = y * cols + x;
                    vh.AddTriangle(i, i + cols, i + cols + 1);
                    vh.AddTriangle(i + cols + 1, i + 1, i);
                }
            }
        }

        public bool ignoreLayout => true;
    }
}
