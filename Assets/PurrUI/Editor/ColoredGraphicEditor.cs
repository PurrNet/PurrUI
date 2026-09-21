using System;
using PurrNet.UI;
using UnityEditor;
using UnityEngine;

namespace PurrNet.Editor.UI
{
    [CustomEditor(typeof(ColoredGraphic))]
    [CanEditMultipleObjects]
    public class ColoredGraphicEditor : UnityEditor.Editor
    {
        SerializedProperty _graphicProp;
        SerializedProperty _transitionDurationProp;
        SerializedProperty _colorProp;
        SerializedProperty _coloredInfosProp;

        void OnEnable()
        {
            _graphicProp = serializedObject.FindProperty("_graphic");
            _transitionDurationProp = serializedObject.FindProperty("_transitionDuration");
            _colorProp = serializedObject.FindProperty("_color");
            _coloredInfosProp = serializedObject.FindProperty("_coloredInfos");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_graphicProp);
            EditorGUILayout.PropertyField(_transitionDurationProp);

            if (_graphicProp.hasMultipleDifferentValues)
            {
                EditorGUILayout.HelpBox("Multi-selection has different Graphic targets — edit individually.", MessageType.Info);
                ApplyAndRefresh();
                return;
            }

            var graphic = _graphicProp.objectReferenceValue;

            if (graphic == null)
            {
                EditorGUILayout.HelpBox("Assign a Graphic to color.", MessageType.Info);
                ApplyAndRefresh();
                return;
            }

            if (graphic is IColored colored)
                DrawMultiKey(colored);
            else
                EditorGUILayout.PropertyField(_colorProp, new GUIContent("Color"));

            ApplyAndRefresh();
        }

        void DrawMultiKey(IColored colored)
        {
            var keys = colored.keys ?? Array.Empty<string>();

            if (_coloredInfosProp.arraySize != keys.Length)
                _coloredInfosProp.arraySize = keys.Length;

            if (keys.Length == 0)
            {
                EditorGUILayout.HelpBox("Target exposes no color keys.", MessageType.Info);
                return;
            }

            for (int i = 0; i < keys.Length; i++)
            {
                var element = _coloredInfosProp.GetArrayElementAtIndex(i);
                var label = new GUIContent(keys[i]);
                EditorGUILayout.PropertyField(element, label, true);
            }
        }

        void ApplyAndRefresh()
        {
            if (!serializedObject.ApplyModifiedProperties())
                return;

            foreach (var obj in targets)
            {
                if (obj is not ColoredGraphic coloredGraphic) continue;
                coloredGraphic.Refresh();
                EditorUtility.SetDirty(coloredGraphic);
            }
        }
    }
}
