using System;
using PurrNet.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PurrNet.Editor.UI
{
    [CustomEditor(typeof(ThemedGraphicTone)), CanEditMultipleObjects]
    public sealed class ThemedGraphicToneEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));

            var graphic = serializedObject.FindProperty("_graphic");
            EditorGUILayout.PropertyField(graphic);
            DrawChannel(graphic, serializedObject.FindProperty("_slot"));
            DrawPropertiesExcluding(serializedObject, "m_Script", "_graphic", "_slot");
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawChannel(SerializedProperty graphicProperty, SerializedProperty slot)
        {
            if (graphicProperty.hasMultipleDifferentValues)
            {
                EditorGUILayout.HelpBox("Select a single Graphic target to choose its color channel.", MessageType.Info);
                return;
            }

            var graphic = graphicProperty.objectReferenceValue as Graphic;
            if (!graphic && targets.Length == 1)
                graphic = ((ThemedGraphicTone)target).GetComponent<Graphic>();

            var keys = !graphic ? Array.Empty<string>()
                : graphic is IColored colored ? colored.keys ?? Array.Empty<string>()
                : new[] { "Color" };
            if (keys.Length == 0)
            {
                EditorGUILayout.HelpBox(graphic ? "Target exposes no color channels." : "Assign a Graphic to choose its color channel.", MessageType.Info);
                return;
            }

            bool invalid = !slot.hasMultipleDifferentValues && (slot.intValue < 0 || slot.intValue >= keys.Length);
            var labels = new GUIContent[keys.Length + (invalid ? 1 : 0)];
            var values = new int[labels.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                labels[i] = new GUIContent(keys[i]);
                values[i] = i;
            }
            // Retain an unavailable serialized value until the user selects a valid channel.
            if (invalid)
            {
                labels[keys.Length] = new GUIContent("Missing channel (" + slot.intValue + ")");
                values[keys.Length] = slot.intValue;
            }

            var label = new GUIContent("Color Channel", "The target graphic color affected by this tone.");
            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(rect, label, slot);
            bool previousMixed = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = slot.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            int selected = EditorGUI.IntPopup(rect, label, slot.intValue, labels, values);
            if (EditorGUI.EndChangeCheck())
                slot.intValue = selected;
            EditorGUI.showMixedValue = previousMixed;
            EditorGUI.EndProperty();
        }
    }
}
