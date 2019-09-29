using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//渲染图像，同时添加碰撞盒，记录点击位置

[RequireComponent(typeof(MeshFilter),typeof(MeshRenderer))]
public class HexMesh : MonoBehaviour
{
    Mesh hexMesh;
    /// <summary>
    /// 但愿没有多线程冲突
    /// </summary>
    //顶点
    static List<Vector3> vertices = new List<Vector3>();
    //三角形
    static List<int> triangles = new List<int>();
    //颜色
    static List<Color> colors = new List<Color>();
    //碰撞盒
    MeshCollider meshCollider;
    
    private void Awake()
    {
        GetComponent<MeshFilter>().mesh = hexMesh = new Mesh();
        meshCollider = gameObject.AddComponent<MeshCollider>();
        hexMesh.name = "Hex Mesh";
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
        //e1,e2,e3皆为本方顶点
        Vector3 e1 = Vector3.Lerp(v1, v2, 0.25f);
        Vector3 e2 = Vector3.Lerp(v1, v2, 0.5f);
        Vector3 e3 = Vector3.Lerp(v1, v2, 0.75f);

        if (cell.HasRiver)
        {
            if (cell.HasRiverThroughEdge(d))
            {
                e2.y = cell.StreamBedY;
                TriangulateWithRiver(d, cell, center, v1, e1, e2, e3, v2);
            }
            else
            {
                TriangulatAdjacentToRiver(d, cell, center, v1, e1, e2, e3, v2);
            }
        }
        else
        {
            //内部三角形，颜色为纯色
            AddTriangle4(center, v1, e1, e2, e3, v2, cell.Color);
        }

        TriangulateConnection(d, cell, v1, e1, e2, e3, v2);
    }

