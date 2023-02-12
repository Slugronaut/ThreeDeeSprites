using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ThreeDee
{
    /// <summary>
    /// Rotates a target transform based on two other transforms relative facing direction.
    /// This can be used to rotate a 3D model that is being prerendered so that the sprite matches
    /// the true facing direction of the real world-space object relative to a camera.
    /// </summary>
    public class CameraRelativeFacing : MonoBehaviour
    {
        public enum ViewModes
        {
            Upright,
            Full3D,
        }

        public static float AngleSnap = 30;

        [Tooltip("The transform that will be rotated.")]
        public Transform Target;

        [Tooltip("The transform of the real object in world space that is being viewed.")]
        public Transform Observed;

        [Tooltip("The transform of the point of view. In most cases this will be a camera.")]
        public Transform Viewer;

        [Tooltip("Is the position only determined around a single vertical axis or can it handle full 3D positioning?")]
        public ViewModes Mode;


        /// <summary>
        /// 
        /// </summary>
        void Update()
        {
            if (Mode == ViewModes.Upright)
            {
                var dir = (Observed.position - Viewer.position) + Viewer.forward;
                var relForward = new Vector2(dir.x, dir.z);
                var observedForward = new Vector2(Observed.forward.x, Observed.forward.z);

                float facingAngle = Vector2.Angle(relForward, observedForward);
                Vector3 cross = Vector3.Cross(relForward, observedForward);

                if (cross.z > 0)
                    facingAngle = 360 - facingAngle;
                facingAngle = Mathf.Round(facingAngle / AngleSnap) * AngleSnap;
                Target.eulerAngles = new Vector3(0, facingAngle, 0);
            }
            else
            {
                //TODO
            }
        }
    }
}
