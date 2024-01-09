using UnityEngine;

namespace ThreeDee
{
    /// <summary>
    /// PoD command for passing data to a sprite engine for rendering.
    /// </summary>
    public struct RenderCommand
    {
        public int SpriteHandle;
        readonly public bool RepositionModel;
        readonly public int TileResolution;
        readonly public float SpriteScale;
        readonly public Vector2 Offset2D;
        readonly public Vector3 Offset3D;
        readonly public Transform ModelRoot;
        readonly public int ChainId;

        public static RenderCommand CancelCmd => new(-1, false, 0, 0, Vector2.zero, Vector3.zero, null, -1);

        public RenderCommand(int spriteHandle, bool repositionModel, int tileResolution, float spriteScale, Vector2 offset2D, Vector3 offset3D, Transform obj, int chainId)
        {
            SpriteHandle = spriteHandle;
            RepositionModel = repositionModel;
            TileResolution = tileResolution;
            SpriteScale = spriteScale;
            Offset2D = offset2D;
            Offset3D = offset3D;
            ModelRoot = obj;
            ChainId = chainId;
        }
    }
}