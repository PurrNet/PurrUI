using PurrNet.UI;
using UnityEditor;
using UnityEngine;

namespace PurrNet.Editor.UI
{
    [CustomEditor(typeof(RectangleGraphic))]
    public class RectangleGraphicEditor : UnityEditor.Editor
    {
        // Shape
        SerializedProperty _roundnessRole;
        SerializedProperty _useMaxRoundness;
        SerializedProperty _uniformRoundness;
        SerializedProperty _roundnessInPixels;

        // Graphic
        SerializedProperty _texture;
        SerializedProperty _sprite;
        SerializedProperty _pixelsPerUnitMultiplier;
        SerializedProperty _imageType;
        SerializedProperty _imageSource;
        SerializedProperty _fillCenter;
        SerializedProperty _graphicColor;
        SerializedProperty _gradientType;
        SerializedProperty _gradientColor;
        SerializedProperty _gradientQuality;
        SerializedProperty _radialMode;
        SerializedProperty _gradientSize;
        SerializedProperty _gradient;
        SerializedProperty _gradientAngle;

        // Frame
        SerializedProperty _noFill;
        SerializedProperty _frameWidth;
        SerializedProperty _framePlacement;

        // Outline
        SerializedProperty _outlineSize;
        SerializedProperty _outlineColor;

        // Shadow
        SerializedProperty _shadowSize;
        SerializedProperty _shadowBlur;
        SerializedProperty _shadowPower;
        SerializedProperty _shadowColor;

        // Emboss
        SerializedProperty _embossSize;
        SerializedProperty _embossAngle;
        SerializedProperty _embossStrength;
        SerializedProperty _embossHighlightColor;
        SerializedProperty _embossShadowColor;

        // Built-in Graphic
        SerializedProperty _raycastTarget;
        SerializedProperty _raycastPadding;
        SerializedProperty _maskable;

