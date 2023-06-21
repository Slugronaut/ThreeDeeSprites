using System.Collections.Generic;
using System.Linq;

namespace ThreeDee
{
    /// <summary>
    /// Tool used for pre-allocating space for and tracking the tiles used by sprites so that they
    /// can easily be compacted when sprites are removed from surfaces.
    /// </summary>
    class SpriteBucket
    {
        public const int SmallishPrime = 5297;
        readonly SortedDictionary<int, OrderedBucketSprite> OrderedSprites;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="tilesX"></param>
        /// <param name="tilesY"></param>
        /// <param name="spriteTileSize"></param>
        public SpriteBucket()
        {
            OrderedSprites = new();
        }

        /// <summary>
        /// Assigns a sprites to a trackable position with this object, allowing us to retrieve it later
        /// when swapping or sorting needs to be performed.
        /// </summary>
        /// <param name="rend"></param>
        /// <param name="surfaceHandle"></param>
        /// <param name="spriteHandle"></param>
        /// <exception cref=""></exception>
        public void InsertAtBack(int surfaceHandle, int spriteHandle, int tileStartPos)
        {
            int hashId = (SmallishPrime * (surfaceHandle + 1)) + tileStartPos;
            OrderedSprites[hashId] = new OrderedBucketSprite()
            {
                SurfaceHandle = surfaceHandle,
                SpriteHandle = spriteHandle,
            };
        }

        /// <summary>
        /// Finds the sprite from the furthest back in the surface chain and removes it from that position, returning
        /// the relevant data.
        /// </summary>
        /// <returns></returns>
        public OrderedBucketSprite RemoveBack()
        {
            if (OrderedSprites.Count == 0) return null;

            var last = OrderedSprites.Last();
            OrderedSprites.Remove(last.Key);
            return last.Value;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public bool IsLast(int surfaceHandle, int tileStartPos)
        {
            return OrderedSprites.Last().Key == (SmallishPrime * (surfaceHandle + 1)) + tileStartPos;
        }

        /// <summary>
        /// Finds the sprite from the furthest back in the surface chain and removes it from that position, returning
        /// the relevant data.
        /// </summary>
        /// <returns></returns>
        public void Remove(int surfaceHandle, int tileStartPos)
        {
            int hashId = (SmallishPrime * (surfaceHandle + 1)) + tileStartPos;
            OrderedSprites.Remove(hashId);
        }
    }
}
