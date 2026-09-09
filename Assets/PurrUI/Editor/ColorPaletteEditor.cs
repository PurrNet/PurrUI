using PurrNet.UI;
using UnityEditor;
using UnityEngine;

namespace PurrNet.Editor.UI
{
    [CustomEditor(typeof(ColorPalette))]
    [CanEditMultipleObjects]
    public class ColorPaletteEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (targets.Length != 1)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            var palette = (ColorPalette)target;
            var rect = GUILayoutUtility.GetRect(100f, 180f, GUILayout.ExpandWidth(true));
            DrawPreview(rect, palette);
        }

        static void DrawPreview(Rect rect, ColorPalette palette)
        {
            EditorGUI.DrawRect(rect, palette.GetColor(ColorType.Background));

            const float outerPad = 12f;
            var card = new Rect(rect.x + outerPad, rect.y + outerPad,
                                rect.width - outerPad * 2f, rect.height - outerPad * 2f);
            DrawRoundedRect(card, palette.GetColor(ColorType.Surface), palette.styles.panelRoundness);

            const float innerPad = 10f;
            float x = card.x + innerPad;
            float y = card.y + innerPad;
            float contentWidth = card.width - innerPad * 2f;

            var surfaceFg = palette.GetContrast(ColorType.Surface);
            var muted = palette.GetColor(ColorType.Muted);

            DrawBar(x, y, contentWidth * 0.60f, 8f, surfaceFg);
            y += 14f;

            DrawBar(x, y, contentWidth * 0.45f, 5f, muted);
            y += 10f;

            DrawBar(x, y, contentWidth * 0.30f, 5f, surfaceFg);
            y += 18f;

            const float buttonHeight = 26f;
            float buttonWidth = contentWidth * 0.38f;
            var button = new Rect(x, y, buttonWidth, buttonHeight);
            DrawRoundedRect(button, palette.GetColor(ColorType.Accent), palette.styles.buttonRoundness);
            DrawBar(button.x + 10f, button.y + button.height * 0.5f - 3f,
                    button.width - 20f, 6f, palette.GetContrast(ColorType.Accent));
            y += buttonHeight + 12f;

            const float chipGap = 6f;
            float chipWidth = (contentWidth - chipGap * 2f) / 3f;
            const float chipHeight = 22f;
            DrawChip(new Rect(x, y, chipWidth, chipHeight),
                palette.GetColor(ColorType.Success), palette.GetContrast(ColorType.Success), palette.styles.buttonRoundness);
            DrawChip(new Rect(x + chipWidth + chipGap, y, chipWidth, chipHeight),
                palette.GetColor(ColorType.Warning), palette.GetContrast(ColorType.Warning), palette.styles.buttonRoundness);
            DrawChip(new Rect(x + (chipWidth + chipGap) * 2f, y, chipWidth, chipHeight),
                palette.GetColor(ColorType.Danger), palette.GetContrast(ColorType.Danger), palette.styles.buttonRoundness);
        }

        static void DrawBar(float x, float y, float w, float h, Color c)
        {
            EditorGUI.DrawRect(new Rect(x, y, w, h), c);
        }

        static void DrawRoundedRect(Rect rect, Color color, CornerRoundness roundness)
        {
            GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill,
                true, 0f, color, 0f, roundness.Resolve(rect.width, rect.height).x);
        }

        static void DrawChip(Rect rect, Color background, Color foreground, CornerRoundness roundness)
        {
            DrawRoundedRect(rect, background, roundness);
            DrawBar(rect.x + 6f, rect.y + rect.height * 0.5f - 2f,
                    rect.width - 12f, 4f, foreground);
        }
    }

    [CustomPropertyDrawer(typeof(CornerRoundness))]
    public class CornerRoundnessDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var content = EditorGUI.PrefixLabel(position, label);
            var full = property.FindPropertyRelative("full");
            var pixels = property.FindPropertyRelative("pixels");
            int previousIndent = EditorGUI.indentLevel;
            bool previousMixed = EditorGUI.showMixedValue;
            EditorGUI.indentLevel = 0;

            var fullRect = new Rect(content.x, content.y, 52f, content.height);
            EditorGUI.showMixedValue = full.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            bool fullValue = EditorGUI.ToggleLeft(fullRect, "Full", full.boolValue);
            if (EditorGUI.EndChangeCheck())
                full.boolValue = fullValue;

            var pixelsRect = new Rect(fullRect.xMax + 4f, content.y, Mathf.Max(0f, content.width - 79f), content.height);
            using (new EditorGUI.DisabledScope(full.boolValue && !full.hasMultipleDifferentValues))
            {
                EditorGUI.showMixedValue = pixels.hasMultipleDifferentValues;
                EditorGUI.BeginChangeCheck();
                float value = EditorGUI.FloatField(pixelsRect, pixels.floatValue);
                if (EditorGUI.EndChangeCheck())
                    pixels.floatValue = Mathf.Max(0f, value);
                EditorGUI.LabelField(new Rect(pixelsRect.xMax + 3f, content.y, 20f, content.height), "px");
            }

            EditorGUI.showMixedValue = previousMixed;
            EditorGUI.indentLevel = previousIndent;
            EditorGUI.EndProperty();
        }
    }
}
