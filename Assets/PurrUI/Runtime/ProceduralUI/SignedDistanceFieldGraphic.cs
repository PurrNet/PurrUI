using System;
using UnityEngine;
using UnityEngine.UI;

namespace PurrNet.UI
{
    public enum GradientType
    {
        None,
        Vertical,
        Horizontal,
        Radial,
        Gradient
    }

    public enum RadialMode
    {
        CircleCover,
        CircleFit,
        Ellipse
    }

    public enum FramePlacement
    {
        Inside,
        Center,
        Outside
    }

    /// <summary>Which image a graphic draws. Texture is the default, so graphics saved before sprites existed are unchanged.</summary>
    public enum ImageSource
    {
        Texture,
        Sprite
    }

    /// <summary>How a sprite is drawn, matching the Simple and Sliced types of a UI Image.</summary>
    public enum SpriteImageType
    {
        Simple,
        Sliced
    }

    [RequireComponent(typeof(CanvasRenderer))]
    public class SignedDistanceFieldGraphic : MaskableGraphic, IColored
    {
        const string SHADER_NAME = "Hidden/PurrUI/RectangleRenderer";

        [SerializeField, HideInInspector] Shader _shader;

        [SerializeField] Texture _texture;
        [Tooltip("Which of the two images below is drawn. Both stay assigned when you switch.")]
        [SerializeField] ImageSource _imageSource;
        [Tooltip("Drawn when Source is Sprite, using the sprite's atlas rect and nine-slice borders.")]
        [SerializeField] Sprite _sprite;
        [Tooltip("Simple stretches the whole sprite. Sliced keeps its borders and stretches only the centre.")]
        [SerializeField] SpriteImageType _imageType;
        [Tooltip("Sliced only. Turn off to draw just the borders.")]
        [SerializeField] bool _fillCenter = true;
        [Tooltip("Scales the sprite's nine-slice borders, like Pixels Per Unit Multiplier on a UI Image.")]
        [SerializeField, Min(0.01f)] float _pixelsPerUnitMultiplier = 1f;
        [SerializeField] Color _graphicColor = Color.white;
        [SerializeField] GradientType _gradientType;
        [SerializeField] Color _gradientColor = Color.white;
        [SerializeField, Range(2, 20)] int _gradientQuality = 8;
        [SerializeField] RadialMode _radialMode;
        [SerializeField, Min(0.01f)] float _gradientSize = 1f;
        [SerializeField] Gradient _gradient;
        [SerializeField, Range(0f, 360f)] float _gradientAngle;

        [SerializeField] bool _noFill;
        [SerializeField, Min(0f)] float _frameWidth = 5f;
        [SerializeField] FramePlacement _framePlacement;

        [SerializeField, Min(0f)] float _outlineSize;
        [SerializeField] Color _outlineColor = Color.black;

        [SerializeField, Min(0f)] float _shadowSize;
        [SerializeField, Min(0f)] float _shadowBlur;
        [SerializeField, Min(0f)] float _shadowPower = 1f;
        [SerializeField] Color _shadowColor = Color.black;

        [SerializeField, Min(0f)] float _embossSize;
        [SerializeField, Range(0f, 360f)] float _embossAngle = 135f;
        [SerializeField, Range(0f, 1f)] float _embossStrength = 0.5f;
        [SerializeField] Color _embossHighlightColor = Color.white;
        [SerializeField] Color _embossShadowColor = Color.black;

        static Material _sharedMaterial;
        public const AdditionalCanvasShaderChannels RequiredCanvasChannels =
            AdditionalCanvasShaderChannels.TexCoord1
            | AdditionalCanvasShaderChannels.TexCoord2
            | AdditionalCanvasShaderChannels.TexCoord3
            | AdditionalCanvasShaderChannels.Normal
            | AdditionalCanvasShaderChannels.Tangent;
#if UNITY_EDITOR
        // Graphics waiting for an editor rebuild. All of them are refreshed in ONE delayCall followed by
        // ONE Canvas.ForceUpdateCanvases(), instead of a forced rebuild of every canvas per graphic.
        static readonly System.Collections.Generic.HashSet<SignedDistanceFieldGraphic> s_PendingEditorRebuilds =
            new System.Collections.Generic.HashSet<SignedDistanceFieldGraphic>();
        static bool s_EditorFlushQueued;
#endif

        public Texture texture
        {
            get => _texture;
            set { if (_texture == value) return; _texture = value; SetVerticesDirty(); SetMaterialDirty(); }
        }

