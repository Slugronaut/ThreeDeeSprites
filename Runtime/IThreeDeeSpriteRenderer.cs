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
        int TileResolution { get; set; }
        bool isActiveAndEnabled { get; }
    }
}
