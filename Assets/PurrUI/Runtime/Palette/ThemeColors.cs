using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UI;

namespace PurrNet.UI
{
    [MovedFrom(true, "PurrNet.UI.HeroUI", "PurrUI.HeroUI.Runtime", "ThemeColors")]
    public static class ThemeColors
    {
        public static void Set(Graphic graphic, ColorInfo color, int slot = 0)
        {
            if (!graphic)
                return;

            if (graphic.TryGetComponent<ColoredGraphic>(out var colored))
            {
                colored.SetColor(slot, color);
                colored.Refresh();
                return;
            }

            // Custom entry prefabs can still resolve their colors without a binding.
            var palette = graphic.GetComponentInParent<IPaletteProvider>(true)?.palette;
            if (!color.enabled || !palette)
                return;

            if (graphic is IColored slots)
                slots.SetColor(slot, color.GetColor(palette));
            else
                graphic.color = color.GetColor(palette);
        }

        public static void Set(ButtonElement button, ColorInfo normal, ColorInfo hover,
            ColorTone? normalTone = null, ColorTone? hoverTone = null)
        {
            if (!button)
                return;

            button.SetColors(normal, hover, normalTone, hoverTone);
        }
    }
}
