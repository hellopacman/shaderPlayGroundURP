using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SLGGame
{
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    public class Subfog : MonoBehaviour
    {
        private bool _isDirty = false;

        public bool useProcedureMesh = false;   // 是否使用程序建模
        public float meshAssetSize = 10f;       // mesh asset的尺寸，用来把submesh缩放到目标尺寸(总是假设所用Mesh是正方形)。使用程序建模时无效
        public Vector2 meshAssetPivot = new Vector2(0.5f, 0.5f);    // mesh asset的pivot，用来协助放计算位置。使用程序建模时无效
        private Mesh _mesh;
        private MeshRenderer _mr;
        private MeshFilter _mf;

        // 该subfog所映射的数据区域
        [SerializeField]
        private RectInt _dataRegion = new RectInt(0, 0, 10, 10);
        public RectInt dataRegion
        {
            set
            {
                if (!_dataRegion.Equals(value))
                {
                    _dataRegion = value;
                    _isDirty = true;
                }
            }
            get
            {
                return _dataRegion;
            }
        }

        public float blockSize = 10;    // 该subfog每个【小迷雾块】的物理尺寸(米)
        //[SerializeField] private Rect _region;   // 该subfog覆盖区域(米)        

        public int sortOrder = 0;


        private void LateUpdate()
        {
            if (_isDirty)
            {
                InternalUpdate();
            }
        }

        private void InternalUpdate()
        {
            Debug.Log("InternalUpdate");

            // 程控mesh
            if (useProcedureMesh)
            {
                if (_mesh == null)
                {
                    _mesh = new Mesh();
                    _mf.mesh = _mesh;
                }
                // todo 检查是否要重建mesh
                // RebuildMesh();
            }
            else  // mesh asset
            {
                // 这里有问题....
                transform.localScale = new Vector3((float)_dataRegion.width * blockSize / meshAssetSize,
                        (float)_dataRegion.height * blockSize / meshAssetSize, 1f);
            }

            _isDirty = false;
        }

        
        public void SetTransformPos(Vector3 pos)
        {
            // todo 根据【覆盖相机视野】需求更新代码
            transform.position = pos;
        }

        private void RebuildMesh()
        {
            // todo 升级为jobs mesh api


        }


        public void Init()
        { 
            if (_mf == null)
            {
                _mf = GetComponent<MeshFilter>();
            }

            InternalUpdate();
        }


        private void OnValidate()
        {
            Debug.Log("OnValidate");
        }

    }

}
