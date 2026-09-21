using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace PurrNet.UI.Tests
{
    public class HeroUIButtonTests
    {
        private const string PrefabFolder = "Assets/PurrUI/Templates/HeroUI/Prefabs";
        private GameObject _parent;
        private ColorPalette _palette;
        private ButtonElement _button;
        private RectangleGraphic _graphic;
        private HashSet<int> _existingSoundPlayers;

        [SetUp]
        public void SetUp()
        {
            _existingSoundPlayers = new HashSet<int>(Resources.FindObjectsOfTypeAll<Sounds2D>().Select(player => player.GetInstanceID()));
        }

        [TearDown]
        public void TearDown()
        {
            if (_parent) Object.DestroyImmediate(_parent);
            if (_palette) Object.DestroyImmediate(_palette);
            foreach (var player in Resources.FindObjectsOfTypeAll<Sounds2D>())
                if (!_existingSoundPlayers.Contains(player.GetInstanceID())) Object.DestroyImmediate(player.gameObject);
        }

        [Test]
        public void PaletteChangeWhileHovered_UpdatesBothResolvedColorsAndVisibleHover()
        {
            CreateButton();
            _button.SetColors(Info(ColorType.Accent), Info(ColorType.Warning), default(ColorTone), default(ColorTone));
            Step();
            AssertColor(_graphic.graphicColor, _palette.GetColor(ColorType.Accent));
            SimulateState(true, false);
            Step();
            AssertColor(_graphic.graphicColor, _palette.GetColor(ColorType.Warning));

            var normal = new Color(.13f, .65f, .31f, 1f);
            var hover = new Color(.82f, .24f, .46f, 1f);
            _palette.SetColor(ColorType.Accent, normal);
            _palette.SetColor(ColorType.Warning, hover);
            // No manual palette refresh: these changes must reach the button
            // through PaletteProvider notifications while it remains hovered.
            Step();
            AssertColor(_button.backgroundNormal, normal);
            AssertColor(_button.backgroundHover, hover);
            AssertColor(_graphic.graphicColor, hover);
            SimulateState(false, false);
            Step();
            AssertColor(_graphic.graphicColor, normal);
        }

        [Test]
        public void SetColors_ResolvesExplicitTonesOnTheNativeButton()
        {
            CreateButton();
            var normal = Info(ColorType.Accent);
            var hover = Info(ColorType.Warning);
            var shade = Info(ColorType.Surface, true);
            _button.SetColors(normal, hover,
                new ColorTone { shade = shade, blend = .25f },
                new ColorTone { shade = shade, blend = .6f });
            Step();
            var expectedNormal = Color.Lerp(normal.GetColor(_palette), shade.GetColor(_palette), .25f);
            var expectedHover = Color.Lerp(hover.GetColor(_palette), shade.GetColor(_palette), .6f);
            AssertColor(_button.backgroundNormal, expectedNormal);
            AssertColor(_button.backgroundHover, expectedHover);
            AssertColor(_graphic.graphicColor, expectedNormal);
            SimulateState(true, false);
            Step();
            AssertColor(_graphic.graphicColor, expectedHover);
        }

        [Test]
        public void FreshGhostPrefab_IsTransparentNormallyAndOpaqueWhileHovered()
        {
            CreateButton("Button-Ghost");
            Assert.That(_button.GetComponents<MonoBehaviour>().Any(component => component && component.GetType().Name == "ThemedButton"), Is.False);
            Assert.That(_button, Is.InstanceOf<ThemeBinding>());
            Step();
            Assert.That(_button.backgroundNormal.a, Is.Zero.Within(.001f));
            Assert.That(_graphic.graphicColor.a, Is.Zero.Within(.001f));
            SimulateState(true, false);
            Step();
            Assert.That(_button.backgroundHover.a, Is.EqualTo(1f).Within(.001f));
            Assert.That(_graphic.graphicColor.a, Is.EqualTo(1f).Within(.001f));
            _palette.SetColor(ColorType.Surface, new Color(.8f, .85f, .9f, 1f));
            Step();
            Assert.That(_graphic.graphicColor.a, Is.EqualTo(1f).Within(.001f));
            SimulateState(false, false);
            Step();
            Assert.That(_graphic.graphicColor.a, Is.Zero.Within(.001f));
        }

        [Test]
        public void EditorPointerCallbacks_DoNotSimulateGameplayInteraction()
        {
            CreateButton();
            _button.SetColors(Info(ColorType.Accent), Info(ColorType.Warning), default(ColorTone), default(ColorTone));
            var authoredScale = new Vector3(.72f, .91f, 1.3f);
            _button.transform.localScale = authoredScale;
            var clicks = 0;
            _button.onClick += () => clicks++;
            _button.OnPointerEnter(null);
            _button.OnPointerDown(null);
            _button.OnPointerClick(null);
            Invoke(_button, "Update");
            Assert.That(clicks, Is.Zero);
            AssertColor(_graphic.graphicColor, _button.backgroundNormal);
            Assert.That(_button.transform.localScale, Is.EqualTo(authoredScale));
        }

        [Test]
        public void PressReleaseAndDisabledState_PreserveScaleAndDisabledOpacity()
        {
            CreateButton();
            _button.SetColors(Info(ColorType.Accent), Info(ColorType.Warning), default(ColorTone), default(ColorTone));
            SimulateState(false, true);
            Step();
            Assert.That(_button.transform.localScale, Is.EqualTo(new Vector3(.85f, .85f, 1f)));
            AssertColor(_graphic.graphicColor, _button.backgroundHover);
            SimulateState(false, false);
            Step();
            Assert.That(_button.transform.localScale, Is.EqualTo(Vector3.one));
            AssertColor(_graphic.graphicColor, _button.backgroundNormal);
            _button.interactable = false;
            SimulateState(false, true);
            Step();
            Assert.That(_button.transform.localScale, Is.EqualTo(Vector3.one));
            var disabled = _button.backgroundNormal;
            disabled.a *= .4f;
            AssertColor(_graphic.graphicColor, disabled);
        }

        [Test]
        public void ZeroDuration_AllInteractionStatesHaveFiniteColorsAndScale()
        {
            CreateButton();
            foreach (var action in new Action[]
            {
                () => { }, () => SimulateState(true, false), () => SimulateState(true, true),
                () => SimulateState(true, false), () => SimulateState(false, false),
                () => _button.interactable = false, () => _button.interactable = true
            })
            {
                action();
                Step();
                var color = _graphic.graphicColor;
                var scale = _button.transform.localScale;
                foreach (var value in new[] { color.r, color.g, color.b, color.a, scale.x, scale.y, scale.z })
                    Assert.That(float.IsNaN(value) || float.IsInfinity(value), Is.False,
                        "A zero-duration transition produced NaN/Infinity.");
            }
        }

        [TestCase("Button-Primary", "4093494571155517460", "8394812538829302174", 4)]
        [TestCase("Button-Round", "6545535638871718880", "3370907101242238404", 4)]
        [TestCase("CloseButton", "8429281824467705191", "5449262722314579291", 1)]
        public void SourceButtons_PreserveComponentIdentityAndClickData(string name, string componentId, string graphicId, int soundCount)
        {
            // These are the existing native component IDs consumed by nested
            // prefab overrides. Folding theme data in must not replace them.
            var path = PrefabFolder + "/" + name + ".prefab";
            var yaml = File.ReadAllText(path);
            var block = Regex.Match(yaml, @"(?ms)^--- !u!114 &" + componentId + @"\r?\n.*?(?=^--- |\z)");
            Assert.That(block.Success, Is.True, name + ": original ButtonElement identity was lost");
            StringAssert.Contains("guid: e9c20e29ce9c2744aa74f141b285a2b8", block.Value, "Native script GUID changed");
            StringAssert.Contains("_graphic: {fileID: " + graphicId + "}", block.Value);
            StringAssert.Contains("onClickUnity:", block.Value);
            var clips = Regex.Match(block.Value, @"(?ms)^  _clickSounds:\r?\n(.*?)(?=^  [a-zA-Z_]|\z)").Groups[1].Value;
            var guids = Regex.Matches(clips, @"guid: ([a-f0-9]{32})").Cast<Match>().Select(match => match.Groups[1].Value).ToArray();
            var expected = soundCount == 1
                ? new[] { "2c54de40fc105c449bbfd51da9fd7394" }
                : new[] { "24300c13439bdc34ba6a448b09876de8", "0f09be60e6734a54bb3ddf2e8581e374", "10c49e10c6d59ca4b86070f3374b69ff", "3da018ea9941fab498be0a1753490ef2" };
            Assert.That(guids, Is.EqualTo(expected), name + ": authored click-sound references changed");
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(asset.GetComponent<ButtonElement>(), Is.Not.Null);
            Assert.That(new SerializedObject(asset.GetComponent<ButtonElement>()).FindProperty("onClickUnity.m_PersistentCalls.m_Calls"), Is.Not.Null);
        }

        private void CreateButton(string prefabName = null)
        {
            _palette = ScriptableObject.CreateInstance<ColorPalette>();
            _palette.SetColor(ColorType.Accent, new Color(.1f, .3f, .7f, 1f));
            _palette.SetColor(ColorType.Warning, new Color(.7f, .4f, .1f, 1f));
            _palette.SetColor(ColorType.Surface, new Color(.1f, .12f, .14f, 1f));
            _palette.SetContrast(ColorType.Surface, Color.white);
            _parent = new GameObject("Native HeroUI button test", typeof(RectTransform));
            _parent.SetActive(false);
            _parent.AddComponent<PaletteProvider>().palette = _palette;
            if (prefabName == null)
            {
                var child = new GameObject("Button", typeof(RectTransform), typeof(CanvasRenderer));
                child.transform.SetParent(_parent.transform, false);
                _graphic = child.AddComponent<RectangleGraphic>();
                _button = child.AddComponent<ButtonElement>();
            }
            else
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/" + prefabName + ".prefab");
                Assert.That(prefab, Is.Not.Null);
                var child = (GameObject)PrefabUtility.InstantiatePrefab(prefab, _parent.transform);
                _button = child.GetComponent<ButtonElement>();
                Assert.That(_button, Is.Not.Null);
                _graphic = new SerializedObject(_button).FindProperty("_graphic").objectReferenceValue as RectangleGraphic;
            }
            Assert.That(_graphic, Is.Not.Null);
            var data = new SerializedObject(_button);
            data.FindProperty("_graphic").objectReferenceValue = _graphic;
            data.FindProperty("_transitionDuration").floatValue = 0f;
            if (prefabName == null)
            {
                data.FindProperty("_scaleNormal").floatValue = 1f;
                data.FindProperty("_scaleClicked").floatValue = .85f;
                data.FindProperty("_disabledAlpha").floatValue = .4f;
                data.FindProperty("_clickSounds").arraySize = 0;
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            _parent.SetActive(true);
        }

        private static ColorInfo Info(ColorType type, bool contrast = false) => new ColorInfo { enabled = true, color = type, contrast = contrast };
        // UpdateVisuals is the same calculation called by the runtime Update.
        // Keeping the public pointer guards intact avoids editing prefab scale
        // merely because an ExecuteAlways component is present in the editor.
        private void Step() => Invoke(_button, "UpdateVisuals", 0f);
        private void SimulateState(bool hover, bool press)
        {
            typeof(ButtonElement).GetField("_isHovering", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_button, hover);
            typeof(ButtonElement).GetField("_isPressing", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_button, press);
        }
        private static void Invoke(object target, string name, params object[] arguments)
        {
            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                var method = type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (method == null) continue;
                method.Invoke(target, arguments);
                return;
            }
            Assert.Fail(target.GetType().Name + ": lifecycle method " + name + " not found");
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
