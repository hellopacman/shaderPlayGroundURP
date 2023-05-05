using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


[ExecuteInEditMode]
public class TestDebug : MonoBehaviour
{
    public Transform[] StaticObstacles;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < StaticObstacles.Length; i++)
        {
            // Vector3 position = StaticObstacles[i].transform.position;
            // var rotation = StaticObstacles[i].transform.rotation;
            // Vector3 scale = StaticObstacles[i].transform.lossyScale;

            // Vector3 v1 = new Vector3(position.x + scale.x / 2, 0,position.z + scale.z / 2);
            // Vector3 v2 = new Vector3(position.x - scale.x / 2, 0,position.z + scale.z / 2);
            // Vector3 v3 = new Vector3(position.x - scale.x / 2, 0,position.z - scale.z / 2);
            // Vector3 v4 = new Vector3(position.x + scale.x / 2, 0,position.z - scale.z / 2);
            
            // Debug.DrawLine(v1, v2, Color.green, 100);
            // Debug.DrawLine(v2, v3, Color.green, 100);
            // Debug.DrawLine(v3, v4, Color.green, 100);
            // Debug.DrawLine(v4, v1, Color.green, 100);
            
            // Debug.DrawLine( rotation * v1, rotation * v2, Color.blue, 100);
            // Debug.DrawLine(rotation * v2 , rotation * v3, Color.blue, 100);
            // Debug.DrawLine(rotation * v3 , rotation * v4, Color.blue, 100);
            // Debug.DrawLine(rotation * v4 , rotation * v1, Color.blue, 100);

            /*
            // 在local空间画方框
            Vector4 v1 = new Vector4(scale.x / 2, 0,scale.z / 2, 1);
            Vector4 v2 = new Vector4(-scale.x / 2, 0,scale.z / 2, 1);
            Vector4 v3 = new Vector4(-scale.x / 2, 0,-scale.z / 2, 1);
            Vector4 v4 = new Vector4(scale.x / 2, 0,-scale.z / 2, 1);
            
            // 对方框的每一个边上的点应用矩阵变换
            Matrix4x4 mtrx = StaticObstacles[i].transform.localToWorldMatrix;
            Debug.DrawLine( mtrx * v1, mtrx * v2, Color.red);
            Debug.DrawLine(mtrx * v2 , mtrx * v3, Color.red);
            Debug.DrawLine(mtrx * v3 , mtrx * v4, Color.red);
            Debug.DrawLine(mtrx * v4 , mtrx * v1, Color.red);
            */
            
            
            // Vector3 position = StaticObstacles[i].transform.position;
            Vector3 scale = StaticObstacles[i].transform.localScale;
            
            Vector4 v1 = new Vector4(scale.x / 2, 0,scale.z / 2,1);
            Vector4 v2 = new Vector4(-scale.x / 2, 0,scale.z / 2,1);
            Vector4 v3 = new Vector4(-scale.x / 2, 0,-scale.z / 2,1);
            Vector4 v4 = new Vector4(scale.x / 2, 0,-scale.z / 2,1);
            
                        
            
            Matrix4x4 mtrx = StaticObstacles[i].transform.localToWorldMatrix;
                        
            Debug.DrawLine( mtrx * v1, mtrx * v2, Color.yellow);
            Debug.DrawLine(mtrx * v2 , mtrx * v3, Color.yellow);
            Debug.DrawLine(mtrx * v3 , mtrx * v4, Color.yellow);
            Debug.DrawLine(mtrx * v4 , mtrx * v1, Color.yellow);
            
        }
    }
}
