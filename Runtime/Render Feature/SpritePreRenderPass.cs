using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ThreeDee
{
    /// <summary>
    /// 
    /// </summary>
    public class SpritePreRenderPass : ScriptableRenderPass
    {
        SpriteRenderSurface[] Surfaces;
        List<RenderCommand> Commands;
        Camera PreRenderCamera;
        int LayerMask;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="surfaces"></param>
        public SpritePreRenderPass(int LayerMask, SpriteRenderSurface[] surfaces)
        {
            Surfaces = surfaces;
            Commands = new List<RenderCommand>(100);
            PreRenderCamera = GameObject.FindAnyObjectByType<SurfaceRenderCamera>().GetComponent<Camera>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="com"></param>
        public void SumbitDraw(RenderCommand com)
        {
            Commands.Add(com);
        }

        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.camera != PreRenderCamera) return;
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.camera != PreRenderCamera) return;
            DrawingSettings ds = new DrawingSettings();
            FilteringSettings fs = new FilteringSettings(null, LayerMask);
            foreach(var com in Commands)
            {
                //TODO: we need a way to ONLY render the current command's model and 
                //ensure it's in the correct position in view for the camera

                //As it is right now this would render everything for each command.... not what we want.
                context.DrawRenderers(renderingData.cullResults, ref ds, ref fs);
            }
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }
    }
}
