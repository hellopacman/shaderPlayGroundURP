using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using System;
//using DG.Tweening;

namespace SLGGame
{
    // 2023-8-11 原WarfogCtrler被很多旧代码所使用不好重构，在这里继续迭代
    // 【迷雾格子】-> 小块迷雾
    public class WarfogCtrler2 : MonoBehaviour
    {
        protected static readonly int _fogMaskTexId = Shader.PropertyToID("_FogMaskTex");
        protected static readonly int _clipMaskTexId = Shader.PropertyToID("_ClipMask"); 


        // wusy 2023-2-28 该枚举用于计算，请不要随意调整其取值
        protected enum EFogDataType
        {
            Occupy = 0,// 也可以当做alpha来用
            Selected = 1,
            ChangeColor = 2,
            Alpha = 3,
        }

        // 格子尺寸
        public Vector2 GridSize = new Vector2(10, 10);  // 迷雾格子尺寸(单位:米)
        // 迷雾动态区域迷雾格子数
        public Vector2Int GridNum = new Vector2Int(30, 30); // 迷雾格子数量(不包括封边)
        // 2022-6-15 wusy 遮罩遮边用，拓展mask(xy左右；zw下上)
        // 不要使用负值
        public int4 maskExpand;

        // 迷雾开合值
        protected readonly byte openFlag = 0;    // 迷雾开(没雾)
        protected readonly byte unopenFlag = 1; // 迷雾未开(有雾)

        // 测试开关，开启后迷雾会显示为棋盘格子，方便观察迷雾格子大小
        public bool test = false;

        // 为了保证迷雾能遮住游戏里各种奇奇怪怪的renderQueue...
        public int sortOrder = 0;

        // 微调迷雾水平位置，单位(米)
        public Vector2 posOffset = Vector2.zero;

        // wusy 2023-3-1 单个小块迷雾的像素分辨率，为了更加细致的边缘表现
        // 默认迷雾格子都是方形的，即长宽像素数量相同
        // 必须大于0
        public int pixelTessellation = 32;
        private int ctrlMaskTessellation = 4; // ctrlMask每个格子的像素尺寸

        // mask纹理一个像素有几个通道
        // (目前只支持8bit通道)
        public int maskPixelChannelCount = 4;

        //public Texture2D borderMask;    // 迷雾封边图
        public Texture2D blockPattern;  // 迷雾格子填充纹理
        public Texture2D holeCorner;    // 拐角填充纹理
        public int patternBorderSize = 8;

        protected MeshRenderer _meshRenderer;
        protected int actualGridNum_x, actualGridNum_y;    // 加上封边后的迷雾横竖格子数。 单位：小块迷雾
        protected int actualPixelWidth, actualPixelHeight;      // 单位：像素。应用了PixelTessellation之后的mask像素尺寸
        protected int actualCtrlMaskWidth, actualCtrlMaskHeight;

        public Texture2D _fogMask;
        protected bool _needApply = false;

        public Texture2D _clipMask;
        private int _clipMaskChannelCount = 1;
        protected bool _needApplyClipMask = false;

        public Texture2D _ctrlMask;
        private int _ctrlMaskChannelCount = 3;
        protected bool _needApplyCtrlMask = false;

        static Color32[] blankCornerColArr;
        static Color32[] blockColArr;
        protected byte[,] _fogData;   // 本迷雾片格子开合数据
        protected NativeArray<byte> _fogTexData; // 迷雾遮罩纹理底层数据
        private byte[] _fogMaskByteArr;

        private Color32[] _clipOpenGridCol32Arr;
        private Color32[] _clipCloseGridCol32Arr;


        protected void PreInit()
        {
            // 初始化格子开合
            _fogData = new byte[GridNum.x, GridNum.y];
            for (int i = 0; i < GridNum.x; i++)
            {
                for (int j = 0; j < GridNum.y; j++)
                {
                    _fogData[i, j] = unopenFlag;
                }
            }

            _meshRenderer = GetComponent<MeshRenderer>();
            // 开启迷雾的显示(为避免干扰美术同学编辑场景，它默认是隐藏的)
            _meshRenderer.enabled = true;
            _meshRenderer.sortingOrder = sortOrder;

            // 加上封边后的格子数
            actualGridNum_x = GridNum.x + (int)(maskExpand.x + maskExpand.y);
            actualGridNum_y = GridNum.y + (int)(maskExpand.z + maskExpand.w);

            actualPixelWidth = actualGridNum_x * pixelTessellation;
            actualPixelHeight = actualGridNum_y * pixelTessellation;

            // ctrlMask的一个像素恰好覆盖住出血区域最经济
            if (pixelTessellation % patternBorderSize != 0)
            {
                Debug.LogError("clipMask和ctrlMask的尺寸不是整数倍");
                return;
            }
            ctrlMaskTessellation = pixelTessellation / patternBorderSize;
            actualCtrlMaskWidth = actualGridNum_x * ctrlMaskTessellation;
            actualCtrlMaskHeight = actualGridNum_y * ctrlMaskTessellation;

            if (blankCornerColArr == null)
            {
                int size = patternBorderSize * patternBorderSize;
                blankCornerColArr = new Color32[size];
                for (int i = 0; i < patternBorderSize * patternBorderSize; i++)
                {
                    blankCornerColArr[i] = (Color32)(Color.black);
                }
            }

            if (blockColArr == null)
            {
                blockColArr = blockPattern.GetPixels32();
            }

            // 填充_clipMask无迷雾格子的数据
            _clipOpenGridCol32Arr = new Color32[pixelTessellation * pixelTessellation];
            for (int i = 0; i < _clipOpenGridCol32Arr.Length; i++)
            {
                _clipOpenGridCol32Arr[i] = new Color32(0, 0, 0, 0);
            }

            // 填充_clipMask有迷雾格子的数据
            _clipCloseGridCol32Arr = new Color32[pixelTessellation * pixelTessellation];
            for (int i = 0; i < _clipCloseGridCol32Arr.Length; i++)
            {
                _clipCloseGridCol32Arr[i] = new Color32(0xff, 0, 0, 0);
            }

            // 创建迷雾纹理
            TryCreateFogMask();
        }


        protected virtual void LateUpdate()
        {
            // 一帧只Apply一次
            if (_needApply)
            {                
                _fogMask.SetPixelData<byte>(_fogMaskByteArr, 0);                
                _fogMask.Apply();
                _needApply = false;
            }
            if (_needApplyClipMask)
            {
                _clipMask.Apply();
                _needApplyClipMask = false;
            }
            if (_needApplyCtrlMask)
            {
                _ctrlMask.Apply();
                _needApplyCtrlMask = false;
            }
        }



        protected virtual void OnDestroy()
        {
            if (_fogMask != null)
            {
                //_fogTexData.Dispose();
                Destroy(_fogMask);
            }
            if (_clipMask != null)
            {
                Destroy(_clipMask);
            }
            if (_ctrlMask != null)
            {
                Destroy(_ctrlMask);
            }


            _texPixelsDic = null;
            _texPixelCol32sDic = null;
        }




        public void TryCreateFogMask()
        {
            if (_fogMask == null)
            {
                CreateFogMask();
            }


        }

