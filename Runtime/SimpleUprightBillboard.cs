using UnityEngine;

namespace ThreeDee
{
    /// <summary>
    /// 
    /// </summary>
    public class SimpleUprightBillboard : MonoBehaviour
    {
        Transform Trans;
        Transform CamTrans;

        public void Awake()
        {
            Trans = transform;
            CamTrans = Camera.main.transform;
        }

        void Update()
        {
            Vector3 dir = CamTrans.forward;
            var rot = Quaternion.LookRotation(dir);

            var eangles = rot.eulerAngles;
            eangles.x = 0;
            eangles.z = 0;
            Trans.eulerAngles = eangles;
        }
    }
}