        /// <summary>
        /// Optional sprite drawn instead of <see cref="texture"/>. Sprites from an atlas use their own
        /// rect, and sprites with borders are nine-sliced.
        /// </summary>
        public Sprite sprite
        {
            get => _sprite;
            set { if (_sprite == value) return; _sprite = value; SetVerticesDirty(); SetMaterialDirty(); }
        }

        /// <summary>
        /// Whether <see cref="texture"/> or <see cref="sprite"/> is drawn. The other one stays assigned,
        /// so assigning a sprite from code also needs this set to <see cref="ImageSource.Sprite"/>.
        /// </summary>
        public ImageSource imageSource
        {
            get => _imageSource;
            set { if (_imageSource == value) return; _imageSource = value; SetVerticesDirty(); SetMaterialDirty(); }
        }

        /// <summary>Whether <see cref="sprite"/> is stretched whole or nine-sliced by its borders.</summary>
        public SpriteImageType imageType
        {
            get => _imageType;
            set { if (_imageType == value) return; _imageType = value; SetMaterialDirty(); }
        }

        /// <summary>For a sliced <see cref="sprite"/>: false draws only its borders.</summary>
        public bool fillCenter
        {
            get => _fillCenter;
            set { if (_fillCenter == value) return; _fillCenter = value; SetMaterialDirty(); }
        }

        /// <summary>Scales the nine-slice borders of <see cref="sprite"/>. Higher values make them thinner.</summary>
        public float pixelsPerUnitMultiplier
        {
            get => _pixelsPerUnitMultiplier;
            set
            {
                value = Mathf.Max(0.01f, value);
                if (Mathf.Approximately(_pixelsPerUnitMultiplier, value)) return;
                _pixelsPerUnitMultiplier = value;
                SetMaterialDirty();
            }
        }

        public Color graphicColor
        {
            get => _graphicColor;
            set { if (_graphicColor == value) return; _graphicColor = value; SetVerticesDirty(); }
        }

        public GradientType gradientType
        {
            get => _gradientType;
            set { if (_gradientType == value) return; _gradientType = value; SetVerticesDirty(); }
        }

        public Color gradientColor
        {
            get => _gradientColor;
            set { if (_gradientColor == value) return; _gradientColor = value; SetVerticesDirty(); }
        }

        public int gradientQuality
        {
            get => _gradientQuality;
            set { value = Mathf.Clamp(value, 2, 20); if (_gradientQuality == value) return; _gradientQuality = value; SetVerticesDirty(); }
        }

        public RadialMode radialMode
        {
            get => _radialMode;
            set { if (_radialMode == value) return; _radialMode = value; SetVerticesDirty(); }
        }

        public float gradientSize
        {
            get => _gradientSize;
            set { value = Mathf.Max(0.01f, value); if (Mathf.Approximately(_gradientSize, value)) return; _gradientSize = value; SetVerticesDirty(); }
        }

        public Gradient gradient
        {
            get => _gradient;
            set { _gradient = value; SetVerticesDirty(); }
        }

        public float gradientAngle
        {
            get => _gradientAngle;
            set { if (Mathf.Approximately(_gradientAngle, value)) return; _gradientAngle = value; SetVerticesDirty(); }
        }

        public bool noFill
        {
            get => _noFill;
            set { if (_noFill == value) return; _noFill = value; SetVerticesDirty(); }
        }

        public float frameWidth
        {
            get => _frameWidth;
            set { value = Mathf.Max(0f, value); if (Mathf.Approximately(_frameWidth, value)) return; _frameWidth = value; SetVerticesDirty(); }
        }

        public FramePlacement framePlacement
        {
            get => _framePlacement;
            set { if (_framePlacement == value) return; _framePlacement = value; SetVerticesDirty(); }
        }

        public float outlineSize
        {
            get => _outlineSize;
            set { if (Mathf.Approximately(_outlineSize, value)) return; _outlineSize = value; SetVerticesDirty(); }
        }

        public Color outlineColor
        {
            get => _outlineColor;
            set { if (_outlineColor == value) return; _outlineColor = value; SetVerticesDirty(); }
        }

        public float shadowSize
        {
            get => _shadowSize;
            set { if (Mathf.Approximately(_shadowSize, value)) return; _shadowSize = value; SetVerticesDirty(); }
        }