        private void CreateFogMask()
        {
            // 2023年10月3日 wusy 原_fogMask拆分成_clipMask和_ctrlMask两个纹理
            // _ctrlMask可以用较低的分辨率，以便优化
            _clipMask = new Texture2D(actualPixelWidth, actualPixelHeight, TextureFormat.R8, false);
            if (test)
            {
                _clipMask.filterMode = FilterMode.Point;
            }
            _clipMask.wrapMode = TextureWrapMode.Clamp;


            // R 迷雾选择, G未解锁迷雾区域颜色, B alpha
            _ctrlMask = new Texture2D(actualCtrlMaskWidth, actualCtrlMaskHeight, TextureFormat.RGB24, false);
            _ctrlMask.filterMode = FilterMode.Point;
            _ctrlMask.wrapMode = TextureWrapMode.Clamp;


            // 创建迷雾遮罩
            // 2023-2-24 wusy 新增【迷雾选择高亮】需求，打算新开通道来记录选择信息
            // _fogMask格式从原Alpha8升为RGB24。R 迷雾开合, G 迷雾选择, B 预留
            // 2023-3-21 wusy 新增【未解锁迷雾区域颜色】需求，正好把剩下的B通道用上
            // 2023-3-23 wusy 为了在控制局部透明度的同时不影响边缘形状，把_fogMask格式升级为RGBA32。用A通道做不透明控制
            _fogMask = new Texture2D(actualPixelWidth, actualPixelHeight, TextureFormat.RGBA32, false);
            if (test)
            {
                _fogMask.filterMode = FilterMode.Point;
            }
            _fogMask.wrapMode = TextureWrapMode.Clamp;

            //TryUpdateFogTexData();

            /*
            // 有borderMask且规格相符时复制其内容
            if (borderMask != null)
            {
                if (borderMask.width != _fogMask.width || borderMask.height != _fogMask.height || borderMask.format != _fogMask.format)
                {
                    Debug.LogError("迷雾遮罩与基础图尺寸或者格式不相符");
                    borderMask = null;
                }
                else
                {
                    // Unity手册上说
                    // GetRawTextureData does not allocate memory;
                    // the returned NativeArray directly points to the texture system memory data buffer. 
                    // 所以应该没有必要dispose
                    var srcTexData = borderMask.GetRawTextureData<byte>();
                    srcTexData.CopyTo(_fogTexData);
                    _fogMaskByteArr = borderMask.GetPixels32();

                }
            }

            // 没有borderMask时填充初始颜色
            if (borderMask == null)
            {
                InitFogMask();
            }
            */

            InitFogMask();

            Material mat = GetComponent<MeshRenderer>().material;
            mat.SetTexture(_fogMaskTexId, _fogMask);
            mat.SetTexture(_clipMaskTexId, _clipMask);
            _needApply = true;
            // 测试
            // image.texture = _fogMask;
            // SetFogPixels(20, 2, 10, 5, true);
        }

 

        protected virtual void InitFogMask()
        {
            // todo 如果游戏开始后一定会重绘整个迷雾，那就不用初始化_clipMask
            int clipMaskPixelId = 0;
            var clipMaskRawData = _clipMask.GetRawTextureData<byte>();
            for (int y = 0; y < _clipMask.height; y++)
            {
                for (int x = 0; x < _clipMask.width; x++)
                {
                    int byteId = clipMaskPixelId * _clipMaskChannelCount;   // _clipMaskChannelCount目前是1
                    clipMaskRawData[byteId++] = 0xff;  // R 迷雾开合，默认有迷雾
                    clipMaskPixelId++;
                }
            }
            _needApplyClipMask = true;


            // todo 外封边区域迷雾颜色调暗一些
            int ctrlMaskPixelId = 0;
            var ctrlMaskRawData = _ctrlMask.GetRawTextureData<byte>();
            for (int y = 0; y < _ctrlMask.height; y++)
            {
                for (int x = 0; x < _ctrlMask.width; x++)
                {
                    int byteId = ctrlMaskPixelId * _ctrlMaskChannelCount; // _ctrlMaskChannelCount目前是3
                    ctrlMaskRawData[byteId++] = 0;      // R 高亮选择，默认无高亮
                    ctrlMaskRawData[byteId++] = 0;  // G 变色
                    ctrlMaskRawData[byteId++] = 0xff;   // B，不透明度，默认不透明
                    ctrlMaskPixelId++;

                }
            }
            _needApplyCtrlMask = true;


            int index = 0;
            _fogMaskByteArr = new byte[_fogMask.width * _fogMask.height * maskPixelChannelCount];

            // 2023-9-1 wusy 临时需求，外封边区域迷雾颜色调暗一些
            //Rect range = new Rect(maskExpand[0] * pixelTessellation + patternBorderSize, maskExpand[2] * pixelTessellation + patternBorderSize,
            //    GridNum.x * pixelTessellation - patternBorderSize * 2,
            //    GridNum.y * pixelTessellation - patternBorderSize * 2
            //    );
            Rect range = new Rect(maskExpand[0] * pixelTessellation, maskExpand[2] * pixelTessellation,
                GridNum.x * pixelTessellation,
                GridNum.y * pixelTessellation
                );
            Vector2 pixelPos = new Vector2();

            // Unity Texture coordinates start at lower left corner.
            for (int y = 0, jcount = _fogMask.height; y < jcount; y++)
            {
                for (int x = 0, icount = _fogMask.width; x < icount; x++)
                {
                    byte channelB = 0xff;   // 有变色
                    pixelPos.x = x;
                    pixelPos.y = y;
                    if (range.Contains(pixelPos))
                    {
                        // 非封边迷雾无变色
                        channelB = 0;
                    }

                    int byteId = index * 4;
                    _fogMaskByteArr[byteId++] = 0xff;  // R 迷雾开合，默认有迷雾
                    _fogMaskByteArr[byteId++] = 0;      // G 高亮选择，默认无高亮
                    _fogMaskByteArr[byteId++] = channelB;  // B 变色
                    _fogMaskByteArr[byteId++] = 0xff;   // A，不透明度，默认不透明
                    index++;
                }
            }
            _needApply = true;
        }



        // 填充一个迷雾格子内的像素
        // fogX,fogY,小块迷雾坐标，含封边
        // isTrue，迷雾是否开合，是否高亮选择等等
        // channelOffset，用来控制写入Mask的哪个通道
        // isAdaptAdjacent 高亮选择，渐隐等效果，是否要做【出血】效果：向相邻迷雾格子渗透一些像素
        protected void FillGridPixels(int fogX, int fogY, bool isTrue, byte fogMaskVal, int ChannelCount = 1, int maskChannelOffset = 0, bool isAdaptAdjacent = false)
        {
            // 迷雾格子坐标转换为纹理坐标
            int pixelX = fogX * pixelTessellation;
            int pixelY = fogY * pixelTessellation;
            int pixelMaxX = pixelX + pixelTessellation;
            int pixelMaxY = pixelY + pixelTessellation;

            if (isAdaptAdjacent)
            {
                int fogDataX = fogX - maskExpand.x;
                int fogDataY = fogY - maskExpand.z;

                // wusy 2023-6-9
                byte[,] statusArr = _fogData;
                int statusArrWidth = statusArr.GetLength(0);
                int statusArrHeight = statusArr.GetLength(1);

                // todo 如果上下左右全是迷雾，就不用缩边了

                // 为了搭配高亮选择，渐隐等效果
                // 相邻侧是空洞则向其出血一个patternBorderSize，是迷雾则收缩一个patternBorderSize
                // 左边
                float expandFactor = 1f;
                float shrinkFactor = 0.7f;

                if (fogDataX == 0)  // 贴边的迷雾，先默认按照有封边处理 todo 看是否考虑没有封边的情况
                {
                    pixelX += (int)(patternBorderSize * shrinkFactor);
                }
                else if (fogDataX - 1 >= 0)
                {
                    if (statusArr[fogDataX - 1, fogDataY] == 0)
                    {
                        pixelX -= (int)(patternBorderSize * expandFactor);
                    }
                    else
                    {
                        pixelX += (int)(patternBorderSize * shrinkFactor);
                    }
                }
                // 右边
                if (fogDataX == statusArrWidth - 1)
                {
                    pixelMaxX -= (int)(patternBorderSize * shrinkFactor);
                }
                if (fogDataX + 1 < statusArrWidth)
                {
                    if (statusArr[fogDataX + 1, fogDataY] == 0)
                    {
                        pixelMaxX += (int)(patternBorderSize * expandFactor);
                    }
                    else
                    {
                        pixelMaxX -= (int)(patternBorderSize * shrinkFactor);
                    }
                }
                // 下边
                if (fogDataY == 0)
                {
                    pixelY += (int)(patternBorderSize * shrinkFactor);
                }
                else if (fogDataY - 1 >= 0)
                {
                    if (statusArr[fogDataX, fogDataY - 1] == 0)
                    {
                        pixelY -= (int)(patternBorderSize * expandFactor);
                    }
                    else
                    {
                        pixelY += (int)(patternBorderSize * shrinkFactor);
                    }
                }
                // 上边
                if (fogDataY == statusArrHeight - 1)
                {
                    pixelMaxY -= (int)(patternBorderSize * shrinkFactor);
                }
                else if (fogDataY + 1 < statusArrHeight)
                {
                    if (statusArr[fogDataX, fogDataY + 1] == 0)
                    {
                        pixelMaxY += (int)(patternBorderSize * expandFactor);
                    }
                    else
                    {
                        pixelMaxY -= (int)(patternBorderSize * shrinkFactor);
                    }
                }
            }

            int pixelId = pixelY * actualPixelWidth + pixelX;

            // 2023-3-1 wusy 填充该小块迷雾下的所有像素
            for (int v = 0; v < pixelMaxY - pixelY; v++)
            {
                for (int u = 0; u < pixelMaxX - pixelX; u++)
                {
                    _fogMaskByteArr[(pixelId + u) * ChannelCount + maskChannelOffset] = (byte)(isTrue ? fogMaskVal : 0);
                }

                pixelId += actualPixelWidth;
            }

            _needApply = true;
        }


