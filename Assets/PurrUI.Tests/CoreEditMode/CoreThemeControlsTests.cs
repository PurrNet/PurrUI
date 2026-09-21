using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PurrNet.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Object = UnityEngine.Object;

namespace PurrNet.UI.Tests.Core
{
    // This assembly deliberately has no reference to PurrUI.HeroUI.Runtime.
    public class CoreThemeControlsTests
    {
        private static readonly Dictionary<string, Type> MovedTypes = new Dictionary<string, Type>
        {
            { "72f1ab1a9b064fd6a580b09f917ceec5", typeof(ThemeColors) },
            { "e9c20e29ce9c2744aa74f141b285a2b8", typeof(ButtonElement) },
            { "6ce47c96979ef6a47a55656dd49379c3", typeof(ToggleElement) },
            { "bedb6fc38647bd946b0aba6e5c13d18a", typeof(SelectedOutlineInputField) },
            { "b633f0b5b7514987a26e95b96e13aa0b", typeof(ThemedToggle) },
            { "9e562379be3a47f79278a3244f0ebbb2", typeof(ThemedInputOutline) },
            { "991aef4382fa452cab2c98978c0c60f1", typeof(ThemedInputFieldMandatory) }
        };

        [Test]
        public void ReusableControls_BelongToCoreWithoutAHeroUIAssemblyDependency()
        {
            var core = typeof(ColoredGraphic).Assembly;
            foreach (var type in MovedTypes.Values)
            {
                Assert.That(type.Assembly, Is.EqualTo(core), type.Name + " still requires the template assembly");
                Assert.That(type.Namespace, Is.EqualTo("PurrNet.UI"));
            }
            Assert.That(core.GetReferencedAssemblies().Any(reference => reference.Name == "PurrUI.HeroUI.Runtime"), Is.False);
            Assert.That(GetType().Assembly.GetReferencedAssemblies().Any(reference => reference.Name == "PurrUI.HeroUI.Runtime"), Is.False);
        }

        [Test]
        public void MovedScripts_PreserveTheirGuidsAndOldNamespaceAssemblyMetadata()
        {
            foreach (var entry in MovedTypes)
            {
                var path = AssetDatabase.GUIDToAssetPath(entry.Key);
                Assert.That(path, Does.Contain("/Runtime/"), entry.Value.Name + ": script GUID no longer resolves to core");
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                Assert.That(script, Is.Not.Null, entry.Value.Name + ": original script GUID was lost");
                Assert.That(script.GetClass(), Is.EqualTo(entry.Value), path + ": original GUID points to a different class");
                var migration = entry.Value.GetCustomAttributesData().SingleOrDefault(attribute => attribute.AttributeType == typeof(MovedFromAttribute));
                Assert.That(migration, Is.Not.Null, entry.Value.Name + ": prior namespace/assembly is not recorded");
                Assert.That(migration.ConstructorArguments.Count, Is.GreaterThanOrEqualTo(3));
                Assert.That(migration.ConstructorArguments[1].Value, Is.EqualTo("PurrNet.UI.HeroUI"));
                Assert.That(migration.ConstructorArguments[2].Value, Is.EqualTo("PurrUI.HeroUI.Runtime"));
            }
        }

