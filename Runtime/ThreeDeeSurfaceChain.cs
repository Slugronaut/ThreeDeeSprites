//#define SORT_FOR_SWAP
//#define SWAP

using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

namespace ThreeDee
{
    /// <summary>
    /// The primary interface for registering sprites and issuing render commands. Internally manages
    /// the indivual render surfacess to abstract away which ones are used and how.
    /// 
    /// This is necessary due to the fact that there are hardware limits on how big a texture can be
    /// and more than one may be needed to fit all renderable objects that are currently in use.
    /// 
    /// </summary>
    [DefaultExecutionOrder(ThreeDeeSpriteSurface.ExecutionOrder + ExecutionOffset)]
    public class ThreeDeeSurfaceChain : MonoBehaviour
    {
        //defines the direction and space to offset the surfaces as they are duplicated so that clip planes dont overlap
        public static readonly float DupedSurfaceOffsetDirection = 1; 
        public static readonly float DupedSurfaceOffsetMargin = 1;

        public const int ExecutionOffset = 1;
        public static ThreeDeeSurfaceChain Instance { get; private set; }

        [Tooltip("Should we manually trigger camera rendering in Update()? This may cause some issues with animations but also fix other issues with flickering sprites. Cannot be changed during runtime.")]
        public bool ManualRendering = true;
        [Tooltip("When there is not enough room on any currently existing surfaces, should sprites be compacted before creating a new surface?")]
        public bool CompactSpritesOnAlloc = false;
        [Tooltip("If a sprite is disabled, last sprite in the surface chain of the same size will be swapped into its position, thus allowing for efficient compacting of sprites. Generates a small amount of garbage during the swap.")]
        public bool SwapDisabledSprites = true;

        [SceneObjectsOnly]
        [Tooltip("A set of pre-configured surfaces that already exist in the scene. These will be used to render sprites to billboards.")]
        public List<ThreeDeeSpriteSurface> Surfaces;

        public static bool AppQuitting { get; private set; } = false;

        public UnityEvent<int, ThreeDeeSpriteSurface> OnCreatedNewSurface;


        public double CompactTimeLimiter = 5;
        double LastCompactTime;
        readonly List<IThreeDeeSpriteRenderer> CompactingSpriteList = new(128);
        readonly Dictionary<int, SpriteBucket> SpriteSizeBuckets = new();

        #region Unity Events
        public void Awake()
        {
            AppQuitting = false;
            Instance = this;

            Application.quitting += HandleQuitting;

            //InvokeRepeating(nameof(CompactSprites), 5, 5);
        }

        /// <summary>
        /// 
        /// </summary>
        void HandleQuitting()
        {
            AppQuitting = true;
        }

        #endregion


        #region Public Methods
        /// <summary>
        /// Reallocates all sprites on all surfaces so that earlier surfaces are assigned first.
        /// This should help compact sprites onto fewer surfaces and allow for disabling of unecessary
        /// surfaces.
        /// </summary>
        [Button("Compact Sprites")]
        public void CompactSprites()
        {
            if (Time.timeAsDouble - LastCompactTime < CompactTimeLimiter)
                return;
            LastCompactTime = Time.timeAsDouble;

            foreach(var surface in Surfaces)
                CompactingSpriteList.AddRange(surface.ReleaseAllForCompacting());

            foreach (var rend in CompactingSpriteList)
            {
                (int surfaceHandle, int spriteHandle) = AllocateNewSprite(rend, true);
                rend.UpdateHandles(surfaceHandle, spriteHandle, GetSurfaceBillboardMaterial(surfaceHandle)); //forces the sprite to update it's internal command and issue a new render request
            }
            CompactingSpriteList.Clear();
        }
        
