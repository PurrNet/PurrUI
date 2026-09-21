using UnityEngine;
using UnityEngine.UI;

namespace PurrNet.UI
{
    /// <summary>Keeps visual opacity independent from the palette's RGB colors.</summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class GraphicOpacity : MonoBehaviour
    {
        [SerializeField] private Graphic _graphic;
        [SerializeField, Range(0f, 1f)] private float _opacity = 1f;
        [SerializeField] private bool _fillOnly;

        private void Reset() => _graphic = GetComponent<Graphic>();
        private void OnEnable() => Apply();
        private void LateUpdate() => Apply();

        private void Apply()
        {
            if (!_graphic)
                return;
            if (_fillOnly && _graphic is SignedDistanceFieldGraphic sdf)
            {
                var color = sdf.graphicColor;
                color.a = _opacity;
                sdf.graphicColor = color;
            }
            else
            {
                _graphic.canvasRenderer.SetAlpha(_opacity);
            }
        }
    }
}