        void OnEnable()
        {
            _roundnessRole = serializedObject.FindProperty("_roundnessRole");
            _useMaxRoundness = serializedObject.FindProperty("_useMaxRoundness");
            _uniformRoundness = serializedObject.FindProperty("_uniformRoundness");
            _roundnessInPixels = serializedObject.FindProperty("_roundnessInPixels");

            _texture = serializedObject.FindProperty("_texture");
            _sprite = serializedObject.FindProperty("_sprite");
            _pixelsPerUnitMultiplier = serializedObject.FindProperty("_pixelsPerUnitMultiplier");
            _imageType = serializedObject.FindProperty("_imageType");
            _imageSource = serializedObject.FindProperty("_imageSource");
            _fillCenter = serializedObject.FindProperty("_fillCenter");
            _graphicColor = serializedObject.FindProperty("_graphicColor");
            _gradientType = serializedObject.FindProperty("_gradientType");
            _gradientColor = serializedObject.FindProperty("_gradientColor");
            _gradientQuality = serializedObject.FindProperty("_gradientQuality");
            _radialMode = serializedObject.FindProperty("_radialMode");
            _gradientSize = serializedObject.FindProperty("_gradientSize");
            _gradient = serializedObject.FindProperty("_gradient");
            _gradientAngle = serializedObject.FindProperty("_gradientAngle");

            _noFill = serializedObject.FindProperty("_noFill");
            _frameWidth = serializedObject.FindProperty("_frameWidth");
            _framePlacement = serializedObject.FindProperty("_framePlacement");

            _outlineSize = serializedObject.FindProperty("_outlineSize");
            _outlineColor = serializedObject.FindProperty("_outlineColor");

            _shadowSize = serializedObject.FindProperty("_shadowSize");
            _shadowBlur = serializedObject.FindProperty("_shadowBlur");
            _shadowPower = serializedObject.FindProperty("_shadowPower");
            _shadowColor = serializedObject.FindProperty("_shadowColor");

            _embossSize = serializedObject.FindProperty("_embossSize");
            _embossAngle = serializedObject.FindProperty("_embossAngle");
            _embossStrength = serializedObject.FindProperty("_embossStrength");
            _embossHighlightColor = serializedObject.FindProperty("_embossHighlightColor");
            _embossShadowColor = serializedObject.FindProperty("_embossShadowColor");

            _raycastTarget = serializedObject.FindProperty("m_RaycastTarget");
            _raycastPadding = serializedObject.FindProperty("m_RaycastPadding");
            _maskable = serializedObject.FindProperty("m_Maskable");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Image
            BeginSection("Image");
            DrawImageSection();
            EndSection();

            // Color
            BeginSection("Color");
            EditorGUILayout.PropertyField(_graphicColor, new GUIContent("Color"));
            EditorGUILayout.PropertyField(_gradientType, new GUIContent("Gradient"));

            var gt = (GradientType)_gradientType.enumValueIndex;

            if (gt == GradientType.Vertical || gt == GradientType.Horizontal ||
                gt == GradientType.Radial)
                EditorGUILayout.PropertyField(_gradientColor, new GUIContent("Gradient Color"));

            if (gt == GradientType.Radial || gt == GradientType.Gradient)
                EditorGUILayout.PropertyField(_gradientQuality, new GUIContent("Quality"));

            if (gt == GradientType.Radial)
            {
                EditorGUILayout.PropertyField(_radialMode, new GUIContent("Mode"));
                EditorGUILayout.PropertyField(_gradientSize, new GUIContent("Size"));
            }

            if (gt == GradientType.Gradient)
            {
                EditorGUILayout.PropertyField(_gradient, new GUIContent("Gradient"));
                EditorGUILayout.PropertyField(_gradientAngle, new GUIContent("Angle"));
            }

            EndSection();

            // Shape
            BeginSection("Shape");
            EditorGUILayout.PropertyField(_roundnessRole, new GUIContent("Roundness"));

            if ((RoundnessRole)_roundnessRole.enumValueIndex == RoundnessRole.Custom)
            {
                EditorGUILayout.PropertyField(_useMaxRoundness);

                if (!_useMaxRoundness.boolValue)
                {
                    EditorGUILayout.PropertyField(_uniformRoundness);

                    if (_uniformRoundness.boolValue)
                    {
                        var v = _roundnessInPixels.vector4Value;
                        float val = EditorGUILayout.FloatField("Roundness", v.x);
                        if (!Mathf.Approximately(val, v.x))
                            _roundnessInPixels.vector4Value = new Vector4(val, val, val, val);
                    }
                    else
                    {
                        var v = _roundnessInPixels.vector4Value;
                        v.z = EditorGUILayout.FloatField("Top Left", v.z);
                        v.x = EditorGUILayout.FloatField("Top Right", v.x);
                        v.y = EditorGUILayout.FloatField("Bottom Right", v.y);
                        v.w = EditorGUILayout.FloatField("Bottom Left", v.w);
                        _roundnessInPixels.vector4Value = v;
                    }
                }
            }
            else
            {
                var graphic = (RectangleGraphic)target;
                var palette = graphic.GetComponentInParent<IPaletteProvider>(true)?.palette;
                EditorGUILayout.HelpBox(palette
                    ? "Uses the " + _roundnessRole.enumDisplayNames[_roundnessRole.enumValueIndex] + " roundness from " + palette.name + "."
                    : "Uses the nearest palette's default styles. Local corners are retained as the fallback when no palette is available.",
                    MessageType.Info);
            }

            EditorGUILayout.PropertyField(_noFill, new GUIContent("No Fill"));

            if (_noFill.boolValue)
            {
                EditorGUILayout.PropertyField(_frameWidth, new GUIContent("Frame Width"));
                EditorGUILayout.PropertyField(_framePlacement, new GUIContent("Placement"));
            }

            EndSection();

            // Outline
            BeginSection("Outline");
            FloatWithColorSwatch(_outlineSize, _outlineColor, "Size");
            EndSection();

            // Shadow
            BeginSection("Shadow");
            FloatWithColorSwatch(_shadowSize, _shadowColor, "Size");

            if (_shadowSize.floatValue > 0f)
            {
                EditorGUILayout.PropertyField(_shadowBlur, new GUIContent("Blur"));
                EditorGUILayout.PropertyField(_shadowPower, new GUIContent("Power"));
            }

            EndSection();

            // Emboss
            BeginSection("Emboss");
            EditorGUILayout.PropertyField(_embossSize, new GUIContent("Size"));

            if (_embossSize.floatValue > 0f)
            {
                EditorGUILayout.PropertyField(_embossAngle, new GUIContent("Angle"));
                EditorGUILayout.PropertyField(_embossStrength, new GUIContent("Strength"));
                EditorGUILayout.PropertyField(_embossHighlightColor, new GUIContent("Highlight Color"));
                EditorGUILayout.PropertyField(_embossShadowColor, new GUIContent("Shadow Color"));
            }

            EndSection();

            // Raycast
            BeginSection("Raycast");
            EditorGUILayout.PropertyField(_raycastTarget);
            EditorGUILayout.PropertyField(_raycastPadding);
            EditorGUILayout.PropertyField(_maskable);
            EndSection();

            if (serializedObject.ApplyModifiedProperties())
            {
                foreach (var obj in targets)
                {
                    if (obj is not RectangleGraphic graphic) continue;
                    graphic.Refresh();
                    EditorUtility.SetDirty(graphic);
                }
            }
        }

