using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;

namespace ThreeDee
{
    /// <summary>
    /// A renderer for an individual sprite. This is used in a way similar to a MeshRenderer or SpriteRenderer component.
    /// </summary>
    [DefaultExecutionOrder(ThreeDeeSpriteSurface.ExecutionOrder)]
    public class ThreeDeeSpriteRenderer : MonoBehaviour, IThreeDeeSpriteRenderer
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
                        ReleaseSprite();
                        AllocateSprite();
                    }
                    SpriteBillboardScale = TileResolution / PrerenderScale;
                    AlignBillboard();
                    IssueRenderRequest();
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
                    UpdateSpriteImage();
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
                    UpdateSpriteImage();
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
                if (_BillboardOffset != value)
                {
                    _BillboardOffset = value;
                    UpdateSpriteImage();
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

        [SerializeField]
        RendererDescriptorAsset _DescriptorAsset;
        public RendererDescriptorAsset DescriptorAsset { get => _DescriptorAsset; }

        public bool Allocated { get; private set; }
        public int LastCommandHandle { get; private set; }


        bool Started;
        int SpriteHandle = -1;
        int SurfaceHandle = -1;
        RenderCommand RenderCmd;
        Transform OriginalModelParent;


        #region Unity Events
        private void Start()
        {
            //forces billboard to update
            float prs = _PreRenderScale;
            PrerenderScale = 104.375f;
            PrerenderScale = prs;
            Init();
            Started = true;
        }

        private void OnEnable()
        {
            //Unity can be a real stupid piece of shit sometimes we we have to write bullshit like this now and then...
            //mostly due to race conditions and the fact that we can't enable things at certain times anymore. they
            //use 'backwards compatibility' all of the time to avoid fixing stuff and then go and break shit like this
            //anway. whatever.
            if (!Started) return;
            Init();
        }

        private void Init()
        {
            AllocateSprite();
            IssueRenderRequest();
        }

        private void OnDisable()
        {
            ReleaseSprite();
            if(_DescriptorAsset.CompactSpritesOnDisable)
                ThreeDeeSurfaceChain.Instance.CompactSprites();
        }

        private void OnDestroy()
        {
            if(DescriptorAsset.ModelParentMode == RendererDescriptorAsset.ModelParentModes.UnparentOnly &&
               !ThreeDeeSurfaceChain.AppQuitting)
                Destroy(ModelTrans.gameObject);
        }
        #endregion


        #region Private Methods
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
                        SurfaceHandle
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
            {
                (SurfaceHandle, SpriteHandle) = ThreeDeeSurfaceChain.Instance.AllocateNewSprite(this);

                //rebind our billboard to use the rendertexture from the surface it pre-renders to
                this.SpriteBillboard.GetComponent<MeshRenderer>().sharedMaterial = ThreeDeeSurfaceChain.Instance.GetSurfaceBillboardMaterial(SurfaceHandle);
                AlignBillboard();
                GenerateCommand();
                Allocated = true;
            }
            else Debug.LogError("No ThreeDeeSurfaceChain found in the scene. Cannot allocate sprite.");
        }

        /// <summary>
        /// 
        /// </summary>
        void ReleaseSprite()
        {
            if (ThreeDeeSurfaceChain.Instance == null || SpriteHandle < 0)
                return;

            Allocated = false;
            ThreeDeeSurfaceChain.Instance.ReleaseSprite(SpriteHandle, SurfaceHandle);
            SpriteHandle = -1;
            SurfaceHandle = -1;
        }

        /// <summary>
        /// Helper for repositioning the billboard quad based on alignment and scale.
        /// Currently only supports Center, TopCenter, and BottomCenter.
        /// </summary>
        void AlignBillboard()
        {
            _SpriteBillboard.transform.localPosition = _BillboardOffset + (new Vector3(0, -_TileOffset.y, 0) * SpriteBillboardScale);
        }

        /// <summary>
        /// Resets the sprite within the render chain to fully update all stats.
        /// A render request is issued afterwards to update the sprite billboard.
        /// </summary>
        void UpdateSpriteImage()
        {
            #if UNITY_EDITOR
            if (!Application.isPlaying) return;
            #endif

            if (SpriteHandle >= 0 && ThreeDeeSurfaceChain.Instance != null)
            {
                //we need to use this special reallocation method so that reparenting information is preserved.
                //normal release/allocation would loose this info and
                (SurfaceHandle, SpriteHandle) = ThreeDeeSurfaceChain.Instance.ReallocateSprite(this, SpriteHandle, SurfaceHandle);
                AlignBillboard();
                GenerateCommand();
                IssueRenderRequest();
            }
        }
        #endregion


        #region Public Methods
        /// <summary>
        /// Returns the rect assigned to this sprite by the surface it is being rendered to.
        /// </summary>
        /// <returns></returns>
        public Rect SurfaceRect()
        {
            Assert.IsNotNull(ThreeDeeSurfaceChain.Instance);
            return ThreeDeeSurfaceChain.Instance.QueryTileRect(SurfaceHandle, SpriteHandle);
        }

        /// <summary>
        /// Returns a worldspace position in relation to the tile rect allocated to the 3D model.
        /// </summary>
        /// <returns></returns>
        public Vector3 GetWorldspaceTileOffset(Vector3 pos)
        {
            ThreeDeeSpriteSurface surface = ThreeDeeSurfaceChain.Instance.QuerySurface(SurfaceHandle);

            float tileScale = surface.TileSize * TileResolution;
            tileScale /= surface.ScreenHeight;
            var center = (Vector2)pos + TileOffset * tileScale;

            var cam = ThreeDeeSurfaceChain.Instance.QueryCamera(SurfaceHandle);
            return cam.ViewportToWorldPoint(new Vector3(center.x, center.y, pos.z));
        }

        /// <summary>
        /// This is primarily used by the compacting algorithm when to update sprites
        /// when they have been shifted to new surfaces.
        /// </summary>
        /// <param name="surfaceHandle"></param>
        /// <param name="spriteHandle"></param>
        public void UpdateHandles(int surfaceHandle, int spriteHandle, Material surfaceMaterial)
        {
            SpriteHandle = spriteHandle;
            SurfaceHandle = surfaceHandle;
            this.SpriteBillboard.GetComponent<MeshRenderer>().sharedMaterial = surfaceMaterial;
            Allocated = spriteHandle >= 0;
            //AlignBillboard();
            GenerateCommand();
            IssueRenderRequest();
        }

        /// <summary>
        /// 
        /// </summary>
        public void IssueRenderRequest()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) return;
