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
        Vector3 center = cell.Position;
        Vector3 v1 = center+HexMetrics.GetFirstSolidCornet(d);
        Vector3 v2 = center+HexMetrics.GetSecondSolidCornet(d);

        //将三角形细分为三分，以便于更好地随机化
        Vector3 e1 = Vector3.Lerp(v1, v2, 1/3f);
        Vector3 e2 = Vector3.Lerp(v1, v2, 2/3f);

        //内部三角形，颜色为纯色
        AddTriangle(center, v1, e1);
        AddTriangleColor(cell.color);

        AddTriangle(center, e1, e2);
        AddTriangleColor(cell.color);

        AddTriangle(center, e2, v2);
        AddTriangleColor(cell.color);

        TriangulateConnection(d, cell, v1, e1, e2, v2);
    }

    /// <summary>
    /// 对方向进行判断，若方向符合条件则渲染连接桥（四边型）以及对应的三角形
    /// </summary>
    /// <param name="direction">对应处理的方向</param>
    /// <param name="cell">六边形</param>
    /// <param name="v1">六边形内上顶点</param>
    /// <param name="v2">六边形内下顶点</param>
    void TriangulateConnection(HexDirection direction, HexCell cell, Vector3 v1, Vector3 e1, Vector3 e2, Vector3 v2)
    {
        if (direction <= HexDirection.SE && cell.GetNeighbor(direction)!=null) { 
            //添加矩形连接桥
            HexCell neighbor = cell.GetNeighbor(direction) ?? cell;
            Vector3 bright = HexMetrics.GetBirdge(direction);
            Vector3 v3 = v1 + bright;
            Vector3 v4 = v2 + bright;

            //将对面连接点添加高度修正
            v3.y = v4.y = neighbor.Position.y;

            Vector3 e3 = Vector3.Lerp(v3, v4, 1/3f);
            Vector3 e4 = Vector3.Lerp(v3, v4, 2/3f);

            //限制只在倾斜时进行阶梯化
            if (cell.GetEdgeType(direction) == HexEdgeType.Slope)
            {
                TriangulateEdgeTerraces(v1, e1, cell, v3, e3, neighbor);
                TriangulateEdgeTerraces(e1, e2, cell, e3, e4, neighbor);
                TriangulateEdgeTerraces(e2, v2, cell, e4, v4, neighbor);
            }
            else
            {
                //AddQuad(v1, v2, v3, v4);
                //AddQuadColor(cell.color, neighbor.color);
                AddQuad(v1, e1, v3, e3);
                AddQuadColor(cell.color, neighbor.color);
                AddQuad(e1, e2, e3, e4);
                AddQuadColor(cell.color, neighbor.color);
                AddQuad(e2,v2,e4,v4);
                AddQuadColor(cell.color, neighbor.color);
            }


            //处理三角形连接
            HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
            if (nextNeighbor)
            {
                Vector3 v5 = v2 + HexMetrics.GetBirdge(direction.Next());
                v5.y = nextNeighbor.Position.y;
                //TriangulateCorner(v2, cell, v4, neighbor, v5, nextNeighbor);
                //找出最小的一个cell，然后顺时针将三个cell传入
                if(cell.Elevation <= neighbor.Elevation)
                {
                    if (cell.Elevation <= nextNeighbor.Elevation)
                    {
                        TriangulateCorner(v2, cell, v4, neighbor, v5, nextNeighbor);
                    }
                    else
                    {
                        TriangulateCorner(v5, nextNeighbor, v2, cell, v4, neighbor);
                    }
                }else
                {
                    if (neighbor.Elevation <= nextNeighbor.Elevation)
                    {
                        TriangulateCorner(v4, neighbor, v5, nextNeighbor, v2, cell);
                    }
                    else
                    {
                        TriangulateCorner(v5, nextNeighbor, v2, cell, v4, neighbor);
                    }
                }
            }
        }
    }

    //添加连接桥并阶梯化
    void TriangulateEdgeTerraces(Vector3 beginLeft,Vector3 beginRight,HexCell beginCell,Vector3 endLeft,Vector3 endRight,HexCell endCell)
    {

        Vector3 v1 = HexMetrics.TerraceLerp(beginLeft, endLeft, 1);
        Vector3 v2 = HexMetrics.TerraceLerp(beginRight, endRight, 1);
        Color c1 = HexMetrics.TerraceLerp(beginCell.color, endCell.color, 1);

        AddQuad(beginLeft, beginRight, v1, v2);
        AddQuadColor(beginCell.color, c1);
        for (int i = 2; i <= HexMetrics.terraceSteps; i++)
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

    //添加连接三角形并阶梯化
    void TriangulateCorner(Vector3 bottom,HexCell bottomCell,Vector3 left,HexCell leftCell,Vector3 right,HexCell rightCell)
    {
        //AddTriangle(bottom, left, right);
        //AddTriangleColor(bottomCell.color, leftCell.color, rightCell.color);
        //bottom最低，然后以顺时针排列
        //注意考虑渲染顶点顺时针排列
        HexEdgeType leftEdgeType = bottomCell.GetEdgeType(leftCell);
        HexEdgeType rightEdgeType = bottomCell.GetEdgeType(rightCell);

        if(leftEdgeType == HexEdgeType.Flat && rightEdgeType == HexEdgeType.Flat)
        {
            TriangulateCornerNoTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
            //AddTriangle(bottom, left, right);
            //AddTriangleColor(bottomCell.color, leftCell.color, rightCell.color);
        }else if(leftEdgeType == HexEdgeType.Flat && rightEdgeType == HexEdgeType.Slope)
        {
            TriangulateCornerFlatWithSlope( bottom, bottomCell,left, leftCell, right, rightCell);
        }
        else if(leftEdgeType == HexEdgeType.Slope && rightEdgeType == HexEdgeType.Flat)
        {
            TriangulateCornerFlatWithSlope(right, rightCell,bottom, bottomCell, left, leftCell);
        }
        else if(leftEdgeType == HexEdgeType.Flat && rightEdgeType == HexEdgeType.Cliff)
        {
            TriangulateCornerNoTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
        }
        else if(leftEdgeType == HexEdgeType.Cliff && rightEdgeType == HexEdgeType.Flat)
        {
            TriangulateCornerNoTerraces(right, rightCell, bottom, bottomCell, left, leftCell);
        }
        else if(leftEdgeType == HexEdgeType.Slope && rightEdgeType == HexEdgeType.Slope)
        {
            TriangulateCornerSlopeWithSlope(bottom, bottomCell, left, leftCell, right, rightCell);
        }else if(leftEdgeType == HexEdgeType.Slope && rightEdgeType == HexEdgeType.Cliff)
        {
            TriangulateSlopeWithCliff(bottom, bottomCell, left, leftCell, right, rightCell);
        }
        else if(leftEdgeType == HexEdgeType.Cliff && rightEdgeType == HexEdgeType.Slope)
        {
            TriangulateSlopeWithCliff(bottom, bottomCell, left, leftCell, right, rightCell);
        }
        else if(leftEdgeType == HexEdgeType.Cliff && rightEdgeType == HexEdgeType.Cliff)
        {
            TriangulateCornerNoTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
        }

    }

    /// <summary>
    /// 处理fs的情况。其中vs为较高的一点
    /// 需要以顺时针添加节点
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="c1"></param>
    /// <param name="v2"></param>
    /// <param name="c2"></param>
    /// <param name="vs"></param>
    /// <param name="cs"></param>
    void TriangulateCornerFlatWithSlope(Vector3 v1, HexCell c1, 
        Vector3 v2, HexCell c2, Vector3 vs, HexCell cs)
    {
        //Debug.Log(v1.ToString()+","+v2.ToString()+","+vs.ToString());
        Vector3 v3 = HexMetrics.TerraceLerp(v1, vs, 1);
        Vector3 v4 = HexMetrics.TerraceLerp(v2, vs, 1);
        Color color1 = HexMetrics.TerraceLerp(c1.color, cs.color, 1);
        Color color2 = HexMetrics.TerraceLerp(c2.color, cs.color, 1);
        AddQuad(v3, v4,v1, v2);
        AddQuadColor( color1, color2, c1.color, c2.color);

        for(int i = 2; i < HexMetrics.terraceSteps; i++)
        {
            Vector3 v33 = HexMetrics.TerraceLerp(v1, vs, i);
            Vector3 v44 = HexMetrics.TerraceLerp(v2, vs, i);
            Color color3 = HexMetrics.TerraceLerp(c1.color, cs.color, i);
            Color color4 = HexMetrics.TerraceLerp(c2.color, cs.color, i);
            AddQuad(v33, v44, v3, v4);
            AddQuadColor(color3, color4, color1, color2);
            v3 = v33;
            v4 = v44;
            color1 = color3;
            color2 = color4;
        }

        AddTriangle(v3, v4, vs);
        AddTriangleColor(color1,color2,cs.color);
    }

    /// <summary>
    /// 不进行阶梯化,cc可以是最高顶点,需要以顺时针添加顶点
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="c1"></param>
    /// <param name="v2"></param>
    /// <param name="c2"></param>
    /// <param name="vc"></param>
    /// <param name="cc"></param>
    void TriangulateCornerNoTerraces(Vector3 v1, HexCell c1,
        Vector3 v2, HexCell c2, Vector3 vc, HexCell cc)
    {
        AddTriangle(v1,v2,vc);
        AddTriangleColor(c1.color, c2.color, cc.color);
    }

    /// <summary>
    /// 处理ss情况，需要以顺时针添加顶点
    /// </summary>
    /// <param name="bottom"></param>
    /// <param name="bottomCell"></param>
    /// <param name="left"></param>
    /// <param name="leftCell"></param>
    /// <param name="right"></param>
    /// <param name="rightCell"></param>
    void TriangulateCornerSlopeWithSlope(Vector3 bottom,HexCell bottomCell,Vector3 left,HexCell leftCell,Vector3 right,HexCell rightCell)
    {
        Vector3 v3 = HexMetrics.TerraceLerp(bottom, left, 1);
        Vector3 v4 = HexMetrics.TerraceLerp(bottom, right, 1);
        Color c1 = HexMetrics.TerraceLerp(bottomCell.color, leftCell.color, 1);
        Color c2 = HexMetrics.TerraceLerp(bottomCell.color, rightCell.color, 1);
        AddTriangle(bottom, v3, v4);
        AddTriangleColor(bottomCell.color, c1, c2);

        for(int i = 2; i <= HexMetrics.terraceSteps; i++)
        {
            Vector3 v33 = HexMetrics.TerraceLerp(bottom, left, i);
            Vector3 v44 = HexMetrics.TerraceLerp(bottom, right, i);
            Color c3 = HexMetrics.TerraceLerp(bottomCell.color, leftCell.color, i);
            Color c4 = HexMetrics.TerraceLerp(bottomCell.color, rightCell.color, i);
            AddQuad( v3, v4, v33, v44);
            AddQuadColor(c1, c2, c3, c4);
            v3 = v33;
            v4 = v44;
            c1 = c3;
            c2 = c4;
        }
    }

    /// <summary>
    /// 处理sc的情况
    /// </summary>
    /// <param name="bottom"></param>
    /// <param name="bottomCell"></param>
    /// <param name="left"></param>
    /// <param name="leftCell"></param>
    /// <param name="right"></param>
    /// <param name="rightCell"></param>
    void TriangulateSlopeWithCliff(Vector3 bottom, HexCell bottomCell, Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell)
    {
        if (leftCell.Elevation > rightCell.Elevation)
        {
            float b = 1f / (leftCell.Elevation - bottomCell.Elevation);
            Vector3 boundary = Vector3.Lerp(Perturb(bottom), Perturb(left), b);
            Color boundaryColor = Color.Lerp(bottomCell.color, leftCell.color, b);
            if(rightCell.GetEdgeType(leftCell) == HexEdgeType.Slope)
            {
                TriangulateTerraces(right, rightCell.color, bottom, bottomCell.color, boundary,boundaryColor);
                TriangulateTerraces(left, leftCell.color, right, rightCell.color, boundary,boundaryColor);
            }
            else
            {
                TriangulateTerraces(right, rightCell.color, bottom, bottomCell.color, boundary, boundaryColor);
                AddTriangleNoPerturb(Perturb(left), Perturb(right), boundary);
                AddTriangleColor(leftCell.color, rightCell.color, boundaryColor);
            }
        }
        else
        {
            float b = 1f / (rightCell.Elevation - bottomCell.Elevation);
            Vector3 boundary = Vector3.Lerp(Perturb(bottom), Perturb(right), b);
            Color boundaryColor = Color.Lerp(bottomCell.color, rightCell.color, b);
            if (rightCell.GetEdgeType(leftCell) == HexEdgeType.Slope)
            {
                TriangulateTerraces(bottom, bottomCell.color,left,leftCell.color, boundary, boundaryColor);
                TriangulateTerraces(left, leftCell.color, right, rightCell.color, boundary, boundaryColor);
            }
            else
            {
                TriangulateTerraces(bottom, bottomCell.color, left, leftCell.color, boundary, boundaryColor);
                AddTriangleNoPerturb(Perturb(left), Perturb(right), boundary);
                AddTriangleColor(leftCell.color, rightCell.color, boundaryColor);
            }
        }
    }

    /// <summary>
    /// 接替画三角形辅助类，begin，end，target必须以顺时针排列，begin，end构成一个阶梯化向量
    /// </summary>
    /// <param name="begin">标准点</param>
    /// <param name="beginColor"></param>
    /// <param name="end">标准点</param>
    /// <param name="endColor"></param>
    /// <param name="target"></param>
    /// <param name="targetColor"></param>
    void TriangulateTerraces(Vector3 begin,Color beginColor,Vector3 end,Color endColor,Vector3 target,Color targetColor)
    {
        Vector3 pbegin = Perturb(begin);
        //Vector3 pend = Perturb(end);
        Vector3 v1 = Perturb(HexMetrics.TerraceLerp(begin, end, 1));
        Color c1 = HexMetrics.TerraceLerp(beginColor, endColor, 1);
        AddTriangleNoPerturb(pbegin, v1, target);
        AddTriangleColor(beginColor, c1, targetColor);
        for (int i = 2; i <= HexMetrics.terraceSteps; i++)
        {
            Vector3 v11 = Perturb(HexMetrics.TerraceLerp(begin, end, i));
            Color c2 = HexMetrics.TerraceLerp(beginColor, endColor, i);
            AddTriangleNoPerturb(v1, v11, target);
            AddTriangleColor(c1, c2, targetColor);
            v1 = v11;
            c1 = c2;
        }
    }

    /// <summary>
    /// 添加一个三角形，采用顶点扰动的方式
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <param name="v3"></param>
    void AddTriangle(Vector3 v1,Vector3 v2,Vector3 v3)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(Perturb(v1));
        vertices.Add(Perturb(v2));
        vertices.Add(Perturb(v3));
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
    }

    /// <summary>
    /// 添加一个三角形，顶点不进行扰动
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <param name="v3"></param>
    void AddTriangleNoPerturb(Vector3 v1,Vector3 v2,Vector3 v3)
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
        vertices.Add(Perturb(v1));
        vertices.Add(Perturb(v2));
        vertices.Add(Perturb(v3));
        vertices.Add(Perturb(v4));

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

    void AddQuadColor(Color c1,Color c2,Color c3,Color c4)
    {
        colors.Add(c1);
        colors.Add(c2);
        colors.Add(c3);
        colors.Add(c4);

    }


    ////处理噪声

    //对点进行扰动
    Vector3 Perturb(Vector3 position)
    {
        Vector4 sample = HexMetrics.SampleNoise(position);
        position.x += (sample.x * 2f - 1) * HexMetrics.cellPerturbStrength;
        //position.y += (sample.y * 2f - 1) * HexMetrics.cellPerturbStrength;
        position.z += (sample.z * 2f - 1) * HexMetrics.cellPerturbStrength;

        return position;
    }
}
