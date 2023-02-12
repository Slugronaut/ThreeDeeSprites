using UnityEngine;

namespace ThreeDee
{
    /// <summary>
    /// PoD command for passing data to a sprite engine for rendering.
    /// </summary>
    public readonly struct RenderCommand
    {
        readonly public int SpriteHandle;
        readonly public int TileResolution;
        readonly public float SpriteScale;
        readonly public Vector2 Offset;
        readonly public Transform Obj;
        readonly public int ChainId;

        public RenderCommand(int spriteHandle, int tileResolution, float spriteScale, Vector2 offset, Transform obj, int chainId)
        {
            SpriteHandle = spriteHandle;
            TileResolution = tileResolution;
            SpriteScale = spriteScale;
            Offset = offset;
            Obj = obj;
            ChainId = chainId;
        }
    }
}