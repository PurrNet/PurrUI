using System.Collections;
using System.Linq;
using NUnit.Framework;
using PurrNet.UI.HeroUI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;

namespace PurrNet.UI.Tests
{
    public class HeroUIPrefabThemeTests
    {
        private const string PrefabFolder = "Assets/PurrUI/Templates/HeroUI/Prefabs";

        [Test]
        public void SourcePrefabs_ProvideCompleteThemeBindingsWithoutViewOverrides()
        {
            Assert.That(typeof(ButtonElement).GetProperty(nameof(ButtonElement.backgroundNormal)).CanWrite, Is.False);
            Assert.That(typeof(ButtonElement).GetProperty(nameof(ButtonElement.backgroundHover)).CanWrite, Is.False);
            var paths = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder })
                .Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path).ToArray();
            Assert.That(paths.Length, Is.GreaterThanOrEqualTo(25), "HeroUI source prefab collection is incomplete.");

            foreach (var path in paths)
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    foreach (var component in root.GetComponentsInChildren<Component>(true))
                    {
                        Assert.That(component, Is.Not.Null, path + ": missing component script");
                        // This contract owns theme references; audio/fonts are supplied by consuming projects.
                        if (component is ButtonElement nativeButton)
                        {
                            RequireReferences(nativeButton, path, "_graphic");
                            continue;
                        }
                        if (!(component is ColoredGraphic || component is ThemeBinding || component is GraphicOpacity))
                            continue;
                        if (component is ToggleElement nativeToggle)
                        {
                            RequireReferences(nativeToggle, path, "_background", "_nob");
                            continue;
                        }
                        var property = new SerializedObject(component).GetIterator();
                        while (property.Next(true))
                        {
                            if (property.propertyType == SerializedPropertyType.ObjectReference &&
                                !property.objectReferenceValue)
                                Assert.That(property.objectReferenceInstanceIDValue, Is.Zero,
                                    path + ": broken reference " + component.name + "." + property.propertyPath);
                        }
                    }

                    Assert.That(root.GetComponentsInChildren<MonoBehaviour>(true).Any(component => component && component.GetType().Name == "ThemedButton"),
                        Is.False, path + ": obsolete button adapter remains in the hierarchy");
                    var bindings = root.GetComponentsInChildren<ColoredGraphic>(true);
                    foreach (var binding in bindings)
                        RequireReferences(binding, path, "_graphic");
                    foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
                    {
                        var matches = bindings.Where(binding => Reference(binding, "_graphic") == graphic).ToArray();
                        Assert.That(matches.Length, Is.EqualTo(1), path + ": " + graphic.name + " requires one ColoredGraphic");
                        Assert.That(matches[0].gameObject, Is.EqualTo(graphic.gameObject), path + ": binding is attached elsewhere");
                    }

                    foreach (var button in root.GetComponentsInChildren<ButtonElement>(true))
                    {
                        Assert.That(button, Is.InstanceOf<ThemeBinding>(), path + ": native button does not own its theme");
                        Assert.That(button.GetComponents<MonoBehaviour>().Any(component => component && component.GetType().Name == "ThemedButton"),
                            Is.False, path + ": obsolete ThemedButton adapter remains");
                        RequireReferences(button, path, "_graphic");
                        var buttonData = new SerializedObject(button);
                        foreach (var field in new[] { "_normalColor", "_hoverColor", "_normalTone", "_hoverTone", "_normalOpacity", "_hoverOpacity", "_normalBrightness", "_hoverBrightness" })
                            Assert.That(buttonData.FindProperty(field), Is.Not.Null, path + ": native button lacks " + field);
                        Assert.That(buttonData.FindProperty("_normalColor.enabled").boolValue, Is.True);
                        Assert.That(buttonData.FindProperty("_hoverColor.enabled").boolValue, Is.True);
                        Assert.That(buttonData.FindProperty("_backgroundNormal"), Is.Null, path + ": legacy serialized normal Color remains");
                        Assert.That(buttonData.FindProperty("_backgroundHover"), Is.Null, path + ": legacy serialized hover Color remains");
                        var graphic = Reference(button, "_graphic") as Graphic;
                        var fillBinding = bindings.Single(binding => Reference(binding, "_graphic") == graphic);
                        Assert.That(new SerializedObject(fillBinding).FindProperty("_coloredInfos.Array.data[0].enabled").boolValue,
                            Is.False, path + ": ColoredGraphic competes with the native button fill owner");
                    }
                    foreach (var toggle in root.GetComponentsInChildren<ToggleElement>(true))
                    {
                        Assert.That(toggle.GetComponents<ThemedToggle>().Length, Is.EqualTo(1), path + ": toggle is not themed");
                        RequireReferences(toggle.GetComponent<ThemedToggle>(), path, "_toggle", "_background");
                    }
                    var outlines = root.GetComponentsInChildren<ThemedInputOutline>(true);
                    foreach (var outline in root.GetComponentsInChildren<SelectedOutlineInputField>(true))
                        Assert.That(outline.GetComponents<ThemedInputOutline>().Length, Is.EqualTo(1), path + ": input outline is not themed");
                    foreach (var outline in outlines)
                        RequireReferences(outline, path, "_outline", "_graphic", "_input");
                    foreach (var input in root.GetComponentsInChildren<TMP_InputField>(true))
                        Assert.That(outlines.Count(outline => Reference(outline, "_input") == input), Is.EqualTo(1),
                            path + ": input selection requires one theme binding");
                    Assert.That(root.GetComponentsInChildren<InputFieldMandatory>(true), Is.Empty,
                        path + ": required-field feedback still uses legacy colors");
                    foreach (var required in root.GetComponentsInChildren<ThemedInputFieldMandatory>(true))
                        RequireReferences(required, path, "_inputField", "_graphic", "_label");
                    foreach (var tone in root.GetComponentsInChildren<ThemedGraphicTone>(true))
                        RequireReferences(tone, path, "_graphic");
                    foreach (var opacity in root.GetComponentsInChildren<GraphicOpacity>(true))
                        RequireReferences(opacity, path, "_graphic");
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        [UnityTest]
        public IEnumerator FreshInputField_RemainsOpaqueAndFollowsSurfaceWhenPaletteChanges()
        {
            return CheckPaletteChange("InputField-Labeled", false);
        }

        [UnityTest]
        public IEnumerator FreshIconCircle_RemainsOpaqueAndDistinctWhenPaletteChanges()
        {
            return CheckPaletteChange("Icon-Circle", true);
        }

        private static IEnumerator CheckPaletteChange(string name, bool expectedRaised)
        {
            var palette = ScriptableObject.CreateInstance<ColorPalette>();
            var parent = new GameObject("HeroUI theme test", typeof(RectTransform));
            parent.SetActive(false);
            try
            {
                palette.SetColor(ColorType.Surface, new Color(.1f, .1f, .1f, 1f));
                palette.SetContrast(ColorType.Surface, Color.white);
                var provider = parent.AddComponent<PaletteProvider>();
                provider.palette = palette;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/" + name + ".prefab");
                Assert.That(prefab, Is.Not.Null);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
                parent.SetActive(true);

                var input = instance.GetComponentInChildren<TMP_InputField>(true);
                var fill = input ? input.GetComponent<SignedDistanceFieldGraphic>() : instance.GetComponent<SignedDistanceFieldGraphic>();
                Assert.That(fill, Is.Not.Null, name + ": visible background is missing");
                AssertFill(fill, palette, true, expectedRaised);
                var dark = fill.graphicColor;

                // Mutate the live palette so this also exercises provider notifications.
                palette.SetColor(ColorType.Surface, new Color(.9f, .9f, .9f, 1f));
                palette.SetContrast(ColorType.Surface, new Color(.1f, .1f, .1f, 1f));
                // ColoredGraphic applies palette notifications on the next editor update.
                yield return null;
                AssertFill(fill, palette, false, expectedRaised);
                Assert.That(Mathf.Abs(fill.graphicColor.grayscale - dark.grayscale), Is.GreaterThan(.5f),
                    name + ": background did not follow the palette change");
            }
            finally
            {
                Object.DestroyImmediate(parent);
                Object.DestroyImmediate(palette);
            }
        }

        private static void AssertFill(SignedDistanceFieldGraphic fill, ColorPalette palette, bool dark, bool expectedRaised)
        {
            var surface = palette.GetColor(ColorType.Surface);
            if (expectedRaised)
            {
                var difference = fill.graphicColor.grayscale - surface.grayscale;
                Assert.That(difference * (dark ? 1f : -1f), Is.GreaterThan(.02f),
                    fill.name + ": raised background collapsed into the parent surface");
            }
            else
            {
                // The shared input uses Surface; Lobby views author their own raised fill overrides.
                Assert.That(fill.graphicColor.r, Is.EqualTo(surface.r).Within(.001f));
                Assert.That(fill.graphicColor.g, Is.EqualTo(surface.g).Within(.001f));
                Assert.That(fill.graphicColor.b, Is.EqualTo(surface.b).Within(.001f));
            }
            Assert.That(fill.graphicColor.a * fill.color.a, Is.EqualTo(1f).Within(.001f),
                fill.name + ": background became transparent");
        }

        private static Object Reference(Object component, string property)
        {
            return new SerializedObject(component).FindProperty(property).objectReferenceValue;
        }

        private static void RequireReferences(Object component, string path, params string[] properties)
        {
            foreach (var property in properties)
                Assert.That(Reference(component, property), Is.Not.Null, path + ": " + component.name + "." + property + " is missing");
        }
    }
}
