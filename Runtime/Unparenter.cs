using UnityEngine;


namespace ThreeDee
{
    public class Unparenter : MonoBehaviour
    {
        public Transform TargetParent;
        public Transform[] Children;
        Transform[] OriginalParents;

        public void OnEnable()
        {
            OriginalParents = new Transform[Children.Length];
            for(int i = 0; i < Children.Length; i++)
            {
                OriginalParents[i] = Children[i].parent;
                Children[i].SetParent(TargetParent);
            }
        }

        public void RestoreChildren()
        {
            for(int i = 0; i < Children.Length; i++)
                Children[i].SetParent(OriginalParents[i]);

            OriginalParents = null;
        }
    }
}
