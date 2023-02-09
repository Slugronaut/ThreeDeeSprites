using System.Collections;
using UnityEngine;

namespace ThreeDee
{
    public struct RenderCommand
    {
        public int SpriteHandle;
        public int TileResolution;
        public float SpriteScale;
        public Vector2 Offset;
        public Transform Obj;
        public int ChainId;
    }
}