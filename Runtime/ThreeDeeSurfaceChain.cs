using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
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
    /// 
    /// 
    /// BUGS:
    ///     -not properly allocate sprites to next surface in the chain if a previous one is full
    /// 
    /// </summary>
    [ExecuteAlways]
    public class ThreeDeeSurfaceChain : MonoBehaviour
    {
        public const int MaxSprites = 500; //this is hardcoded in the shader so if you change this you MUST change the shader too!
        public readonly static int ThreeDeeProp_InPlayMode = Shader.PropertyToID(nameof(ThreeDeeProp_InPlayMode));
        public readonly static int ThreeDeeProp_SpriteTransforms = Shader.PropertyToID(nameof(ThreeDeeProp_SpriteTransforms));
        public static readonly List<Matrix4x4> DynamicSpriteTransforms = new(ThreeDeeSurfaceChain.MaxSprites);
        public static ThreeDeeSurfaceChain Instance { get; private set; }
        public static readonly float DupedSurfaceOffsetDirection = 1;
        public static readonly float DupedSurfaceOffsetExtra = 1;
        
        [SceneObjectsOnly]
        [Tooltip("A set of pre-configured surfaces that already exist in the scene. These will be used to render sprites to billboards.")]
        public List<ThreeDeeSpriteSurface> Surfaces;

        [Tooltip("If set, the following surface will be dupelicated any time a sprite allocation is requested and there is no room. The surface duplication is based on the chain id of the sprite allocation. If the id is -1 the first prefab is always used.")]
        public bool DuplicateOnDemand;
        [ShowIf("DuplicateOnDemand")]
        [AssetsOnly]
        public ThreeDeeSpriteSurface[] SurfacePrefabs;

        public static bool AppQuitting { get; private set; } = false;


        public void Awake()
        {

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                //we need to do this so that in the editor when we are not in playmode
                //it allows us to see the true position of the 3D model
                Shader.SetGlobalInt(ThreeDeeProp_InPlayMode, 0);
                return;
            }
#endif
            DynamicSpriteTransforms.Clear(); //clear just in case we are in the editor and it's having a stacking effect
            DynamicSpriteTransforms.AddRange(new Matrix4x4[ThreeDeeSurfaceChain.MaxSprites]);
            Shader.SetGlobalMatrixArray(ThreeDeeProp_SpriteTransforms, DynamicSpriteTransforms);
            Shader.SetGlobalInt(ThreeDeeProp_InPlayMode, 1);
            Instance = this;

            Application.quitting += HandleQuitting;
        }

        /// <summary>
        /// 
        /// </summary>
        void HandleQuitting()
        {
            AppQuitting = true;
        }

        /// <summary>
        /// 
        /// </summary>
        private void OnDestroy()
        {
            //this is mostly for the sake of the editor, we need to be sure we
            //return to normal world-space rendering once playmode has ended.
            Shader.SetGlobalInt(ThreeDeeProp_InPlayMode, 0);
        }

        /// <summary>
        /// 
        /// </summary>
        public void UpdateSpriteTransformsInShader()
        {
            Shader.SetGlobalMatrixArray(ThreeDeeProp_SpriteTransforms, DynamicSpriteTransforms);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <param name="spriteTransform"></param>
        public void PushSpriteTransform(int index, Matrix4x4 spriteTransform)
        {
            DynamicSpriteTransforms[index] = spriteTransform;
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
        public void AddCommand(RenderCommand com)
        {
            Assert.IsTrue(com.ChainId >= 0);
            Assert.IsTrue(com.ChainId < Surfaces.Count);
            Surfaces[com.ChainId].AddCommand(com);
        }

        /// <summary>
        /// Add an advanced render command to the command queue. The command is issued to the appropriate
        /// RenderSurface based on its internal chain id.
        /// 
        /// Advanced commands are different from normal ones in that they must be issued every frame in order
        /// for the object to continue being rendered.
        /// </summary>
        /// <param name="com"></param>
        public void AddCommand(RenderCommandDynamic com)
        {
            Assert.IsTrue(com.ChainId >= 0);
            Assert.IsTrue(com.ChainId < Surfaces.Count);
            Surfaces[com.ChainId].AddCommand(com);
        }

        /// <summary>
        /// Reallocates a sprite using updated info. This method is needed when dynamically resizing sprite resolutions or tile counts
        /// due to the fact that normal release and allocation requires a single frame to pass in order to perform reparenting of models.
        /// This method simply transfers to old parent data so everything happens within a single frame.
        /// </summary>
        /// <param name="spriteRef"></param>
        /// <param name="forceChainId"></param>
        /// <returns></returns>
        public (int chainId, int spriteHandle) ReallocateSprite(IThreeDeeSpriteRenderer spriteRef, int handle, int chainId, int requestedChainId = -1)
        {
            var oldSpriteData = Surfaces[chainId].QueryInternalSpriteData(handle);
            Assert.IsNotNull(oldSpriteData);

            ReleaseSprite(handle, chainId, false);
            (chainId, handle) = AllocateNewSprite(spriteRef, requestedChainId, false);
            var newSpriteData = Surfaces[chainId].QueryInternalSpriteData(handle);
            newSpriteData.OriginalParent = oldSpriteData.OriginalParent;

            return (chainId, handle);
        }

        /// <summary>
        /// Allocates a rectangular space on the render target for a sprite of the
        /// desired size squared and returns a handle for that space.
        /// </summary>
        /// <param name="tileResolution"></param>
        /// <returns>The chain id and sprite handles.</returns>
        public (int chainId, int spriteHandle) AllocateNewSprite(IThreeDeeSpriteRenderer spriteRef, int forcedChainId = -1, bool unparentModel = true)
        {
            Assert.IsTrue(forcedChainId < Surfaces.Count);

            if (forcedChainId < 0)
            {
                //try each surface until one gives a successful handle
                for (int i = 0; i < Surfaces.Count; i++)
                {
                    var handle = Surfaces[i].AllocateNewSprite(spriteRef, unparentModel);
                    if (handle >= 0)
                        return (i, handle);
                }
            }
            else
            {
                int handle = Surfaces[forcedChainId].AllocateNewSprite(spriteRef, unparentModel);
                if(handle >= 0)
                    return (forcedChainId, handle);
            }

            if(DuplicateOnDemand)
            {
                Assert.IsNotNull(SurfacePrefabs);
                Assert.IsTrue(SurfacePrefabs.Length > 0);
                Assert.IsTrue(forcedChainId < SurfacePrefabs.Length);

                //if surface chain id was -1 (or less), use the first surface
                ////prefab, otherwise use the prefab given by the chain id
                forcedChainId = forcedChainId < 0 ? 0 : forcedChainId;
                Assert.IsNotNull(SurfacePrefabs[forcedChainId]);
                var dupeSurface = DuplicateSurface(SurfacePrefabs[forcedChainId]);

                if(dupeSurface != null)
                {
                    //we've added totally new surface so we'll need to return that chain id
                    Surfaces.Add(dupeSurface);
                    forcedChainId = Surfaces.Count - 1; //the new chain id is the max number of surfaces minus one

                    if (forcedChainId > 0)
                    {
                        //So... we have more than one surface in the chain already, we need to offset this one
                        //by some value to ensure it doesn't overlap with any others. Let's take the previous surface,
                        //and offset its z-position by it's camera's depth to get our new offset (plus a little extra)
                        var prevSurface = Surfaces[forcedChainId - 1];
                        var pos =   prevSurface.transform.position +
                                    new Vector3(0, 0, DupedSurfaceOffsetDirection * (DupedSurfaceOffsetExtra + prevSurface.PrerenderCamera.farClipPlane));
                        dupeSurface.transform.position = pos;
                    }
                    else dupeSurface.transform.position = Vector3.zero;


                    dupeSurface.name = $"Surface ({forcedChainId} - duped)";
                    dupeSurface.PrerenderCamera.targetTexture.name = $"Surface RT ({forcedChainId} - duped)";
                    dupeSurface.BillboardMaterial.name = $"Surface Mat ({forcedChainId} - duped)";
                    dupeSurface.transform.SetParent(this.transform, true);
                    int handle = Surfaces[forcedChainId].AllocateNewSprite(spriteRef, unparentModel);
                    if (handle >= 0)
                        return (forcedChainId, handle);
                }
            }

            throw new UnityException("Could not allocate a sprite on any available rendering surfaces in the chain.");
        }

        /// <summary>
        /// Deallocates a previously allocated space on the render target that was reserved for a sprite.
        /// </summary>
        /// <param name="handle"></param>
        public void ReleaseSprite(int handle, int chainId, bool unparentModel = true)
        {
            Assert.IsTrue(chainId >= 0);
            Assert.IsTrue(chainId < Surfaces.Count);
            Surfaces[chainId].ReleaseSprite(handle, unparentModel);

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
