using UnityEngine;
using static ThreeDee.ThreeDeeSpriteSurface;

namespace ThreeDee
{

    


    /// <summary>
    /// 
    /// </summary>
    public class PreRenderCamera : MonoBehaviour
    {
        public RenderTexture TargetTexture;
        public float NearClip = 0;
        public float FarClip = 10;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="point"></param>
        /// <param name="pivot"></param>
        /// <param name="eulerAngles"></param>
        /// <returns></returns>
        public static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 eulerAngles)
        {
            var dir = point - pivot;
            dir = Quaternion.Euler(eulerAngles) * dir;
            return dir + pivot;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rt"></param>
        /// <param name="cameraP"></param>
        /// <param name="cameraV"></param>
        public static void BeginDraw(RenderTexture rt, Matrix4x4 cameraP, Matrix4x4 cameraV)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        public static void EndDraw()
        {

        }

        /// <summary>
        /// Helper for rendering an entire transform hierarchy.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="renderers"></param>
        /// <param name="transforms"></param>
        public static void DrawRenderHierarchy(RenderTexture rt, Vector3 position, Quaternion rotation, Vector3 scale, Transform root, RendererSet[] rendererSets)
        {
            var rootWorldPos = root.position;
            var rootWorldRot = root.rotation;

            foreach (var rend in rendererSets)
            {
                var localPos = rootWorldPos - rend.Transform.position;
                var localRot = Quaternion.Inverse(rootWorldRot) * rend.Transform.rotation;
                var localScale = Vector3.Scale(scale, rend.Transform.localScale); //not exactly perfect but it is what it is

                Matrix4x4 m = Matrix4x4.TRS(localPos, localRot, localScale);

                var mesh = rend.Mesh;
                for (int i = 0; i < mesh.subMeshCount; i++)
                {
                    //DrawMesh(rt, mesh, rend.Materials[i], m, )
                }

                //DrawMesh(RenderTarget, mesh, mat, m, _PrerenderCamera.projectionMatrix, _PrerenderCamera.worldToCameraMatrix);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rt"></param>
        /// <param name="clearDepth"></param>
        /// <param name="clearColor"></param>
        /// <param name="color"></param>
        public static void Clear(RenderTexture rt, bool clearDepth, bool clearColor, Color color)
        {
            RenderTexture oldRt = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(clearDepth, clearColor, color);
            RenderTexture.active = oldRt;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rt"></param>
        /// <param name="mesh"></param>
        /// <param name="material"></param>
        /// <param name="meshW"></param>
        /// <param name="cameraP"></param>
        /// <param name="cameraV"></param>
        public static void DrawMesh(RenderTexture rt, Mesh mesh, Material material, Matrix4x4 meshW, Matrix4x4 cameraP, Matrix4x4 cameraV)
        {
            //sometimes this will not be null and fuck us, thanks to a random github
            //and someone by the name of @guycalledfrank on the internet for figuring this out
            if (Camera.current != null)
                cameraP *= Camera.current.worldToCameraMatrix.inverse;

            RenderTexture oldRt = RenderTexture.active;
            RenderTexture.active = rt;

            material.SetPass(0);
            GL.PushMatrix();
            GL.LoadProjectionMatrix(cameraV);
            GL.modelview = cameraV;
            //GL.invertCulling = true; //another wonderful contribution from the internet!! no idea why but it works!!

            Graphics.DrawMeshNow(mesh, meshW);

            //now set everything back to the way it was before
            GL.PopMatrix();
            //GL.invertCulling = false;
            RenderTexture.active = oldRt;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rt"></param>
        /// <param name="mesh"></param>
        /// <param name="subMeshIndex"></param>
        /// <param name="material"></param>
        /// <param name="meshW"></param>
        /// <param name="cameraVP"></param>
        public static void DrawMesh(RenderTexture rt, Mesh mesh, int subMeshIndex, Material material, Matrix4x4 meshW, Matrix4x4 cameraVP)
        {
            //sometimes this will not be null and fuck us, thanks to a random github
            //and someone by the name of @guycalledfrank on the internet for figuring this out
            if (Camera.current != null)
                cameraVP *= Camera.current.worldToCameraMatrix.inverse;

            RenderTexture oldRt = RenderTexture.active;
            RenderTexture.active = rt;

            material.SetPass(0);
            GL.PushMatrix();
            GL.LoadProjectionMatrix(cameraVP);
            GL.invertCulling = true; //another wonderful contribution from the internet!! no idea why but it works!!

            Graphics.DrawMeshNow(mesh, meshW, subMeshIndex);

            //now set everything back to the way it was before
            GL.PopMatrix();
            GL.invertCulling = false;
            RenderTexture.active = oldRt;

        }


    }
}