        static readonly string[] s_ImageSourceLabels = { "Texture", "Sprite" };

        // Source is stored on the component, so the texture and the sprite both stay assigned and
        // only the one that is drawn is shown. The sprite settings mirror UI.Image.
        void DrawImageSection()
        {
            bool mixed = _imageSource.hasMultipleDifferentValues;

            var rect = EditorGUI.PrefixLabel(EditorGUILayout.GetControlRect(), new GUIContent("Source"));
            EditorGUI.BeginChangeCheck();
            int picked = GUI.Toolbar(rect, mixed ? -1 : _imageSource.enumValueIndex, s_ImageSourceLabels, EditorStyles.miniButton);
            if (EditorGUI.EndChangeCheck() && picked >= 0)
            {
                _imageSource.enumValueIndex = picked;
                mixed = false;
            }

            if (mixed)
            {
                EditorGUILayout.LabelField("The selected graphics use different sources. Pick one to apply it to all of them.",
                    EditorStyles.wordWrappedMiniLabel);
                return;
            }

            var source = (ImageSource)_imageSource.enumValueIndex;
            var sprite = _sprite.hasMultipleDifferentValues ? null : _sprite.objectReferenceValue as Sprite;

            if (source == ImageSource.Texture)
            {
                EditorGUILayout.PropertyField(_texture);
                return;
            }

            EditorGUILayout.PropertyField(_sprite, new GUIContent("Source Image"));
            if (!sprite && !_sprite.hasMultipleDifferentValues)
                return;

            EditorGUILayout.PropertyField(_imageType);

            bool sliced = !_imageType.hasMultipleDifferentValues &&
                          _imageType.enumValueIndex == (int)SpriteImageType.Sliced;
            if (sliced)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_fillCenter);
                EditorGUILayout.PropertyField(_pixelsPerUnitMultiplier);
                EditorGUI.indentLevel--;
            }

            // Different sprites are selected: the settings above apply to all, the checks below to none.
            if (!sprite)
                return;

            if (sliced)
            {
                if (sprite.border.sqrMagnitude <= 0f)
                    EditorGUILayout.HelpBox("This sprite has no borders, so it is drawn as Simple. Set borders in the Sprite Editor to slice it.", MessageType.Warning);
                else if (sprite.texture && sprite.texture.mipmapCount > 1)
                    EditorGUILayout.HelpBox("The sprite's texture has mipmaps, which can show a thin seam along the slice borders. Turn off Generate Mipmaps on the texture to avoid it.", MessageType.Warning);
            }

            if (!sprite.texture)
                EditorGUILayout.HelpBox("This sprite's texture is not loaded yet (its atlas may be unavailable), so nothing is drawn from it until it is.", MessageType.Warning);
            else if (sprite.packed && (sprite.packingMode == SpritePackingMode.Tight || sprite.packingRotation != SpritePackingRotation.None))
                EditorGUILayout.HelpBox("This sprite is packed tightly or rotated in its atlas, which rectangle graphics cannot draw correctly. Use rectangle packing without rotation.", MessageType.Warning);
        }

        static GUIStyle _sectionStyle;

        static GUIStyle sectionStyle
        {
            get
            {
                if (_sectionStyle == null)
                {
                    _sectionStyle = new GUIStyle(EditorStyles.helpBox)
                    {
                        padding = new RectOffset(14, 0, 0, 0),
                        margin = new RectOffset(0, 0, 2, 2)
                    };
                }
                return _sectionStyle;
            }
        }

        static void BeginSection(string title)
        {
            EditorGUILayout.BeginVertical(sectionStyle);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }

        static void EndSection()
        {
            EditorGUILayout.EndVertical();
        }

        static void FloatWithColorSwatch(SerializedProperty floatProp, SerializedProperty colorProp, string label)
        {
            const float colorWidth = 50f;
            const float gap = 4f;

            var rect = EditorGUILayout.GetControlRect();
            var floatRect = new Rect(rect.x, rect.y, rect.width - colorWidth - gap, rect.height);
            var colorRect = new Rect(rect.xMax - colorWidth, rect.y, colorWidth, rect.height);

            EditorGUI.PropertyField(floatRect, floatProp, new GUIContent(label));
            EditorGUI.PropertyField(colorRect, colorProp, GUIContent.none);
        }
    }
}
