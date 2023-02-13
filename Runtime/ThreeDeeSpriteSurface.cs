#define THREEDEE_UNPARENTMODEL

using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;

namespace ThreeDee
{
    /// <summary>
    /// The logical core of the rendering system. This component manages a single camera and its
    /// render-texture and all of the 3D models that drawn to it. However, this system isn't usually
    /// interacted with directly. Instead, higher-level wrapper call <see cref="ThreeDeeRenderChain"/> is used
    /// to manage a series of these surfaces and forward commands to them.
    /// </summary>
    [DefaultExecutionOrder(ExecutionOrder+1)]
    public class ThreeDeeSpriteSurface : MonoBehaviour
    {
        class RegisteredSprite
        {
            public Rect Rect;
            public int[] AllocatedTiles;
            public ThreeDeeSpriteRenderer Ref;
            #if THREEDEE_UNPARENTMODEL
            public Transform OriginalParent;
            #endif
        }


#region Public Fields
        public const int ExecutionOrder = 1000;
        public static ThreeDeeSpriteSurface Instance { get; private set; }

#if UNITY_EDITOR
        public bool DebugMode;
#endif

        [SerializeField]
        [HideInInspector]
        float _PixelScale = 1;
        [ShowInInspector]
        [Tooltip("The relation between the render texture size, the tile size, and the orthographic view size.")]
        [MinValue(0.01f)]
        public float PixelScale
        {
            get => _PixelScale;
            set
            {
                if(_PixelScale != value)
                {
                    _PixelScale = value;
                    PrerenderCamera.orthographicSize = CameraRatio;
                    CreateTileMap(_TileSize);
                }
            }
        }

        [SerializeField]
        [HideInInspector]
        int _TileSize = 32;
        [ShowInInspector]
        [Tooltip("Must be a factor of the RenderTexture's resolution. The smallest squared tile of pixels on the reder texture that can be allocated to a single sprite. Larger sprites may require multiples of these.")]
        [ValidateInput("ValidateTileSize", "Must be a factor of camera's target resolution.", InfoMessageType.Error)]
        [MinValue(1)]
        public int TileSize
        {
            get => _TileSize;
            set
            {
                if(_TileSize != value)
                {
                    _TileSize = value;
                    CreateTileMap(value);
                }
            }
        }

        [SerializeField]
        [HideInInspector]
        Camera _PrerenderCamera;
        [ShowInInspector]
        [Required("A camera must be supplied.")]
        [Tooltip("The camera that will be used to pre-render all models as sprites to a render texture.")]
        public Camera PrerenderCamera
        {
            get => _PrerenderCamera;
            set
            {
                if(value != _PrerenderCamera)
                {
                    _PrerenderCamera = value;
                    CreateTileMap(_TileSize);
                }
            }
        }
#endregion


#region Private Fields
        int ScreenWidth;
        int ScreenHeight;
        int TileCountX;
        int TileCountY;
        int Uids = 0; //incremented each time a sprite is allocated so we can give each a new id

        List<bool> UsedTiles = new(); //indicies into the Tiles list of what is currently in use
        Dictionary<int, RegisteredSprite> Sprites = new();
        List<RenderCommand> Commands = new(100);

        float CameraRatio => ScreenHeight / _TileSize / 2 / _PixelScale;
#endregion


#region Unity Events
        /// <summary>
        /// 
        /// </summary>
        private void Awake()
        {
            CreateTileMap(_TileSize);
            Instance = this;
        }

        /// <summary>
        /// 
        /// </summary>
        void LateUpdate()
        {
            if (PrerenderCamera == null) return;
            PrerenderCamera.orthographicSize = CameraRatio;

            foreach (var com in Commands)
            {
                if (com.SpriteHandle < 0) continue;
                float tileScale =  _TileSize * com.TileResolution;
                tileScale /= ScreenHeight;
                var center = Sprites[com.SpriteHandle].Rect.center + com.Offset * tileScale;
                var pos = PrerenderCamera.ViewportToWorldPoint(new Vector3(center.x, center.y, (PrerenderCamera.farClipPlane - PrerenderCamera.nearClipPlane) * 0.5f));
                com.Obj.position = pos;
                com.Obj.transform.SetGlobalScale(Vector3.one * com.SpriteScale);// com.TileResolution;
            }

#if UNITY_EDITOR
            if(DebugMode)
                DrawTiles();
#endif

            PrerenderCamera.Render();
            Commands.Clear();
        }
#endregion