        // statusArr 迷雾开合数据矩阵
        // startX，startY是含expand封边小块迷雾坐标
        // x,y 是从(startX, startY)开始的相对(小块迷雾)坐标
        // isTrue 迷雾开/合
        // 用预设纹理填充空洞，优化边缘效果
        protected void PopulatePixelPattern(ref byte[,] statusArr, int x, int y, int startX, int startY, bool isTrue, bool isBatching, int ChannelCount = 1, int maskChannelOffset = 0)
        {
            if (!isTrue)
            {
                int fogX = x + startX;
                int fogY = y + startY;
                // 不含封边小块迷雾坐标
                int fogDataX = fogX - maskExpand.x;
                int fogDataY = fogY - maskExpand.z;

                // 小迷雾坐标转换为纹理坐标
                int pixelX = fogX * pixelTessellation;
                int pixelY = fogY * pixelTessellation;

                // 先用Get/SetPixels32把效果弄出来，如果效率有问题或者纹理格式有变化再说
                // 先把blockPattern图案内容覆盖到空洞上
                //Color32[] col32Arr = blockPattern.GetPixels32();
                _fogMask.SetPixels32(pixelX, pixelY, pixelTessellation, pixelTessellation, blockColArr);

                // 检查处理边缘
                // 当前迷雾块某方向上有相邻迷雾区域，且其迷雾已消除，此时需要处理
                // 左侧
                // 从二维数组中获取处理区域的宽高
                int width = statusArr.GetLength(0);
                int height = statusArr.GetLength(1);

                // 批量处理时我们按照从左向右，从下往上的顺序
                // 因此每个迷雾只需要处理左，下两个方向即可
                // (处理右，上的话会被下一个迷雾给覆盖掉)
                // 下侧
                if (fogDataY > 0 && statusArr[fogDataX, fogDataY - 1] == 0)
                {
                    //Debug.LogFormat("({0},{1})下侧有迷雾空洞需要处理", fogDataX, fogDataY);
                    ConnectFogHole(fogX, fogY, 0, -1);
                }

                // 左侧

                if (fogDataX > 0 && statusArr[fogDataX - 1, fogDataY] == 0)
                {
                    //Debug.LogFormat("({0},{1})左侧有迷雾空洞需要处理", fogDataX, fogDataY);
                    ConnectFogHole(fogX, fogY, -1, 0);
                }


                // 非批量处理时为保证效果，四个方向都检查
                if (!isBatching)
                {
                    // 右侧
                    if (fogDataX < width - 1 && statusArr[fogDataX + 1, fogDataY] == 0)
                    {
                        //Debug.LogFormat("({0},{1})右侧有迷雾空洞需要处理", fogDataX, fogDataY);
                        ConnectFogHole(fogX, fogY, 1, 0);
                    }

                    // 上侧
                    if (fogDataY < height - 1 && statusArr[fogDataX, fogDataY + 1] == 0)
                    {
                        //Debug.LogFormat("({0},{1})上侧有迷雾空洞需要处理", fogDataX, fogDataY);
                        ConnectFogHole(fogX, fogY, 0, 1);
                    }
                }
                _needApply = true;
            }
        }

        // fogX, fogY 含封边的小块迷雾坐标
        // offsetX, offsetY 描述空洞连接的方向[-1, 0, 1]
        private void ConnectFogHole(int fogX, int fogY, int offsetX, int offsetY)
        {
            offsetX = math.clamp(offsetX, -1, 1);
            offsetY = math.clamp(offsetY, -1, 1);

            // 只处理直接相邻的情况
            if (math.abs(offsetX) == math.abs(offsetY))
            {
                return;
            }

            // 小迷雾坐标转换为纹理坐标
            int pixelX = fogX * pixelTessellation;
            int pixelY = fogY * pixelTessellation;

            if (offsetX != 0)
            {
                // 当前迷雾块纹理，用离border最近的像素区域覆盖border，区域尺寸参考patternBorderSize
                int patternStartPixelX = (offsetX == -1) ? patternBorderSize : blockPattern.width - patternBorderSize * 2;
                //Color[] colArr = blockPattern.GetPixels(patternStartPixelX, 0, patternBorderSize, blockPattern.height);
                Color[] colArr = GetCachedPixels(blockPattern, patternStartPixelX, 0, patternBorderSize, blockPattern.height); 
                int fogMaskStartPixelX = (offsetX == -1) ? pixelX : pixelX + pixelTessellation - patternBorderSize;
                _fogMask.SetPixels(fogMaskStartPixelX, pixelY, patternBorderSize, blockPattern.height, colArr);

                // 水平相邻迷雾块
                patternStartPixelX = (offsetX == 1) ? patternBorderSize : blockPattern.width - patternBorderSize * 2;
                //colArr = blockPattern.GetPixels(patternStartPixelX, 0, patternBorderSize, blockPattern.height);
                colArr = GetCachedPixels(blockPattern, patternStartPixelX, 0, patternBorderSize, blockPattern.height);
                fogMaskStartPixelX = (offsetX == 1) ? pixelX + pixelTessellation : pixelX - patternBorderSize;
                _fogMask.SetPixels(fogMaskStartPixelX, pixelY, patternBorderSize, blockPattern.height, colArr);

                _needApply = true;
            }

            if (offsetY != 0)
            {
                // 当前迷雾块纹理，同样用离border最近的像素区域(宽度patternBorderSize)覆盖border
                int patternStartPixelY = (offsetY == -1) ? patternBorderSize : blockPattern.height - patternBorderSize * 2;
                //Color[] colArr = blockPattern.GetPixels(0, patternStartPixelY, blockPattern.width, patternBorderSize);
                Color[] colArr = GetCachedPixels(blockPattern, 0, patternStartPixelY, blockPattern.width, patternBorderSize);
                int fogMaskStartPixelY = (offsetY == -1) ? pixelY : pixelY + pixelTessellation - patternBorderSize;
                _fogMask.SetPixels(pixelX, fogMaskStartPixelY, blockPattern.width, patternBorderSize, colArr);

                
                // 竖直相邻迷雾块
                patternStartPixelY = (offsetY == 1) ? patternBorderSize : blockPattern.height - patternBorderSize * 2;
                //colArr = blockPattern.GetPixels(0, patternStartPixelY, blockPattern.width, patternBorderSize);
                colArr = GetCachedPixels(blockPattern, 0, patternStartPixelY, blockPattern.width, patternBorderSize);
                fogMaskStartPixelY = (offsetY == 1) ? pixelY + blockPattern.height : pixelY - patternBorderSize;
                _fogMask.SetPixels(pixelX, fogMaskStartPixelY, blockPattern.width, patternBorderSize, colArr);

                _needApply = true;
            }


        }


