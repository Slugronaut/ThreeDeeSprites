using Sirenix.OdinInspector;
using UnityEngine;

namespace ThreeDee
{
    /// <summary>
    /// A renderer for an individual sprite. This is used in a way similar to a MeshRenderer or SpriteRenderer component.
    /// </summary>
    [DefaultExecutionOrder(ThreeDeeSpriteSurface.ExecutionOrder)]
    public class ThreeDeeSpriteRenderer : MonoBehaviour
    {
        [SerializeField]
        [HideInInspector]
        int _TileResolution = 1;
        [ShowInInspector]
        [Tooltip("The number of tile chunks squared to use for rendering the model to a sprite. A tile chunk's size is defined within the ThreeDeeSpriteSurface. The more chunks used, the higher the sprite resolution.")]
        [MinValue(1)]
        public int TileResolution
        {
            get => _TileResolution;
            set
            {
                if (value < 1) return;
                if (_TileResolution != value)
                {
                    _TileResolution = value;
                    if (SpriteHandle >= 0)
                    {
                        if (ThreeDeeSurfaceChain.Instance != null)
                        {
                            ThreeDeeSurfaceChain.Instance.ReleaseSprite(SpriteHandle, ChainHandle);
                            (ChainHandle, SpriteHandle) = ThreeDeeSurfaceChain.Instance.AllocateNewSprite(this, ChainHandle);
                        }
                    }
                    SpriteBillboardScale = TileResolution / PrerenderScale;
                    AlignBillboard(_SpriteBillboardAlignment);
                }
            }
        }

        [SerializeField]
        [HideInInspector]
        float _PreRenderScale = 1;
        [ShowInInspector]
        [Tooltip("The scale of the model being rendered within the allocated pixel region of the pre-render texture. This can be used to scale the 3D model so that it fits within the desired space of it's defined tile region. WARNING: If this value is too big the 3D model will overflow its tile bounds and bleed into other sprite tiles.")]
        [MinValue(0.001f)]
        public float PrerenderScale
        {
            get => _PreRenderScale;
            set
            {
                if (_PreRenderScale != value)
                {
                    _PreRenderScale = value;
                    SpriteBillboardScale = TileResolution / PrerenderScale;
                    AlignBillboard(_SpriteBillboardAlignment);
                }
            }
        }
       
        [SerializeField]
        [HideInInspector]
        float _SpriteBillboardScale = 1;

        [ShowInInspector]
        [ReadOnly]
        [Tooltip("A uniform scale to apply to the sprite billboard. This is equal to the Tile Resolution divided by the Prerender Scale and is used to determine the uniform scale of the sprite billboard quad so that it maintains a worldspace size equal to that of the original model.")]
        public float SpriteBillboardScale
        {
            get => _SpriteBillboardScale;
            private set
            {
                if (_SpriteBillboardScale != value)
                {
                    _SpriteBillboardScale = value;
                    _SpriteBillboard.transform.localScale = Vector3.one * value;
                }
            }
        }

        [Tooltip("An offset used to refine position of the sprite within its allocated tile space.")]
        public Vector2 TileOffset;

        [SerializeField]
        [HideInInspector]
        SpriteAlignment _SpriteBillboardAlignment = SpriteAlignment.BottomCenter;

        [ShowInInspector]
        [Tooltip("How the billboard sprite quad is aligned relative to its scale.")]
        public SpriteAlignment SpriteBillboardAlignment
        {
            get => _SpriteBillboardAlignment;
            set
            {
                if(value != _SpriteBillboardAlignment)
                {
                    _SpriteBillboardAlignment = value;
                    AlignBillboard(value);
                }
            }
        }

        /// <summary>
        /// Helper for repositioning the billboard quad based on alignment and scale.
        /// Currently only support Center, TopCenter, ad BottomCenter.
        /// </summary>
        void AlignBillboard(SpriteAlignment alignment)
        {
            switch(alignment)
            {
                case SpriteAlignment.Center:
                    {
                        _SpriteBillboard.transform.localPosition = Vector3.zero;
                        break;
                    }
                case SpriteAlignment.BottomCenter:
                    {
                        _SpriteBillboard.transform.localPosition = new Vector3(0, _SpriteBillboardScale / 2, 0);
                        break;
                    }
                case SpriteAlignment.TopCenter:
                    {
                        _SpriteBillboard.transform.localPosition = new Vector3(0, _SpriteBillboardScale + (_SpriteBillboardScale / 2), 0);
                        break;
                    }
                default:
                    {
                        _SpriteBillboard.transform.localPosition = new Vector3(0, 0, 0);
                        break;
                    }
            }
        }

        [SerializeField]
        [HideInInspector]
        MeshFilter _SpriteBillboard;
        [ShowInInspector]
        [Tooltip("The mesh that will be used to display the pre-rendered sprite. This must point to a quad mesh.")]
        public MeshFilter SpriteBillboard
        {
            get => _SpriteBillboard;
            set
            {
                if (_SpriteBillboard != value)
                    _SpriteBillboard = value;
            }
        }

        [SerializeField]
        [HideInInspector]
        Transform _ModelTrans;
        [ShowInInspector]
        [Tooltip("The root of the 3D model that will be pre-rendered.")]
        public Transform ModelTrans
        {
            get => _ModelTrans;
            private set
            {
                if (value != _ModelTrans)
                    _ModelTrans = value;
            }
        }

        [Tooltip("This can be used to forceably set the chain id you want this sprite to be created for. Any value less than zero will result in the first chain with available space being used.")]
        public int ForcedChainId = -1;

        int SpriteHandle = -1;
        int ChainHandle = -1;



        private void Start()
        {
            PrerenderScale = _PreRenderScale; //forces billboard to update
            AllocateSprite();
        }

        private void OnEnable()
        {
            AllocateSprite();
        }

        private void OnDisable()
        {
            ThreeDeeSurfaceChain.Instance.ReleaseSprite(SpriteHandle, ChainHandle);
            SpriteHandle = -1;
            ChainHandle = -1;
        }

        void LateUpdate()
        {
            if (SpriteHandle >= 0 && ChainHandle >= 0)
            {
                ThreeDeeSurfaceChain.Instance.AddCommand(
                    new RenderCommand(
                        this.SpriteHandle,
                        this.TileResolution,
                        this.PrerenderScale,
                        TileOffset,
                        ModelTrans,
                        ChainHandle)
                    );
            }
        }

        void AllocateSprite()
        {
            if (ThreeDeeSpriteSurface.Instance != null && SpriteHandle < 0)
                (ChainHandle, SpriteHandle) = ThreeDeeSurfaceChain.Instance.AllocateNewSprite(this, ForcedChainId);
        }

    }


}