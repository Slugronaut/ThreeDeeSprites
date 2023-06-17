using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

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

        [InfoBox("Experimental: This feature is not currently working and should remain disabled.", InfoMessageType.Warning)]
        [Tooltip("When there is not enough room on any currently existing surfaces, should sprites be compacted before creating a new surface?")]
        public bool CompactSpritesOnAlloc = false;

        [SceneObjectsOnly]
        [Tooltip("A set of pre-configured surfaces that already exist in the scene. These will be used to render sprites to billboards.")]
        public List<ThreeDeeSpriteSurface> Surfaces;

        public static bool AppQuitting { get; private set; } = false;


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


        public float CompactTimeLimiter = 10;
        readonly List<IThreeDeeSpriteRenderer> CompactingSpriteList = new(128);
        /// <summary>
        /// Reallocates all sprites on all surfaces so that earlier surfaces are assigned first.
        /// This should help compact sprites onto fewer surfaces and allow for disabling of unecessary
        /// surfaces.
        /// </summary>
        [Button("Compact Sprites")]
        public void CompactSprites()
        {
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
        public (int surfaceId, int spriteHandle) ReallocateSprite(IThreeDeeSpriteRenderer spriteRef, int handle, int chainId)
        {
            var oldSpriteData = Surfaces[chainId].QueryInternalSpriteData(handle);
            Assert.IsNotNull(oldSpriteData);

            ReleaseSprite(handle, chainId);
            (chainId, handle) = AllocateNewSprite(spriteRef);

            return (chainId, handle);
        }

        /// <summary>
        /// Allocates a rectangular space on the render target for a sprite of the
        /// desired size squared and returns a handle for that space.
        /// </summary>
        /// <param name="tileResolution"></param>
        /// <returns>The chain id and sprite handles.</returns>
        public (int surfaceId, int spriteHandle) AllocateNewSprite(IThreeDeeSpriteRenderer spriteRef, bool compacting = false)
        {
            var desc = spriteRef.DescriptorAsset;

            switch(desc.SurfaceIDMode)
            {
                case RendererDescriptorAsset.SurfaceIdModes.FirstAvailable:
                    {
                        //try each surface until one gives a successful handle
                        for (int i = 0; i < Surfaces.Count; i++)
                        {
                            var handle = Surfaces[i].AllocateNewSprite(spriteRef);
                            if (handle >= 0)
                                return (i, handle);
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

            int surfaceHandle = CreateNewSurface(desc);
            if (surfaceHandle >= 0)
            {
                int spriteHandle = Surfaces[surfaceHandle].AllocateNewSprite(spriteRef);
                if (spriteHandle >= 0)
                    return (surfaceHandle, spriteHandle);
            }

            throw new UnityException("Could not allocate a sprite on any available rendering surfaces in the chain.");
        }

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
                    //So... we have more than one surface in the chain already, we need to offset this one
                    //by some value to ensure it doesn't overlap with any others. Let's take the previous surface,
                    //and offset its z-position by it's camera's depth to get our new offset (plus a little extra)
                    var prevSurface = Surfaces[surfaceHandle - 1];
                    var pos = prevSurface.transform.position +
                                new Vector3(0, 0, DupedSurfaceOffsetDirection * (DupedSurfaceOffsetMargin + prevSurface.PrerenderCamera.farClipPlane));
                    dupeSurface.transform.position = pos;
                }
                else dupeSurface.transform.position = Vector3.zero;


                dupeSurface.name = $"Surface ({surfaceHandle} - duped)";
                dupeSurface.PrerenderCamera.targetTexture.name = $"Surface RT ({surfaceHandle} - duped)";
                dupeSurface.BillboardMaterial.name = $"Surface Mat ({surfaceHandle} - duped)";
                dupeSurface.transform.SetParent(this.transform, true);
                return surfaceHandle;
            }

            return -1;
        }

        /// <summary>
        /// Deallocates a previously allocated space on the render target that was reserved for a sprite.
        /// </summary>
        /// <param name="handle"></param>
        public void ReleaseSprite(int handle, int chainId)
        {
            Assert.IsTrue(chainId >= 0);
            Assert.IsTrue(chainId < Surfaces.Count);
            Surfaces[chainId].ReleaseSprite(handle);

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

        /// <summary>
        /// Re-allocates all registered sprites. Useful when changing tilemap sizes and scales.
        /// </summary>
        public void ReallocateTiles(int chainId)
        {
            Assert.IsTrue(chainId >= 0);
            Assert.IsTrue(chainId < Surfaces.Count);
            Surfaces[chainId].ReallocateTiles();
        }

        /// <summary>
        /// Encodes a single integer into a Vector3.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Vector3 EncodeFloat3(int index)
        {
            float x = (float)(index & 1023) / 1023.0f;
            float y = (float)((index >> 10) & 1023) / 1023.0f;
            float z = (float)((index >> 20) & 511) / 511.0f;
            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Decodes a Vector3 into a previously encoded integer.
        /// </summary>
        /// <param name="float3"></param>
        /// <returns></returns>
        public static int DecodeFloat3(Vector3 float3)
        {
            int x = (int)(float3.x * 1023);
            int y = (int)(float3.y * 1023) << 10;
            int z = (int)(float3.z * 511) << 20;
            return x | y | z;
        }
    }
}
