using UnityEngine;


namespace ThreeDee
{
    /// <summary>
    /// 
    /// </summary>
    public static class TransformHelper
    {
        /// <summary>
        /// Sets the global (but lossy) scale for a Transform. This scale does not (and indeed cannot) take into account rotations
        /// of any parent of this transform hence the reason it is considered 'lossy'.
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="globalScale"></param>
        public static void SetGlobalScale(this Transform transform, Vector3 globalScale)
        {
            transform.localScale = Vector3.one;
            transform.localScale = new Vector3(globalScale.x / transform.lossyScale.x, globalScale.y / transform.lossyScale.y, globalScale.z / transform.lossyScale.z);
        }
    }
}
