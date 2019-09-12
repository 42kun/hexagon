using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexGridChunk : MonoBehaviour
{
    HexCell[] cells;

    HexMesh hexMesh;
    Canvas gridCanvas;

    public bool refreshEnable = true;

    void Awake()
    {
        gridCanvas = GetComponentInChildren<Canvas>();
        hexMesh = GetComponentInChildren<HexMesh>();

        cells = new HexCell[HexMetrics.chunkSizeX * HexMetrics.chunkSizeZ];

        ShowUI(false);
    }

    //void Start()
    //{
    //    hexMesh.Triangulate(cells);
    //}

    void Update()
    {
        
    }

    private void LateUpdate()
    {
        if (refreshEnable)
        {
            hexMesh.Triangulate(cells);
            refreshEnable = false;
        }
    }

    //通过索引，将HexCell添加至Chunk
    public void AddCell(int index,HexCell cell)
    {
        //Debug.Log(index);
        cells[index] = cell;
        cell.transform.SetParent(transform, false);
        cell.uiRect.SetParent(gridCanvas.transform, false);
        cell.chunk = this;
    }

    //刷新网格
    public void Refresh()
    {
        //设置允许刷新
        refreshEnable = true;
    }

    //是否显示UI
    public void ShowUI(bool visible)
    {
        gridCanvas.gameObject.SetActive(visible);
    }
}
