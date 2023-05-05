using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class MatrixSerializeWrapper
{
    private SerializedProperty _matrixProp;

    public SerializedProperty matrixProp
    {
        set
        {
            _matrixProp = value;
            ParseMatrixSubProperty(_matrixProp);
        }
    }

    // 矩阵第一列
    public SerializedProperty _m00;
    public SerializedProperty _m10;
    public SerializedProperty _m20;
    public SerializedProperty _m30;

    // 矩阵第二列
    public SerializedProperty _m01;
    public SerializedProperty _m11;
    public SerializedProperty _m21;
    public SerializedProperty _m31;

    // 矩阵第三列
    public SerializedProperty _m02;
    public SerializedProperty _m12;
    public SerializedProperty _m22;
    public SerializedProperty _m32;

    // 矩阵第四列
    public SerializedProperty _m03;
    public SerializedProperty _m13;
    public SerializedProperty _m23;
    public SerializedProperty _m33;


    public void ParseMatrixSubProperty(SerializedProperty matrixProp)
    {
        // 第一列
        _m00 = matrixProp.FindPropertyRelative("e00");
        _m10 = matrixProp.FindPropertyRelative("e10");
        _m20 = matrixProp.FindPropertyRelative("e20");
        _m30 = matrixProp.FindPropertyRelative("e30");

        // 第二列
        _m01 = matrixProp.FindPropertyRelative("e01");
        _m11 = matrixProp.FindPropertyRelative("e11");
        _m21 = matrixProp.FindPropertyRelative("e21");
        _m31 = matrixProp.FindPropertyRelative("e31");

        // 第三列
        _m02 = matrixProp.FindPropertyRelative("e02");
        _m12 = matrixProp.FindPropertyRelative("e12");
        _m22 = matrixProp.FindPropertyRelative("e22");
        _m32 = matrixProp.FindPropertyRelative("e32");

        // 第四列
        _m03 = matrixProp.FindPropertyRelative("e03");
        _m13 = matrixProp.FindPropertyRelative("e13");
        _m23 = matrixProp.FindPropertyRelative("e23");
        _m33 = matrixProp.FindPropertyRelative("e33");
    }

}
