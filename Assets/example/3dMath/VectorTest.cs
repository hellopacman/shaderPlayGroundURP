using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class VectorTest : MonoBehaviour
{
    public bool showEndLine = false;

    public Mesh arrawMesh;
    public Vector3 arrawScale = Vector3.one;
    public float dotScale = 0.05f;

    public Vector3 v1;
    public Vector3 v2;
    public Vector3 v3;
    
    public float x;
    public float y;
    public float z;

    public Vector3 v;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        v = v1 * x + v2 * y + v3 * z;

    }

    private void OnDrawGizmos()
    {
        Color col = Gizmos.color;
        Matrix4x4 matr = Gizmos.matrix;

        Gizmos.matrix = transform.localToWorldMatrix;

        Vector3 start = Vector3.zero;
        Vector3 end = Vector3.zero;


        // 最终终点
        if (showEndLine)
        {
            Gizmos.color = Color.black;
            Gizmos.DrawLine(start, v);
        }

        // --- v1
        Vector3 v1_ = v1 * x;
        end += v1_;
        DrawVectorGizmo(start, v1, end, x, Color.red);  // 绘制位移gizmo
        DrawBaseVectorGizmo(v1, Color.red); // 绘制基轴gizmo

        // --- v2
        Vector3 v2_ = v2 * y;
        start = end;
        end += v2_;
        DrawVectorGizmo(start, v2, end, y, Color.green);  // 绘制位移gizmo
        DrawBaseVectorGizmo(v2, Color.green); // 绘制基轴gizmo

        // --- v2
        Vector3 v3_ = v3 * z;
        start = end;
        end += v3_;
        DrawVectorGizmo(start, v3, end, z, Color.blue);  // 绘制位移gizmo
        DrawBaseVectorGizmo(v3, Color.blue); // 绘制基轴gizmo


        Gizmos.color = col;
        Gizmos.matrix = matr;

    }

    private void DrawVectorGizmo(Vector3 start, Vector3 baseVec, Vector3 endVec, float movement, Color col)
    {
        // 实际位移
        Color col2 = col * 0.5f;
        col2.a = 1;
        Gizmos.color = col2;
        Gizmos.DrawLine(start, endVec);
        Gizmos.DrawSphere(endVec, dotScale);
        for (int i = 1; i <= movement; i++)
        {
            Gizmos.DrawMesh(arrawMesh, start + baseVec * i, Quaternion.FromToRotation(Vector3.up, baseVec), arrawScale);
        }
    }

    private void DrawBaseVectorGizmo(Vector3 baseVec, Color col)
    {
        // 基轴
        Gizmos.color = col;
        Gizmos.DrawLine(Vector3.zero, baseVec);
        Gizmos.DrawMesh(arrawMesh, baseVec, Quaternion.FromToRotation(Vector3.up, baseVec), arrawScale);

    }


}
