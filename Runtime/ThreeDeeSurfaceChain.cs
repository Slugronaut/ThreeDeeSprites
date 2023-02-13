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
    public class ThreeDeeSurfaceChain : MonoBehaviour
    {
        public static ThreeDeeSurfaceChain Instance { get; private set; }
        public ThreeDeeSpriteSurface[] Surfaces;



        public void Awake()
        {
            Instance = this;
        }

        /// <summary>
        /// Add a render command to the command queue. The command is issued to the appropriate
        /// RenderSurface based on its internal chain id.
        /// </summary>
        /// <param name="com"></param>
        public void AddCommand(RenderCommand com)
        {
            Assert.IsTrue(com.ChainId >= 0);
            Assert.IsTrue(com.ChainId < Surfaces.Length);
            Surfaces[com.ChainId].AddCommand(com);
        }

        /// <summary>
        /// Allocates a rectangular space on the render target for a sprite of the
        /// desired size squared and returns a handle for that space.
        /// </summary>
        /// <param name="tileResolution"></param>
        /// <returns>The chain id and sprite handles.</returns>
        public (int chainId, int spriteHandle) AllocateNewSprite(ThreeDeeSpriteRenderer spriteRef, int forcedChainId = -1)
        {
            Assert.IsTrue(forcedChainId < Surfaces.Length);

            if(forcedChainId < 0)
            {
                //try each surface until one gives a successful handle
                for(int i = 0; i < Surfaces.Length; i++)
                {
                    var handle = Surfaces[i].AllocateNewSprite(spriteRef);
                    if (handle >= 0)
                        return (i, handle);
                }
            }
            else
                return (forcedChainId, Surfaces[forcedChainId].AllocateNewSprite(spriteRef));

            throw new UnityException("Could not allocate a sprite on any available rendering surfaces in the chain.");
        }

        /// <summary>
        /// Deallocates a previously allocated space on the render target that was reserved for a sprite.
        /// </summary>
        /// <param name="handle"></param>
        public void ReleaseSprite(int handle, int chainId)
        {
            Assert.IsTrue(chainId >= 0);
            Assert.IsTrue(chainId < Surfaces.Length);
            Surfaces[chainId].ReleaseSprite(handle);

        }

        /// <summary>
        /// Re-allocates all registered sprites. Useful when changing tilemap sizes and scales.
        /// </summary>
        public void ReallocateTiles(int chainId)
        {
            Assert.IsTrue(chainId >= 0);
            Assert.IsTrue(chainId < Surfaces.Length);
            Surfaces[chainId].ReallocateTiles();
        }
    }
}
