using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace PurrNet.UI.Tests
{
    public class HeroUIButtonPlayModeTests
    {
        private GameObject _parent;
        private ColorPalette _palette;
        private AudioClip _clip;
        private HashSet<int> _existingSoundPlayers;

        [UnityTest]
        public IEnumerator RuntimePointers_PreserveHoverPressPaletteClickAudioAndInteractionGate()
        {
            Assert.That(Application.isPlaying, Is.True, "This fixture must run in the PlayMode test assembly.");
            _existingSoundPlayers = new HashSet<int>(Resources.FindObjectsOfTypeAll<Sounds2D>().Select(player => player.GetInstanceID()));
            _palette = ScriptableObject.CreateInstance<ColorPalette>();
            _palette.SetColor(ColorType.Accent, new Color(.1f, .3f, .7f, 1f));
            _palette.SetColor(ColorType.Warning, new Color(.7f, .4f, .1f, 1f));
            _clip = AudioClip.Create("Silent button regression clip", 128, 1, 8000, false);
            _parent = new GameObject("Runtime native button test", typeof(RectTransform));
            _parent.SetActive(false);
            _parent.AddComponent<PaletteProvider>().palette = _palette;
            var child = new GameObject("Button", typeof(RectTransform), typeof(CanvasRenderer));
            child.transform.SetParent(_parent.transform, false);
            var graphic = child.AddComponent<RectangleGraphic>();
            var button = child.AddComponent<ButtonElement>();
            Set(button, "_graphic", graphic);
            Set(button, "_transitionDuration", 0f);
            Set(button, "_scaleNormal", 1f);
            Set(button, "_scaleClicked", .85f);
            Set(button, "_disabledAlpha", .4f);
            Set(button, "_clickSounds", new[] { _clip });
            button.onClickUnity = new UnityEvent();
            var unityClicks = 0;
            var eventClicks = 0;
            button.onClickUnity.AddListener(() => unityClicks++);
            button.onClick += () => eventClicks++;
            _parent.SetActive(true);
            button.SetColors(Info(ColorType.Accent), Info(ColorType.Warning), default(ColorTone), default(ColorTone));
            yield return null;
            AssertColor(graphic.graphicColor, button.backgroundNormal);

            button.OnPointerEnter(null);
            yield return null;
            AssertColor(graphic.graphicColor, button.backgroundHover);
            button.OnPointerDown(null);
            yield return null;
            Assert.That(button.transform.localScale, Is.EqualTo(new Vector3(.85f, .85f, 1f)));
            button.OnPointerUp(null);
            yield return null;
            Assert.That(button.transform.localScale, Is.EqualTo(Vector3.one));
            button.OnPointerClick(null);
            Assert.That(new[] { unityClicks, eventClicks }, Is.EqualTo(new[] { 1, 1 }));
            Assert.That(Resources.FindObjectsOfTypeAll<Sounds2D>().SelectMany(player => player.GetComponents<AudioSource>()).Any(source => source.clip == _clip),
                Is.True, "Native click no longer passes its configured clip to Sounds2D.");

            var normal = new Color(.2f, .6f, .3f, 1f);
            var hover = new Color(.8f, .2f, .5f, 1f);
            _palette.SetColor(ColorType.Accent, normal);
            _palette.SetColor(ColorType.Warning, hover);
            yield return null;
            AssertColor(button.backgroundNormal, normal);
            AssertColor(button.backgroundHover, hover);
            AssertColor(graphic.graphicColor, hover);

            button.interactable = false;
            button.OnPointerDown(null);
            button.OnPointerClick(null);
            yield return null;
            Assert.That(new[] { unityClicks, eventClicks }, Is.EqualTo(new[] { 1, 1 }));
            Assert.That(button.transform.localScale, Is.EqualTo(Vector3.one));
            var disabled = normal;
            disabled.a = .4f;
            AssertColor(graphic.graphicColor, disabled);

            button.interactable = true;
            var parentGroup = _parent.AddComponent<CanvasGroup>();
            parentGroup.interactable = false;
            yield return null;
            Assert.That(button.isInteractable, Is.False);
            button.OnPointerClick(null);
            Assert.That(new[] { unityClicks, eventClicks }, Is.EqualTo(new[] { 1, 1 }));
            var ownGroup = child.AddComponent<CanvasGroup>();
            ownGroup.ignoreParentGroups = true;
            yield return null;
            Assert.That(button.isInteractable, Is.True);
            button.OnPointerClick(null);
            Assert.That(new[] { unityClicks, eventClicks }, Is.EqualTo(new[] { 2, 2 }));
            button.OnPointerExit(null);
            yield return null;
            AssertColor(graphic.graphicColor, normal);
        }

        [TearDown]
        public void TearDown()
        {
            if (_parent) Object.DestroyImmediate(_parent);
            foreach (var player in Resources.FindObjectsOfTypeAll<Sounds2D>())
                if (_existingSoundPlayers != null && !_existingSoundPlayers.Contains(player.GetInstanceID())) Object.DestroyImmediate(player.gameObject);
            if (_palette) Object.DestroyImmediate(_palette);
            if (_clip) Object.DestroyImmediate(_clip);
        }

        private static ColorInfo Info(ColorType type) => new ColorInfo { enabled = true, color = type };
        private static void Set(ButtonElement button, string name, object value)
        {
            var field = typeof(ButtonElement).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(button, value);
        }
        private static void AssertColor(Color actual, Color expected)
        {
            foreach (var value in new[] { actual.r, actual.g, actual.b, actual.a })
                Assert.That(float.IsNaN(value) || float.IsInfinity(value), Is.False, "Zero-duration native button color is not finite.");
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(.001f));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(.001f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(.001f));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(.001f));
        }
    }
}