    //仅当河道中存在河道时才会调用
    //绘制河道
    void TriangulateWithRiver(HexDirection direction, HexCell cell, 
        Vector3 center, Vector3 v1, Vector3 e1, Vector3 e2, Vector3 e3, Vector3 v2)
    {
        //如果为河流的尽头，则仅仅弯下去一个角，其他角不受影响
        if(cell.HasRiverBeginOrEnd)
        {
            Vector3 centerRiver = center;
            centerRiver.y = cell.StreamBedY;
            float lerpVal = 0.5f;
            Vector3 mL = Vector3.Lerp(v1, center, lerpVal);
            Vector3 mR = Vector3.Lerp(v2, center, lerpVal);
            Vector3 m1 = Vector3.Lerp(e1, center, lerpVal);
            Vector3 m2 = Vector3.Lerp(e2, centerRiver, lerpVal);
            Vector3 m3 = Vector3.Lerp(e3, center, lerpVal);

            AddTrapezoid(mL, m1, m2, m3, mR, v1, e1, e2, e3, v2, cell.Color);
            AddTriangle4(center, mL, m1, m2, m3, mR, cell.Color);
        }
        //如果是直线穿过的河流，则将六边形中间挖一个类似矩形的空间，会影响几乎所有的角
        else if (cell.RiverState == riverState.Dif3)
        {
            Vector3 cL = center + HexMetrics.GetFirstSolidCornet(direction.Previous()) * 0.25f;
            Vector3 cR = center + HexMetrics.GetSecondSolidCornet(direction.Next()) * 0.25f;

            center.y = cell.StreamBedY;

            float lerpVal = 0.5f;
            Vector3 mL = Vector3.Lerp(v1, cL, lerpVal);
            Vector3 mR = Vector3.Lerp(v2, cR, lerpVal);
            Vector3 m1 = Vector3.Lerp(e1, cL, lerpVal);
            Vector3 m2 = Vector3.Lerp(e2, center, lerpVal);
            Vector3 m3 = Vector3.Lerp(e3, cR, lerpVal);
            AddTrapezoid(mL, m1, m2, m3, mR, v1, e1, e2, e3, v2, cell.Color);
            AddTrapezoid(cL, cL, center, cR, cR, mL, m1, m2, m3, mR, cell.Color);
        }
        //一度只差
        else if (cell.RiverState== riverState.Dif1){
            if (cell.HasRiverThroughEdge(direction.Previous()))
            {
                Vector3 c1 = Vector3.Lerp(v1, center, 2 / 3f);
                c1.y = cell.StreamBedY;
                Vector3 c2 = Vector3.Lerp(v1, center, 1 / 3f);
                Vector3 c3 = Vector3.Lerp(v1, center, 1 / 5f);
                Vector3 leftMid = Vector3.Lerp(v2, center, 0.5f);
                Vector3 m1 = Vector3.Lerp(e3, center, 0.5f);
                Vector3 m2 = Vector3.Lerp(e2, c1, 0.5f);
                Vector3 m3 = Vector3.Lerp(e1, c2, 0.5f);
                AddTrapezoid(c3, m3, m2, m1, leftMid, v1, e1, e2, e3, v2, cell.Color);
                AddTriangle(m1, leftMid, center, cell.Color);
                AddQuad(c1, center, m2, m1, cell.Color);
                AddQuad(c2, c1, m3, m2, cell.Color);
                AddTriangle(c3, m3, c2, cell.Color);
            }else if (cell.HasRiverThroughEdge(direction.Next())){
                Vector3 c1 = Vector3.Lerp(v2, center, 2 / 3f);
                c1.y = cell.StreamBedY;
                Vector3 c2 = Vector3.Lerp(v2, center, 1 / 3f);
                Vector3 c3 = Vector3.Lerp(v2, center, 1 / 5f);
                Vector3 rightMid = Vector3.Lerp(v1, center, 0.5f);
                Vector3 m1 = Vector3.Lerp(e1, center, 0.5f);
                Vector3 m2 = Vector3.Lerp(e2, c1, 0.5f);
                Vector3 m3 = Vector3.Lerp(e3, c2, 0.5f);
                AddTrapezoid(rightMid, m1, m2, m3, c3, v1, e1, e2, e3, v2,cell.Color);
                AddTriangle(m1, center, rightMid, cell.Color);
                AddQuad(m2, m1, c1, center, cell.Color);
                AddQuad(m3, m2, c2, c1, cell.Color);
                AddTriangle(c3, c2, m3, cell.Color);

            }
        }
        //两度只差
        else if(cell.RiverState == riverState.Dif2)
        {
            if(cell.HasRiverThroughEdge(direction.Previous2()))
            {
                Vector3 cL = Vector3.Lerp(center + HexMetrics.GetFirstSolidCornet(direction.Previous()) * 0.5f,
                    center + HexMetrics.GetSecondSolidCornet(direction.Previous()) * 0.5f, 0.5f);
                Vector3 cR = Vector3.Lerp(center + HexMetrics.GetFirstSolidCornet(direction.Next2()) * 0.25f,
                    center + HexMetrics.GetSecondSolidCornet(direction.Next2()) * 0.25f, 0.5f);
                center.y = cell.StreamBedY;
                Vector3 mL = Vector3.Lerp(cL, v1, 0.5f);
                Vector3 mR = Vector3.Lerp(cR, v2, 0.5f);
                Vector3 m1 = Vector3.Lerp(cL, e1, 0.5f);
                Vector3 m2 = Vector3.Lerp(center, e2, 0.5f);
                Vector3 m3 = Vector3.Lerp(cR, e3, 0.5f);

                AddTrapezoid(mL, m1, m2, m3, mR, v1, e1, e2, e3, v2, cell.Color);
                AddTrapezoid(cL, cL, center, cR, cR, mL, m1, m2, m3, mR, cell.Color);
            }
            else if(cell.HasRiverThroughEdge(direction.Next2()))
            {
                Vector3 cL = Vector3.Lerp(center + HexMetrics.GetFirstSolidCornet(direction.Previous2()) * 0.25f,
                    center + HexMetrics.GetSecondSolidCornet(direction.Previous2()) * 0.25f, 0.5f);
                Vector3 cR = Vector3.Lerp(center + HexMetrics.GetFirstSolidCornet(direction.Next()) * 0.5f,
                    center + HexMetrics.GetSecondSolidCornet(direction.Next()) * 0.5f, 0.5f);
                center.y = cell.StreamBedY;
                Vector3 mL = Vector3.Lerp(cL, v1, 0.5f);
                Vector3 mR = Vector3.Lerp(cR, v2, 0.5f);
                Vector3 m1 = Vector3.Lerp(cL, e1, 0.5f);
                Vector3 m2 = Vector3.Lerp(center, e2, 0.5f);
                Vector3 m3 = Vector3.Lerp(cR, e3, 0.5f);

                AddTrapezoid(mL, m1, m2, m3, mR, v1, e1, e2, e3, v2, cell.Color);
                AddTrapezoid(cL, cL, center, cR, cR, mL, m1, m2, m3, mR, cell.Color);
            }
        }
    }

