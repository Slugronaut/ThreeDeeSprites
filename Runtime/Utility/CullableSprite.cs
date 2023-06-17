using UnityEngine;

namespace ThreeDee
{
    /// <summary>
    /// When the object with this component is attached is determined to be culled it will disable
    /// all objects in int's list. When it is visible they will be enabled instead.
    /// 
    /// This component uses culling groups to simplify the process so it is mostly suited to 2D culling only.
    /// If you need 3D culling based on view frustums, use the FrusttumCullableSprite behaviour instead.
    /// </summary>
    public class CullableSprite : MonoBehaviour
    {
        public SpriteCuller Culler;
        [Min(0)]
        public float Radius = 1;
        public Vector3 Offset;
        [Tooltip("The list of object who's activation will depend on the visibility of this object.")]
        public GameObject[] CullingList;

        int Handle = -1;


        private void Start()
        {
            Handle = Culler.AddToGroup(this);
        }

        private void OnDestroy()
        {
            if(Culler != null)
                Culler.RemoveFromGroup(Handle);
        }

        private void Update()
        {
            Culler.UpdateSphere(Handle, transform.position + Offset);
        }

        public void BecameVisible()
        {
            foreach (var go in CullingList)
                go.SetActive(true);
        }

        public void BecameInvisible()
        {
            foreach (var go in CullingList)
                go.SetActive(false);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1, 0.6f, 0, 0.5f);
            Gizmos.DrawWireSphere(transform.position + Offset, Radius);
        }
    }
}