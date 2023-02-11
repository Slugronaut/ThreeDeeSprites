using UnityEngine;
using UnityEngine.Assertions;

namespace ThreeDee
{
    /// <summary>
    /// The primary interface for registering sprites and issuing render commands. Internally manages
    /// the indivual render engines to abstract away which ones are used and how.
    /// </summary>
    public class ThreeDeeRenderChain : MonoBehaviour
    {
        public static ThreeDeeRenderChain Instance { get; private set; }
        public ThreeDeeSpriteEngine[] Engines;



        public void Awake()
        {
            Instance = this;
        }

        /// <summary>
        /// Add a render command to the command queue. The command is issued to the appropriate
        /// RenderEngine based on it's internal chain id.
        /// </summary>
        /// <param name="com"></param>
        public void AddCommand(RenderCommand com)
        {
            Assert.IsTrue(com.ChainId >= 0);
            Assert.IsTrue(com.ChainId < Engines.Length);
            Engines[com.ChainId].AddCommand(com);
        }

        /// <summary>
        /// Allocates a rectangular space on the render target for a sprite of the desired size squared and returns a handle for that space.
        /// </summary>
        /// <param name="tileResolution"></param>
        /// <returns>The chain id and sprite handles.</returns>
        public (int chainId, int spriteHandle) AllocateNewSprite(ThreeDeeSprite spriteRef, int forcedChainId = -1)
        {
            Assert.IsTrue(forcedChainId < Engines.Length);

            if(forcedChainId < 0)
            {
                //try each engine until one gives a successful handle
                for(int i = 0; i < Engines.Length; i++)
                {
                    var handle = Engines[i].AllocateNewSprite(spriteRef);
                    if (handle >= 0)
                        return (i, handle);
                }
            }
            else
                return (forcedChainId, Engines[forcedChainId].AllocateNewSprite(spriteRef));

            throw new UnityException("Could not allocate a sprite on any available rendering engines in the chain.");
        }

        /// <summary>
        /// Deallocates a previously allocated space on the render target that was reserved for a sprite.
        /// </summary>
        /// <param name="handle"></param>
        public void ReleaseSprite(int handle, int chainId)
        {
            Assert.IsTrue(chainId >= 0);
            Assert.IsTrue(chainId < Engines.Length);
            Engines[chainId].ReleaseSprite(handle);

        }

        /// <summary>
        /// Re-allocates all registered sprites. Useful when changing tilemap sizes and scales.
        /// </summary>
        public void ReallocateTiles(int chainId)
        {
            Assert.IsTrue(chainId >= 0);
            Assert.IsTrue(chainId < Engines.Length);
            Engines[chainId].ReallocateTiles();
        }
    }
}
