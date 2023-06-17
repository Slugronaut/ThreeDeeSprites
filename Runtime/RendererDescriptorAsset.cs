using Sirenix.OdinInspector;
using UnityEngine;


namespace ThreeDee
{
    /// <summary>
    /// Asset used to describe how a <see cref="ThreeDeeSpriteRenderer"/> should be allocated to abailable surfaces.
    /// </summary>
    [CreateAssetMenu(fileName = "ThreeDeeSprite Descriptor", menuName = "ThreeDeeSprites/Renderer Descriptor")]
    public class RendererDescriptorAsset : ScriptableObject
    {
        public enum SurfaceIdModes
        {
            FirstAvailable,
            FirstMatchingTileSize,
        }

        public enum ModelParentModes
        {
            None,
            UnparentReparent,
            UnparentOnly,
        }

        [Tooltip("The RenderTexture wrapper surface object to dupelicate when more tile spaces are needed.")]
        [AssetsOnly]
        public ThreeDeeSpriteSurface SurfacePrefab;

        [Tooltip("The method used to decide which surfaces to check for tile space when allocating a sprite.")]
        public SurfaceIdModes SurfaceIDMode = SurfaceIdModes.FirstAvailable;

        [Tooltip("If set, the 3D model will be unparented upon creation and when enabled and reparented upon being disabled or destroyed.")]
        public ModelParentModes ModelParentMode = ModelParentModes.UnparentOnly;

        [Tooltip("Should the Surface Chain compact all sprites when any sprite using this asset is disabled? This can potentially increase framerate though it may cause large frame spikes if many objects are frequently disabled.")]
        public bool CompactSpritesOnDisable = false; //leave this false! currently buggy as fuck!!
    }
}