        [Test]
        public void FreshCoreControls_ResolveAndUpdatePaletteWithoutTemplatePrefabs()
        {
            var palette = ScriptableObject.CreateInstance<ColorPalette>();
            var parent = new GameObject("Core control palette test", typeof(RectTransform));
            parent.SetActive(false);
            try
            {
                palette.SetColor(ColorType.Accent, new Color(.1f, .3f, .7f, 1f));
                palette.SetColor(ColorType.Warning, new Color(.7f, .4f, .1f, 1f));
                palette.SetColor(ColorType.Surface, new Color(.1f, .12f, .14f, 1f));
                palette.SetContrast(ColorType.Surface, Color.white);
                palette.SetColor(ColorType.Danger, new Color(.8f, .1f, .2f, 1f));
                parent.AddComponent<PaletteProvider>().palette = palette;

                var buttonObject = Child(parent, "Button");
                var buttonFill = buttonObject.AddComponent<RectangleGraphic>();
                var button = buttonObject.AddComponent<ButtonElement>();
                References(button, ("_graphic", buttonFill));

                var toggleObject = Child(parent, "Toggle");
                var toggleFill = toggleObject.AddComponent<RectangleGraphic>();
                var knob = Child(toggleObject, "Knob").AddComponent<RectangleGraphic>();
                var toggle = toggleObject.AddComponent<ToggleElement>();
                References(toggle, ("_background", toggleFill), ("_nob", knob));
                var themedToggle = toggleObject.AddComponent<ThemedToggle>();
                References(themedToggle, ("_toggle", toggle), ("_background", toggleFill));

                var inputObject = Child(parent, "Input");
                var inputFill = inputObject.AddComponent<RectangleGraphic>();
                var input = inputObject.AddComponent<TMP_InputField>();
                var outline = inputObject.AddComponent<SelectedOutlineInputField>();
                References(outline, ("_input", input), ("_graphic", inputFill));
                var themedOutline = inputObject.AddComponent<ThemedInputOutline>();
                References(themedOutline, ("_outline", outline), ("_graphic", inputFill), ("_input", input));
                var label = Child(parent, "Required label").AddComponent<TextMeshProUGUI>();
                var required = inputObject.AddComponent<ThemedInputFieldMandatory>();
                References(required, ("_inputField", input), ("_graphic", outline), ("_label", label));
                var requiredData = new SerializedObject(required);
                requiredData.FindProperty("_labelFormat").stringValue = "Name <color=#{0}>*</color>";
                requiredData.ApplyModifiedPropertiesWithoutUndo();

                parent.SetActive(true);
                ThemeColors.Set(button, Info(ColorType.Accent), Info(ColorType.Warning), default(ColorTone), default(ColorTone));
                toggle.value = true;
                Invoke(themedToggle, "LateUpdate");
                AssertColor(button.backgroundNormal, palette.GetColor(ColorType.Accent));
                AssertColor(button.backgroundHover, palette.GetColor(ColorType.Warning));
                AssertColor(buttonFill.graphicColor, palette.GetColor(ColorType.Accent));
                AssertColor(toggleFill.graphicColor, palette.GetColor(ColorType.Accent));
                AssertColor(outline.outlineColor, palette.GetColor(ColorType.Accent));
                AssertMarker(label, palette);

                // A live palette change must reach all controls through the
                // core provider, with no template asset or template component.
                palette.SetColor(ColorType.Accent, new Color(.3f, .7f, .2f, 1f));
                palette.SetColor(ColorType.Warning, new Color(.2f, .6f, .8f, 1f));
                palette.SetContrast(ColorType.Surface, new Color(.15f, .17f, .19f, 1f));
                palette.SetColor(ColorType.Danger, new Color(.65f, .2f, .35f, 1f));
                Invoke(themedToggle, "LateUpdate");
                AssertColor(button.backgroundNormal, palette.GetColor(ColorType.Accent));
                AssertColor(button.backgroundHover, palette.GetColor(ColorType.Warning));
                AssertColor(buttonFill.graphicColor, palette.GetColor(ColorType.Accent));
                AssertColor(toggleFill.graphicColor, palette.GetColor(ColorType.Accent));
                AssertColor(outline.outlineColor, palette.GetColor(ColorType.Accent));
                AssertColor(label.color, palette.GetContrast(ColorType.Surface));
                AssertMarker(label, palette);
                var selection = palette.GetColor(ColorType.Accent);
                selection.a = new SerializedObject(themedOutline).FindProperty("_selectionOpacity").floatValue;
                AssertColor(input.selectionColor, selection);
            }
            finally
            {
                Object.DestroyImmediate(parent);
                Object.DestroyImmediate(palette);
            }
        }

        private static GameObject Child(GameObject parent, string name)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent.transform, false);
            return child;
        }

        private static void References(Object component, params (string field, Object value)[] references)
        {
            var data = new SerializedObject(component);
            foreach (var reference in references)
                data.FindProperty(reference.field).objectReferenceValue = reference.value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Invoke(object target, string name)
        {
            var method = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(target, null);
        }

        private static ColorInfo Info(ColorType role) => new ColorInfo { enabled = true, color = role };
        private static void AssertMarker(TMP_Text label, ColorPalette palette)
        {
            Assert.That(label.text, Is.EqualTo("Name <color=#" + ColorUtility.ToHtmlStringRGBA(palette.GetColor(ColorType.Danger)) + ">*</color>"));
        }

        private static void AssertColor(Color actual, Color expected)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(.001f));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(.001f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(.001f));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(.001f));
        }
    }
}
