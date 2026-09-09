using PurrNet.UI;
using UnityEditor;
using UnityEngine;

namespace PurrNet.Editor.UI
{
    public abstract class ThemeControlEditor : UnityEditor.Editor
    {
        protected readonly struct Metric
        {
            public readonly string field, theme, label;
            public Metric(string field, string theme, string label)
            {
                this.field = field;
                this.theme = theme;
                this.label = label;
            }
        }

        protected abstract Metric[] metrics { get; }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var property = serializedObject.GetIterator();
            for (bool children = true; property.NextVisible(children); children = false)
            {
                bool handled = false;
                foreach (var metric in metrics)
                {
                    if (property.name == metric.theme)
                    {
                        handled = true;
                        break;
                    }
                    if (property.name != metric.field) continue;
                    var useTheme = serializedObject.FindProperty(metric.theme);
                    EditorGUILayout.PropertyField(useTheme, new GUIContent(metric.label + " From Theme",
                        "Use this value from the nearest palette's Styles. Turn off to use a local override. Without a palette, the local value is used."));
                    if (!useTheme.boolValue || useTheme.hasMultipleDifferentValues)
                        EditorGUILayout.PropertyField(property, new GUIContent(metric.label + " (Local)"), true);
                    handled = true;
                    break;
                }
                if (handled) continue;
                using (new EditorGUI.DisabledScope(property.propertyPath == "m_Script"))
                    EditorGUILayout.PropertyField(property, true);
            }
            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomEditor(typeof(ButtonElement), true), CanEditMultipleObjects]
    public sealed class ButtonElementEditor : ThemeControlEditor
    {
        private static readonly Metric[] Metrics =
        {
            new Metric("_transitionDuration", "_useThemeTransition", "Transition Duration"),
            new Metric("_disabledAlpha", "_useThemeDisabledOpacity", "Disabled Opacity"),
            new Metric("_scaleClicked", "_useThemePressedScale", "Pressed Scale")
        };
        protected override Metric[] metrics => Metrics;
    }

    [CustomEditor(typeof(ToggleElement), true), CanEditMultipleObjects]
    public sealed class ToggleElementEditor : ThemeControlEditor
    {
        private static readonly Metric[] Metrics = { new Metric("_transitionDuration", "_useThemeTransition", "Transition Duration") };
        protected override Metric[] metrics => Metrics;
    }

    [CustomEditor(typeof(ThemedToggle)), CanEditMultipleObjects]
    public sealed class ThemedToggleEditor : ThemeControlEditor
    {
        private static readonly Metric[] Metrics = { new Metric("_transitionDuration", "_useThemeTransition", "Transition Duration") };
        protected override Metric[] metrics => Metrics;
    }

    [CustomEditor(typeof(SelectedOutlineInputField), true), CanEditMultipleObjects]
    public sealed class SelectedOutlineInputFieldEditor : ThemeControlEditor
    {
        private static readonly Metric[] Metrics =
        {
            new Metric("_transitionDuration", "_useThemeTransition", "Focus Transition Duration"),
            new Metric("_outlineWidth", "_useThemeFocusWidth", "Focus Outline Width")
        };
        protected override Metric[] metrics => Metrics;
    }
}