        /// <summary>
        /// Returns the material associated with the identified surface handle. This material
        /// is used by sprite billboards to render using the surface's rendertexture.
        /// </summary>
        /// <param name="chainId"></param>
        /// <returns></returns>
        public Material GetSurfaceBillboardMaterial(int chainId)
        {
            Assert.IsNotNull(Surfaces);
            Assert.IsTrue(chainId > -1);
            Assert.IsTrue(chainId < Surfaces.Count);
            return Surfaces[chainId].BillboardMaterial;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public Rect QueryTileRect(int chainId, int handle)
        {
            Assert.IsTrue(chainId > -1);
            Assert.IsTrue(chainId <= Surfaces.Count);
            return Surfaces[chainId].QueryTileRect(handle);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="chainId"></param>
        /// <returns></returns>
        public ThreeDeeSpriteSurface QuerySurface(int chainId)
        {
            Assert.IsTrue(chainId > -1);
            Assert.IsTrue(chainId <= Surfaces.Count);
            return Surfaces[chainId];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="chainId"></param>
        /// <returns></returns>
        public Camera QueryCamera(int chainId)
        {
            Assert.IsTrue(chainId > -1);
            Assert.IsTrue(chainId <= Surfaces.Count);
            return Surfaces[chainId].QueryCamera();
        }

        /// <summary>
        /// Add a render command to the command queue. The command is issued to the appropriate
        /// RenderSurface based on its internal chain id.
        /// </summary>
        /// <param name="com"></param>
        public int AddCommand(RenderCommand com)
        {
            Assert.IsTrue(com.ChainId >= 0);
            Assert.IsTrue(com.ChainId < Surfaces.Count);
            return Surfaces[com.ChainId].AddCommand(com);
        }

        /// <summary>
        /// Reallocates a sprite using updated info. This method is needed when dynamically resizing sprite resolutions or tile counts
        /// due to the fact that normal release and allocation requires a single frame to pass in order to perform reparenting of models.
        /// This method simply transfers to old parent data so everything happens within a single frame.
        /// </summary>
        /// <param name="spriteRef"></param>
        /// <param name="forceChainId"></param>
        /// <returns></returns>
        public (int surfaceId, int spriteHandle) ReallocateSprite(IThreeDeeSpriteRenderer spriteRef, int spriteHandle, int surfaceHandle)
        {
            var oldSpriteData = Surfaces[surfaceHandle].QueryInternalSpriteData(spriteHandle);
            Assert.IsNotNull(oldSpriteData);

            ReleaseSprite(spriteHandle, surfaceHandle, false);
            (surfaceHandle, spriteHandle) = AllocateNewSprite(spriteRef);

            return (surfaceHandle, spriteHandle);
        }

        /// <summary>
        /// Allocates a rectangular space on the render target for a sprite of the
        /// desired size squared and returns a handle for that space.
        /// </summary>
        /// <param name="tileResolution"></param>
        /// <returns>The chain id and sprite handles.</returns>
        public (int surfaceHandle, int spriteHandle) AllocateNewSprite(IThreeDeeSpriteRenderer rend, bool compacting = false)
        {
            var desc = rend.DescriptorAsset;
            int surfaceHandle;

            switch(desc.SurfaceIDMode)
            {
                case RendererDescriptorAsset.SurfaceIdModes.FirstAvailable:
                    {
                        //try each surface until one gives a successful handle
                        for (surfaceHandle = 0; surfaceHandle < Surfaces.Count; surfaceHandle++)
                        {
                            var spriteHandle = Surfaces[surfaceHandle].AllocateNewSprite(rend);
                            if (spriteHandle >= 0)
                            {
                                if(SwapDisabledSprites) 
                                    RegisterSpriteForSwapbackLookup(surfaceHandle, spriteHandle);
                                return (surfaceHandle, spriteHandle);
                            }
                        }
                        break;
                    }
                case RendererDescriptorAsset.SurfaceIdModes.FirstMatchingTileSize:
                    {
                        throw new System.Exception("Tile size matching not yet implemented.");
                    }
            }

            if(CompactSpritesOnAlloc && !compacting)
                CompactSprites();

            surfaceHandle = CreateNewSurface(desc);
            if (surfaceHandle >= 0)
            {
                int spriteHandle = Surfaces[surfaceHandle].AllocateNewSprite(rend);
                if (spriteHandle >= 0)
                {
                    if(SwapDisabledSprites) 
                        RegisterSpriteForSwapbackLookup(surfaceHandle, spriteHandle);
                    return (surfaceHandle, spriteHandle);
                }
            }

            throw new UnityException("Could not allocate a sprite on any available rendering surfaces in the chain.");
        }

        /// <summary>
        /// Deallocates a previously allocated space on the render target that was reserved for a sprite.
        /// </summary>
        /// <param name="spriteHandle"></param>
        public void ReleaseSprite(int spriteHandle, int surfaceHandle, bool allowSwapping = true)
        {
            if (AppQuitting) return;

            Assert.IsTrue(surfaceHandle >= 0);
            Assert.IsTrue(surfaceHandle < Surfaces.Count);

            if (SwapDisabledSprites)
            {
                //attempt to swap the furthest-back sprite into the spot of the removed one
                if(allowSwapping && RemoveSpriteSwapBack(spriteHandle, surfaceHandle))
                        return;
                //otherwise just remove the sprite as normal
                var registered = Surfaces[surfaceHandle].QueryInternalSpriteData(spriteHandle);
                var bucket = SpriteSizeBuckets[registered.Rend.TileResolution];
                bucket.Remove(surfaceHandle, registered.AllocatedTiles[0]);
            }
            Surfaces[surfaceHandle].ReleaseSprite(spriteHandle);
        }
        
        /// <summary>
        /// Re-allocates all registered sprites. Useful when changing tilemap sizes and scales.
        /// </summary>
        public void ReallocateTiles(int surfaceHandle)
        {
            Assert.IsTrue(surfaceHandle >= 0);
            Assert.IsTrue(surfaceHandle < Surfaces.Count);
            Surfaces[surfaceHandle].ReallocateTiles();
        }
        #endregion


        #region Private Methods
        /// <summary>
        /// Internally registers a threedee sprite renderer in a sorted order so that later it can be quickly
        /// lookup when remove-swap-back operations need to be performed.
        /// </summary>
        /// <param name="rend"></param>
        void RegisterSpriteForSwapbackLookup(int surfaceHandle, int spriteHandle)
        {
            var surf = Surfaces[surfaceHandle];
            var regSprite = surf.QueryInternalSpriteData(spriteHandle);
            var rend = regSprite.Rend;
            if (!SpriteSizeBuckets.TryGetValue(rend.TileResolution, out var bucket))
            {
                bucket = new SpriteBucket();
                SpriteSizeBuckets.Add(rend.TileResolution, bucket);
            }
            bucket.InsertAtBack(surfaceHandle, spriteHandle, regSprite.AllocatedTiles[0]);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="System.Exception"></exception>
        bool RemoveSpriteSwapBack(int spriteHandle, int surfaceHandle)
        {
            var destSurface = Surfaces[surfaceHandle];
            var registered = destSurface.QueryInternalSpriteData(spriteHandle);
            int tilePos = registered.AllocatedTiles[0];
            var bucket = SpriteSizeBuckets[registered.Rend.TileResolution];

            //check to ensure the sprite being swapped out isn't actually the back one.
            //if so we can skip this whole process
            if (bucket.IsLast(surfaceHandle, tilePos))
                return false;

            //get the state of the sprite that is being swapped in
            var backSpriteInBucket = bucket.RemoveBack();
            if (backSpriteInBucket == null)
                return false; //no other sprites left to swap in, do nothing

            /*
            //clear state of the old sprite that is being removed, but leave the bucket ref since we're recycling it
            var oldRend = registered.Rend;
            oldRend.UpdateHandles(-1, -1, null);
            destSurface.ReleaseDestSprite(spriteHandle);

            //unregister the swap-in sprite from its old surface
            var swapSurf = Surfaces[backSpriteInBucket.SurfaceHandle];
            var swapSpriteReg = swapSurf.QueryInternalSpriteData(backSpriteInBucket.SpriteHandle);
            var swapRend = swapSpriteReg.Rend;
            swapSurf.ReleaseSourceSprite(backSpriteInBucket.SpriteHandle);

            //finally, swap the actual renderer reference and indicies as well as update the bucket sorting info
            registered.Rend = swapRend;
            swapRend.UpdateHandles(surfaceHandle, spriteHandle, GetSurfaceBillboardMaterial(surfaceHandle)); //this will force a render update
            */

            var swapSurf = Surfaces[backSpriteInBucket.SurfaceHandle];
            var swapSpriteReg = swapSurf.QueryInternalSpriteData(backSpriteInBucket.SpriteHandle);
            var swapRend = swapSpriteReg.Rend;


            destSurface.ReleaseSprite(spriteHandle);
            swapSurf.ReleaseSprite(backSpriteInBucket.SpriteHandle);
            (int newSurfaceHandle, int newSpriteHandle) = AllocateNewSprite(swapRend, false);
            swapRend.UpdateHandles(newSurfaceHandle, newSpriteHandle, GetSurfaceBillboardMaterial(newSurfaceHandle));
            return true;
        }


        /// <summary>
        /// The method of positioning pre-render cameras in worldspace so that they don't overlap contents.
        /// </summary>
        public enum CameraStackingModes
        {
            Depth,
            SideBySide,
        }

        [Tooltip("The method of positioning pre-render cameras in worldspace so that they don't overlap contents.")]
        public CameraStackingModes CameraStackingMode = CameraStackingModes.Depth;

        /// <summary>
        /// Helper for creating a new surface by duplicating a render texture asset.
        /// </summary>
        /// <returns></returns>
        int CreateNewSurface(RendererDescriptorAsset desc)
        {
            var dupeSurface = DuplicateSurface(desc.SurfacePrefab);
            if (dupeSurface != null)
            {
                //we've added a totally new surface so we'll need to return that chain id
                Surfaces.Add(dupeSurface);
                int surfaceHandle = Surfaces.Count - 1; //the new chain id is the max number of surfaces minus one

                if (surfaceHandle > 0)
                {
                    //I decided to add different stacking methods because I needed to do side-by-side stacking because depth
                    //issues with my cameras necessitated much larger far planes but I didn't want to fuck up any older setups.
                    if (CameraStackingMode == CameraStackingModes.Depth)
                    {

                        //So... we have more than one surface in the chain already, we need to offset this one
                        //by some value to ensure it doesn't overlap with any others. Let's take the previous surface,
                        //and offset its z-position by it's camera's depth to get our new offset (plus a little extra)
                        var prevSurface = Surfaces[surfaceHandle - 1];
                        var pos = prevSurface.transform.position +
                                    new Vector3(0, 0, DupedSurfaceOffsetDirection * (DupedSurfaceOffsetMargin + prevSurface.PrerenderCamera.farClipPlane));
                        dupeSurface.transform.position = pos;
                    }
                    else
                    {
                        //So... we have more than one surface in the chain already, we need to offset this one
                        //by some value to ensure it doesn't overlap with any others. Let's take the previous surface,
                        //and offset its z-position by it's camera's depth to get our new offset (plus a little extra)
                        var prevSurface = Surfaces[surfaceHandle - 1];
                        var pos = prevSurface.transform.position +
                                    new Vector3(DupedSurfaceOffsetDirection * prevSurface.PrerenderCamera.orthographicSize * 2, 0, 0);
                        dupeSurface.transform.position = pos;
                    }
                }
                else dupeSurface.transform.position = Vector3.zero;


                dupeSurface.name = $"Surface ({surfaceHandle} - duped)";
                dupeSurface.PrerenderCamera.targetTexture.name = $"Surface RT ({surfaceHandle} - duped)";
                dupeSurface.BillboardMaterial.name = $"Surface Mat ({surfaceHandle} - duped)";
                dupeSurface.transform.SetParent(this.transform, true);
                OnCreatedNewSurface.Invoke(surfaceHandle, dupeSurface);
                return surfaceHandle;
            }

            return -1;
        }

        /// <summary>
        /// Creates a deep copy of the given ThreeDeeSpriteSurface. It is considered 'deep' because it
        /// also creates a new render target and billboard material based on the ones linked in the source surface.
        /// </summary>
        /// <param name="surfacePrefab"></param>
        /// <returns></returns>
        ThreeDeeSpriteSurface DuplicateSurface(ThreeDeeSpriteSurface surfacePrefab)
        {
            var surface = Instantiate(surfacePrefab);
            surface.ManualRendering = this.ManualRendering;
            var dupedRT = new RenderTexture(surfacePrefab.PrerenderCamera.targetTexture);
            dupedRT.filterMode = surfacePrefab.PrerenderCamera.targetTexture.filterMode; //this isn't being duped properly
            var dupedMat = new Material(surfacePrefab.BillboardMaterial);

            foreach (var id in ThreeDeeSpriteSurface.MainTexIds)
            {
                if(dupedMat.HasTexture(id))
                    dupedMat.SetTexture(id, dupedRT);
            }
            surface.InjectNewSurfaceSources(dupedMat, dupedRT);
            return surface;
        }
        #endregion
    }
}