    //补全空缺
    void TriangulatAdjacentToRiver(HexDirection direction,HexCell cell, Vector3 center, 
        Vector3 v1, Vector3 e1, Vector3 e2, Vector3 e3, Vector3 v2)
    {
        //尽头或源头的情况
        if (cell.HasRiverBeginOrEnd)
        {
            if (cell.HasRiverThroughEdge(direction.Previous()))
            {
                Vector3 leftMid = Vector3.Lerp(center, v1, 0.5f);
                TriangulatAdjacentToRiverLeft(center, v1, e1, e2, e3, v2, cell.Color, leftMid);
            }
            else if (cell.HasRiverThroughEdge(direction.Next()))
            {
                Vector3 rightMid = Vector3.Lerp(center, v2, 0.5f);
                TriangulatAdjacentToRiverRight(center, v1, e1, e2, e3, v2, cell.Color, rightMid);
            }
            else
            {
                AddTriangle4(center, v1, e1, e2, e3, v2, cell.Color);
            }
        }
        //平直流过
        else if (cell.RiverState == riverState.Dif3)
        {
            if (cell.HasRiverThroughEdge(direction.Previous()))
            {
                Vector3 centerX = center + HexMetrics.GetSecondSolidCornet(direction) * 0.25f;
                Vector3 leftMid = Vector3.Lerp(centerX, v1, 0.5f);
                TriangulatAdjacentToRiverLeft(centerX, v1, e1, e2, e3, v2, cell.Color, leftMid);
            }
            else if (cell.HasRiverThroughEdge(direction.Next()))
            {
                Vector3 centerX = center + HexMetrics.GetFirstSolidCornet(direction) * 0.25f;
                Vector3 rightMid = Vector3.Lerp(centerX, v2, 0.5f);
                TriangulatAdjacentToRiverRight(centerX, v1, e1, e2, e3, v2, cell.Color, rightMid);

            }
        }
        //锐角转弯
        else if (cell.RiverState == riverState.Dif1)
        {
            if (cell.HasRiverThroughEdge(direction.Previous()))
            {
                Vector3 leftMid = Vector3.Lerp(center, v1, 0.5f);
                TriangulatAdjacentToRiverLeft(center, v1, e1, e2, e3, v2, cell.Color, leftMid);
            }
            else if (cell.HasRiverThroughEdge(direction.Next()))
            {
                Vector3 rightMid = Vector3.Lerp(center, v2, 0.5f);
                TriangulatAdjacentToRiverRight(center, v1, e1, e2, e3, v2, cell.Color, rightMid);
            }
            else
            {
                AddTriangle4(center, v1, e1, e2, e3, v2, cell.Color);
            }
        }
        //平缓转弯
        else if(cell.RiverState == riverState.Dif2)
        {
            if (cell.HasRiverThroughEdge(direction.Previous()) && cell.HasRiverThroughEdge(direction.Next())){
                Vector3 centerX = Vector3.Lerp(center + HexMetrics.GetFirstSolidCornet(direction) * 0.5f,
                    center + HexMetrics.GetSecondSolidCornet(direction) * 0.5f, 0.5f);
                Vector3 mL = Vector3.Lerp(v1, centerX, 0.5f);
                Vector3 m1 = Vector3.Lerp(e1, centerX, 0.5f);
                Vector3 m2 = Vector3.Lerp(e2, centerX, 0.5f);
                Vector3 m3 = Vector3.Lerp(e3, centerX, 0.5f);
                Vector3 mR = Vector3.Lerp(v2, centerX, 0.5f);
                AddTriangle4(centerX, mL, m1, m2, m3, mR, cell.Color);
                AddTrapezoid(mL, m1, m2, m3, mR, v1, e1, e2, e3, v2, cell.Color);
            }
            else if (cell.HasRiverThroughEdge(direction.Previous()))
            {
                Vector3 centerX = Vector3.Lerp(center + HexMetrics.GetFirstSolidCornet(direction.Next()) * 0.25f,
                    center + HexMetrics.GetSecondSolidCornet(direction.Next()) * 0.25f, 0.5f);
                Vector3 leftMid = Vector3.Lerp(v1, centerX, 0.5f);
                TriangulatAdjacentToRiverLeft(centerX, v1, e1, e2, e3, v2, cell.Color, leftMid);
            }
            else if (cell.HasRiverThroughEdge(direction.Next()))
            {
                Vector3 centerX = Vector3.Lerp(center + HexMetrics.GetFirstSolidCornet(direction.Previous()) * 0.25f,
                    center + HexMetrics.GetSecondSolidCornet(direction.Previous()) * 0.25f, 0.5f);
                Vector3 rightMid = Vector3.Lerp(v2, centerX, 0.5f);
                TriangulatAdjacentToRiverRight(centerX, v1, e1, e2, e3, v2, cell.Color, rightMid);
            }
            else
            {
                Vector3 centerX = Vector3.Lerp(center + HexMetrics.GetFirstSolidCornet(direction) * 0.25f,
                    center + HexMetrics.GetSecondSolidCornet(direction) * 0.25f, 0.5f);
                AddTriangle4(centerX, v1, e1, e2, e3, v2, cell.Color);
            }
        }
    }