        public float shadowBlur
        {
            get => _shadowBlur;
            set { if (Mathf.Approximately(_shadowBlur, value)) return; _shadowBlur = value; SetVerticesDirty(); }
        }

        public float shadowPower
        {
            get => _shadowPower;
            set { if (Mathf.Approximately(_shadowPower, value)) return; _shadowPower = value; SetVerticesDirty(); }
        }

        public Color shadowColor
        {
            get => _shadowColor;
            set { if (_shadowColor == value) return; _shadowColor = value; SetVerticesDirty(); }
        }

        public float embossSize
        {
            get => _embossSize;
            set { if (Mathf.Approximately(_embossSize, value)) return; _embossSize = value; SetVerticesDirty(); }
        }

        public float embossAngle
        {
            get => _embossAngle;
            set { if (Mathf.Approximately(_embossAngle, value)) return; _embossAngle = value; SetVerticesDirty(); }
        }

        public float embossStrength
        {
            get => _embossStrength;
            set { if (Mathf.Approximately(_embossStrength, value)) return; _embossStrength = value; SetVerticesDirty(); }
        }

        public Color embossHighlightColor
        {
            get => _embossHighlightColor;
            set { if (_embossHighlightColor == value) return; _embossHighlightColor = value; SetVerticesDirty(); }
        }

        public Color embossShadowColor
        {
            get => _embossShadowColor;
            set { if (_embossShadowColor == value) return; _embossShadowColor = value; SetVerticesDirty(); }
        }

        float FrameOutwardExtension
        {
            get
            {
                if (!_noFill) return 0f;
                float fw = Mathf.Round(_frameWidth);
                if (_framePlacement == FramePlacement.Center) return fw * 0.5f;
                if (_framePlacement == FramePlacement.Outside) return fw;
                return 0f;
            }
        }

        float extraMargin => _outlineSize + FrameOutwardExtension + _shadowSize + _shadowBlur + 1f;

        // False while the sprite's texture is not available yet (an atlas that has not loaded).
        bool hasUsableSprite => _imageSource == ImageSource.Sprite && _sprite && _sprite.texture;

        public override Texture mainTexture
        {
            get
            {
                if (_imageSource == ImageSource.Sprite)
                    return hasUsableSprite ? _sprite.texture : s_WhiteTexture;
                return _texture ? _texture : s_WhiteTexture;
            }
        }

        public override Material defaultMaterial
        {
            get
            {
                if (_sharedMaterial == null && _shader != null)
                    _sharedMaterial = new Material(_shader) { hideFlags = HideFlags.HideAndDontSave };

                var spriteMaterial = ResolveSpriteMaterial();
                return spriteMaterial ? spriteMaterial : _sharedMaterial;
            }
        }

