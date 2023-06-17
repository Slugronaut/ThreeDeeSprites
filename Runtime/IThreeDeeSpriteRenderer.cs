using UnityEngine;

namespace ThreeDee
{
    /// <summary>
    /// Interface shared by any 3D-Sprite renderer.
    /// </summary>
    public interface IThreeDeeSpriteRenderer
    {
        Transform ModelTrans { get; }
        MeshFilter SpriteBillboard { get; set; }
        RendererDescriptorAsset DescriptorAsset { get; }
        int TileResolution { get; set; }
        int LastCommandHandle { get; }
        bool isActiveAndEnabled { get; }
        void UpdateHandles(int surfaceHandle, int spriteHandle, Material surfaceMaterial);
        void FlagRenderRequestComplete();
        void ProcessModelParenting(bool allocating);
    }
}