        public static readonly int MainTexId = Shader.PropertyToID("_MainTex");

#region Public Functions
        /// <summary>
        /// 
        /// </summary>
        /// <param name="com"></param>
        public void AddCommand(RenderCommand com)
        {
            this.Commands.Add(com);
        }

        /// <summary>
        /// Allocates a space of the render target for a sprite of the desired size squared and returns a handle for that space.
        /// </summary>
        /// <param name="tileResolution"></param>
        /// <returns></returns>
        public int AllocateNewSprite(ThreeDeeSpriteRenderer spriteRef)
        {
            var (success, rect, indicies) = FindAvailableSpace(spriteRef.TileResolution);
            if (success)
            {
                int handle = Uids++;

                RegisteredSprite sprite = new()
                {
                    AllocatedTiles = indicies,
                    Rect = rect,
                    Ref = spriteRef,
                };
#if THREEDEE_UNPARENTMODEL
                sprite.OriginalParent = spriteRef.ModelTrans.parent;
                spriteRef.ModelTrans.SetParent(null);
#endif
                Sprites.Add(handle, sprite);

                //spriteRef.SpriteBillboard.GetComponent<MeshRenderer>().material.SetTexture(MainTexId, _PrerenderCamera.targetTexture);

                AllocateSpriteTiles(handle, indicies);
                ApplyTileRectToMesh(rect, spriteRef.SpriteBillboard.mesh);
                return handle;
            }

            //uh oh! we have no room left!
            return -1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="handle"></param>
        public void ReleaseSprite(int handle)
        {
#if THREEDEE_UNPARENTMODEL
            var tds = Sprites[handle].Ref;
            if(tds.isActiveAndEnabled)
                Sprites[handle].Ref.ModelTrans.SetParent(Sprites[handle].OriginalParent);
#endif
            ReleaseSpriteTiles(handle);
            Sprites.Remove(handle);
        }

        /// <summary>
        /// Re-allocates all registered sprites. Useful when changing tilemap sizes and scales.
        /// </summary>
        public void ReallocateTiles()
        {
            if (PrerenderCamera.targetTexture == null) return;
            PrerenderCamera.orthographicSize = CameraRatio;

            UsedTiles = new List<bool>(new bool[TileCountX * TileCountY]);
            List<int> removeList = new();

            foreach (var kvp in Sprites)
            {
                var sprite = kvp.Value;
                var (success, rect, indicies) = FindAvailableSpace(sprite.Ref.TileResolution);
                if (success)
                {
                    sprite.AllocatedTiles = indicies;
                    sprite.Rect = rect;
                    AllocateSpriteTiles(kvp.Key, indicies);
                    ApplyTileRectToMesh(rect, sprite.Ref.SpriteBillboard.mesh);
                }
                else removeList.Add(kvp.Key);
            }

            foreach (var handle in removeList)
                Sprites.Remove(handle);
        }
#endregion

    
#region Private Functions
        /// <summary>
        /// 
        /// </summary>
        /// <param name="handle"></param>
        void ReleaseSpriteTiles(int handle)
        {
            int[] indicies = Sprites[handle].AllocatedTiles;
            foreach (var index in indicies)
                UsedTiles[index] = false;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="quad"></param>
        void ApplyTileRectToMesh(Rect rect, Mesh quad)
        {
            //remember, (0,0) is actually bottom-left in Unity. (1,1) is top-right
            //Winding order
            // 0 1
            // 2 3
            var uvs = quad.uv;
            uvs[0].Set(rect.x,               rect.y);
            uvs[1].Set(rect.x + rect.width,  rect.y);
            uvs[2].Set(rect.x,               rect.y + rect.height);
            uvs[3].Set(rect.x + rect.width,  rect.y + rect.width);

            //quad.SetUVs(UVChannel, TempUV);
            quad.uv = uvs;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="indicies"></param>
        void AllocateSpriteTiles(int handle, int[] indicies)
        {
            foreach (var index in indicies)
                UsedTiles[index] = true;
        }

        /// <summary>
        /// Searches for an unused 
        /// </summary>
        /// <param name="tileResolution"></param>
        /// <returns></returns>
        (bool success, Rect rect, int[] tiles) FindAvailableSpace(int tileResolution)
        {
            //now we need to search through the list of rects until we find a space
            for (int y = 0; y <= TileCountY-tileResolution; y++)
            {
                for(int x = 0; x <= TileCountX-tileResolution; x++)
                {
                    if (IsTileRangeEmpty(x, y, tileResolution, tileResolution))
                    {
                        return (true,
                                CalculateRectFromTile(x, y, tileResolution, tileResolution),
                                TileIndicesFromRange(x, y, tileResolution, tileResolution));
                    }
                }
            }

            return (false, Rect.zero, null);
        }

        /// <summary>
        /// Returns the linera indicies for a range of tiles.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="tileWidth"></param>
        /// <param name="tileHeight"></param>
        /// <returns></returns>
        int[] TileIndicesFromRange(int x, int y, int tileWidth, int tileHeight)
        {
            AssertTileSizes(x, y, tileWidth, tileHeight);

            int[] output = new int[tileWidth * tileHeight];
            int count = 0;
            for (int i = y; i < y+tileHeight; i++)
            {
                for (int j = x; j < x+tileWidth; j++, count++)
                {
                    output[count] = (i * TileCountX) + j;
                }
            }

            return output;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        bool IsTileEmpty(int x, int y)
        {
            Assert.IsTrue(x >= 0);
            Assert.IsTrue(y >= 0);
            Assert.IsTrue(x < TileCountX);
            Assert.IsTrue(y < TileCountY);
            return !UsedTiles[(y * TileCountX) + x];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="tileWidth"></param>
        /// <param name="tileHeight"></param>
        /// <returns></returns>
        bool IsTileRangeEmpty(int x, int y, int tileWidth, int tileHeight)
        {
            AssertTileSizes(x, y, tileWidth, tileHeight);

            for (int i = y; i < y+tileHeight; i++)
            {
                for(int j = x; j < x+tileWidth; j++)
                {
                    if (UsedTiles[(i * TileCountX) + j])
                        return false;
                }
            }


            //Debug.Log($"Empty tile range found for ({x}, {y}) - [{tileWidth}, {tileWidth}]");
            return true;
        }

        /// <summary>
        /// Calculates the UV rect for a given tile. This uses the same 0-1 space
        /// as unity where (0,0) is bottom-left and (1,1) is top-right.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="widht"></param>
        /// <param name="tileHeight"></param>
        /// <returns></returns>
        Rect CalculateRectFromTile(int x, int y, int tileWidth, int tileHeight)
        {
            AssertTileSizes(x, y, tileWidth, tileHeight);
            
            float screenWidth = ScreenWidth;
            float screenHeight = ScreenHeight;

            float xPixel = TileSize * x;
            float yPixel = TileSize * y;
            float pixelWidth = TileSize * tileWidth;
            float pixelHeight = TileSize * tileHeight;

            float xClip = xPixel / screenWidth;
            float yClip = yPixel / screenHeight;
            float clipWidth = pixelWidth / screenWidth;// * ViewRatio;
            float clipHeight = pixelHeight / screenHeight;// * ViewRatio;
            return new Rect(xClip, yClip, clipWidth, clipHeight);
        }

        /// <summary>
        /// Allocates a list of Rects that represent the sprite tiles that can be used by this system.
        /// </summary>
        /// <param name="tileSize"></param>
        void CreateTileMap(int tileSize)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) return;
#endif
            if (PrerenderCamera.targetTexture == null) return;
            PrerenderCamera.orthographicSize = CameraRatio;

            var desc = PrerenderCamera.targetTexture.descriptor;
            ScreenWidth = desc.width;
            ScreenHeight = desc.height;

            Assert.AreEqual(0, ScreenWidth % tileSize);
            Assert.AreEqual(0, ScreenHeight % tileSize);

            TileCountX = ScreenWidth / tileSize;
            TileCountY = ScreenHeight / tileSize;

            ReallocateTiles();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="tileWidth"></param>
        /// <param name="tileHeight"></param>
        [Conditional("UNITY_EDITOR")]
        void AssertTileSizes(int x, int y, int tileWidth, int tileHeight)
        {
            Assert.IsTrue(x >= 0);
            Assert.IsTrue(y >= 0);
            Assert.IsTrue(x < TileCountX);
            Assert.IsTrue(y < TileCountY);
            Assert.IsTrue(x + tileWidth <= TileCountX);
            Assert.IsTrue(y + tileHeight <= TileCountY);
        }
#endregion


#region Editor
#if UNITY_EDITOR
        /// <summary>
        /// Validates the inspector input.
        /// </summary>
        /// <param name="tileSize"></param>
        /// <returns></returns>
        private bool ValidateTileSize(int tileSize)
        {
            if (PrerenderCamera == null || PrerenderCamera.targetTexture == null) return false;
            var desc = PrerenderCamera.targetTexture.descriptor;
            if (desc.width < tileSize) return false;
            if (desc.height < tileSize) return false;
            if (desc.width % tileSize != 0) return false;
            if (desc.height % tileSize != 0) return false;
            return true;
        }

        /// <summary>
        /// Helper for visualizing what the fuck-the-hell is actually fucking going the fuck on.
        /// In other words; it draws the tile grid and allocated spaces for each sprite in the editor window.
        /// </summary>
        void DrawTiles()
        {
            for (int y = 0; y < TileCountY; y++)
            {
                for (int x = 0; x < TileCountX; x++)
                {
                    Color c = (x % 2 != 0) ? new Color(0.5f, 0.5f, 1, 1) : Color.white;

                    var tile = CalculateRectFromTile(x, y, 1, 1);
                    float tileScale = 1;

                    var vert0 = new Vector3(tile.x, tile.y) * tileScale;
                    var vert1 = new Vector3(tile.xMax, tile.y) * tileScale;
                    var vert2 = new Vector3(tile.xMax, tile.yMax) * tileScale;
                    var vert3 = new Vector3(tile.x, tile.yMax) * tileScale;

                    var wv0 = PrerenderCamera.ViewportToWorldPoint(vert0);
                    var wv1 = PrerenderCamera.ViewportToWorldPoint(vert1);
                    var wv2 = PrerenderCamera.ViewportToWorldPoint(vert2);
                    var wv3 = PrerenderCamera.ViewportToWorldPoint(vert3);


                    Debug.DrawLine(wv0, wv1, c);
                    Debug.DrawLine(wv1, wv2, c);
                    Debug.DrawLine(wv2, wv3, c);
                    Debug.DrawLine(wv3, wv0, c);
                }
            }

            for (int y = 0; y < TileCountY; y++)
            {
                for (int x = 0; x < TileCountX; x++)
                {
                    if (!IsTileEmpty(x, y))
                    {
                        Color c = Color.red;

                        var tile = CalculateRectFromTile(x, y, 1, 1);
                        float tileScale = 1;

                        var vert0 = new Vector3(tile.x, tile.y) * tileScale;
                        var vert1 = new Vector3(tile.xMax, tile.y) * tileScale;
                        var vert2 = new Vector3(tile.xMax, tile.yMax) * tileScale;
                        var vert3 = new Vector3(tile.x, tile.yMax) * tileScale;

                        var wv0 = PrerenderCamera.ViewportToWorldPoint(vert0);
                        var wv1 = PrerenderCamera.ViewportToWorldPoint(vert1);
                        var wv2 = PrerenderCamera.ViewportToWorldPoint(vert2);
                        var wv3 = PrerenderCamera.ViewportToWorldPoint(vert3);


                        Debug.DrawLine(wv0, wv1, c);
                        Debug.DrawLine(wv1, wv2, c);
                        Debug.DrawLine(wv2, wv3, c);
                        Debug.DrawLine(wv3, wv0, c);
                    }
                }
            }
        }
#endif
#endregion
    }
}