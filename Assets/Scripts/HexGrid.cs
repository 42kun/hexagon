using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HexGrid : MonoBehaviour
{
    public int width = 6;
    public int height = 6;
    public HexCell cellPrefab;
    public Text cellLabelPrefab;

    public Color defaultColor = Color.white;

    //对应每一格六边形
    HexCell[] cells;
    Canvas gridCanvans;
    HexMesh hexMesh;
    
    private void Awake()
    {
        cells = new HexCell[height * width];
        gridCanvans = GetComponentInChildren<Canvas>();
        hexMesh = GetComponentInChildren<HexMesh>();

        for(int z = 0, i = 0; z < height; z++)
        {
            for(int x = 0; x < width; x++)
            {
                CreateCell(x, z, i++);
            }
        }
    }

    private void Start()
    {
        hexMesh.Triangulate(cells);
    }

    /// <summary>
    /// 在特定位置创建一个六边形
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

        //HexCell cell = cells[i] = Instantiate<HexCell>(cellPrefab);
        //cell.transform.SetParent(transform, false);
        //cell.transform.localPosition = position;
        HexCell cell = cells[i] = Instantiate<HexCell>(cellPrefab,position,Quaternion.identity,transform);
        cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
        cell.color = defaultColor;

        //放置cellLabelPrefab
        Text label = Instantiate<Text>(cellLabelPrefab);
        label.rectTransform.SetParent(gridCanvans.transform, false);
        label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);
        //Text label = Instantiate<Text>(cellLabelPrefab,new Vector2(position.x,position.z),Quaternion.identity,gridCanvans.transform);
        label.text = cell.coordinates.ToStringOnSparateLines();
    }

    public void ColorCell(Vector3 position,Color color)
    {
        position = transform.InverseTransformPoint(position);
        HexCoordinates coordinates = HexCoordinates.FromPosition(position);
        int index = coordinates.X + coordinates.Z * width + coordinates.Z / 2;
        HexCell cell = cells[index];
        cell.color = color;
        hexMesh.Triangulate(cells);
    }

}
