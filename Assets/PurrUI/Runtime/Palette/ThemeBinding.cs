using UnityEngine;

namespace PurrNet.UI
{
    /// <summary>Connects a component to its nearest palette and tracks palette changes.</summary>
    public abstract class ThemeBinding : MonoBehaviour
    {
        private IPaletteProvider _provider;
        private bool _dirty;

        protected ColorPalette palette => _provider?.palette;

        protected virtual void OnEnable()
        {
            ResolveProvider();
            ApplyPalette();
        }

        protected virtual void OnDisable()
        {
            if (_provider != null)
                _provider.onColorChange -= ApplyPalette;
            _provider = null;
        }

        protected virtual void Update()
        {
            if (_provider == null || _dirty)
            {
                ResolveProvider();
                if (_provider != null)
                    ApplyPalette();
                _dirty = false;
            }
        }

        private void OnTransformParentChanged()
        {
            if (!isActiveAndEnabled)
                return;
            ResolveProvider();
            ApplyPalette();
        }

        protected virtual void OnValidate()
        {
            // OnValidate may run while Unity loads objects; apply on the main loop.
            _dirty = true;
        }

        private void ResolveProvider()
        {
            var provider = GetComponentInParent<IPaletteProvider>(true);
            if (ReferenceEquals(provider, _provider))
                return;
            if (_provider != null)
                _provider.onColorChange -= ApplyPalette;
            _provider = provider;
            if (_provider != null)
                _provider.onColorChange += ApplyPalette;
        }

        protected abstract void ApplyPalette();
    }
}
