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

    void Triangulate(HexCell cell)
    {
        for(HexDirection d = HexDirection.NE; d <=HexDirection.NW; d++)
        {
            Triangulate(d, cell);
        }
    }

    void Triangulate(HexDirection d,HexCell cell)
    {
        Vector3 center = cell.transform.localPosition;
        Vector3 v1 = center+HexMetrics.GetFirstSolidCornet(d);
        Vector3 v2 = center+HexMetrics.GetSecondSolidCornet(d);

        //内部三角形，颜色为纯色
        AddTriangle(center, v1, v2);
        AddTriangleColor(cell.color);

        TriangulateConnection(d, cell, v1, v2);

        

        //添加两个小三角形，其颜色为对应顶点三色平均值

        //AddTriangle(v1, center + HexMetrics.GetFirstCornet(d), v3);
        //AddTriangleColor(
        //    cell.color,
        //    (cell.color + preNeighbor.color + neighbor.color) / 3f,
        //    brightColor);

        ////AddTriangle(v2, center + HexMetrics.GetSecondCornet(d), v4);
        ////AddTriangleColor(
        ////    cell.color,
        ////    (cell.color + neighbor.color + nextNeighbor.color) / 3f,
        ////    brightColor);

        //AddTriangle(v2, v4, center + HexMetrics.GetSecondCornet(d));
        //AddTriangleColor(
        //    cell.color,
        //    brightColor,
        //    (cell.color + neighbor.color + nextNeighbor.color) / 3f);
    }

    void TriangulateConnection(HexDirection direction, HexCell cell, Vector3 v1, Vector3 v2)
    {
        if (direction <= HexDirection.SE && cell.GetNeighbor(direction)!=null) { 
            HexCell neighbor = cell.GetNeighbor(direction) ?? cell;
            Vector3 bright = HexMetrics.GetBirdge(direction);
            Vector3 v3 = v1 + bright;
            Vector3 v4 = v2 + bright;

            AddQuad(v1, v2, v3, v4);
            Color brightColor = (cell.color + neighbor.color) * 0.5f;
            AddQuadColor(
                cell.color,
                neighbor.color);

            HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
            if (nextNeighbor)
            {
                AddTriangle(v2, v4, v2 + HexMetrics.GetBirdge(direction.Next()));
                AddTriangleColor(cell.color, neighbor.color, nextNeighbor.color);
            }


        }
    }
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

    void AddTriangleColor(Color color0,Color color1,Color color2)
    {
        colors.Add(color0);
        colors.Add(color1);
        colors.Add(color2);
    }

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