        // The vertex channels are full, so a sprite's atlas rect and borders travel in a material.
        // Materials are shared by every graphic with the same values, which keeps them batching, and
        // are never modified after creation because masking keeps its own copies of them.
        readonly struct SpriteMaterialKey : IEquatable<SpriteMaterialKey>
        {
            public readonly Shader shader;
            public readonly Vector4 outerUV, innerUV, border, padding;
            public readonly bool relativePadding, fillCenter;

            public SpriteMaterialKey(Shader shader, Vector4 outerUV, Vector4 innerUV, Vector4 border, Vector4 padding, bool relativePadding, bool fillCenter)
            {
                this.fillCenter = fillCenter;
                this.shader = shader;
                this.outerUV = outerUV;
                this.innerUV = innerUV;
                this.border = border;
                this.padding = padding;
                this.relativePadding = relativePadding;
            }

            public bool Equals(SpriteMaterialKey other) =>
                shader == other.shader && outerUV.Equals(other.outerUV) && innerUV.Equals(other.innerUV) &&
                border.Equals(other.border) && padding.Equals(other.padding) && relativePadding == other.relativePadding &&
                fillCenter == other.fillCenter;

            public override bool Equals(object obj) => obj is SpriteMaterialKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = outerUV.GetHashCode();
                    hash = hash * 397 ^ innerUV.GetHashCode();
                    hash = hash * 397 ^ border.GetHashCode();
                    hash = hash * 397 ^ padding.GetHashCode();
                    hash = hash * 397 ^ (fillCenter ? 1 : 0);
                    return relativePadding ? ~hash : hash;
                }
            }
        }

        sealed class SpriteMaterialEntry
        {
            public Material material;
            public int users;
        }

        static readonly System.Collections.Generic.Dictionary<SpriteMaterialKey, SpriteMaterialEntry> s_SpriteMaterials =
            new System.Collections.Generic.Dictionary<SpriteMaterialKey, SpriteMaterialEntry>();

        static readonly int s_SpriteOuterUV = Shader.PropertyToID("_SpriteOuterUV");
        static readonly int s_SpriteInnerUV = Shader.PropertyToID("_SpriteInnerUV");
        static readonly int s_SpriteBorder = Shader.PropertyToID("_SpriteBorder");
        static readonly int s_SpritePadding = Shader.PropertyToID("_SpritePadding");
        static readonly int s_SpritePaddingRelative = Shader.PropertyToID("_SpritePaddingRelative");
        static readonly int s_SpriteFillCenter = Shader.PropertyToID("_SpriteFillCenter");

        SpriteMaterialKey _spriteMaterialKey;
        SpriteMaterialEntry _spriteMaterialEntry;

        Material ResolveSpriteMaterial()
        {
            // Released on disable, so only an enabled graphic may hold a share.
            if (!hasUsableSprite || !_shader || !isActiveAndEnabled)
            {
                ReleaseSpriteMaterial();
                return null;
            }

            var key = BuildSpriteMaterialKey();
            if (_spriteMaterialEntry != null && _spriteMaterialEntry.material && _spriteMaterialKey.Equals(key))
                return _spriteMaterialEntry.material;

            ReleaseSpriteMaterial();

            if (!s_SpriteMaterials.TryGetValue(key, out var entry) || !entry.material)
            {
                var created = new Material(_shader) { hideFlags = HideFlags.HideAndDontSave };
                created.EnableKeyword("PURR_SPRITE");
                created.SetVector(s_SpriteOuterUV, key.outerUV);
                created.SetVector(s_SpriteInnerUV, key.innerUV);
                created.SetVector(s_SpriteBorder, key.border);
                created.SetVector(s_SpritePadding, key.padding);
                created.SetFloat(s_SpritePaddingRelative, key.relativePadding ? 1f : 0f);
                created.SetFloat(s_SpriteFillCenter, key.fillCenter ? 1f : 0f);
                entry = new SpriteMaterialEntry { material = created };
                s_SpriteMaterials[key] = entry;
            }

            entry.users++;
            _spriteMaterialKey = key;
            _spriteMaterialEntry = entry;
            return entry.material;
        }

        SpriteMaterialKey BuildSpriteMaterialKey()
        {
            var outer = UnityEngine.Sprites.DataUtility.GetOuterUV(_sprite);
            var padding = UnityEngine.Sprites.DataUtility.GetPadding(_sprite);
            var border = _sprite.border;

            if (_imageType != SpriteImageType.Sliced || border.sqrMagnitude <= 0f)
            {
                // Simple, or nothing to slice: the sprite stretches over the rect, so its padding scales with it.
                var size = _sprite.rect.size;
                padding = new Vector4(padding.x / size.x, padding.y / size.y, padding.z / size.x, padding.w / size.y);
                return new SpriteMaterialKey(_shader, outer, outer, Vector4.zero, padding, true, true);
            }

            // Same border scale as UI.Image: sprite pixels to canvas units.
            float referencePixelsPerUnit = canvas ? canvas.referencePixelsPerUnit : 100f;
            float pixelsPerUnit = _sprite.pixelsPerUnit / referencePixelsPerUnit * Mathf.Max(0.01f, _pixelsPerUnitMultiplier);
            float toRect = pixelsPerUnit > 0f ? 1f / pixelsPerUnit : 1f;

            var inner = UnityEngine.Sprites.DataUtility.GetInnerUV(_sprite);
            return new SpriteMaterialKey(_shader, outer, inner, border * toRect, padding * toRect, false, _fillCenter);
        }

        void ReleaseSpriteMaterial()
        {
            var entry = _spriteMaterialEntry;
            if (entry == null)
                return;

            _spriteMaterialEntry = null;
            if (--entry.users > 0)
                return;

            if (s_SpriteMaterials.TryGetValue(_spriteMaterialKey, out var current) && current == entry)
                s_SpriteMaterials.Remove(_spriteMaterialKey);

            if (!entry.material)
                return;
            if (Application.isPlaying)
                Destroy(entry.material);
            else
                DestroyImmediate(entry.material);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Refresh();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            ReleaseSpriteMaterial();
        }

        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();
            Refresh();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            Refresh();
        }

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();
            Refresh();
        }

        protected override void OnDidApplyAnimationProperties()
        {
            base.OnDidApplyAnimationProperties();
            Refresh();
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            _shader = Shader.Find(SHADER_NAME);
            EnsureAdditionalCanvasChannels();
        }

        protected override void OnValidate()
        {
            if (!_shader)
                _shader = Shader.Find(SHADER_NAME);
            base.OnValidate();
            Refresh();
        }
