using Sirenix.OdinInspector;
using UnityEngine;


namespace ThreeDee
{
    /// <summary>
    /// Asset used to describe how a <see cref="ThreeDeeSpriteRenderer"/> should be allocated to available surfaces.
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

        [Tooltip("The RenderTexture wrapper surface object to duplicate when more tile spaces are needed.")]
        [AssetsOnly]
        public ThreeDeeSpriteSurface SurfacePrefab;

        [Tooltip("Should the gameobject containing the model actually be repositioned in front of the pre-render camera that will use it? If disabled, the position will instead be set to the local material's _ThreeDeeSprite_PreRenderPos value to be used in a shader to perform the actual vertex transformation.")]
        public bool RepositionModel = true;

        [Tooltip("The method used to decide which surfaces to check for tile space when allocating a sprite.")]
        public SurfaceIdModes SurfaceIDMode = SurfaceIdModes.FirstAvailable;

        [Tooltip("If set, the 3D model will be unparented upon creation and when enabled and reparented upon being disabled or destroyed.")]
        public ModelParentModes ModelParentMode = ModelParentModes.UnparentOnly;
    }
}