    void TriangulatAdjacentToRiverLeft(Vector3 center,Vector3 v1, Vector3 e1, Vector3 e2, Vector3 e3, Vector3 v2,
        Color color,Vector3 leftMid)
    {
        AddTriangle(center, e3, v2, color);
        AddTriangle(center, e2, e3, color);
        AddTriangle(leftMid, e2, center, color);
        AddTriangle(leftMid, e1, e2, color);
        AddTriangle(leftMid, v1, e1, color);
    }
    void TriangulatAdjacentToRiverRight(Vector3 center, Vector3 v1, Vector3 e1, Vector3 e2, Vector3 e3, Vector3 v2,
        Color color, Vector3 RightMid)
    {
        AddTriangle(center, v1, e1, color);
        AddTriangle(center, e1, e2, color);
        AddTriangle(RightMid, center, e2, color);
        AddTriangle(RightMid, e2, e3, color);
        AddTriangle(RightMid, e3, v2, color);
    }
    /// <summary>
    /// 对方向进行判断，若方向符合条件则渲染连接桥（四边型）以及对应的三角形
    /// </summary>
    /// <param name="direction">对应处理的方向</param>
    /// <param name="cell">六边形</param>
    /// <param name="v1">六边形内上顶点</param>
    /// <param name="v2">六边形内下顶点</param>
    void TriangulateConnection(HexDirection direction, HexCell cell, Vector3 v1, Vector3 e1, Vector3 e2, Vector3 e3,Vector3 v2)
    {
        if (direction <= HexDirection.SE && cell.GetNeighbor(direction)!=null) { 
            //添加矩形连接桥
            HexCell neighbor = cell.GetNeighbor(direction) ?? cell;
            Vector3 bright = HexMetrics.GetBirdge(direction);
            Vector3 v3 = v1 + bright;
            Vector3 v4 = v2 + bright;

            //将对面连接点添加高度修正
            v3.y = v4.y = neighbor.Position.y;

            //增加多顶点，添加细节
            //e4,e5,e6为对面顶点
            Vector3 e4 = Vector3.Lerp(v3, v4, 0.25f);
            Vector3 e5 = Vector3.Lerp(v3, v4, 0.5f);
            Vector3 e6 = Vector3.Lerp(v3, v4, 0.75f);

            if (cell.HasRiverThroughEdge(direction))
            {
                e5.y = neighbor.StreamBedY;
            }

            //限制只在倾斜时进行阶梯化
            if (cell.GetEdgeType(direction) == HexEdgeType.Slope)
            {
                TriangulateEdgeTerraces(v1, e1, cell, v3, e4, neighbor);
                TriangulateEdgeTerraces(e1, e2, cell, e4, e5, neighbor);
                TriangulateEdgeTerraces(e2, e3, cell, e5, e6, neighbor);
                TriangulateEdgeTerraces(e3, v2, cell, e6, v4, neighbor);
            }
            else
            {
                //AddQuad(v1, v2, v3, v4);
                //AddQuadColor(cell.Color, neighbor.Color);
                AddQuad(v1, e1, v3, e4);
                AddQuadColor(cell.Color, neighbor.Color);
                AddQuad(e1, e2, e4, e5);
                AddQuadColor(cell.Color, neighbor.Color);
                AddQuad(e2,e3,e5,e6);
                AddQuadColor(cell.Color, neighbor.Color);
                AddQuad(e3, v2, e6, v4);
                AddQuadColor(cell.Color, neighbor.Color);
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
        Color c1 = HexMetrics.TerraceLerp(beginCell.Color, endCell.Color, 1);

        AddQuad(beginLeft, beginRight, v1, v2);
        AddQuadColor(beginCell.Color, c1);
        for (int i = 2; i <= HexMetrics.terraceSteps; i++)
        {
            Color c2 = HexMetrics.TerraceLerp(beginCell.Color, endCell.Color, i);
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
        //AddTriangleColor(bottomCell.Color, leftCell.Color, rightCell.Color);
        //bottom最低，然后以顺时针排列
        //注意考虑渲染顶点顺时针排列
        HexEdgeType leftEdgeType = bottomCell.GetEdgeType(leftCell);
        HexEdgeType rightEdgeType = bottomCell.GetEdgeType(rightCell);

        if(leftEdgeType == HexEdgeType.Flat && rightEdgeType == HexEdgeType.Flat)
        {
            TriangulateCornerNoTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
            //AddTriangle(bottom, left, right);
            //AddTriangleColor(bottomCell.Color, leftCell.Color, rightCell.Color);
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
        Color Color1 = HexMetrics.TerraceLerp(c1.Color, cs.Color, 1);
        Color Color2 = HexMetrics.TerraceLerp(c2.Color, cs.Color, 1);
        AddQuad(v3, v4,v1, v2);
        AddQuadColor( Color1, Color2, c1.Color, c2.Color);

        for(int i = 2; i < HexMetrics.terraceSteps; i++)
        {
            Vector3 v33 = HexMetrics.TerraceLerp(v1, vs, i);
            Vector3 v44 = HexMetrics.TerraceLerp(v2, vs, i);
            Color Color3 = HexMetrics.TerraceLerp(c1.Color, cs.Color, i);
            Color Color4 = HexMetrics.TerraceLerp(c2.Color, cs.Color, i);
            AddQuad(v33, v44, v3, v4);
            AddQuadColor(Color3, Color4, Color1, Color2);
            v3 = v33;
            v4 = v44;
            Color1 = Color3;
            Color2 = Color4;
        }

        AddTriangle(v3, v4, vs);
        AddTriangleColor(Color1,Color2,cs.Color);
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
        AddTriangleColor(c1.Color, c2.Color, cc.Color);
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
        Color c1 = HexMetrics.TerraceLerp(bottomCell.Color, leftCell.Color, 1);
        Color c2 = HexMetrics.TerraceLerp(bottomCell.Color, rightCell.Color, 1);
        AddTriangle(bottom, v3, v4);
        AddTriangleColor(bottomCell.Color, c1, c2);

        for(int i = 2; i <= HexMetrics.terraceSteps; i++)
        {
            Vector3 v33 = HexMetrics.TerraceLerp(bottom, left, i);
            Vector3 v44 = HexMetrics.TerraceLerp(bottom, right, i);
            Color c3 = HexMetrics.TerraceLerp(bottomCell.Color, leftCell.Color, i);
            Color c4 = HexMetrics.TerraceLerp(bottomCell.Color, rightCell.Color, i);
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
            Vector3 boundary = Vector3.Lerp(HexMetrics.Perturb(bottom), HexMetrics.Perturb(left), b);
            Color boundaryColor = Color.Lerp(bottomCell.Color, leftCell.Color, b);
            if(rightCell.GetEdgeType(leftCell) == HexEdgeType.Slope)
            {
                TriangulateTerraces(right, rightCell.Color, bottom, bottomCell.Color, boundary,boundaryColor);
                TriangulateTerraces(left, leftCell.Color, right, rightCell.Color, boundary,boundaryColor);
            }
            else
            {
                TriangulateTerraces(right, rightCell.Color, bottom, bottomCell.Color, boundary, boundaryColor);
                AddTriangleNoPerturb(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
                AddTriangleColor(leftCell.Color, rightCell.Color, boundaryColor);
            }
        }
        else
        {
            float b = 1f / (rightCell.Elevation - bottomCell.Elevation);
            Vector3 boundary = Vector3.Lerp(HexMetrics.Perturb(bottom), HexMetrics.Perturb(right), b);
            Color boundaryColor = Color.Lerp(bottomCell.Color, rightCell.Color, b);
            if (rightCell.GetEdgeType(leftCell) == HexEdgeType.Slope)
            {
                TriangulateTerraces(bottom, bottomCell.Color,left,leftCell.Color, boundary, boundaryColor);
                TriangulateTerraces(left, leftCell.Color, right, rightCell.Color, boundary, boundaryColor);
            }
            else
            {
                TriangulateTerraces(bottom, bottomCell.Color, left, leftCell.Color, boundary, boundaryColor);
                AddTriangleNoPerturb(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
                AddTriangleColor(leftCell.Color, rightCell.Color, boundaryColor);
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
        Vector3 pbegin = HexMetrics.Perturb(begin);
        //Vector3 pend = HexMetrics.Perturb(end);
        Vector3 v1 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, end, 1));
        Color c1 = HexMetrics.TerraceLerp(beginColor, endColor, 1);
        AddTriangleNoPerturb(pbegin, v1, target);
        AddTriangleColor(beginColor, c1, targetColor);
        for (int i = 2; i <= HexMetrics.terraceSteps; i++)
        {
            Vector3 v11 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, end, i));
            Color c2 = HexMetrics.TerraceLerp(beginColor, endColor, i);
            AddTriangleNoPerturb(v1, v11, target);
            AddTriangleColor(c1, c2, targetColor);
            v1 = v11;
            c1 = c2;
        }
    }

    //添加4分三角形
    void AddTriangle4(Vector3 center,Vector3 v1,Vector3 e1,Vector3 e2,
        Vector3 e3,Vector3 v2,Color color)
    {
        AddTriangle(center, v1, e1);
        AddTriangleColor(color);

        AddTriangle(center, e1, e2);
        AddTriangleColor(color);

        AddTriangle(center, e2, e3);
        AddTriangleColor(color);

        AddTriangle(center, e3, v2);
        AddTriangleColor(color);
    }

    //添加梯形
    //对于倒三角形时，从下往上
    void AddTrapezoid(Vector3 v1,Vector3 a1,Vector3 a2,Vector3 a3,Vector3 v2,
        Vector3 v3,Vector3 e1,Vector3 e2,Vector3 e3,Vector3 v4,Color color)
    {
        AddQuad(v1, a1, v3, e1);
        AddQuadColor(color);
        AddQuad(a1, a2, e1, e2);
        AddQuadColor(color);
        AddQuad(a2, a3, e2, e3);
        AddQuadColor(color);
        AddQuad(a3, v2, e3, v4);
        AddQuadColor(color);
    }

    //添加一个三角形，顺便添加一种纯色
    void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3,Color color)
    {
        AddTriangle(v1, v2, v3);
        AddTriangleColor(color);
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
        vertices.Add(HexMetrics.Perturb(v1));
        vertices.Add(HexMetrics.Perturb(v2));
        vertices.Add(HexMetrics.Perturb(v3));
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
    void AddTriangleColor(Color Color0,Color Color1,Color Color2)
    {
        colors.Add(Color0);
        colors.Add(Color1);
        colors.Add(Color2);
    }

    //添加三角形颜色（纯色）
    void AddTriangleColor(Color Color)
    {
        colors.Add(Color);
        colors.Add(Color);
        colors.Add(Color);
    }

    void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,Color color)
    {
        AddQuad(v1, v2, v3, v4);
        AddQuadColor(color);
    }

    //为四边形添加顶点
    void AddQuad(Vector3 v1,Vector3 v2,Vector3 v3,Vector3 v4)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(HexMetrics.Perturb(v1));
        vertices.Add(HexMetrics.Perturb(v2));
        vertices.Add(HexMetrics.Perturb(v3));
        vertices.Add(HexMetrics.Perturb(v4));

        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 3);
    }

    void AddQuadColor(Color c1)
    {
        for(int i = 0; i < 4; i++)
        {
            colors.Add(c1);
        }
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



}
