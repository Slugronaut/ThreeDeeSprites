using UnityEngine;

namespace ThreeDee
{
    /// <summary>
    /// Simply used to tag a camera in the scene so that we know which
    /// one should inherit the render features.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class SurfaceRenderCamera : MonoBehaviour
    {
    }
}