#endif

        public void Refresh()
        {
            EnsureAdditionalCanvasChannels();
            SetAllDirty();
#if UNITY_EDITOR
            QueueEditorRebuild();
#endif
        }

#if UNITY_EDITOR
        void QueueEditorRebuild()
        {
            if (Application.isPlaying)
                return;

            s_PendingEditorRebuilds.Add(this);
            if (s_EditorFlushQueued)
                return;

            s_EditorFlushQueued = true;
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
            UnityEditor.EditorApplication.delayCall += FlushEditorRebuilds;
        }

        // Canvas.ForceUpdateCanvases() rebuilds every canvas in the editor, so calling it once per
        // queued graphic made opening a prefab cost (graphic count x whole-canvas rebuild). With a few
        // dozen graphics on a heavy canvas that stalled the editor for minutes. One flush per tick keeps
        // the immediate refresh while making the cost independent of how many graphics asked for it.
        static void FlushEditorRebuilds()
        {
            s_EditorFlushQueued = false;
            if (s_PendingEditorRebuilds.Count == 0)
                return;

            // Copy first: refreshing can queue graphics again, and those wait for the next flush.
            var pending = new System.Collections.Generic.List<SignedDistanceFieldGraphic>(s_PendingEditorRebuilds);
            s_PendingEditorRebuilds.Clear();

            bool anyRefreshed = false;
            foreach (var graphic in pending)
            {
                if (!graphic || !graphic.isActiveAndEnabled)
                    continue;

                graphic.EnsureAdditionalCanvasChannels();
                graphic.SetAllDirty();
                anyRefreshed = true;
            }

            if (!anyRefreshed)
                return;

            Canvas.ForceUpdateCanvases();
            UnityEditor.SceneView.RepaintAll();
        }
