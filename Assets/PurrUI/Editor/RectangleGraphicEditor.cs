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

            // Color
            BeginSection("Color");
            EditorGUILayout.PropertyField(_texture);
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