        protected void UpdateWithFogData(bool isUpdateFogTexRawData = true)
        {
            UpdateWithFogData(Vector2Int.zero, new Vector2Int(GridNum.x - 1, GridNum.y - 1), isUpdateFogTexRawData);
        }


        // 设置小块迷雾开合
        protected void UpdateWithFogData(Vector2Int min, Vector2Int max, bool isUpdateFogTexRawData = true)
        {
            if (isUpdateFogTexRawData)
            {
                //TryUpdateFogTexData();
            }

            int offsetX = maskExpand[0];
            int offsetY = maskExpand[2];

            min.x = Mathf.Clamp(min.x, 0, GridNum.x - 1);
            min.y = Mathf.Clamp(min.y, 0, GridNum.y - 1);
            max.x = Mathf.Clamp(max.x, 0, GridNum.x - 1);
            max.y = Mathf.Clamp(max.y, 0, GridNum.y - 1);

            //int dataWidth = this._fogData.GetLength(0);
            //dataWidth = Mathf.Clamp(dataWidth, 0, actualGridNum_x - offsetX - maskExpand[1]);
            int dataWidth = (max - min).x + 1;
            if (dataWidth < 0)
            {
                dataWidth = 0;
            }

            //int dataHeight = this._fogData.GetLength(1);
            //dataHeight = Mathf.Clamp(dataHeight, 0, actualGridNum_y - offsetY - maskExpand[3]);
            int dataHeight = (max - min).y + 1;
            if (dataHeight < 0)
            {
                dataHeight = 0;
            }
            //Debug.LogFormat("{0}, {1}, {2}, {3}, {4}, {5}", min, max, offsetX, offsetY, dataWidth, dataHeight);

            for (int j = min.y; j <= max.y; j++)
            {
                int fogY = j + offsetY;

                for (int i = min.x; i <= max.x; i++)
                {
                    int fogX = i + offsetX;

                    byte fogMaskVal = (byte)(test ?
                        (((fogX + fogY) % 2 == 0) ? 100 : 255)
                        : 255);
                    bool isTrue = (this._fogData[i, j] == unopenFlag);

                    // 如果有指定空洞迷雾纹理
                    if (blockPattern != null && !isTrue)
                    {
                        if (blockPattern.format != this._fogMask.format)
                        {
                            // Debug.LogError("迷雾图案纹理格式不符");
                        }
                        else if (blockPattern.width != blockPattern.height && blockPattern.width != pixelTessellation)
                        {
                            // Debug.LogError("迷雾图案纹理尺寸不符");
                        }

                        // Debug.LogFormat("PopulatePixelPattern {0}, {1}, {2}, {3}, {4}", i, j, offsetX, offsetY, isTrue);
                        PopulatePixelPattern(ref _fogData, i, j, offsetX, offsetY, isTrue, false);
                    }
                    else
                    {
                        // 其余情况下用原始手段填充mask像素
                        FillGridPixels(fogX, fogY, isTrue,
                            fogMaskVal, maskPixelChannelCount, (int)EFogDataType.Occupy);
                    }
                }
            }

            // 进一步细化拐角效果
            //RefineAllHoleCorner(_fogData, offsetX, offsetY);
            RefineHoleCorners(_fogData, new Vector2Int(min.x-1, min.y-1), new Vector2Int(max.x+1, max.y+1));

            // todo 是否要重绘区域外边一圈空洞迷雾的图案

            _needApply = true;
        }


