using Sirenix.OdinInspector;
using UnityEngine;

namespace ThreeDee
{
    /// <summary>
    /// A renderer for an individual sprite. This is used in a way similar to a MeshRenderer or SpriteRenderer component.
    /// </summary>
    [DefaultExecutionOrder(ThreeDeeSpriteSurface.ExecutionOrder)]
    public class ThreeDeeSpriteRenderer : MonoBehaviour, IThreeDeeSpriteRenderer
    {
        //This is used to control if 3D models are unparented from their original source upon being allocated.
        //If this is set to true the the sprites will only ever have to Issue a new render command if something
        //fundamentally changes with the ratios or offsets, otherwise the model is effectively where it needs to be forever.
        //If false, sprites will have to issue a new render command every single frame because they will need to update their
        //model position temporarily for them to be captured on the surface by the RT camera.
        public static readonly bool ReparentModel = true;

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
                        ReleaseSprite();
                        AllocateSprite();
                    }
                    SpriteBillboardScale = TileResolution / PrerenderScale;
                    AlignBillboard();
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
                    AlignBillboard();
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

        [SerializeField]
        [HideInInspector]
        public Vector2 _TileOffset;

        [Tooltip("An offset used to refine position of the sprite within its allocated tile space.")]
        [ShowInInspector]
        public Vector2 TileOffset
        {
            get => _TileOffset;
            set
            {
                if (_TileOffset != value)
                {
                    _TileOffset = value;
                    AlignBillboard();
                }
            }
        }

        [SerializeField]
        [HideInInspector]
        public Vector3 _BillboardOffset;

        [Tooltip("An offset used to refine position of the sprite billboard in worldspace relative to it's parent.")]
        [ShowInInspector]
        public Vector3 BillboardOffset
        {
            get => _BillboardOffset;
            set
            {
                if(_BillboardOffset != value)
                {
                    _BillboardOffset = value;
                    AlignBillboard();
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

        [Tooltip("This can be used to forceably set the surface id you want this sprite to be created for. Any value less than zero will result in the first surface with available space being used.")]
        public int SurfaceId = -1;

        int SpriteHandle = -1;
        int ChainHandle = -1;
        RenderCommand RenderCmd;


        private void Start()
        {
            //forces billboard to update
            float prs = _PreRenderScale;
            PrerenderScale = 104.375f;
            PrerenderScale = prs;
        }

        private void OnEnable()
        {
            //make a one-time request for our section of the render texture.
            //this only needs to be done again if we reallocate the sprite
            //We need to add a delay of at least 1 frame or the surface's command
            //buffer may be wiped before we ever process it
            Invoke(nameof(Init), 0.05f);
        }

        private void Init()
        {
            AllocateSprite();
            IssueRenderRequest();
        }

        private void OnDisable()
        {
            ReleaseSprite();
        }

        /// <summary>
        /// 
        /// </summary>
        void IssueRenderRequest()
        {
            #if UNITY_EDITOR
            if (!Application.isPlaying) return;
            #endif
            if (SpriteHandle >= 0 && ChainHandle >= 0)
                ThreeDeeSurfaceChain.Instance.AddCommand(RenderCmd);
        }

        /// <summary>
        /// 
        /// </summary>
        void GenerateCommand()
        {
            RenderCmd = new RenderCommand(
                        this.SpriteHandle,
                        this.TileResolution,
                        this.PrerenderScale,
                        TileOffset,
                        Vector3.zero,
                        ModelTrans,
                        ChainHandle
                        );
        }

        /// <summary>
        /// Helper metho used to initialize this sprite on a surface chain.
        /// </summary>
        void AllocateSprite()
        {
            #if UNITY_EDITOR
            if (!Application.isPlaying) return;
            #endif
            if (ThreeDeeSurfaceChain.Instance != null && SpriteHandle < 0)
                (ChainHandle, SpriteHandle) = ThreeDeeSurfaceChain.Instance.AllocateNewSprite(this, SurfaceId, ReparentModel);
           
            //rebind our billboard to use the rendertexture from the surface it pre-renders to
            this.SpriteBillboard.GetComponent<MeshRenderer>().sharedMaterial = ThreeDeeSurfaceChain.Instance.GetSurfaceBillboardMaterial(ChainHandle);
            GenerateCommand();
        }

        /// <summary>
        /// 
        /// </summary>
        void ReleaseSprite()
        {
            if (ThreeDeeSurfaceChain.Instance == null || SpriteHandle < 0)
                return;

            ThreeDeeSurfaceChain.Instance.ReleaseSprite(SpriteHandle, ChainHandle, ReparentModel);
            SpriteHandle = -1;
            ChainHandle = -1;
        }

        /// <summary>
        /// Helper for repositioning the billboard quad based on alignment and scale.
        /// Currently only supports Center, TopCenter, ad BottomCenter.
        /// </summary>
        void AlignBillboard()
        {
            _SpriteBillboard.transform.localPosition = _BillboardOffset + (new Vector3(0, -_TileOffset.y, 0) * SpriteBillboardScale);
            IssueRenderRequest();
        }
    }


}