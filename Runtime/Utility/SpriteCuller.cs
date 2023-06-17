using System.Collections.Generic;
using UnityEngine;

namespace ThreeDee
{
    /// <summary>
    /// 
    /// </summary>
    public class SpriteCuller : MonoBehaviour
    {
        public static SpriteCuller Instance;
        public Camera Camera;
        [Min(1)]
        public int GrowSize = 100;

        CullingGroup Group;
        BoundingSphere[] Spheres = new BoundingSphere[1000];
        readonly Dictionary<int, CullableSprite> Sprites = new();
        int ActiveCount = 0;



        private void Awake()
        {
            Group = new CullingGroup();
            Group.targetCamera = Camera;
            Group.SetBoundingSpheres(Spheres);
            Group.onStateChanged = HandleStateChanged;
        }

        private void OnDestroy()
        {
            Group.Dispose();
            Group = null;
        }

        public void UpdateSphere(int handle, Vector3 pos)
        {
            Spheres[handle].position = pos;
        }

        public int AddToGroup(CullableSprite sprite)
        {
            if (ActiveCount >= Spheres.Length)
                ReAllocate(GrowSize);

            Spheres[ActiveCount] = new BoundingSphere(sprite.transform.position, sprite.Radius);
            Group.SetBoundingSphereCount(ActiveCount+1);
            Sprites[ActiveCount] = sprite;

            ActiveCount++;
            return ActiveCount-1;
        }

        public void RemoveFromGroup(int handle)
        {
            //guard in case this whole component was already destroyed
            if (Group == null)
                return;

            int last = ActiveCount - 1;
            var sprite = Sprites[last];
            Sprites.Remove(last);
            Sprites[handle] = sprite;
            Group.EraseSwapBack(handle);
        }

        void ReAllocate(int extra)
        {
            Debug.Log("Reallocating");
            var current = Spheres;
            Spheres = new BoundingSphere[Spheres.Length + extra];
            Group.SetBoundingSpheres(Spheres);
            for (int i = 0; i < current.Length; i++)
                Spheres[i] = current[i];
            Group.SetBoundingSphereCount(current.Length);
        }

        void HandleStateChanged(CullingGroupEvent evt)
        {
            var sprite = Sprites[evt.index];

            if(evt.hasBecomeVisible)
            {
                sprite.BecameVisible();
            }
            else if(evt.hasBecomeInvisible)
            {
                sprite.BecameInvisible();
            }
        }
    }


}
