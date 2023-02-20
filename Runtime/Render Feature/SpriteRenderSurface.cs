using Sirenix.OdinInspector;
using UnityEngine;

namespace ThreeDee
{
    /// <summary>
    /// 
    /// </summary>
    [CreateAssetMenu(fileName = "Sprite Render Surface", menuName = "Assets/Sprite Render Surface")]
    public class SpriteRenderSurface : ScriptableObject
    {
        public bool DirtyFlag = false;

        #region Public Properties
        [SerializeField]
        [HideInInspector]
        float _PixelScale = 1;
        [ShowInInspector]
        [Tooltip("The relation between the render texture size, the tile size, and the orthographic view size.")]
        [MinValue(0.01f)]
        public float PixelScale
        {
            get => _PixelScale;
            set
            {
                if (_PixelScale != value)
                {
                    _PixelScale = value;
                    DirtyFlag = true;
                }
            }
        }

        [SerializeField]
        [HideInInspector]
        int _TileSize = 32;
        [ShowInInspector]
        [Tooltip("Must be a factor of the RenderTexture's resolution. The smallest squared tile of pixels on the reder texture that can be allocated to a single sprite. Larger sprites may require multiples of these.")]
        [ValidateInput("ValidateTileSize", "Must be a factor of camera's target resolution.", InfoMessageType.Error)]
        [MinValue(1)]
        public int TileSize
        {
            get => _TileSize;
            set
            {
                if (_TileSize != value)
                {
                    _TileSize = value;
                    DirtyFlag = true;
                }
            }
        }

        [SerializeField]
        [HideInInspector]
        RenderTexture _RenderTarget;
        [ShowInInspector]
        [Required("A render texture must be supplied!")]
        [Tooltip("The render texture to which models will be rendered as sprites.")]
        public RenderTexture RenderTarget
        {
            get => _RenderTarget;
            set
            {
                if (value != _RenderTarget)
                {
                    _RenderTarget = value;
                    DirtyFlag = true;
                }
            }
        }

        public float NearClip = 0;
        public float FarClip = 6;
        #endregion


        #region Editor
#if UNITY_EDITOR
        /// <summary>
        /// Validates the inspector input.
        /// </summary>
        /// <param name="tileSize"></param>
        /// <returns></returns>
        private bool ValidateTileSize(int tileSize)
        {
            if (_RenderTarget == null) return false;
            var desc = _RenderTarget.descriptor;
            if (desc.width < tileSize) return false;
            if (desc.height < tileSize) return false;
            if (desc.width % tileSize != 0) return false;
            if (desc.height % tileSize != 0) return false;
            return true;
        }
#endif
        #endregion
    }

}
