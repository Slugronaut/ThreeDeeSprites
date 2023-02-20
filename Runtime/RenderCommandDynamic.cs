using System.Collections.Generic;
using UnityEngine;

namespace ThreeDee
{
    /// <summary>
    /// PoD command for passing data to a sprite engine for rendering.
    /// </summary>
    public readonly struct RenderCommandDynamic
    {
        readonly public int SpriteHandle;
        readonly public int TileResolution;
        readonly public float SpriteScale;
        readonly public Vector2 Offset2D;
        readonly public Vector3 Offset3D;
        readonly public Transform ModelRoot;
        readonly public int ChainId;
        readonly public List<RendererSet> RendererSets;

        public RenderCommandDynamic(int spriteHandle, int tileResolution, float spriteScale, Vector2 offset2D, Vector3 offset3D, Transform modelRoot, int chainId)
        {
            SpriteHandle = spriteHandle;
            TileResolution = tileResolution;
            SpriteScale = spriteScale;
            Offset2D = offset2D;
            Offset3D = offset3D;
            ModelRoot = modelRoot;
            ChainId = chainId;
            
            var renderers = ModelRoot.GetComponentsInChildren<Renderer>();
            RendererSets = new(renderers.Length);
            foreach (var renderer in renderers)
            {
                RendererSets.Add(new RendererSet(renderer.transform,
                                                renderer.GetComponent<MeshFilter>().sharedMesh,
                                                renderer.sharedMaterials));
            }
        }
    }

    /// <summary>
    /// All of the information needed to render a mesh in its original transform hierarchy with submesh material indexes.
    /// </summary>
    public class RendererSet
    {
        readonly public Transform Transform;
        readonly public Mesh Mesh;
        readonly public Material[] Materials;

        public RendererSet(Transform trans, Mesh mesh, Material[] materials)
        {
            Transform = trans;
            Mesh = mesh;
            Materials = materials;
        }
    }
}