#endif
            if (SpriteHandle >= 0 && SurfaceHandle >= 0)
                LastCommandHandle = ThreeDeeSurfaceChain.Instance.AddCommand(RenderCmd);
        }

        /// <summary>
        /// This can be called when a previously issued render request has been completed.
        /// Not strictly necessary but useful.
        /// </summary>
        public void FlagRenderRequestComplete()
        {
            LastCommandHandle = -1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="allocating"></param>
        public void ProcessModelParenting(bool allocating)
        {
           
            #region Enabling / Allocating
            if (allocating)
            {
                switch (DescriptorAsset.ModelParentMode)
                {
                    case RendererDescriptorAsset.ModelParentModes.UnparentReparent:
                        {
                            if (ModelTrans != null && ModelTrans.parent != null)
                            {
                                OriginalModelParent = ModelTrans.parent;
                                ModelTrans.SetParent(null);
                            }
                            break;
                        }
                    case RendererDescriptorAsset.ModelParentModes.UnparentOnly:
                        {
                            if(ModelTrans != null && ModelTrans.parent != null)
                            {
                                OriginalModelParent = ModelTrans.parent;
                                ModelTrans.SetParent(null);
                            }
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }
            #endregion

            #region Disabling / Deallocating
            else
            {
                switch (DescriptorAsset.ModelParentMode)
                {
                    case RendererDescriptorAsset.ModelParentModes.UnparentReparent:
                        {
                            if (!ThreeDeeSurfaceChain.AppQuitting)
                            {
                                //we have to delay this by exactly one frame now that unity doesn't allow
                                //reparenting during the frame that something is enabled/disabled. We'll
                                //use a couroutine to do this. Shouldn't cause *too* much garbage I hope
                                StartCoroutine(DelayedReparent(ModelTrans, OriginalModelParent));
                            }
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }
            #endregion


        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="child"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        IEnumerator DelayedReparent(Transform child, Transform parent)
        {
            Assert.IsNotNull(child);
            Assert.IsNotNull(parent);
            yield return null;
            //it's possible for this thing to have been destroyed since this coroutine was started so check for that here
            if (child == null || parent == null) yield break;
            child.SetParent(parent, false);
            child.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
        #endregion
    }


}