using Sirenix.OdinInspector;
using UnityEngine;

namespace ThreeDee
{
    [DefaultExecutionOrder(ThreeDeeSpriteEngine.ExecutionOrder)]
    public class ThreeDeeSprite : MonoBehaviour
    {
        [SerializeField]
        [HideInInspector]
        int _TileResolution = 1;
        [ShowInInspector]
        [Tooltip("The number of tile chunks squared to use for rendering the model to a sprite. A tile chunk's size is defined within the ThreeDeeSpriteEngine.")]
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
                        if (ThreeDeeRenderChain.Instance != null)
                        {
                            ThreeDeeRenderChain.Instance.ReleaseSprite(SpriteHandle, ChainHandle);
                            (ChainHandle, SpriteHandle) = ThreeDeeRenderChain.Instance.AllocateNewSprite(this, ChainHandle);
                        }
                    }
                }
            }
        }

        [SerializeField]
        [HideInInspector]
        float _SpriteScale = 1;
        [ShowInInspector]
        [Tooltip("The scale of the model being rendered within the allocated pixel region of the pre-render texture.")]
        [MinValue(0.01f)]
        public float SpriteScale
        {
            get => _SpriteScale;
            set
            {
                if (_SpriteScale != value)
                {
                    _SpriteScale = value;
                    //if (_SpriteBillboard != null)
                    //    _SpriteBillboard.transform.localScale = Vector3.one / _SpriteScale;
                }
            }
        }

        [Tooltip("An offset used to refine position of the sprite within its allocated space.")]
        public Vector2 Offset;

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
            SpriteScale = _SpriteScale; //forces billboard to update
            AllocateSprite();
        }

        private void OnEnable()
        {
            AllocateSprite();
        }

        private void OnDisable()
        {
            ThreeDeeRenderChain.Instance.ReleaseSprite(SpriteHandle, ChainHandle);
            SpriteHandle = -1;
            ChainHandle = -1;
        }

        void LateUpdate()
        {
            if (SpriteHandle >= 0 && ChainHandle >= 0)
            {
                ThreeDeeRenderChain.Instance.AddCommand(new RenderCommand()
                {
                    SpriteHandle = this.SpriteHandle,
                    SpriteScale = this.SpriteScale,
                    TileResolution = this.TileResolution,
                    Offset = Offset,
                    Obj = ModelTrans,
                    ChainId = ChainHandle,
                });
            }
        }

        void AllocateSprite()
        {
            if (ThreeDeeSpriteEngine.Instance != null && SpriteHandle < 0)
                (ChainHandle, SpriteHandle) = ThreeDeeRenderChain.Instance.AllocateNewSprite(this, ForcedChainId);
        }

    }


}