        // statusArr 迷雾开合数据矩阵
        // 从(startX, startY)开始细化迷雾拐角处效果
        // startX, startY是含封边的小块迷雾坐标
        private void RefineAllHoleCorner(byte[,] statusArr, int startX = 0, int startY = 0)
        {
            if (blockPattern == null)
                return;

            int width = statusArr.GetLength(0);
            int height = statusArr.GetLength(1);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    RefineSingleHoleCorner(ref statusArr, x, y, startX, startY);
                }
            }
        }

        private void RefineHoleCorners(byte[,] statusArr, Vector2Int min, Vector2Int max)
        {
            if (blockPattern == null)
                return;

            int offsetX = maskExpand[0];
            int offsetY = maskExpand[2];

            min.x = Mathf.Clamp(min.x, 0, GridNum.x - 1);
            min.y = Mathf.Clamp(min.y, 0, GridNum.y - 1);
            max.x = Mathf.Clamp(max.x, 0, GridNum.x - 1);
            max.y = Mathf.Clamp(max.y, 0, GridNum.y - 1);

            int dataWidth = (max - min).x + 1;
            if (dataWidth < 0)
            {
                dataWidth = 0;
            }

            int dataHeight = (max - min).y + 1;
            if (dataHeight < 0)
            {
                dataHeight = 0;
            }

            for (int y = 0; y < dataHeight; y++)
            {
                for (int x = 0; x < dataWidth; x++)
                {
                    RefineSingleHoleCorner(ref statusArr, x, y, min.x + offsetX, min.y + offsetY);
                }
            }
        }


        // 细化(startX + x, startY +y)处迷雾空洞拐角效果
        // startX, startY是含封边的小块迷雾坐标
        private void RefineSingleHoleCorner(ref byte[,] statusArr, int x, int y, int startX = 0, int startY = 0)
        {
            // 不是空洞就不用处理
            int fogX = x + startX;
            int fogY = y + startY;
            int fogDataX = fogX - maskExpand.x;
            int fogDataY = fogY - maskExpand.z;

            if (statusArr[fogDataX, fogDataY] != 0)
                return;

            int width = statusArr.GetLength(0);
            int height = statusArr.GetLength(1);

            int pixelX = fogX * pixelTessellation;
            int pixelY = fogY * pixelTessellation;

            // 左上角
            if (!(fogDataX == 0 || fogDataY == height - 1))  // 跳过贴边的迷雾块
            {
                // 需要处理两种情况
                // 清空左上角
                if (statusArr[fogDataX, fogDataY + 1] == 0 && statusArr[fogDataX - 1, fogDataY + 1] == 0 && statusArr[fogDataX - 1, fogDataY] == 0)
                {
                    _fogMask.SetPixels32(pixelX, pixelY + pixelTessellation - patternBorderSize, patternBorderSize, patternBorderSize, blankCornerColArr);
                    _needApply = true;
                    //Debug.LogFormat("{0},{1}迷雾清空左上角", fogDataX, fogDataY);
                }
                // 左上角圆润化
                else if (statusArr[fogDataX, fogDataY + 1] == 0 && statusArr[fogDataX - 1, fogDataY + 1] != 0 && statusArr[fogDataX - 1, fogDataY] == 0)
                {
                    if (holeCorner != null)
                    {
                        //Color[] cornerColArr = holeCorner.GetPixels(0, holeCorner.height - patternBorderSize, patternBorderSize, patternBorderSize);
                        Color[] cornerColArr = GetCachedPixels(holeCorner, 0, holeCorner.height - patternBorderSize, patternBorderSize, patternBorderSize);
                        _fogMask.SetPixels(pixelX, pixelY + pixelTessellation - patternBorderSize, patternBorderSize, patternBorderSize, cornerColArr);
                        _needApply = true;
                        //Debug.LogFormat("{0},{1}迷雾左上角圆润过渡", fogDataX, fogDataY);
                    }
                }
            }

            // 左下角
            if (!(fogDataX == 0 || fogDataY == 0))  // 跳过贴边的迷雾块
            {
                // 需要处理两种情况
                // 清空左下角
                if (statusArr[fogDataX - 1, fogDataY] == 0 && statusArr[fogDataX - 1, fogDataY - 1] == 0 && statusArr[fogDataX, fogDataY - 1] == 0)
                {
                    _fogMask.SetPixels32(pixelX, pixelY, patternBorderSize, patternBorderSize, blankCornerColArr);
                    _needApply = true;
                    //Debug.LogFormat("{0},{1}迷雾清空左下角", fogDataX, fogDataY);
                }
                // 左下角圆润化
                else if (statusArr[fogDataX - 1, fogDataY] == 0 && statusArr[fogDataX - 1, fogDataY - 1] != 0 && statusArr[fogDataX, fogDataY - 1] == 0)
                {
                    if (holeCorner != null)
                    {
                        //Color[] cornerColArr = holeCorner.GetPixels(0, 0, patternBorderSize, patternBorderSize);
                        Color[] cornerColArr = GetCachedPixels(holeCorner, 0, 0, patternBorderSize, patternBorderSize);
                        _fogMask.SetPixels(pixelX, pixelY, patternBorderSize, patternBorderSize, cornerColArr);
                        _needApply = true;
                        //Debug.LogFormat("{0},{1}迷雾左下角圆润过渡", fogDataX, fogDataY);
                    }
                }
            }

            // 右下角
            if (!(fogDataX == width - 1 || fogDataY == 0))  // 跳过贴边的迷雾块
            {
                // 需要处理两种情况
                // 清空
                if (statusArr[fogDataX, fogDataY - 1] == 0 && statusArr[fogDataX + 1, fogDataY - 1] == 0 && statusArr[fogDataX + 1, fogDataY] == 0)
                {
                    _fogMask.SetPixels32(pixelX + pixelTessellation - patternBorderSize, pixelY, patternBorderSize, patternBorderSize, blankCornerColArr);
                    _needApply = true;
                    //Debug.LogFormat("{0},{1}迷雾清空右下角", fogDataX, fogDataY);
                }
                // 圆润化
                else if (statusArr[fogDataX, fogDataY - 1] == 0 && statusArr[fogDataX + 1, fogDataY - 1] != 0 && statusArr[fogDataX + 1, fogDataY] == 0)
                {
                    if (holeCorner != null)
                    {
                        //Color[] cornerColArr = holeCorner.GetPixels(patternBorderSize, 0, patternBorderSize, patternBorderSize);
                        Color[] cornerColArr = GetCachedPixels(holeCorner, patternBorderSize, 0, patternBorderSize, patternBorderSize);
                        _fogMask.SetPixels(pixelX + pixelTessellation - patternBorderSize, pixelY, patternBorderSize, patternBorderSize, cornerColArr);
                        _needApply = true;
                        //Debug.LogFormat("{0},{1}迷雾右下角圆润过渡", fogDataX, fogDataY);
                    }
                }
            }

            // 右上角
            if (!(fogDataX == width - 1 || fogDataY == height - 1))  // 跳过贴边的迷雾块
            {
                // 需要处理两种情况
                // 清空
                if (statusArr[fogDataX + 1, fogDataY] == 0 && statusArr[fogDataX + 1, fogDataY + 1] == 0 && statusArr[fogDataX, fogDataY + 1] == 0)
                {
                    _fogMask.SetPixels32(pixelX + pixelTessellation - patternBorderSize, pixelY + pixelTessellation - patternBorderSize, patternBorderSize, patternBorderSize, blankCornerColArr);
                    _needApply = true;
                    //Debug.LogFormat("{0},{1}迷雾清空右下角", fogDataX, fogDataY);
                }
                // 圆润化
                else if (statusArr[fogDataX + 1, fogDataY] == 0 && statusArr[fogDataX + 1, fogDataY + 1] != 0 && statusArr[fogDataX, fogDataY + 1] == 0)
                {
                    if (holeCorner != null)
                    {
                        //Color[] cornerColArr = holeCorner.GetPixels(patternBorderSize, patternBorderSize, patternBorderSize, patternBorderSize);
                        Color[] cornerColArr = GetCachedPixels(holeCorner, patternBorderSize, patternBorderSize, patternBorderSize, patternBorderSize);
                        _fogMask.SetPixels(pixelX + pixelTessellation - patternBorderSize, pixelY + pixelTessellation - patternBorderSize, patternBorderSize, patternBorderSize, cornerColArr);
                        _needApply = true;
                        //Debug.LogFormat("{0},{1}迷雾右下角圆润过渡", fogDataX, fogDataY);
                    }
                }
            }
        }

        // 2023-2-28 wusy 设置单个像素(小块迷雾)是否高亮选择
        // x,y，小块迷雾坐标，不含封边
        public void SetSinglePixelSelected(int x, int y, bool isSelected, bool isAdaptAdjacent = false)
        {
            SetPixelSeletecd(x + maskExpand.x, y + maskExpand.z, 1, 1, isSelected, isAdaptAdjacent);
        }

        // 2023-2-24 wusy 
        // 批量设置(startX, startY)(含封边)开始的(width, height)区域迷雾像素高亮选择
        public void SetPixelSeletecd(int startX, int startY, int width, int height, bool isSelected, bool isAdaptAdjacent = false)
        {
            SetFogPixelData(startX, startY, width, height, EFogDataType.Selected, isSelected, isAdaptAdjacent);
        }

        // 通用设置迷雾像素数据函数
        // 以图像左下角为起点，(startX, startY)(含封边)开始的(width, height)区域
        // 单位： 小块迷雾
        private void SetFogPixelData(int startX, int startY, int width, int height, EFogDataType fogDataType, bool isTrue, bool isAdaptAdjacent = false)
        {
            startX = Mathf.Clamp(startX, 0, actualGridNum_x - 1);
            startY = Mathf.Clamp(startY, 0, actualGridNum_y - 1);

            // 循环，设置数据
            var texData = _fogMask.GetRawTextureData<byte>();

            for (int fogY = startY, maxY = startY + height; fogY < maxY; fogY++)
            {
                if (fogY >= actualGridNum_y)
                    continue;

                for (int fogX = startX, maxX = startX + width; fogX < maxX; fogX++)
                {
                    if (fogX >= actualGridNum_x)
                        continue;

                    // 255是表示 有雾/已选择 的默认值
                    // 100是一个用来测试的值，无视它
                    // 0表示 没有雾/未选择
                    byte fogMask = (byte)(test ?
                        (((fogX + fogY) % 2 == 0) ? 100 : 255)
                        : 255);

                    FillGridPixels(fogX, fogY, isTrue, fogMask, maskPixelChannelCount, (int)fogDataType, isAdaptAdjacent);
                }
            }

            _needApply = true;
        }


        // 指定位置小块迷雾渐隐
        // x,y，小块迷雾坐标，不含封边
        public void Fadeout(int x, int y, Action onFadeoutComplete = null)
        {
            // 为了让迷雾淡出，如果此时目标迷雾已经开启，需要把它重置为闭合状态
            // 这一步是不是在新手引导代码中执行了？
            int fogDataX = x + maskExpand[0];
            int fogDataY = y + maskExpand[2];

            if (fogDataX >= actualGridNum_x || fogDataY >= actualGridNum_y)
                return;

            // 为保证空洞描边等效果的正确，需要把有关迷雾设为开启
            _fogData[fogDataX, fogDataY] = unopenFlag;

            /*
            var tween = DOTween.To((val) =>
            {
                PopulatePixelTessellation(fogDataX, fogDataY, true, (byte)(val * 255), maskPixelChannelCount, (int)EFogDataType.Alpha, true);
            }, 1f, 0.3f, 2f);


            tween.onComplete = () =>
            {
                // 为保证空洞描边等效果的正确，需要把有关迷雾设为开启
                _fogData[x, y] = openFlag;
                PopulatePixelTessellation(fogDataX, fogDataY, true, (byte)(255), maskPixelChannelCount, (int)EFogDataType.Alpha, true);
                UpdateWithFogData(false);
                onFadeoutComplete?.Invoke();
            };
            */
        }


        protected int GetFogDataAt(int dataX, int dataY)
        {
            // 越界视为有迷雾
            if (dataX < 0 || dataX >= GridNum.x
                || dataY < 0 || dataY >= GridNum.y)
                return unopenFlag;

            return _fogData[dataX, dataY];
        }


        protected enum eDir
        {
            up,
            upRight,
            right,
            downRight,
            down,
            downLeft, 
            left,
            // continue here
        }

        protected int GetAdjacentFogData(int dataX, int dataY, eDir dir)
        {

            return 0;
        }


        // 2023-8-29 wusy 临时处理，缓存getPixels的结果，日后看换成rawTextureData怎么样
        private Dictionary<Texture, Dictionary<int4, Color[]>> _texPixelsDic = new Dictionary<Texture, Dictionary<int4, Color[]>>();
        private Color[] GetCachedPixels(Texture2D tex, int x, int y, int blockWidth, int blockHeight)
        {
            // 找到该纹理的缓存池
            if (!_texPixelsDic.ContainsKey(tex))
            {
                //Debug.LogFormat("迷雾图案纹理缓存新添纹理{0}", tex.name);
                _texPixelsDic.Add(tex, new Dictionary<int4, Color[]>());
            }
            else
            {
                //Debug.LogFormat("迷雾图案纹理缓存命中纹理{0}", tex.name);
            }
            var texPixels = _texPixelsDic[tex];

            int4 rect = new int4(x, y, blockWidth, blockHeight);
            // 找到缓存的该区域像素数组
            if (!texPixels.ContainsKey(rect))
            {
                //Debug.LogFormat("迷雾图案纹理缓存{0}新添区域:{1}", tex.name, rect);
                texPixels[rect] = tex.GetPixels(x, y, blockWidth, blockHeight);
            }
            else
            {
                //Debug.LogFormat("迷雾图案纹理缓存{0}命中区域:{1}, {2}", tex.name, rect, texPixels[rect].GetHashCode());
            }

            return texPixels[rect];
        }

        // 缓存getPixel32的结果
        private Dictionary<Texture, Dictionary<int4, Color32[]>> _texPixelCol32sDic = new Dictionary<Texture, Dictionary<int4, Color32[]>>();
        private Color32[] GetCachedPixelCol32(Texture2D tex, int x, int y, int blockWidth, int blockHeight)
        {
            // 找到该纹理的缓存池
            if (!_texPixelCol32sDic.ContainsKey(tex))
            {
                //Debug.LogFormat("迷雾图案纹理缓存新添纹理{0}", tex.name);
                _texPixelCol32sDic.Add(tex, new Dictionary<int4, Color32[]>());
            }
            else
            {
                Debug.LogFormat("迷雾图案纹理缓存命中纹理{0}", tex.name);
            }
            var texPixelCol32s = _texPixelCol32sDic[tex];

            int4 rect = new int4(x, y, blockWidth, blockHeight);
            // 找到缓存的该区域像素数组
            if (!texPixelCol32s.ContainsKey(rect))
            {
                Debug.LogFormat("迷雾图案纹理缓存{0}新添区域:{1}", tex.name, rect);
                texPixelCol32s[rect] = new Color32[blockWidth * blockHeight];
                Color[] pixels = tex.GetPixels(x, y, blockWidth, blockHeight);
                // color 转成 color32 
                for (int i = 0; i < pixels.Length; i ++)
                {
                    texPixelCol32s[rect][i] = pixels[i];
                }
            }
            else
            {
                Debug.LogFormat("迷雾图案纹理缓存{0}命中区域:{1}, {2}", tex.name, rect, texPixelCol32s[rect].GetHashCode());
            }

            return texPixelCol32s[rect];
        }


        // 把纹理color32[]某区域中的特定通道数据，复制到byteArr的
        private void CopyPixelDataToByteArr(Color32[] col32Arr, int x, int y, int blockWidth, int blockHeight, int channelMask, 
            byte[] byteArr)
        {


        }



        // 填充一个迷雾格子上的clipMask像素
        // fogX,fogY,小块迷雾坐标，含封边
        // isTrue，true:有迷雾， false:无迷雾
        // isAdaptAdjacent 是否要向相邻无迷雾格子做【出血】效果
        // continue here!!!!!!!!!!!!!!
        protected void FillClipMaskGrid(int fogX, int fogY, bool isTrue, bool isAdaptAdjacent = false)
        {
            // 迷雾格子坐标转换为纹理坐标
            int pixelX = fogX * pixelTessellation;
            int pixelY = fogY * pixelTessellation;
            int pixelMaxX = pixelX + pixelTessellation;
            int pixelMaxY = pixelY + pixelTessellation;


            // 填充该grid的clipMask像素
            _clipMask.SetPixels32(pixelX, pixelY, pixelTessellation, pixelTessellation, isTrue ? _clipCloseGridCol32Arr : _clipOpenGridCol32Arr);

            /*
            if (isAdaptAdjacent)
            {
                int fogDataX = fogX - maskExpand.x;
                int fogDataY = fogY - maskExpand.z;

                // wusy 2023-6-9
                byte[,] statusArr = _fogData;
                int statusArrWidth = statusArr.GetLength(0);
                int statusArrHeight = statusArr.GetLength(1);

                // todo 如果上下左右全是迷雾，就不用缩边了

                // 为了搭配高亮选择，渐隐等效果
                // 相邻侧是空洞则向其出血一个patternBorderSize，是迷雾则收缩一个patternBorderSize
                // 左边
                float expandFactor = 1f;
                float shrinkFactor = 0.7f;

                if (fogDataX == 0)  // 贴边的迷雾，先默认按照有封边处理 todo 看是否考虑没有封边的情况
                {
                    pixelX += (int)(patternBorderSize * shrinkFactor);
                }
                else if (fogDataX - 1 >= 0)
                {
                    if (statusArr[fogDataX - 1, fogDataY] == 0)
                    {
                        pixelX -= (int)(patternBorderSize * expandFactor);
                    }
                    else
                    {
                        pixelX += (int)(patternBorderSize * shrinkFactor);
                    }
                }
                // 右边
                if (fogDataX == statusArrWidth - 1)
                {
                    pixelMaxX -= (int)(patternBorderSize * shrinkFactor);
                }
                if (fogDataX + 1 < statusArrWidth)
                {
                    if (statusArr[fogDataX + 1, fogDataY] == 0)
                    {
                        pixelMaxX += (int)(patternBorderSize * expandFactor);
                    }
                    else
                    {
                        pixelMaxX -= (int)(patternBorderSize * shrinkFactor);
                    }
                }
                // 下边
                if (fogDataY == 0)
                {
                    pixelY += (int)(patternBorderSize * shrinkFactor);
                }
                else if (fogDataY - 1 >= 0)
                {
                    if (statusArr[fogDataX, fogDataY - 1] == 0)
                    {
                        pixelY -= (int)(patternBorderSize * expandFactor);
                    }
                    else
                    {
                        pixelY += (int)(patternBorderSize * shrinkFactor);
                    }
                }
                // 上边
                if (fogDataY == statusArrHeight - 1)
                {
                    pixelMaxY -= (int)(patternBorderSize * shrinkFactor);
                }
                else if (fogDataY + 1 < statusArrHeight)
                {
                    if (statusArr[fogDataX, fogDataY + 1] == 0)
                    {
                        pixelMaxY += (int)(patternBorderSize * expandFactor);
                    }
                    else
                    {
                        pixelMaxY -= (int)(patternBorderSize * shrinkFactor);
                    }
                }
            }
            */


            _needApplyClipMask = true;
        }

