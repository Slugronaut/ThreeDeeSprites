
namespace ThreeDee
{
    /// <summary>
    /// PoD that stores data about a sprite that will be sorted into
    /// buckets based on tile size and then position in the surface chain.
    /// </summary>
    public class OrderedBucketSprite
    {
        public int SurfaceHandle;
        public int SpriteHandle;
    }
}