#endif

        void EnsureAdditionalCanvasChannels()
        {
            if (!canvas) return;
            var rootCanvas = canvas.rootCanvas;
            rootCanvas.additionalShaderChannels |= RequiredCanvasChannels;
        }

        protected virtual Vector4 GetRoundness()
        {
            return Vector4.zero;
        }

        public static Vector2 PackColor(Color color)
        {
            Color32 c = color;
            return new Vector2(c.r + c.g * 256f, c.b + c.a * 256f);
        }

        UIVertex BuildBaseVertex()
        {
            var packedOutline = PackColor(_outlineColor);
            var packedShadow = PackColor(_shadowColor);

            var roundness = GetRoundness();

            // Encode frame info into existing channels:
            //   uv2.x: negative signals frame mode. abs = 1 + placement * 4096 + outlineSize
            //   uv2.w: shadowPower + round(frameWidth) * 256
            float frameWidthRounded = _noFill ? Mathf.Round(_frameWidth) : 0f;

            float encodedOutline = _noFill
                ? -(1f + (int)_framePlacement * 4096f + _outlineSize)
                : _outlineSize;
            float encodedShadowPow = _shadowPower + frameWidthRounded * 256f;

            var effects = new Vector4(encodedOutline, _shadowSize, _shadowBlur, encodedShadowPow);
            var colors = new Vector4(
                packedOutline.x, packedOutline.y,
                packedShadow.x, packedShadow.y);

            var vertex = UIVertex.simpleVert;
            vertex.normal = new Vector3(_embossSize, _embossAngle * Mathf.Deg2Rad, _embossStrength);
            var packedHighlight = PackColor(_embossHighlightColor);
            var packedShadowEmb = PackColor(_embossShadowColor);
            vertex.tangent = new Vector4(packedHighlight.x, packedHighlight.y, packedShadowEmb.x, packedShadowEmb.y);
            vertex.uv1 = roundness;
            vertex.uv2 = effects;
            vertex.uv3 = colors;

            return vertex;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            float width = rectTransform.rect.width;
            float height = rectTransform.rect.height;

            if (width <= 0f || height <= 0f)
                return;

            if (_gradientType == GradientType.Radial ||
                _gradientType == GradientType.Gradient)
            {
                PopulateSubdividedMesh(vh, width, height);
                return;
            }

            float margin = extraMargin;

            var pivot = new Vector3(
                rectTransform.pivot.x * width,
                rectTransform.pivot.y * height, 0);

            Color colorA = _graphicColor * color;
            Color colorB = _gradientType != GradientType.None ? _gradientColor * color : colorA;

            Color c0, c1, c2, c3;
            switch (_gradientType)
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

            var vertex = BuildBaseVertex();

            vertex.color = c0;
            vertex.position = new Vector3(-margin, -margin) - pivot;
            vertex.uv0 = new Vector4(0, 0, width, height);
            vh.AddVert(vertex);

            vertex.color = c1;
            vertex.position = new Vector3(-margin, height + margin) - pivot;
            vertex.uv0 = new Vector4(0, 1, width, height);
            vh.AddVert(vertex);

            vertex.color = c2;
            vertex.position = new Vector3(width + margin, height + margin) - pivot;
            vertex.uv0 = new Vector4(1, 1, width, height);
            vh.AddVert(vertex);

            vertex.color = c3;
            vertex.position = new Vector3(width + margin, -margin) - pivot;
            vertex.uv0 = new Vector4(1, 0, width, height);
            vh.AddVert(vertex);

            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }

        void PopulateSubdividedMesh(VertexHelper vh, float width, float height)
        {
            int n = Mathf.Clamp(_gradientQuality, 2, 20);
            float margin = extraMargin;

            var pivot = new Vector3(
                rectTransform.pivot.x * width,
                rectTransform.pivot.y * height, 0);

            Color masterColor = color;
            Color colorA = _graphicColor * masterColor;
            Color colorB = _gradientColor * masterColor;

            float angleRad = _gradientAngle * Mathf.Deg2Rad;
            var gradDir = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));

            var vertex = BuildBaseVertex();

            // Hoist invariants out of the loop
            float posMinX = -margin;
            float posRangeX = width + margin * 2f;
            float posMinY = -margin;
            float posRangeY = height + margin * 2f;

            // Build (n+1) x (n+1) vertex grid
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

                    // Compute vertex color based on gradient type
                    switch (_gradientType)
                    {
                        case GradientType.Radial:
                        {
                            float t;
                            if (_radialMode == RadialMode.Ellipse)
                            {
                                float dx = fx - 0.5f;
                                float dy = fy - 0.5f;
                                t = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                            }
                            else
                            {
                                float refDim = _radialMode == RadialMode.CircleCover
                                    ? Mathf.Max(width, height)
                                    : Mathf.Min(width, height);
                                float dx = (fx - 0.5f) * width;
                                float dy = (fy - 0.5f) * height;
                                t = Mathf.Sqrt(dx * dx + dy * dy) / (refDim * 0.5f);
                            }
                            vertex.color = Color.Lerp(colorA, colorB, Mathf.Clamp01(t / _gradientSize));
                            break;
                        }
                        case GradientType.Gradient:
                        {
                            float t = Mathf.Clamp01((fx - 0.5f) * gradDir.x + (fy - 0.5f) * gradDir.y + 0.5f);
                            vertex.color = (_gradient != null ? _gradient.Evaluate(t) : Color.white) * masterColor;
                            break;
                        }
                    }

                    vh.AddVert(vertex);
                }
            }

            // Triangles: standard grid
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

        private static readonly string[] _keys = {
            "Graphic",
            "Gradient",
            "Outline",
            "Shadow",
            "Emboss Highlight",
            "Emboss Shadow"
        };

        public string[] keys => _keys;

        public Color GetColor(int keyIndex)
        {
            return keyIndex switch
            {
                0 => graphicColor,
                1 => gradientColor,
                2 => outlineColor,
                3 => shadowColor,
                4 => embossHighlightColor,
                5 => embossShadowColor,
                _ => throw new ArgumentOutOfRangeException(nameof(keyIndex), keyIndex, $"Key index must be between 0 and {_keys.Length - 1}.")
            };
        }

        public void SetColor(int keyIndex, Color color)
        {
            switch (keyIndex)
            {
                case 0: graphicColor = color; break;
                case 1: gradientColor = color; break;
                case 2: outlineColor = color; break;
                case 3: shadowColor = color; break;
                case 4: embossHighlightColor = color; break;
                case 5: embossShadowColor = color; break;
            }
        }
    }
}
