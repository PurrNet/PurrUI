using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PurrNet.UI.Tests.Core
{
    public class ThemeStylesTests
    {
        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (var i = _created.Count - 1; i >= 0; i--)
                if (_created[i]) Object.DestroyImmediate(_created[i]);
            _created.Clear();
        }

        [Test]
        public void SetStyles_NotifiesExistingProviderAndKeepsUnappliedCopiesIndependent()
        {
            var palette = Palette();
            var provider = Parent(palette).GetComponent<PaletteProvider>();
            var original = palette.styles;
            var edited = original;
            edited.panelRoundness = CornerRoundness.Pixels(7f);
            edited.buttonTransitionDuration = .7f;
            var notifications = 0;
            provider.onColorChange += () => notifications++;

            Assert.That(palette.styles.buttonTransitionDuration, Is.EqualTo(original.buttonTransitionDuration));
            palette.SetStyles(edited);
            Assert.That(notifications, Is.EqualTo(1), "Style edits must use the existing palette notification path.");
            Assert.That(palette.styles.panelRoundness.pixels, Is.EqualTo(7f));
            Assert.That(palette.styles.buttonTransitionDuration, Is.EqualTo(.7f));
            edited.panelRoundness = CornerRoundness.Pixels(3f);
            Assert.That(palette.styles.panelRoundness.pixels, Is.EqualTo(7f), "Editing a retrieved style copy must not silently change the palette.");
        }

        [Test]
        public void InvalidStyleValues_AreSanitizedWithoutMutatingTheCaller()
        {
            var styles = ThemeStyles.Default;
            styles.panelRoundness.pixels = -4f;
            styles.buttonTransitionDuration = float.NaN;
            styles.toggleTransitionDuration = float.PositiveInfinity;
            styles.inputTransitionDuration = -2f;
            styles.buttonDisabledOpacity = 2f;
            styles.buttonPressedScale = float.NegativeInfinity;
            styles.inputFocusWidth = -1f;
            var sanitized = styles.Sanitize();
            Assert.That(styles.panelRoundness.pixels, Is.EqualTo(-4f));
            Assert.That(float.IsNaN(styles.buttonTransitionDuration), Is.True);
            Assert.That(sanitized.panelRoundness.pixels, Is.Zero);
            Assert.That(sanitized.buttonTransitionDuration, Is.Zero);
            Assert.That(sanitized.toggleTransitionDuration, Is.Zero);
            Assert.That(sanitized.inputTransitionDuration, Is.Zero);
            Assert.That(sanitized.buttonDisabledOpacity, Is.EqualTo(1f));
            Assert.That(sanitized.buttonPressedScale, Is.Zero);
            Assert.That(sanitized.inputFocusWidth, Is.Zero);
            var palette = Palette();
            palette.SetStyles(styles);
            Assert.That(palette.styles.buttonTransitionDuration, Is.Zero);
            Assert.That(palette.styles.buttonDisabledOpacity, Is.EqualTo(1f));
        }

        [Test]
        public void RoundnessRoles_UseIndependentThemeValuesInGeneratedMesh()
        {
            var palette = Palette();
            var styles = palette.styles;
            styles.buttonRoundness = CornerRoundness.Pixels(2f);
            styles.panelRoundness = CornerRoundness.Pixels(4f);
            styles.inputRoundness = CornerRoundness.Pixels(6f);
            styles.itemRoundness = CornerRoundness.Pixels(8f);
            palette.SetStyles(styles);
            var graphic = Graphic(Parent(palette));

            var roles = new[] { RoundnessRole.Button, RoundnessRole.Panel, RoundnessRole.Input, RoundnessRole.Item };
            for (var i = 0; i < roles.Length; i++)
            {
                graphic.roundnessRole = roles[i];
                AssertRoundness(graphic, Vector4.one * (2f + i * 2f));
            }
        }

        [Test]
        public void ThemeRoundness_PreservesAuthoredCustomCornersAndLocalCircleOption()
        {
            var palette = Palette();
            var graphic = Graphic(Parent(palette));
            var corners = new Vector4(2f, 4f, 6f, 8f);
            graphic.useMaxRoundness = false;
            graphic.uniformRoundness = false;
            graphic.roundnessInPixels = corners;
            graphic.roundnessRole = RoundnessRole.Panel;
            var styles = palette.styles;
            styles.panelRoundness = CornerRoundness.Pixels(12f);
            palette.SetStyles(styles);
            AssertRoundness(graphic, Vector4.one * 12f);
            Assert.That(graphic.useMaxRoundness, Is.False);
            Assert.That(graphic.uniformRoundness, Is.False);
            Assert.That(graphic.roundnessInPixels, Is.EqualTo(corners));

            graphic.roundnessRole = RoundnessRole.Custom;
            AssertRoundness(graphic, corners);
            graphic.uniformRoundness = true;
            AssertRoundness(graphic, Vector4.one * 2f);
            graphic.useMaxRoundness = true;
            graphic.roundnessRole = RoundnessRole.Panel;
            AssertRoundness(graphic, Vector4.one * 12f);
            Assert.That(graphic.useMaxRoundness, Is.True);
            Assert.That(graphic.uniformRoundness, Is.True);
            Assert.That(graphic.roundnessInPixels, Is.EqualTo(corners));
            graphic.roundnessRole = RoundnessRole.Custom;
            AssertRoundness(graphic, Vector4.one * 20f);
        }

        [Test]
        public void FullRoundness_TracksPillCircleAndPortraitResizeInTheMesh()
        {
            var palette = Palette();
            var styles = palette.styles;
            styles.buttonRoundness = CornerRoundness.Full;
            palette.SetStyles(styles);
            var graphic = Graphic(Parent(palette));
            graphic.roundnessRole = RoundnessRole.Button;
            AssertRoundness(graphic, Vector4.one * 20f);
            graphic.rectTransform.sizeDelta = new Vector2(50f, 50f);
            AssertRoundness(graphic, Vector4.one * 25f);
            graphic.rectTransform.sizeDelta = new Vector2(20f, 100f);
            AssertRoundness(graphic, Vector4.one * 10f);
            Assert.That(graphic.ResolveRoundness(180f, 120f), Is.EqualTo(Vector4.one * 60f),
                "Expanded glow geometry must resolve Full against its own dimensions.");

            // Existing narrow badges can author radii larger than half their
            // width. A fixed theme radius must preserve those shader inputs.
            styles.buttonRoundness = CornerRoundness.Pixels(16f);
            palette.SetStyles(styles);
            graphic.rectTransform.sizeDelta = new Vector2(8f, 40f);
            AssertRoundness(graphic, Vector4.one * 16f);
        }

        [Test]
        public void Roundness_RebindsOnPaletteSwapAndReparentWithoutOldPaletteNotifications()
        {
            var first = Palette(3f);
            var second = Palette(9f);
            var third = Palette(14f);
            var firstParent = Parent(first);
            var secondParent = Parent(third);
            var graphic = Graphic(firstParent);
            graphic.roundnessRole = RoundnessRole.Panel;
            AssertRoundness(graphic, Vector4.one * 3f);
            var dirties = 0;
            graphic.RegisterDirtyVerticesCallback(() => dirties++);

            firstParent.GetComponent<PaletteProvider>().palette = second;
            AssertRoundness(graphic, Vector4.one * 9f);
            dirties = 0;
            SetPanelRoundness(first, 4f);
            Assert.That(dirties, Is.Zero, "The old palette still has a graphic subscription after a provider swap.");
            SetPanelRoundness(second, 11f);
            Assert.That(dirties, Is.GreaterThan(0), "A current style edit must invalidate the rendered geometry.");
            AssertRoundness(graphic, Vector4.one * 11f);

            graphic.transform.SetParent(secondParent.transform, false);
            AssertRoundness(graphic, Vector4.one * 14f);
            dirties = 0;
            SetPanelRoundness(second, 12f);
            Assert.That(dirties, Is.Zero, "Reparenting must release the previous provider.");
            SetPanelRoundness(third, 16f);
            Assert.That(dirties, Is.GreaterThan(0));
            AssertRoundness(graphic, Vector4.one * 16f);
        }

        [Test]
        public void GlowSource_UsesExpandedFullRoundnessAndRefreshesForThemePixelChanges()
        {
            var palette = Palette();
            var styles = palette.styles;
            styles.buttonRoundness = CornerRoundness.Full;
            palette.SetStyles(styles);
            var parent = Parent(palette);
            var source = Graphic(parent);
            source.roundnessRole = RoundnessRole.Button;
            var glowObject = new GameObject("Styled glow", typeof(RectTransform));
            glowObject.transform.SetParent(parent.transform, false);
            var glow = glowObject.AddComponent<GlowGraphic>();
            glow.rectTransform.sizeDelta = source.rectTransform.sizeDelta;
            glow.source = source;
            glow.extraSize = 6f;
            AssertMeshRoundness(glow, Vector4.one * 26f, new Vector2(172f, 52f));

            var dirties = 0;
            glow.RegisterDirtyVerticesCallback(() => dirties++);
            styles.buttonRoundness = CornerRoundness.Pixels(7f);
            palette.SetStyles(styles);
            Assert.That(dirties, Is.GreaterThan(0), "A source theme shape edit must invalidate its glow mesh.");
            AssertMeshRoundness(glow, Vector4.one * 7f, new Vector2(172f, 52f));
            glow.extraSize = 10f;
            AssertMeshRoundness(glow, Vector4.one * 7f, new Vector2(180f, 60f));
            styles.buttonRoundness = CornerRoundness.Full;
            palette.SetStyles(styles);
            AssertMeshRoundness(glow, Vector4.one * 30f, new Vector2(180f, 60f));
        }

        [Test]
        public void ControlStyleOptIns_UseNearestPaletteAndRetainLocalOverrides()
        {
            var first = Palette();
            var styles = first.styles;
            styles.buttonTransitionDuration = .6f;
            styles.buttonDisabledOpacity = .3f;
            styles.buttonPressedScale = .8f;
            styles.toggleTransitionDuration = .4f;
            styles.inputTransitionDuration = .7f;
            styles.inputFocusWidth = 5f;
            first.SetStyles(styles);
            var parent = Parent(first, false);
            var button = Graphic(parent).gameObject.AddComponent<ButtonElement>();
            Set(button, "_graphic", button.GetComponent<RectangleGraphic>());
            Set(button, "_transitionDuration", .11f);
            Set(button, "_disabledAlpha", .22f);
            Set(button, "_scaleClicked", .77f);
            var toggleGraphic = Graphic(parent);
            var toggle = toggleGraphic.gameObject.AddComponent<ToggleElement>();
            Set(toggle, "_background", toggleGraphic);
            Set(toggle, "_nob", Graphic(toggle.gameObject));
            Set(toggle, "_transitionDuration", .12f);
            var toggleTheme = toggle.gameObject.AddComponent<ThemedToggle>();
            Set(toggleTheme, "_toggle", toggle);
            Set(toggleTheme, "_background", toggleGraphic);
            Set(toggleTheme, "_transitionDuration", .13f);
            var inputGraphic = Graphic(parent);
            var input = inputGraphic.gameObject.AddComponent<TMP_InputField>();
            var outline = input.gameObject.AddComponent<SelectedOutlineInputField>();
            Set(outline, "_input", input);
            Set(outline, "_graphic", inputGraphic);
            Set(outline, "_transitionDuration", .14f);
            Set(outline, "_outlineWidth", 3f);
            parent.SetActive(true);

            AssertLocalMetrics(button, toggle, toggleTheme, outline);
            button.useThemeTransition = button.useThemeDisabledOpacity = button.useThemePressedScale = true;
            toggle.useThemeTransition = toggleTheme.useThemeTransition = true;
            outline.useThemeTransition = outline.useThemeFocusWidth = true;
            AssertThemeMetrics(styles, button, toggle, toggleTheme, outline);

            var second = Palette();
            styles.buttonTransitionDuration = .8f;
            styles.buttonDisabledOpacity = .45f;
            styles.buttonPressedScale = .9f;
            styles.toggleTransitionDuration = .9f;
            styles.inputTransitionDuration = .5f;
            styles.inputFocusWidth = 7f;
            second.SetStyles(styles);
            parent.GetComponent<PaletteProvider>().palette = second;
            AssertThemeMetrics(styles, button, toggle, toggleTheme, outline);

            // Moving to a different parent must bind every control to that
            // parent's current styles without overwriting the local settings.
            var movedParent = Parent(first);
            button.transform.SetParent(movedParent.transform, false);
            toggle.transform.SetParent(movedParent.transform, false);
            outline.transform.SetParent(movedParent.transform, false);
            AssertThemeMetrics(first.styles, button, toggle, toggleTheme, outline);
            button.useThemeTransition = button.useThemeDisabledOpacity = button.useThemePressedScale = false;
            toggle.useThemeTransition = toggleTheme.useThemeTransition = false;
            outline.useThemeTransition = outline.useThemeFocusWidth = false;
            AssertLocalMetrics(button, toggle, toggleTheme, outline);
        }

        [Test]
        public void ButtonStyleDurationChange_PreservesHoverPressAndDisabledProgress()
        {
            var palette = Palette();
            palette.SetColor(ColorType.Accent, new Color(.1f, .2f, .3f, 1f));
            palette.SetColor(ColorType.Warning, new Color(.7f, .6f, .5f, 1f));
            var styles = palette.styles;
            styles.buttonTransitionDuration = .4f;
            styles.buttonPressedScale = .8f;
            styles.buttonDisabledOpacity = .3f;
            palette.SetStyles(styles);
            var parent = Parent(palette, false);
            var graphic = Graphic(parent);
            var button = graphic.gameObject.AddComponent<ButtonElement>();
            Set(button, "_graphic", graphic);
            Set(button, "_transitionCurve", AnimationCurve.Linear(0f, 0f, 1f, 1f));
            button.useThemeTransition = button.useThemeDisabledOpacity = button.useThemePressedScale = true;
            parent.SetActive(true);
            button.SetColors(Info(ColorType.Accent), Info(ColorType.Warning), default(ColorTone), default(ColorTone));
            Set(button, "_isHovering", true);
            Set(button, "_isPressing", true);
            Set(button, "_backgroundTimer", .1f);
            Set(button, "_pressTimer", .2f);
            Invoke(button, "UpdateVisuals", 0f);
            var beforeColor = graphic.graphicColor;
            var beforeScale = button.transform.localScale;
            Assert.That(beforeScale.x, Is.EqualTo(.9f).Within(.0001f));

            styles.buttonTransitionDuration = .8f;
            palette.SetStyles(styles);
            Invoke(button, "UpdateVisuals", 0f);
            AssertColor(graphic.graphicColor, beforeColor);
            Assert.That(button.transform.localScale, Is.EqualTo(beforeScale));
            Assert.That(Get<float>(button, "_backgroundTimer"), Is.EqualTo(.2f).Within(.0001f));
            Assert.That(Get<float>(button, "_pressTimer"), Is.EqualTo(.4f).Within(.0001f));

            button.interactable = false;
            Set(button, "_backgroundTimer", .8f);
            Set(button, "_pressTimer", .8f);
            Set(button, "_interactableTimer", .2f);
            Invoke(button, "UpdateVisuals", 0f);
            beforeColor = graphic.graphicColor;
            styles.buttonTransitionDuration = 1.6f;
            palette.SetStyles(styles);
            Invoke(button, "UpdateVisuals", 0f);
            AssertColor(graphic.graphicColor, beforeColor);
            Assert.That(Get<float>(button, "_interactableTimer"), Is.EqualTo(.4f).Within(.0001f));

            styles.buttonTransitionDuration = 0f;
            palette.SetStyles(styles);
            Invoke(button, "UpdateVisuals", 0f);
            Assert.That(graphic.graphicColor.a, Is.EqualTo(.3f).Within(.0001f));
            button.interactable = true;
            Invoke(button, "UpdateVisuals", 0f);
            Assert.That(button.transform.localScale.x, Is.EqualTo(.8f).Within(.0001f));
        }

        [Test]
        public void ToggleStyleDurationChange_RetimesKnobAndKeepsExistingColorTransition()
        {
            var palette = Palette();
            palette.SetColor(ColorType.Accent, new Color(.1f, .4f, .8f, 1f));
            palette.SetColor(ColorType.Surface, new Color(.12f, .14f, .16f, 1f));
            var styles = palette.styles;
            styles.toggleTransitionDuration = .4f;
            palette.SetStyles(styles);
            var parent = Parent(palette, false);
            var graphic = Graphic(parent);
            var knob = Graphic(graphic.gameObject);
            var toggle = graphic.gameObject.AddComponent<ToggleElement>();
            Set(toggle, "_background", graphic);
            Set(toggle, "_nob", knob);
            Set(toggle, "_leftnobPosition", Vector2.zero);
            Set(toggle, "_rightNobPosition", new Vector2(40f, 0f));
            Set(toggle, "_transitionCurve", AnimationCurve.Linear(0f, 0f, 1f, 1f));
            toggle.useThemeTransition = true;
            var theme = graphic.gameObject.AddComponent<ThemedToggle>();
            Set(theme, "_toggle", toggle);
            Set(theme, "_background", graphic);
            theme.useThemeTransition = true;
            parent.SetActive(true);
            toggle.value = true;
            Invoke(theme, "ApplyPalette");
            Set(toggle, "_timeSinceToggle", .1f);
            Set(theme, "_elapsed", .1f);
            var from = Get<Color>(theme, "_fromColor");
            var target = Get<Color>(theme, "_targetColor");
            var current = Color.Lerp(from, target, .25f);
            Set(theme, "_currentColor", current);
            Invoke(toggle, "UpdateVisuals", 0f);
            Assert.That(knob.transform.localPosition.x, Is.EqualTo(10f).Within(.0001f));

            styles.toggleTransitionDuration = .8f;
            palette.SetStyles(styles);
            Invoke(toggle, "UpdateVisuals", 0f);
            Assert.That(knob.transform.localPosition.x, Is.EqualTo(10f).Within(.0001f));
            Assert.That(Get<float>(toggle, "_timeSinceToggle"), Is.EqualTo(.2f).Within(.0001f));
            Assert.That(Get<float>(theme, "_elapsed"), Is.EqualTo(.2f).Within(.0001f));
            AssertColor(Get<Color>(theme, "_fromColor"), from);
            AssertColor(Get<Color>(theme, "_targetColor"), target);
            AssertColor(Get<Color>(theme, "_currentColor"), current);
            // An unrelated shape edit must not restart a color animation.
            styles.panelRoundness = CornerRoundness.Pixels(3f);
            palette.SetStyles(styles);
            Assert.That(Get<float>(theme, "_elapsed"), Is.EqualTo(.2f).Within(.0001f));
        }

        [Test]
        public void ImmediateTransitions_ReachTargetsEvenWhenCurveEndsBelowOne()
        {
            var palette = Palette();
            palette.SetColor(ColorType.Accent, new Color(.1f, .4f, .8f, 1f));
            palette.SetColor(ColorType.Surface, new Color(.12f, .14f, .16f, 1f));
            var styles = palette.styles;
            styles.toggleTransitionDuration = 0f;
            styles.inputTransitionDuration = 0f;
            styles.inputFocusWidth = 5f;
            palette.SetStyles(styles);
            var parent = Parent(palette, false);
            var graphic = Graphic(parent);
            var knob = Graphic(graphic.gameObject);
            var toggle = graphic.gameObject.AddComponent<ToggleElement>();
            var curve = AnimationCurve.Linear(0f, 0f, 1f, .5f);
            Set(toggle, "_background", graphic);
            Set(toggle, "_nob", knob);
            Set(toggle, "_leftnobPosition", Vector2.zero);
            Set(toggle, "_rightNobPosition", new Vector2(40f, 0f));
            Set(toggle, "_transitionCurve", curve);
            toggle.useThemeTransition = true;
            var theme = graphic.gameObject.AddComponent<ThemedToggle>();
            Set(theme, "_toggle", toggle);
            Set(theme, "_background", graphic);
            Set(theme, "_transitionCurve", curve);
            Set(theme, "_offTone", default(ColorTone));
            theme.useThemeTransition = true;
            var inputGraphic = Graphic(parent);
            var input = inputGraphic.gameObject.AddComponent<TMP_InputField>();
            var outline = input.gameObject.AddComponent<SelectedOutlineInputField>();
            Set(outline, "_input", input);
            Set(outline, "_graphic", inputGraphic);
            Set(outline, "_transitionCurve", curve);
            outline.useThemeTransition = outline.useThemeFocusWidth = true;
            parent.SetActive(true);

            toggle.value = true;
            Invoke(toggle, "UpdateVisuals", 0f);
            Invoke(theme, "LateUpdate");
            Invoke(outline, "UpdateVisuals", 0f, true);
            Assert.That(knob.transform.localPosition.x, Is.EqualTo(40f).Within(.0001f));
            AssertColor(graphic.graphicColor, palette.GetColor(ColorType.Accent));
            Assert.That(inputGraphic.outlineSize, Is.EqualTo(5f).Within(.0001f));

            // The editor preview settles immediately even if the runtime
            // theme specifies a duration and a curve with a partial endpoint.
            styles.toggleTransitionDuration = .4f;
            palette.SetStyles(styles);
            toggle.value = false;
            Invoke(theme, "LateUpdate");
            AssertColor(graphic.graphicColor, palette.GetColor(ColorType.Surface));
        }

        [Test]
        public void InputStyleChanges_PreserveFocusProgressAndRequiredFieldFeedback()
        {
            var palette = Palette();
            palette.SetColor(ColorType.Accent, new Color(.1f, .3f, .8f, 1f));
            palette.SetColor(ColorType.Danger, new Color(.8f, .1f, .2f, 1f));
            var styles = palette.styles;
            styles.inputTransitionDuration = .4f;
            styles.inputFocusWidth = 5f;
            palette.SetStyles(styles);
            var parent = Parent(palette, false);
            var graphic = Graphic(parent);
            var input = graphic.gameObject.AddComponent<TMP_InputField>();
            var outline = graphic.gameObject.AddComponent<SelectedOutlineInputField>();
            Set(outline, "_input", input);
            Set(outline, "_graphic", graphic);
            Set(outline, "_transitionCurve", AnimationCurve.Linear(0f, 0f, 1f, 1f));
            outline.useThemeTransition = outline.useThemeFocusWidth = true;
            var required = graphic.gameObject.AddComponent<ThemedInputFieldMandatory>();
            Set(required, "_inputField", input);
            Set(required, "_graphic", outline);
            Set(required, "_transitionDuration", 0f);
            parent.SetActive(true);
            Invoke(required, "OnValueUpdated", "Name");
            Invoke(required, "OnValueUpdated", "");
            Invoke(required, "UpdateOutline");
            Assert.That(outline.outlineWidthNotSelected, Is.EqualTo(1f));
            Invoke(outline, "UpdateVisuals", .1f, true);
            Invoke(outline, "UpdateVisuals", 0f, true);
            Assert.That(graphic.outlineSize, Is.EqualTo(2f).Within(.0001f));

            styles.inputTransitionDuration = .8f;
            palette.SetStyles(styles);
            Invoke(outline, "UpdateVisuals", 0f, true);
            Assert.That(graphic.outlineSize, Is.EqualTo(2f).Within(.0001f));
            Assert.That(Get<float>(outline, "_timeSinceToggle"), Is.EqualTo(.2f).Within(.0001f));
            styles.inputFocusWidth = 9f;
            palette.SetStyles(styles);
            Invoke(outline, "UpdateVisuals", 1f, true);
            Invoke(outline, "UpdateVisuals", 0f, true);
            Assert.That(graphic.outlineSize, Is.EqualTo(9f).Within(.0001f));
            AssertColor(graphic.outlineColor, palette.GetColor(ColorType.Danger));
            Assert.That(outline.outlineWidthNotSelected, Is.EqualTo(1f), "Theme focus width must not overwrite required-field feedback.");
            Invoke(outline, "UpdateVisuals", 1f, false);
            Invoke(outline, "UpdateVisuals", 0f, false);
            Assert.That(graphic.outlineSize, Is.EqualTo(1f).Within(.0001f));
            Invoke(required, "OnValueUpdated", "Valid name");
            Invoke(required, "UpdateOutline");
            Invoke(outline, "UpdateVisuals", 0f, false);
            Assert.That(graphic.outlineSize, Is.Zero.Within(.0001f));
            AssertColor(graphic.outlineColor, palette.GetColor(ColorType.Accent));
        }

        private ColorPalette Palette(float? panelRoundness = null)
        {
            var palette = ScriptableObject.CreateInstance<ColorPalette>();
            _created.Add(palette);
            if (panelRoundness.HasValue) SetPanelRoundness(palette, panelRoundness.Value);
            return palette;
        }

        private GameObject Parent(ColorPalette palette, bool active = true)
        {
            var parent = new GameObject("Theme styles test", typeof(RectTransform));
            _created.Add(parent);
            parent.SetActive(false);
            parent.AddComponent<PaletteProvider>().palette = palette;
            parent.SetActive(active);
            return parent;
        }

        private static RectangleGraphic Graphic(GameObject parent)
        {
            var child = new GameObject("Styled graphic", typeof(RectTransform));
            child.transform.SetParent(parent.transform, false);
            var graphic = child.AddComponent<RectangleGraphic>();
            graphic.rectTransform.sizeDelta = new Vector2(160f, 40f);
            return graphic;
        }

        private static void SetPanelRoundness(ColorPalette palette, float pixels)
        {
            var styles = palette.styles;
            styles.panelRoundness = CornerRoundness.Pixels(pixels);
            palette.SetStyles(styles);
        }

        private static void AssertRoundness(RectangleGraphic graphic, Vector4 expected)
        {
            Assert.That(graphic.resolvedRoundness, Is.EqualTo(expected));
            AssertMeshRoundness(graphic, expected);
        }

        private static void AssertMeshRoundness(SignedDistanceFieldGraphic graphic, Vector4 expected, Vector2? dimensions = null)
        {
            using (var mesh = new VertexHelper())
            {
                var populate = graphic.GetType().GetMethod("OnPopulateMesh", BindingFlags.NonPublic | BindingFlags.Instance,
                    null, new[] { typeof(VertexHelper) }, null);
                Assert.That(populate, Is.Not.Null);
                populate.Invoke(graphic, new object[] { mesh });
                Assert.That(mesh.currentVertCount, Is.GreaterThan(0));
                var vertex = UIVertex.simpleVert;
                for (var i = 0; i < mesh.currentVertCount; i++)
                {
                    mesh.PopulateUIVertex(ref vertex, i);
                    Assert.That(vertex.uv1, Is.EqualTo(expected), "The shader must receive the resolved corner radii.");
                    if (dimensions.HasValue)
                        Assert.That(new Vector2(vertex.uv0.z, vertex.uv0.w), Is.EqualTo(dimensions.Value),
                            "Glow corners and its effective geometry must use the same expanded dimensions.");
                }
            }
        }

        private static void AssertLocalMetrics(ButtonElement button, ToggleElement toggle, ThemedToggle toggleTheme, SelectedOutlineInputField outline)
        {
            Assert.That(button.transitionDuration, Is.EqualTo(.11f));
            Assert.That(button.disabledOpacity, Is.EqualTo(.22f));
            Assert.That(button.pressedScale, Is.EqualTo(.77f));
            Assert.That(toggle.transitionDuration, Is.EqualTo(.12f));
            Assert.That(toggleTheme.transitionDuration, Is.EqualTo(.13f));
            Assert.That(outline.transitionDuration, Is.EqualTo(.14f));
            Assert.That(outline.focusWidth, Is.EqualTo(3f));
        }

        private static void AssertThemeMetrics(ThemeStyles styles, ButtonElement button, ToggleElement toggle, ThemedToggle toggleTheme, SelectedOutlineInputField outline)
        {
            Assert.That(button.transitionDuration, Is.EqualTo(styles.buttonTransitionDuration));
            Assert.That(button.disabledOpacity, Is.EqualTo(styles.buttonDisabledOpacity));
            Assert.That(button.pressedScale, Is.EqualTo(styles.buttonPressedScale));
            Assert.That(toggle.transitionDuration, Is.EqualTo(styles.toggleTransitionDuration));
            Assert.That(toggleTheme.transitionDuration, Is.EqualTo(styles.toggleTransitionDuration));
            Assert.That(outline.transitionDuration, Is.EqualTo(styles.inputTransitionDuration));
            Assert.That(outline.focusWidth, Is.EqualTo(styles.inputFocusWidth));
        }

        private static void Set(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }

        private static T Get<T>(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            return (T)field.GetValue(target);
        }

        private static void Invoke(object target, string methodName, params object[] arguments)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            method.Invoke(target, arguments);
        }

        private static ColorInfo Info(ColorType role) => new ColorInfo { enabled = true, color = role };

        private static void AssertColor(Color actual, Color expected)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(.0001f));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(.0001f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(.0001f));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(.0001f));
        }
    }
}
