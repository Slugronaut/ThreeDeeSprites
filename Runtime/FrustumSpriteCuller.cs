using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

namespace ThreeDee
{
    /// <summary>
    /// TODO: The best way I can think of to handle shadow casters is to somehow mask the section of the rendertexture so that it
    /// isn't cleared
    /// </summary>
    public class FrustumSpriteCuller : MonoBehaviour
    {
        [Tooltip("A list of GameObjects that will be set to active when this culler is visible in the camera and inactive otherwise.")]
        public List<GameObject> ObjectsToCull;
        public MeshRenderer SpriteBillboardRenderer;
        public bool UseMainCamera = true;
        [HideIf("UseMainCamera")]
        public Camera Cam;


        VisibilityStates LastVisibleState = VisibilityStates.Unset;

        public enum VisibilityStates
        {
            Unset,
            Visible,
            Invisible,
        }

        private static readonly Plane[] FrustrumPlanes = new Plane[6];


        void Start()
        {
            if (UseMainCamera)
                Cam = Camera.main;
        }

        /// <summary>
        /// 
        /// </summary>
        void Update()
        {
            //this will not scale well. we'll need a bool flag at some point
            if (SpriteBillboardRenderer == null ||
                Cam == null) return;

            if(IsVisible(Cam, SpriteBillboardRenderer.bounds))
            {
                if (LastVisibleState == VisibilityStates.Visible) return;
                else
                {
                    foreach (var go in ObjectsToCull)
                    {
                        if (!go.activeSelf) go.SetActive(true);
                    }
                }
                LastVisibleState= VisibilityStates.Visible;
            }
            else
            {
                if (LastVisibleState == VisibilityStates.Invisible) return;
                else
                {
                    foreach (var go in ObjectsToCull)
                    {
                        if (go.activeSelf) go.SetActive(false);
                    }
                }
                LastVisibleState= VisibilityStates.Invisible;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="bounds"></param>
        /// <returns></returns>
        private static bool IsVisible(Camera camera, Bounds bounds)
        {
            GeometryUtility.CalculateFrustumPlanes(camera, FrustrumPlanes);
            return GeometryUtility.TestPlanesAABB(FrustrumPlanes, bounds);
        }
    }
}
