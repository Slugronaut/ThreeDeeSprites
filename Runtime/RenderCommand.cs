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
        readonly public Vector2 Offset2D;
        readonly public Vector3 Offset3D;
        readonly public Transform ModelRoot;
        readonly public Renderer[] Renderers;
        readonly public int ChainId;

        public RenderCommand(int spriteHandle, int tileResolution, float spriteScale, Vector2 offset2D, Vector3 offset3D, Transform obj, Renderer[] renderers, int chainId)
        {
            SpriteHandle = spriteHandle;
            TileResolution = tileResolution;
            SpriteScale = spriteScale;
            Offset2D = offset2D;
            Offset3D = offset3D;
            ModelRoot = obj;
            Renderers = renderers;
            ChainId = chainId;
        }
    }
}