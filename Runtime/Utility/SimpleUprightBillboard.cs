using Peg.Systems;
using UnityEngine;

namespace ThreeDee
{
    /// <summary>
    /// 
    /// </summary>
    public class SimpleUprightBillboard : MonoBehaviour, IFrameTickable
    {
        Transform Trans;
        Transform CamTrans;

        public bool TickEnabled => enabled;

        public void Awake()
        {
            FrameTickSystem.Instance.Register(this);
            Trans = transform;
            CamTrans = Camera.main.transform;
        }

        void OnDestroy()
        {
            FrameTickSystem.Instance.Unregister(this);
        }

        public void OnTick()
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
