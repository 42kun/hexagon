using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//渲染图像，同时添加碰撞盒，记录点击位置

[RequireComponent(typeof(MeshFilter),typeof(MeshRenderer))]
public class HexMesh : MonoBehaviour
{
    Mesh hexMesh;
    //顶点
    List<Vector3> vertices;
    //三角形
    List<int> triangles;
    //颜色
    List<Color> colors;
    //碰撞盒
    MeshCollider meshCollider;

    private void Awake()
    {
        GetComponent<MeshFilter>().mesh = hexMesh = new Mesh();
        meshCollider = gameObject.AddComponent<MeshCollider>();
        hexMesh.name = "Hex Mesh";
        vertices = new List<Vector3>();
        triangles = new List<int>();
        colors = new List<Color>();
    }

    //传入HexCell数组，重新渲染整个网格
    public void Triangulate(HexCell[] cells)
    {
        hexMesh.Clear();
        vertices.Clear();
        triangles.Clear();
        colors.Clear();
        for(int i = 0; i < cells.Length; i++)
        {
            Triangulate(cells[i]);
        }
        hexMesh.vertices = vertices.ToArray();
        hexMesh.triangles = triangles.ToArray();
        hexMesh.colors = colors.ToArray();
        //重新计算网格法线
        hexMesh.RecalculateNormals();
        meshCollider.sharedMesh = hexMesh;
    }

    //传入一个六边形，渲染这个六边形
    void Triangulate(HexCell cell)
    {
        for(HexDirection d = HexDirection.NE; d <=HexDirection.NW; d++)
        {
            Triangulate(d, cell);
        }
    }

    //传入六边形的一个方向，渲染这个方向相关的Mesh
    void Triangulate(HexDirection d,HexCell cell)
    {
        Vector3 center = cell.transform.localPosition;
        Vector3 v1 = center+HexMetrics.GetFirstSolidCornet(d);
        Vector3 v2 = center+HexMetrics.GetSecondSolidCornet(d);

        //内部三角形，颜色为纯色
        AddTriangle(center, v1, v2);
        AddTriangleColor(cell.color);

        TriangulateConnection(d, cell, v1, v2);
    }

    /// <summary>
    /// 对方向进行判断，若方向符合条件则渲染连接桥（四边型）以及对应的三角形
    /// </summary>
    /// <param name="direction">对应处理的方向</param>
    /// <param name="cell">六边形</param>
    /// <param name="v1">六边形内上顶点</param>
    /// <param name="v2">六边形内下顶点</param>
    void TriangulateConnection(HexDirection direction, HexCell cell, Vector3 v1, Vector3 v2)
    {
        if (direction <= HexDirection.SE && cell.GetNeighbor(direction)!=null) { 
            //添加矩形连接桥
            HexCell neighbor = cell.GetNeighbor(direction) ?? cell;
            Vector3 bright = HexMetrics.GetBirdge(direction);
            Vector3 v3 = v1 + bright;
            Vector3 v4 = v2 + bright;
            //将对面连接点添加高度修正
            v3.y = v4.y = neighbor.Elevation * HexMetrics.elevationStep;

            TriangulateEdgeTerraces(v1, v2, cell, v3, v4, neighbor);
            //AddQuad(v1, v2, v3, v4);
            //Color brightColor = (cell.color + neighbor.color) * 0.5f;
            //AddQuadColor(
            //    cell.color,
            //    neighbor.color);

            //处理三角形连接
            HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
            if (nextNeighbor)
            {
                Vector3 v5 = v2 + HexMetrics.GetBirdge(direction.Next());
                v5.y = nextNeighbor.Elevation * HexMetrics.elevationStep;
                AddTriangle(v2, v4, v5);
                AddTriangleColor(cell.color, neighbor.color, nextNeighbor.color);
            }
        }
    }

    void TriangulateEdgeTerraces(Vector3 beginLeft,Vector3 beginRight,HexCell beginCell,Vector3 endLeft,Vector3 endRight,HexCell endCell)
    {
        //AddQuad(beginLeft, beginRight, endLeft, endRight);
        //AddQuadColor(beginCell.color, endCell.color);

        Vector3 v1 = HexMetrics.TerraceLerp(beginLeft, endLeft, 1);
        Vector3 v2 = HexMetrics.TerraceLerp(beginRight, endRight, 1);
        Color c1 = HexMetrics.TerraceLerp(beginCell.color, endCell.color, 1);

        AddQuad(beginLeft, beginRight, v1, v2);
        AddQuadColor(beginCell.color, c1);
        for (int i = 2; i <=HexMetrics.terraceSteps; i++)
        {
            Color c2 = HexMetrics.TerraceLerp(beginCell.color, endCell.color, i);
            Vector3 v3 = HexMetrics.TerraceLerp(beginLeft, endLeft, i);
            Vector3 v4 = HexMetrics.TerraceLerp(beginRight, endRight, i);
            AddQuad(v1, v2, v3, v4);
            AddQuadColor(c1, c2);
            c1 = c2;
            v1 = v3;
            v2 = v4;
        }
    }

    //添加一个三角形
    void AddTriangle(Vector3 v1,Vector3 v2,Vector3 v3)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
    }

    //添加三角形颜色
    void AddTriangleColor(Color color0,Color color1,Color color2)
    {
        colors.Add(color0);
        colors.Add(color1);
        colors.Add(color2);
    }

    //添加三角形颜色（纯色）
    void AddTriangleColor(Color color)
    {
        colors.Add(color);
        colors.Add(color);
        colors.Add(color);
    }

    //为四边形添加顶点
    void AddQuad(Vector3 v1,Vector3 v2,Vector3 v3,Vector3 v4)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);
        vertices.Add(v4);

        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 3);
    }

    //为四边形添加颜色
    void AddQuadColor(Color c1,Color c2)
    {
        colors.Add(c1);
        colors.Add(c1);
        colors.Add(c2);
        colors.Add(c2);
    }
}
