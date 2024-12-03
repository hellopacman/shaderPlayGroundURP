using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawFogAreaTool : MonoBehaviour
{
    [Header("迷雾块大小")]
    public Vector2Int areaSize;
    [Header("画线模板")]
    public LineRenderer lineTemple;
    public Vector2Int world = new Vector2Int(7200, 7200);

    Transform lineRoot;
    Vector2Int num;
    
    void Start()
    {
        transform.position = Vector3.zero;        
    }

    private void OnEnable()
    {
        Draw();
    }

    private void Draw()
    {
        if (lineRoot != null)
            Destroy(lineRoot.gameObject);
        lineRoot = new GameObject("LineRoot").transform;
        lineRoot.SetParent(transform);
        lineRoot.transform.localPosition = Vector3.zero;
        num = new Vector2Int(world.x/areaSize.x,world.y/areaSize.y);
        for (int i = 0; i <= num.x; i++)
        {
            var line = Instantiate(lineTemple, lineRoot);
            line.positionCount = 2;
            line.startWidth = line.endWidth = 1f;
            line.SetPosition(0, new Vector3(areaSize.y * i, transform.position.y, 0));
            line.SetPosition(1, new Vector3(areaSize.y * i, transform.position.y, areaSize.y * num.y));
        }

        for (int i = 0; i <= num.y; i++)
        {
            var line = Instantiate(lineTemple, lineRoot);
            line.positionCount = 2;
            line.startWidth = line.endWidth = 1f;
            line.SetPosition(0, new Vector3(0, transform.position.y, areaSize.y*i));
            line.SetPosition(1, new Vector3(areaSize.x * num.x, transform.position.y, areaSize.y * i));
        }
    }
}
