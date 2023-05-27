using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Unity.VisualScripting;
using UnityEngine;


namespace SLGGame
{
    [RequireComponent(typeof(LineRenderer))]
    public class LightingLine : MonoBehaviour
    {
        public int segments = 20;
        private int _lastSegments = -1;

        [Space]
        public bool useArcing = false;
        [Range(-1, 1)] public float arcingPowParam1 = 0.5f;
        public float arcingSpeed = 1f;
        public float centerOffset = 0;
        public float adjust = 0;
        private float _arcingCountDown = 1f;    // 取1，一上来就计时完成导致更新

        [Space]
        public bool useSine = true;
        public float sineScaleX = 1;
        public float sineScaleY = 1;
        public float sineSpeed = 1;
        private float _sineCountDown = 1;
        private float _sineRandom = 0;
        private List<Vector3> _sineOffsets = new List<Vector3>();


        [Space]
        public bool useWiggle = true;
        public float wiggleSpeed = 10f;
        private float _wiggleCountDown = 1;
        public float randomSize = 0.5f;
        private List<Vector3> _wiggleRandom = new List<Vector3>();


        [Space]
        public Transform startPosTrsf;
        public Transform endPosTrsf;

        private LineRenderer _lineRder;
        private List<Vector3> _posList = new List<Vector3>();
        //private List<Vector3> posList2 = new List<Vector3>();        
        private Vector3[] _posArr;

        private Vector3 _lastStartPos = Vector3.positiveInfinity; 
        private Vector3 _lastEndPos = Vector3.positiveInfinity;
        private Vector3 _startPos;
        private Vector3 _endPos;


        private void Start()
        {
            _lineRder = GetComponent<LineRenderer>();
            _lineRder.useWorldSpace = true;

            // 初始化挂点位置
            if (startPosTrsf == null)
            {
                _startPos = _lineRder.GetPosition(0);
            }
            else
            {
                _startPos = startPosTrsf.position;
            }

            if (endPosTrsf == null)
            {
                _endPos = _lineRder.GetPosition(1);
            }
            else
            {
                _endPos = endPosTrsf.position;
            }

            LerpPos();
            _lastSegments = segments;
            _lastStartPos = _startPos;
            _lastEndPos = _endPos;
        }

        // 返回是否有变化
        private bool UpdateLinePointPos()
        {
            if (startPosTrsf == null || endPosTrsf == null)
            {
                return false;
            }

            Vector3 startPos = startPosTrsf.position;
            Vector3 endPos = endPosTrsf.position;
            
            // 挂点对象没移动，分段数没变化就不更新
            if ((startPos - _lastStartPos).sqrMagnitude < 0.001 && (endPos - _lastEndPos).sqrMagnitude < 0.001 && _lastSegments == segments)
            {
                return false;
            }

            Debug.LogFormat("更新挂点位置");
            _startPos = startPosTrsf.position;
            _endPos = endPosTrsf.position;
            LerpPos();
            _lastStartPos = _startPos;
            _lastEndPos = _endPos;
            _lastSegments = segments;
            return true;
        }


        private void Update()
        {
            // 每一帧我们总是从一条记录在_posList的直线开始
            bool ifChanged = UpdateLinePointPos();

            // 电弧移动效果
            if (useArcing)
            {
                // 一种保持t在[0,1]区间的计时写法
                _arcingCountDown += Time.deltaTime * arcingSpeed;
                if (_arcingCountDown > 1)
                {
                    _arcingCountDown = 0;
                }

                for (int i = 0; i <= segments; i++)
                {
                    _posArr[i] = Vector3.Lerp(_posList[i], _posList[i] + new Vector3(0, Arcing(i), 0), _arcingCountDown);
                }
                ifChanged = true;
            }
            else
            {
                for (int i = 0; i <= segments; i++)
                {
                    _posArr[i] = _posList[i];
                }
            }

            // 正弦波
            if(useSine)
            {
                _sineCountDown += Time.deltaTime * sineSpeed;

                if (_sineCountDown >= 1)
                {
                    // 每个周期更换一次正弦函数相位随机数
                    // 由分段数决定随机数范围
                    // Random.Range<int>(0, n*10)，然后再 * 0.1，可以得到区间[0, n]，精度为0.1的随机数，但有什么意义吗？
                    _sineRandom = (float)Random.Range(0, _posList.Count * 10) * 0.1f;

                    for (int i = 0; i <= segments; i++)
                    {
                        _sineOffsets[i] = new Vector3(0, Sine(i + _sineRandom), 0);
                    }

                    _sineCountDown = 0;
                }

                // 每帧都要应用sine效果，因为每帧我们都从直线开始
                for (int i = 0; i <= segments; i++)
                {
                    // 略过首尾端点
                    if (i != 0 && i != segments)
                    {
                        _posArr[i] += _sineOffsets[i];
                    }
                }

                ifChanged = true;
            }

            // 分段点位置扰动计算
            if (useWiggle)  
            {
                // 每隔一段时间更新扰动量
                _wiggleCountDown += Time.deltaTime * wiggleSpeed;
                if (_wiggleCountDown > 1)
                {
                    for (int i = 0; i <= segments; i++)
                    {
                        // Random.Range<int>(0, 10) * 0.1f，保留精度到0.1
                        _wiggleRandom[i] = new Vector3(0, Random.Range(0, 10) * 0.1f * randomSize, 0);
                    }
                    _wiggleCountDown = 0;
                }

                // 每帧都要应用wiggle效果，因为每帧我们都从直线开始
                for (int i = 0; i <= segments; i++)
                {
                    _posArr[i] += _wiggleRandom[i];
                }

                ifChanged = true;
            }

            // if (ifChanged)   // 做这个判断的意义不大，当ifChanged为false时总是停留在上一帧不变，而我希望此时恢复为直线
            SetLinePosition(_posArr);
            
        }

        private void SetLinePosition(Vector3[] pointPosArr)
        {
            Debug.Log("设置Line上各点位置");
            _lineRder.positionCount = segments + 1;
            _lineRder.SetPositions(pointPosArr);
            //_lineRder.SetPositions(NoAllocHelpers.ExtractArrayFromListT(listPoint));
        }

        private void LerpPos()
        {
            if (segments > _lastSegments)
            {
                // 先只扩不缩，应该够用
                for (int i = _posList.Count; i <= segments; i ++)
                {
                    _posList.Add(Vector3.zero);
                    _wiggleRandom.Add(Vector3.zero);
                    _sineOffsets.Add(Vector3.zero);
                }

                _posArr = _posList.ToArray();
            }

            for (int i = 0; i <= segments; i++)
            {
                _posList[i] = Vector3.Lerp(_startPos, _endPos, (float)i / segments);
            }
        }


        private float Arcing(float param)
        {
            return arcingPowParam1 * Mathf.Pow((param - (float)_posList.Count / 2 + centerOffset) * arcingPowParam1, 2) + adjust;
        }


        private float Sine(float pointID)
        {
            // posList的长度凑齐一个2PI的周期，然后乘以_SineScaleX调整频率
            return Mathf.Sin((float)pointID / _posList.Count * 2 * Mathf.PI * sineScaleX) * sineScaleY;
        }

    }

}

