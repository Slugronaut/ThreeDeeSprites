using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Toolbox.Math;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

namespace ThreeDee
{
    /// <summary>
    /// The logical core of the rendering system. This component manages a single camera and its
    /// render-texture and all of the 3D models that drawn to it. However, this system isn't usually
    /// interacted with directly. Instead, higher-level wrapper call <see cref="ThreeDeeRenderChain"/> is used
    /// to manage a series of these surfaces and forward commands to them.
    /// 
    /// </summary>
    [DefaultExecutionOrder(ExecutionOrder + 1)]
    public class ThreeDeeSpriteSurface : MonoBehaviour
    {
        class RegisteredSprite
        {
            public Rect Rect;
            public int[] AllocatedTiles;
            public IThreeDeeSpriteRenderer Ref;
            public Transform OriginalParent;
        }


        #region Public Fields
        public const int ExecutionOrder = 1000;

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
                if (_PixelScale != value)
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
                if (_TileSize != value)
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
                if (value != _PrerenderCamera)
                {
                    _PrerenderCamera = value;
                    CreateTileMap(_TileSize);
                }
            }
        }

        [SerializeField]
        [Required("A material for billboards that render sprites from this surface must be supplied. Be sure to set the texture of that material to this surface's render texture!")]
        [Tooltip("The material that will be assigned to any billboard sprite who's model is pre-rendered to this surface. The material must supply a texture that references this surface's render texture.")]
        Material _BillboardMaterial;
        public Material BillboardMaterial
        {
            get => _BillboardMaterial;
        }
        
        #endregion


        #region Private Fields
        int ScreenWidth;
        int ScreenHeight;
        int TileCountX;
        int TileCountY;
        int Uids = 0; //incremented each time a sprite is allocated so we can give each a new id

        //public static readonly int MainTexId = Shader.PropertyToID("_MainTex"); //used to access the texture of the RenderTarget
        List<bool> UsedTiles = new(); //indicies into the Tiles list of what is currently in use
        readonly Dictionary<int, RegisteredSprite> Sprites = new();
        readonly List<RenderCommand> StaticCommands = new(100);
        readonly List<RenderCommandDynamic> DynamicCommands = new(100);
        float CameraRatio => ScreenHeight / _TileSize / _PixelScale * 0.5f;

        #endregion


        #region Unity Events
        /// <summary>
        /// 
        /// </summary>
        private void Awake()
        {
            DynamicCommands.Clear();
            CreateTileMap(_TileSize);
            _PrerenderCamera.enabled = false; //we don't need this rendering every frame, we'll be doing that manually
        }

        /// <summary>
        /// 
        /// </summary>
        void LateUpdate()
        {
#if UNITY_EDITOR
            if (DebugMode)
                DrawTiles();
#endif

            RenderStatic();
            //RenderDynamic();
        }
        #endregion


        #region Public Functions
        /// <summary>
        /// Renders allocated sprites to the surface's associated camera. Static sprites are left as is and
        /// are assumed to have been positioned ahead of time. Dynamic sprites have their position information
        /// uploaded to the global pre-render shader so that they can remain in world-space while
        /// </summary>
        public void RenderStatic()
        {
            if (PrerenderCamera == null) return;
            var clipRangeMidpoint = (PrerenderCamera.farClipPlane - PrerenderCamera.nearClipPlane) * 0.5f;

            //position all of the static models
            foreach (var com in StaticCommands)
            {
                if (com.SpriteHandle < 0) continue;
                float tileScale = _TileSize * com.TileResolution;
                tileScale /= ScreenHeight;
                var center = Sprites[com.SpriteHandle].Rect.center + com.Offset2D * tileScale;
                com.ModelRoot.position = _PrerenderCamera.ViewportToWorldPoint(new Vector3(center.x, center.y, clipRangeMidpoint));
                com.ModelRoot.transform.SetGlobalScale(Vector3.one * com.SpriteScale);
            }

            //then take a snapshot of all of them in one go
            StaticCommands.Clear();
            _PrerenderCamera.Render();
        }

        /// <summary>
        /// 
        /// </summary>
        public void RenderDynamic()
        {
            throw new UnityException("Not yet implemented");
            if (_PrerenderCamera == null) return;
            var clipRangeMidpoint = (PrerenderCamera.farClipPlane - PrerenderCamera.nearClipPlane) * 0.5f;


            //position the camera for each dynamic model, then take a snapshot at each position
            foreach (var com in DynamicCommands)
            {
                if (com.SpriteHandle < 0) continue;

                com.ModelRoot.gameObject.SetActive(true);
                float tileScale = _TileSize * com.TileResolution;
                tileScale /= ScreenHeight;
                var center = Sprites[com.SpriteHandle].Rect.center + com.Offset2D * tileScale;
                var objOffsetPos = _PrerenderCamera.ViewportToWorldPoint(new Vector3(center.x, center.y, clipRangeMidpoint));
                var objScale = Vector3.one * com.SpriteScale;


                com.ModelRoot.position = objOffsetPos;
                com.ModelRoot.transform.SetGlobalScale(objScale);

                var renderers = com.ModelRoot.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                }
            }
            
            DynamicCommands.Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        public void RenderSingleDynamic()
        {
            throw new UnityException("IMplemented for testing purposes only. Will be removed in the future.");
            if (PrerenderCamera == null) return;
            _PrerenderCamera.orthographicSize = CameraRatio;

            var camTrans = _PrerenderCamera.transform;
            var originalCamPosition = camTrans.position;
            var originalCamRotation = camTrans.rotation;
            var clipRangeMidpoint = (PrerenderCamera.farClipPlane - PrerenderCamera.nearClipPlane) * 0.5f;


            //position the camera for each dynamic model, then take a snapshot at each position
            foreach (var com in DynamicCommands)
            {
                if (com.SpriteHandle < 0) continue;

                com.ModelRoot.gameObject.SetActive(true);
                float tileScale = _TileSize * com.TileResolution;
                tileScale /= ScreenHeight;
                var center = Sprites[com.SpriteHandle].Rect.center + com.Offset2D * tileScale;
                var objOffsetPos = _PrerenderCamera.ViewportToWorldPoint(new Vector3(center.x, center.y, clipRangeMidpoint));
                var objScale = Vector3.one * com.SpriteScale;


                //rotate the pre-rendercamera based on position between model and player, then offset the camera in aim-space
                //to ensure the model will be rendered within the assigned rect of the render texture.
                Camera realCam = Camera.main;
                //OLD TEST CODE
                var camEuler = CameraRelativeFacing.QuantizedBillboardRotation(
                                                            CameraRelativeFacing.YawAngleSnap,
                                                            com.ModelRoot.position, 
                                                            realCam.transform.position);
                var camForward = Quaternion.Euler(camEuler) * Vector3.forward;
                var camPos = MathUtils.ForwardSpaceOffset(com.ModelRoot.position, camForward, -objOffsetPos);

                com.ModelRoot.transform.SetGlobalScale(objScale);
                camTrans.eulerAngles = camEuler;
                camTrans.position = camPos;
                _PrerenderCamera.RenderDontRestore();
            }

            //OLD TEST CODE
            camTrans.SetPositionAndRotation(originalCamPosition, originalCamRotation);
            DynamicCommands.Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="com"></param>
        public void AddCommand(RenderCommand com)
        {
            this.StaticCommands.Add(com);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="com"></param>
        public void AddCommand(RenderCommandDynamic com)
        {
            this.DynamicCommands.Add(com);
        }

        /// <summary>
        /// Allocates a space of the render target for a sprite of the desired size squared and returns a handle for that space.
        /// </summary>
        /// <param name="tileResolution"></param>
        /// <returns></returns>
        public int AllocateNewSprite(IThreeDeeSpriteRenderer spriteRef, bool unparentModel)
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

                if (unparentModel)
                {
                    if (spriteRef.ModelTrans != null)
                    {
                        sprite.OriginalParent = spriteRef.ModelTrans.parent;
                        spriteRef.ModelTrans.SetParent(null);
                    }
                }

                Sprites.Add(handle, sprite);

                //spriteRef.SpriteBillboard.GetComponent<MeshRenderer>().sharedMaterial.SetTexture(MainTexId, _PrerenderCamera.targetTexture);

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
        public void ReleaseSprite(int handle, bool unparentModel)
        {
            var tds = Sprites[handle].Ref;
            if (unparentModel)
            {
                //we have to delay this by exactly one frame now that unity doesn't allow
                //reparenting during the frame that something is enabled/disabled. We'll
                //use a couroutine to do this. Shouldn't cause *too* much garbage I hope
                StartCoroutine(DelayedReparent(tds.ModelTrans, Sprites[handle].OriginalParent));
            }
            
            ReleaseSpriteTiles(handle);
            Sprites.Remove(handle);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="child"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        IEnumerator DelayedReparent(Transform child, Transform parent)
        {
            Assert.IsNotNull(child);
            Assert.IsNotNull(parent);
            yield return null;
            child.SetParent(parent, false);
            child.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
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