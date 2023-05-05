using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MatrixTest01))]
public class MatrixTest01Editor : Editor
{
    private MatrixSerializeWrapper matrixWrapper = new MatrixSerializeWrapper();
    private MatrixSerializeWrapper childMatrixWrapper = new MatrixSerializeWrapper();
    private MatrixSerializeWrapper mulMatrixWrapper = new MatrixSerializeWrapper();

    // 用来变换的矢量
    private SerializedProperty _v4;
    private SerializedProperty _v4Mul;

    void OnEnable()
    {
        matrixWrapper.matrixProp = serializedObject.FindProperty("matrix");
        childMatrixWrapper.matrixProp = serializedObject.FindProperty("childMatrix");
        mulMatrixWrapper.matrixProp = serializedObject.FindProperty("mulMatrix");

        // 测试矢量
        _v4 = serializedObject.FindProperty("vec4");
        _v4Mul = serializedObject.FindProperty("vec4Mul");
    }

    public override void OnInspectorGUI()
    {
        //base.OnInspectorGUI();

        serializedObject.Update();

        EditorGUILayout.LabelField("世界坐标系下子坐标系矩阵");
        DrawMatrix(matrixWrapper);

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("子坐标系下孙坐标系矩阵");
        DrawMatrix(childMatrixWrapper);

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("子矩阵*孙矩阵的结果(不要修改此矩阵)");
        DrawMatrix(mulMatrixWrapper);

        EditorGUILayout.LabelField("孙矩阵下的矢量");
        _v4.vector4Value = EditorGUILayout.Vector4Field("", _v4.vector4Value);
        EditorGUILayout.LabelField("子矩阵*孙矩阵*矢量，相当于把矢量变换到世界坐标系下");
        EditorGUILayout.LabelField(string.Format("结果: {0}", _v4Mul.vector4Value.ToString("0.0000")));
        EditorGUILayout.LabelField("在Scene窗口中显示为红色小球");

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawMatrix(MatrixSerializeWrapper matrixWrapper)
    {
        EditorGUILayout.BeginHorizontal();
        // 第一列
        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField("x基轴", GUILayout.MaxWidth(120));
        matrixWrapper._m00.floatValue = EditorGUILayout.FloatField(matrixWrapper._m00.floatValue);
        matrixWrapper._m10.floatValue = EditorGUILayout.FloatField(matrixWrapper._m10.floatValue);
        matrixWrapper._m20.floatValue = EditorGUILayout.FloatField(matrixWrapper._m20.floatValue);
        //matrixWrapper._m30.floatValue = EditorGUILayout.FloatField(matrixWrapper._m30.floatValue);
        EditorGUILayout.LabelField(matrixWrapper._m30.floatValue.ToString(), GUILayout.MaxWidth(25));
        EditorGUILayout.EndVertical();

        // 第二列
        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField("y基轴", GUILayout.MaxWidth(120));
        matrixWrapper._m01.floatValue = EditorGUILayout.FloatField(matrixWrapper._m01.floatValue);
        matrixWrapper._m11.floatValue = EditorGUILayout.FloatField(matrixWrapper._m11.floatValue);
        matrixWrapper._m21.floatValue = EditorGUILayout.FloatField(matrixWrapper._m21.floatValue);
        //matrixWrapper._m31.floatValue = EditorGUILayout.FloatField(matrixWrapper._m31.floatValue);
        EditorGUILayout.LabelField(matrixWrapper._m31.floatValue.ToString(), GUILayout.MaxWidth(25));
        EditorGUILayout.EndVertical();

        // 第三列
        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField("z基轴", GUILayout.MaxWidth(120));
        matrixWrapper._m02.floatValue = EditorGUILayout.FloatField(matrixWrapper._m02.floatValue);
        matrixWrapper._m12.floatValue = EditorGUILayout.FloatField(matrixWrapper._m12.floatValue);
        matrixWrapper._m22.floatValue = EditorGUILayout.FloatField(matrixWrapper._m22.floatValue);
        //matrixWrapper._m32.floatValue = EditorGUILayout.FloatField(matrixWrapper._m32.floatValue);
        EditorGUILayout.LabelField(matrixWrapper._m32.floatValue.ToString(), GUILayout.MaxWidth(25));
        EditorGUILayout.EndVertical();

        // 第四列
        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField("新坐标系原点偏移量", GUILayout.MaxWidth(120));
        matrixWrapper._m03.floatValue = EditorGUILayout.FloatField(matrixWrapper._m03.floatValue);
        matrixWrapper._m13.floatValue = EditorGUILayout.FloatField(matrixWrapper._m13.floatValue);
        matrixWrapper._m23.floatValue = EditorGUILayout.FloatField(matrixWrapper._m23.floatValue);
        //matrixWrapper._m33.floatValue = EditorGUILayout.FloatField(matrixWrapper._m33.floatValue);
        EditorGUILayout.LabelField(matrixWrapper._m33.floatValue.ToString(), GUILayout.MaxWidth(25));
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

}
