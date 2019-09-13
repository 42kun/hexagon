using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexCell : MonoBehaviour
{
    public HexCoordinates coordinates;
    public RectTransform uiRect;

    public HexGridChunk chunk;

    //高度，私有变量
    int elevation = int.MinValue;

    //设置高度
    public int Elevation
    {
        get
        {
            return elevation;
        }
        set
        {
            //在高度改变时刷新区块
            if(elevation == value)
            {
                return;
            }
            elevation = value;
            Vector3 position = transform.localPosition;
            position.y = value * HexMetrics.elevationStep;
            position.y += (HexMetrics.SampleNoise(position).y*2f - 1f)*HexMetrics.elevationPerturbStrength;
            transform.localPosition = position;

            Vector3 uiPosition = uiRect.localPosition;
            uiPosition.z = -position.y;
            uiRect.localPosition = uiPosition;
            Refresh();
        }
    }

    //设置颜色
    Color color;
    public Color Color
    {
        get
        {
            return color;
        }
        set
        {
            //在颜色改变时刷新网格
            if(color == value)
            {
                return;
            }
            color = value;
            Refresh();
        }
    }

    //将cell的position单独拎出来
    public Vector3 Position
    {
        get
        {
            return transform.localPosition;
        }
    }


    [SerializeField]
    HexCell[] neighbors;

    //获取临近单元格
    public HexCell GetNeighbor (HexDirection direction)
    {
        return neighbors[(int)direction];
    }

    //设置临近单元格
    public void SetNeighbor(HexDirection direction,HexCell cell)
    {
        neighbors[(int)direction] = cell;
        cell.neighbors[(int)direction.Opposite()] = this;
    }

    //获取边界类型
    public HexEdgeType GetEdgeType (HexDirection d)
    {
        return HexMetrics.GetHexEdgeType(
            elevation,
            neighbors[(int)d].Elevation);
    }

    public HexEdgeType GetEdgeType(HexCell c)
    {
        return HexMetrics.GetHexEdgeType(Elevation, c.Elevation);
    }

    //会重新刷新本区块网格
    void Refresh()
    {
        //防止在初始化时刷新网格
        //cell初始化完毕后才会添加chunk。这样可以很好的避免初始化时的操作触发网格刷新
        //没有初始化完六边形有时会获得不到某方向上的cell，导致空引用

        //私货：如果边缘发生更改，那么则将边缘邻居的refreshEnable设置为true
        if (chunk)
        {
            chunk.Refresh();
            for (int i = 0; i < neighbors.Length; i++)
            {
                if (neighbors[i] != null && neighbors[i].chunk != chunk)
                {
                    neighbors[i].chunk.refreshEnable = true;
                }
            }
        }

    }

    // 靠扩散很难解决问题
    //public void EditCell(int weight,int elevation,Color color)
    //{
    //    Debug.Log(weight);
    //    weight--;wwdww
    //    Elevation = elevation;
    //    Color = color;
    //    if (weight >= 1)
    //    {
    //        for (int j = 0; j < neighbors.Length; j++)
    //        {
    //            if (neighbors[j] != null)
    //            {
    //                neighbors[j].EditCell(weight, elevation, color);
    //            }
    //        }
    //    }
    //}

}