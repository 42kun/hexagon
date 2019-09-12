using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HexGrid : MonoBehaviour
{
    //public int cellCountX = 6;
    //public int height = 6;

    //六边形总数，由区块数与区块大小决定
    public int cellCountX,cellCountZ;

    //区块数，由这里定义
    public const int chunkCountX = 20, chunkCountZ = 20;


    public HexCell cellPrefab;
    public Text cellLabelPrefab;
    public HexGridChunk chunkPerfab;

    public Color defaultColor = Color.white;

    //对应每一格六边形
    HexCell[] cells;
    //区块数组
    HexGridChunk[] chunks;




    //噪声贴图，用以为TextMetrics进行周转
    public Texture2D noiseSource;

    private void Awake()
    {
        HexMetrics.noiseSource = noiseSource;

        cellCountX = chunkCountX * HexMetrics.chunkSizeX;
        cellCountZ = chunkCountZ * HexMetrics.chunkSizeZ;

        cells = new HexCell[cellCountX*cellCountZ];

        CreateChunks();
        CreateCells();

    }

    void CreateCells()
    {
        for (int z = 0, i = 0; z < cellCountZ; z++)
        {
            for (int x = 0; x < cellCountX; x++)
            {
                CreateCell(x, z, i++);
            }
        }
    }

    void CreateChunks()
    {
        chunks = new HexGridChunk[chunkCountX * chunkCountZ];
        for(int z = 0,i=0; z < chunkCountZ; z++)
        {
            for(int x = 0; x < chunkCountX; x++)
            {
                HexGridChunk chunk = chunks[i++] = Instantiate(chunkPerfab);
                chunk.transform.SetParent(transform);
            }
        }
    }

    //以后再研究
    private void OnEnable()
    {
        HexMetrics.noiseSource = noiseSource;
    }


    /// <summary>
    /// 在特定位置创建一个六边形并设置临近单元格
    /// </summary>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <param name="i"></param>
    void CreateCell(int x,int z,int i)
    {
        //放置cellPrefabs
        Vector3 position;
        position.x = (x + z*0.5f - z/2) * (HexMetrics.innerRadius * 2f);
        position.y = 0;
        position.z = z * (HexMetrics.outerRadius * 1.5f);

        HexCell cell = cells[i] = Instantiate<HexCell>(cellPrefab);
        cell.transform.localPosition = position;
        //HexCell cell = cells[i] = Instantiate<HexCell>(cellPrefab,position,Quaternion.identity,transform);
        cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
        cell.Color = defaultColor;

        if (x > 0)
        {
            cell.SetNeighbor(HexDirection.W, cells[i - 1]);
        }

        //偶数行
        if ((z & 1) == 0 && z!=0)
        {
            cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX]);
            if (i % cellCountX != 0)
            {
                cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX - 1]);
            }
        }
        //奇数行
        if ((z & 1) == 1)
        {
            cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX]);
            if ((i + 1) % cellCountX != 0)
            {
                cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX + 1]);
            }
        }

        ////放置cellLabelPrefab
        Text label = Instantiate<Text>(cellLabelPrefab);
        label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);
        //Text label = Instantiate<Text>(cellLabelPrefab, new Vector2(position.x, position.z), Quaternion.identity, gridCanvans.transform);
        label.text = cell.coordinates.ToStringOnSparateLines();
        cell.uiRect = label.rectTransform;//传递地址,所以可以在其他地方更改

        cell.Elevation = 0;

        AddCellToChunk(x, z, cell);
    }
    
    /// <summary>
    /// 将六边形块添加到地图区块中，其中所有六边形暂时还由HexGrid管理，只不过HexGrid对它们做了一个再分配
    /// </summary>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <param name="cell"></param>
    void AddCellToChunk(int x,int z,HexCell cell)
    {
        int chunkX = x / HexMetrics.chunkSizeX;
        int chunkZ = z / HexMetrics.chunkSizeZ;
        HexGridChunk chunk = chunks[chunkX + chunkZ * chunkCountX];

        //计算x,z在Chunk内部的位置
        int localX = x - chunkX * HexMetrics.chunkSizeX;
        int localZ = z - chunkZ * HexMetrics.chunkSizeZ;

        chunk.AddCell(localX + HexMetrics.chunkSizeX * localZ,cell);
    }

    //获取选中的六边形
    public HexCell GetCell(Vector3 position)
    {
        position = transform.InverseTransformPoint(position);
        HexCoordinates coordinates = HexCoordinates.FromPosition(position);
        int index = coordinates.X + coordinates.Z * cellCountX + coordinates.Z / 2;
        return cells[index];
    }

    //隐藏网格中的所有Label

    public void ShowUI(bool visible)
    {
        foreach(HexGridChunk chunk in chunks)
        {
            chunk.ShowUI(visible);
        }
    }

}