#if UNITY_EDITOR


        //*
        private int x_ = 0;
        private int y_ = 0;
        private int width_ = 1;
        private int height_ = 1;
        private Color maskPixel_;
        private GUIStyle textStyle = null;


        private void Start()
        {
            // 初始化迷雾
            PreInit();

            // 不同情境下迷雾片的尺寸和位置规则根据机制或所用的Mesh可能不同，需要各自实现
            // 2023-8-15 wusy 目前这版新手引导使用的Mesh尺寸为10x10,原点在左下角
            // 缩放比例
            gameObject.transform.localScale = new Vector3(
                GridSize.x * actualGridNum_x * 0.1f,
                1,
                GridSize.y * actualGridNum_y * 0.1f);

            // 初始化位置
            // 让_fogData[0,0]的左下角位于世界原点
            Vector3 pos = transform.position;
            pos.x = pos.z = 0;
            pos.x += -maskExpand.x * GridSize.x;
            pos.z += -maskExpand.z * GridSize.y;
            pos.x += posOffset.x;
            pos.z += posOffset.y;
            transform.position = pos;
        }

        private void OnGUI()
        {
            // 字体
            if (textStyle == null)
                textStyle = new GUIStyle();
            textStyle.normal.background = null;
            textStyle.fontSize = 24;
            textStyle.normal.textColor = Color.red;


            GUILayout.BeginArea(new Rect(10, 300, (int)(Screen.width * 0.25), (int)(Screen.height * 0.5)));
            GUILayout.BeginVertical();
            GUILayout.Label(string.Format("x:{0}", x_), textStyle);
            x_ = Convert.ToInt32(GUILayout.HorizontalSlider(x_, 0, GridNum.x - 1));
            GUILayout.Label(string.Format("y:{0}", y_), textStyle);
            y_ = Convert.ToInt32(GUILayout.HorizontalSlider(y_, 0, GridNum.y - 1));
            GUILayout.Label(string.Format("width:{0}", width_), textStyle);
            width_ = Convert.ToInt32(GUILayout.HorizontalSlider(width_, 1, GridNum.x));
            GUILayout.Label(string.Format("height:{0}", height_), textStyle);
            height_ = Convert.ToInt32(GUILayout.HorizontalSlider(height_, 1, GridNum.y));
            GUILayout.Label(string.Format("遮罩颜色:{0}", maskPixel_), textStyle);
            maskPixel_.r = GUILayout.HorizontalSlider(maskPixel_.r, 0, 1);
            maskPixel_.g = GUILayout.HorizontalSlider(maskPixel_.g, 0, 1);
            maskPixel_.b = GUILayout.HorizontalSlider(maskPixel_.b, 0, 1);


            GUILayout.Label("对上述区域迷雾");
            GUILayout.BeginHorizontal();


            if (GUILayout.Button("显示迷雾"))
            {
                // 设置fogdata数据
                for (int dataY = y_; dataY < y_ + height_; dataY++)
                {
                    for (int dataX = x_; dataX < x_ + width_; dataX++)
                    {
                        _fogData[dataX, dataY] = unopenFlag;
                    }
                }

                // step1 更新目标迷雾格子的mask
                for (int dataY = y_; dataY < y_ + height_; dataY++)
                {
                    int fogY = dataY + maskExpand.z;

                    for (int dataX = x_; dataX < x_ + width_; dataX++)
                    {
                        int fogX = dataX + maskExpand.x;

                        bool isTrue = (this._fogData[dataX, dataY] == unopenFlag);

                        FillClipMaskGrid(fogX, fogY, isTrue);
                    }
                }


                // step2 处理边缘、拐角
                // 检查边缘图合法性
                if (blockPattern == null)
                {
                    Debug.LogError("未指定迷雾边缘图案遮罩，无法处理迷雾边缘");
                    return;
                }
                else
                {
                    /*
                    if (blockPattern.format != this._fogMask.format)
                    {
                        Debug.LogError("迷雾图案纹理格式不符，无法处理迷雾边缘");
                        return;
                    }
                    */
                    if (blockPattern.width != blockPattern.height && blockPattern.width != pixelTessellation * 3)
                    {
                        Debug.LogError("迷雾图案纹理尺寸不对，无法处理迷雾边缘");
                        return;
                    }
                }

                // todo drawnGrids 变成成员
                HashSet<int> drawnGrids = new HashSet<int>();
                // 对本次变更的每个目标
                for (int dataY = y_; dataY < y_ + height_; dataY++)
                {
                    for (int dataX = x_; dataX < x_ + width_; dataX++)
                    {
                        Debug.LogFormat("-------目标{0},{1}", dataX, dataY);

                        // 遍历以目标为中心3x3范围内的迷雾格子
                        for (int dataY2 = dataY-1; dataY2 <= dataY+1; dataY2 ++)
                        {
                            // 越界默认有迷雾，无需处理
                            if (dataY2 < 0 || dataY2 >= GridNum.y)
                                continue;

                            for (int dataX2 = dataX-1; dataX2 <= dataX+1; dataX2 ++)
                            {
                                Debug.LogFormat("---周围格子{0},{1}", dataX2, dataY2);

                                // 越界默认有迷雾，无需处理
                                if (dataX2 < 0 || dataX2 >= GridNum.x)
                                    continue;

                                // 防重复
                                int gridId = dataY2 * GridNum.x + dataX2;
                                if (drawnGrids.Contains(gridId))
                                {
                                    Debug.LogFormat("{0},{1}(id:{2})已做边缘处理，跳过", dataX2, dataY2, gridId);
                                    continue;
                                }

                                // 跳过有迷雾的，目前的出血规则下，只有非迷雾格子才有边缘需要处理
                                if (_fogData[dataX2, dataY2] == unopenFlag)
                                {
                                    Debug.LogFormat("{0},{1}(id:{2})有迷雾，跳过", dataX2, dataY2, gridId);
                                    continue;
                                }

                                // ---绘制边缘
                                // 检查四边是否迷雾，有则画一道边缘

                                int dataX3 = 0;
                                int dataY3 = 0;
                                // 左
                                dataX3 = dataX2 - 1;
                                dataY3 = dataY2;
                                if (GetFogDataAt(dataX3, dataY3) == unopenFlag)
                                {
                                    int startX = pixelTessellation * 2;
                                    int startY = pixelTessellation;

                                    // setPixels时加上封边的尺寸
                                    Color32[] col32Arr = GetCachedPixelCol32(blockPattern, startX, startY, patternBorderSize, pixelTessellation);
                                    _clipMask.SetPixels32((dataX2 + maskExpand.x) * pixelTessellation, (dataY2 + maskExpand.z) * pixelTessellation, patternBorderSize, pixelTessellation, col32Arr);

                                    //Color[] colArr = blockPattern.GetPixels(startX, startY, patternBorderSize, pixelTessellation);
                                    //_fogMask.SetPixels((dataX2 + maskExpand.x) * pixelTessellation, (dataY2 + maskExpand.z) * pixelTessellation, patternBorderSize, pixelTessellation, colArr);

                                    Debug.LogFormat("左 {0},{1},{2},{3}", dataX3, dataY3, startX, startY);
                                }

                                // 下
                                dataX3 = dataX2;
                                dataY3 = dataY2 - 1;
                                if (GetFogDataAt(dataX3, dataY3) == unopenFlag)
                                {
                                    int startX = pixelTessellation;
                                    int startY = pixelTessellation * 2;

                                    Color32[] col32Arr = GetCachedPixelCol32(blockPattern, startX, startY, pixelTessellation, patternBorderSize);
                                    _clipMask.SetPixels32((dataX2 + maskExpand.x) * pixelTessellation, (dataY2 + maskExpand.z) * pixelTessellation, pixelTessellation, patternBorderSize, col32Arr);

                                    //Color[] colArr = blockPattern.GetPixels(startX, startY, pixelTessellation, patternBorderSize);
                                    //_fogMask.SetPixels((dataX2 + maskExpand.x) * pixelTessellation, (dataY2 + maskExpand.z) * pixelTessellation, pixelTessellation, patternBorderSize, colArr);
                                    Debug.LogFormat("下 {0},{1},{2},{3}", dataX3, dataY3, startX, startY);
                                }

                                // 右
                                dataX3 = dataX2 + 1;
                                dataY3 = dataY2;
                                if (GetFogDataAt(dataX3, dataY3) == unopenFlag)
                                {
                                    int startX = pixelTessellation - patternBorderSize;
                                    int startY = pixelTessellation;

                                    Color32[] col32Arr = GetCachedPixelCol32(blockPattern, startX, startY, patternBorderSize, pixelTessellation);
                                    _clipMask.SetPixels32((dataX2 + maskExpand.x + 1) * pixelTessellation - patternBorderSize, (dataY2 + maskExpand.z) * pixelTessellation, patternBorderSize, pixelTessellation, col32Arr);

                                    //Color[] colArr = blockPattern.GetPixels(startX, startY, patternBorderSize, pixelTessellation);
                                    //_fogMask.SetPixels((dataX2 + maskExpand.x + 1) * pixelTessellation - patternBorderSize, (dataY2 + maskExpand.z) * pixelTessellation, patternBorderSize, pixelTessellation, colArr);
                                    Debug.LogFormat("右 {0},{1},{2},{3}", dataX3, dataY3, startX, startY);
                                }

                                // 上
                                dataX3 = dataX2;
                                dataY3 = dataY2 + 1;
                                if (GetFogDataAt(dataX3, dataY3) == unopenFlag)
                                {
                                    int startX = pixelTessellation;
                                    int startY = pixelTessellation - patternBorderSize;

                                    Color32[] col32Arr = GetCachedPixelCol32(blockPattern, startX, startY, pixelTessellation, patternBorderSize);
                                    _clipMask.SetPixels32((dataX2 + maskExpand.x) * pixelTessellation, (dataY2 + maskExpand.z + 1) * pixelTessellation - patternBorderSize, pixelTessellation, patternBorderSize, col32Arr);

                                    //Color[] colArr = blockPattern.GetPixels(startX, startY, pixelTessellation, patternBorderSize);
                                    //_fogMask.SetPixels((dataX2 + maskExpand.x) * pixelTessellation, (dataY2 + maskExpand.z + 1) * pixelTessellation - patternBorderSize, pixelTessellation, patternBorderSize, colArr);
                                    Debug.LogFormat("上 {0},{1},{2},{3}", dataX3, dataY3, startX, startY);

                                }


                                // ---处理拐角
                                // 内拐角、外拐角

                                // 左下角
                                // 顺时针方向统计三块相邻格子的开合状态 （依次为下、左下、左）


                                





                                // 添加到已处理列表
                                drawnGrids.Add(gridId);
                            }
                        }
                    }
                }


                _needApplyClipMask = true;
            }

            if (GUILayout.Button("清除迷雾"))
            {
                // 设置fogdata数据
                for (int y = y_; y < y_ + height_; y++)
                {
                    for (int x = x_; x < x_ + width_; x++)
                    {
                        _fogData[x, y] = openFlag;
                    }
                }

                // 更新目标迷雾格子的mask
                for (int dataY = y_; dataY < y_ + height_; dataY++)
                {
                    int fogY = dataY + maskExpand.z;

                    for (int dataX = x_; dataX < x_ + width_; dataX++)
                    {
                        int fogX = dataX + maskExpand.x;

                        byte fogMaskVal = (byte)(test ?
                            (((fogX + fogY) % 2 == 0) ? 100 : 255)
                            : 255);
                        bool isTrue = (this._fogData[dataX, dataY] == unopenFlag);

                        FillClipMaskGrid(fogX, fogY, isTrue);
                    }
                }
            }



            if (GUILayout.Button("填充图案"))
            {
                Color32[] colArr = blockPattern.GetPixels32();
                _fogMask.SetPixels32(x_, y_, blockPattern.width, blockPattern.height, colArr);
                _fogMask.Apply();
            }

            GUILayout.EndHorizontal();


            if (GUILayout.Button("切换颜色2"))
            {
                int2[] arr = new int2[width_ * height_];
                int id = 0;
                for (int y = 0; y < height_; y ++)
                {
                    for (int x = 0; x < width_; x ++)
                    {
                        arr[id].x = x_ + x;
                        arr[id].y = y_ + y;
                        id++;
                    }
                }

                //Show2ndColor(arr, true);
            }

            if (GUILayout.Button("清除颜色2"))
            {
                int2[] arr = new int2[width_ * height_];
                int id = 0;
                for (int y = 0; y < height_; y++)
                {
                    for (int x = 0; x < width_; x++)
                    {
                        arr[id].x = x_ + x;
                        arr[id].y = y_ + y;
                        id++;
                    }
                }

                //Show2ndColor(arr, false);
            }

            if (GUILayout.Button("放置迷雾淡出特效"))
            {
                Fadeout(x_, y_);
            }


            if (GUILayout.Button("迷雾高亮"))
            {
                SetSinglePixelSelected(x_, y_, true, true);
            }

            if (GUILayout.Button("测试1"))
            {
                //*//
                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

                Texture2D tex1 = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                var texRawData = tex1.GetRawTextureData<byte>();
                var texRawData_uint = tex1.GetRawTextureData<uint>();
                Color32[] color32Arr = tex1.GetPixels32();
                Color[] colorArr = tex1.GetPixels();

                Texture2D tex2 = new Texture2D(1024, 1024, TextureFormat.RGBA32, false);

                int count = 10000;
                byte b = 0;

                int len = 64 * 64 * 4;
                byte[] byteArr = new byte[len];

                int len_uint = 64 * 64;
                uint[] uintArr = new uint[len_uint];


                sw.Restart();

                for (int i = 0; i < count; i++)
                {
                    // read
                    //var pixelds = blockPattern.GetPixels32();
                    //var pixelds = blockPattern.GetPixels(0, 0, 64, 64);
                    //NativeArray<byte>.Copy(texRawData, 0, byteArr, 0, len);
                    //for (int j = 0; j < len; j ++)
                    //{
                    //    b = byteArr[j];
                    //}

                    // write
                    //newTex.SetPixels32(color32Arr);
                    //newTex.SetPixels(colorArr);

                    for (int j = 0; j < len; j++)
                    {
                        texRawData[j] = 255;
                    }

                    //for (int j = 0; j < len; j++)
                    //{
                    //    byteArr[j] = 255;
                    //}
                    //NativeArray<byte>.Copy(byteArr, 0, texRawData, 0, len);


                    //for (int j = 0; j < len_uint; j++)
                    //{
                    //    uintArr[j] = 0xffffffff;
                    //}
                    //NativeArray<uint>.Copy(uintArr, 0, texRawData_uint, 0, len_uint);

                    //tex1.SetPixelData<byte>(byteArr, 0);

                }
                sw.Stop();

                UnityEngine.Debug.LogFormat("{0}: {1}ms", count, sw.ElapsedMilliseconds);
                //UnityEngine.Debug.LogFormat("{0}", b);
                //*///

            }




            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        //*/

#endif







    }


}
