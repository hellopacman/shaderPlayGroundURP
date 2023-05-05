using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[ExecuteInEditMode]
public class MatrixTest01 : MonoBehaviour
{

    // 准备8个点，组成一个立方体
    // 底
    private Vector4 v0_;
    private Vector4 v1_;
    private Vector4 v2_;
    private Vector4 v3_;
    // 顶
    private Vector4 v4_;
    private Vector4 v5_;
    private Vector4 v6_;
    private Vector4 v7_;

    public Matrix4x4 matrix = Matrix4x4.identity;
    public Matrix4x4 childMatrix = Matrix4x4.identity;
    public Matrix4x4 mulMatrix = Matrix4x4.identity;


    public Vector4 vec4;
    public Vector4 vec4Mul;

    private void OnEnable()
    {
        // 底
        v0_ = new Vector4(0, 0, 0, 1);
        v1_ = new Vector4(1, 0, 0, 1);
        v2_ = new Vector4(1, 0, 1, 1);
        v3_ = new Vector4(0, 0, 1, 1);
        
        // 顶
        v4_ = new Vector4(0, 1, 0, 1);
        v5_ = new Vector4(1, 1, 0, 1);
        v6_ = new Vector4(1, 1, 1, 1);
        v7_ = new Vector4(0, 1, 1, 1);
    }


    private void OnDrawGizmos()
    {
        /*
        // 用矩阵改变这8个点的坐标
        Vector4 v0 = mulMatrix * v0_;
        Vector4 v1 = mulMatrix * v1_;
        Vector4 v2 = mulMatrix * v2_;
        Vector4 v3 = mulMatrix * v3_;
        Vector4 v4 = mulMatrix * v4_;
        Vector4 v5 = mulMatrix * v5_;
        Vector4 v6 = mulMatrix * v6_;
        Vector4 v7 = mulMatrix * v7_;

        Gizmos.color = Color.red;

        //底面
        Gizmos.DrawLine(v0, v1);
        Gizmos.DrawLine(v1, v2);
        Gizmos.DrawLine(v2, v3);
        Gizmos.DrawLine(v3, v0);
        //顶面
        Gizmos.DrawLine(v4, v5);
        Gizmos.DrawLine(v5, v6);
        Gizmos.DrawLine(v6, v7);
        Gizmos.DrawLine(v7, v4);
        //竖线
        Gizmos.DrawLine(v0, v4);
        Gizmos.DrawLine(v1, v5);
        Gizmos.DrawLine(v2, v6);
        Gizmos.DrawLine(v3, v7);
        ///*/

        mulMatrix = matrix * childMatrix;

        // 子坐标系
        Gizmos.color = Color.black;
        Gizmos.matrix = matrix;
        //Gizmos.DrawLine(Vector4.zero, new Vector4(1, 0, 0, 1));    // x轴
        //Gizmos.DrawLine(Vector4.zero, new Vector4(0, 1, 0, 1));    // y轴
        //Gizmos.DrawLine(Vector4.zero, new Vector4(0, 0, 1, 1));    // z轴
        Gizmos.DrawWireCube(Vector3.one * 0.5f, Vector3.one);
        Gizmos.DrawSphere(Vector3.zero, 0.05f);



        // 孙坐标
        Gizmos.color = Color.white;
        Gizmos.matrix = mulMatrix;
        //Gizmos.DrawLine(Vector4.zero, new Vector4(1, 0, 0, 1));    // x轴
        //Gizmos.DrawLine(Vector4.zero, new Vector4(0, 1, 0, 1));    // y轴
        //Gizmos.DrawLine(Vector4.zero, new Vector4(0, 0, 1, 1));    // z轴
        Gizmos.DrawWireCube(Vector3.one * 0.5f, Vector3.one);
        Gizmos.DrawSphere(Vector3.zero, 0.05f);

        // 矩阵相乘，变换矢量
        vec4Mul = mulMatrix * vec4;

        Gizmos.color = Color.red;
        Gizmos.matrix = mulMatrix;
        Gizmos.DrawSphere(vec4, 0.1f);

    }

}
