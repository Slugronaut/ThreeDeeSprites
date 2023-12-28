using Peg.Systems;
using UnityEngine;

namespace ThreeDee
{
    /// <summary>
    /// Rotates a target transform based on two other transforms relative facing direction.
    /// This can be used to rotate a 3D model that is being prerendered so that the sprite matches
    /// the true facing direction of the real world-space object relative to a camera.
    /// </summary>
    public class CameraRelativeFacing : MonoBehaviour, IFrameTickable
    {
        public enum ViewModes
        {
            Upright,
            Full3D,
        }

        public static readonly float YawAngleSnap = 45;
        public static readonly float PitchAngleSnap = 15;

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

        public bool TickEnabled => enabled;

        private void Start()
        {
            FrameTickSystem.Instance.Register(this);
            if (Viewer == null)
                Viewer = Camera.main.transform;
        }

        void OnDestroy()
        {
            FrameTickSystem.Instance.Unregister(this);
        }

        /// <summary>
        /// 
        /// </summary>
        public void OnTick()
        {
            if (Mode == ViewModes.Upright)
                Target.eulerAngles = QuantizedBillboardRotationRelative(YawAngleSnap, Observed.position, Observed.forward, Viewer.position, ViewerOffset, Viewer.forward);
            else RotateObjectTowardsTargets(Viewer, Observed, Target);
        }

        /// <summary>
        /// Returns the euler angles for an inverse y-axis rotation.
        /// </summary>
        /// <returns></returns>
        public static Vector3 QuantizedBillboardRotationRelative(float yawAngleSnap, Vector3 targetPos, Vector3 targetForward, Vector3 viewerPos, Vector3 viewerOffset, Vector3 viewerForward)
        {
            var dir = (targetPos - (viewerPos + viewerOffset));
            dir = Vector3.Normalize(dir + viewerForward); //dir is now the halfVector
            var relForward = new Vector2(dir.x, dir.z);
            var observedForward2D = new Vector2(targetForward.x, targetForward.z);

            float yawAngle = Vector2.Angle(relForward, observedForward2D);
            Vector3 cross = Vector3.Cross(relForward, observedForward2D);
            if (cross.z > 0)
                yawAngle = 360 - yawAngle;
            yawAngle = Mathf.Round(yawAngle / yawAngleSnap) * yawAngleSnap;
            return new Vector3(0, yawAngle, 0);
        }

        /// <summary>
        /// Returns the euler angles for a direct y-axis rotation.
        /// </summary>
        /// <returns></returns>
        public static Vector3 QuantizedBillboardRotation(float yawAngleSnap, Vector3 targetPos, Vector3 viewerPos)
        {
            var viewForward = targetPos - viewerPos;
            var quantRot = Quaternion.LookRotation(viewForward.normalized, Vector3.up).eulerAngles;
            quantRot.x = 0;
            quantRot.y = Mathf.Round(quantRot.y / CameraRelativeFacing.YawAngleSnap) * yawAngleSnap;
            quantRot.z = 0;
            return quantRot;
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

            //Quaternion facingRot = Quaternion.LookRotation(observedDirection, Viewer.forward);
            Quaternion viewerYaw = Quaternion.LookRotation(relativeDirection, Vector3.up);
            Quaternion observedYaw = Quaternion.LookRotation(observedDirection, Vector3.up);

            Quaternion newRotation = Quaternion.Inverse(viewerYaw) * Quaternion.Inverse(observedYaw);
            var euler = newRotation.eulerAngles;

            euler.x = Mathf.Round(euler.x / PitchAngleSnap) * PitchAngleSnap;
            euler.y = Mathf.Round(euler.y / YawAngleSnap) * YawAngleSnap;
            euler.z = Mathf.Round(euler.z / PitchAngleSnap) * PitchAngleSnap;
            objectToRotate.rotation = Quaternion.Euler(euler);
        }
    }
}
