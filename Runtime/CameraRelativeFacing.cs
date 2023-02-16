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

        public float YawAngleSnap = 30;
        public float PitchAngleSnap = 15;

        [Tooltip("The transform that will be rotated.")]
        public Transform Target;

        [Tooltip("The transform of the real object in world space that is being viewed.")]
        public Transform Observed;

        [Tooltip("The transform of the point of view. In most cases this will be a camera.")]
        public Transform Viewer;

        [Tooltip("A relative offset applied to the viewer. This may be needed when the observed target's center point is at the ground level but the viewer is a camera that is higher up.")]
        public Vector3 ViewerOffset;

        [Tooltip("Is the position only determined around a single vertical axis or can it handle full 3D positioning?")]
        public ViewModes Mode;


        /// <summary>
        /// 
        /// </summary>
        void Update()
        {
            if (Mode == ViewModes.Upright)
            {
                //TODO: There is a nasty bug in here due to some poor math that causes sprites to technically rotate the wrong way.
                //However, it pretty much only happens at the very edge of the screen or when the player is facing the wrong way so the
                //only way anyone would know is if the sprite casts shadows or if they had multiple cameras.
                //The reason it is left in is because we wanted the facing angle of the viewer to also factor in and not just relative positions
                //between the viewer and the observed.
                var dir = (Observed.position - (Viewer.position + ViewerOffset));// + Viewer.forward; //adding this forward vector here is the cause of the issue described above
                var relForward = new Vector2(dir.x, dir.z);
                var observedForward = new Vector2(Observed.forward.x, Observed.forward.z);

                float yawAngle = Vector2.Angle(relForward, observedForward);
                Vector3 cross = Vector3.Cross(relForward, observedForward);
                if (cross.z > 0)
                    yawAngle = 360 - yawAngle;
                yawAngle = Mathf.Round(yawAngle / YawAngleSnap) * YawAngleSnap;
                Target.eulerAngles = new Vector3(0, yawAngle, 0);
                
            }
            else
            {
                RotateObjectTowardsTargets(Viewer, Observed, Target);

            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="viewer"></param>
        /// <param name="observed"></param>
        /// <param name="objectToRotate"></param>
        public void RotateObjectTowardsTargets(Transform viewer, Transform observed, Transform objectToRotate)
        {
            Vector3 relativeDirection = observed.position - (viewer.position + ViewerOffset);
            Vector3 observedDirection = observed.forward;

            Quaternion facingRot = Quaternion.LookRotation(observedDirection, Viewer.forward);
            Quaternion viewerYaw = Quaternion.LookRotation(relativeDirection, Vector3.up);
            Quaternion observedYaw = Quaternion.LookRotation(observedDirection, Vector3.up);

            Quaternion newRotation = Quaternion.Inverse(viewerYaw) * Quaternion.Inverse(observedYaw);
            var euler = newRotation.eulerAngles;

            euler.x = Mathf.Round(euler.x / PitchAngleSnap) * PitchAngleSnap;
            euler.y = Mathf.Round(euler.y / YawAngleSnap) * YawAngleSnap;
            euler.z = Mathf.Round(euler.z / PitchAngleSnap) * PitchAngleSnap;
            objectToRotate.rotation = Quaternion.Euler(euler);
        }

        void SimpleBillboard()
        {
            /*
            UnityEngine.Matrix4x4 lookAt = Matrix4x4.LookAt(target, source, CAM_UPVECTOR);
            lookAt.Transpose();
            lookAt.Invert();

            Quaternion rotation = Quaternion.FromMatrix(new Matrix3(lookAt));
            */
        }
    }
}
