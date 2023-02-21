using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


namespace ThreeDee
{
    /// <summary>
    /// Defines the asset for the Sprite Pre-Render feature.
    /// Also instantiates the pass and attaches it to the render pipeline queue.
    /// </summary>
    public class SpritePreRenderFeature : ScriptableRendererFeature
    {
        public RenderPassEvent EventPoint = RenderPassEvent.AfterRenderingOpaques;
        public LayerMask Layers;
        public SpriteRenderSurface[] Surfaces;
        SpritePreRenderPass Pass;


        /// <summary>
        /// 
        /// </summary>
        public override void Create()
        {
            Pass = new(Layers.value, Surfaces)
            {
                renderPassEvent = EventPoint,
            };
        }

        /// <summary>
        /// Here you can inject one or multiple render passes in the renderer.
        /// This method is called when setting up the renderer once per-camera.
        /// </summary>
        /// <param name="renderer"></param>
        /// <param name="renderingData"></param>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(Pass);
        }
    }
}


