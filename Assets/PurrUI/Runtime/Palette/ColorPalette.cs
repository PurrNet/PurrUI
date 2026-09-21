using System;
using UnityEngine;

namespace PurrNet.UI
{
    [Serializable]
    [CreateAssetMenu(menuName = "PurrNet/PurrUI/Color Palette", order = 500)]
    public class ColorPalette : ScriptableObject
    {
        [Header("Base")]
        [SerializeField] private Color _black = Color.black;
        [SerializeField] private Color _white  = Color.white;
        [SerializeField] private Color _muted  = Color.gray;
        // Named presets like Color.gray1/dodgerBlue are Unity 6 only; hardcoded for 2022 support.
        [Header("Backgrounds")]
        [SerializeField] private Color _background = Color.black;
        [SerializeField] private Color _foreground = Color.white;
        [SerializeField] private Color _surface = new Color(0.1f, 0.1f, 0.1f, 1f);
        [SerializeField] private Color _surfaceForeground = Color.white;
        [Header("Primary Colors")]
        [SerializeField] private Color _accent = new Color(0.1176471f, 0.5647059f, 1f, 1f);
        [SerializeField] private Color _accentForeground = Color.white;
        [Header("Status Colors")]
        [SerializeField] private Color _success = new Color(0.2352941f, 0.7019608f, 0.4431373f, 1f);
        [SerializeField] private Color _successForeground = new Color(0.1f, 0.1f, 0.1f, 1f);
        [SerializeField] private Color _warning = new Color(0.854902f, 0.6470588f, 0.1254902f, 1f);
        [SerializeField] private Color _warningForeground = new Color(0.1f, 0.1f, 0.1f, 1f);
        [SerializeField] private Color _danger = new Color(0.6470588f, 0.1647059f, 0.1647059f, 1f);
        [SerializeField] private Color _dangerForeground = Color.white;

        [Header("Default Styles")]
        [Tooltip("Shared styling for elements using theme defaults. Custom element settings remain local.")]
        [SerializeField] private ThemeStyles _styles = ThemeStyles.Default;

        public ThemeStyles styles => _styles;

        public event Action onChange;

        private void OnValidate()
        {
            _styles = _styles.Sanitize();
            onChange?.Invoke();
        }

        public void SetStyles(ThemeStyles value)
        {
            _styles = value.Sanitize();
            onChange?.Invoke();
        }

        public void SetColor(ColorType type, Color color)
        {
            switch (type)
            {
                case ColorType.Black:
                    _black = color;
                    break;
                case ColorType.White:
                    _white = color;
                    break;
                case ColorType.Muted:
                    _muted = color;
                    break;
                case ColorType.Background:
                    _background = color;
                    break;
                case ColorType.Surface:
                    _surface = color;
                    break;
                case ColorType.Accent:
                    _accent = color;
                    break;
                case ColorType.Success:
                    _success = color;
                    break;
                case ColorType.Warning:
                    _warning = color;
                    break;
                case ColorType.Danger:
                    _danger = color;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            onChange?.Invoke();
        }

        public Color GetColor(ColorType type)
        {
            return type switch
            {
                ColorType.Black => _black,
                ColorType.White => _white,
                ColorType.Muted => _muted,
                ColorType.Background => _background,
                ColorType.Surface => _surface,
                ColorType.Accent => _accent,
                ColorType.Success => _success,
                ColorType.Warning => _warning,
                ColorType.Danger => _danger,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        public void SetContrast(ColorType type, Color color)
        {
            switch (type)
            {
                case ColorType.Background:
                    _foreground = color;
                    break;
                case ColorType.Surface:
                    _surfaceForeground = color;
                    break;
                case ColorType.Accent:
                    _accentForeground = color;
                    break;
                case ColorType.Success:
                    _successForeground = color;
                    break;
                case ColorType.Warning:
                    _warningForeground = color;
                    break;
                case ColorType.Danger:
                    _dangerForeground = color;
                    break;
                case ColorType.Black:
                case ColorType.White:
                case ColorType.Muted:
                    throw new ArgumentException($"`{type}` has no dedicated contrast color.", nameof(type));
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            onChange?.Invoke();
        }

        public Color GetContrast(ColorType type)
        {
            return type switch
            {
                ColorType.Black => _white,
                ColorType.White => _black,
                ColorType.Muted => _white,
                ColorType.Background => _foreground,
                ColorType.Surface => _surfaceForeground,
                ColorType.Accent => _accentForeground,
                ColorType.Success => _successForeground,
                ColorType.Warning => _warningForeground,
                ColorType.Danger => _dangerForeground,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }
    }
}
