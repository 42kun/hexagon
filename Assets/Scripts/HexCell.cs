using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum riverState
{
    None, Dif1, Dif2,Dif3
}

public class HexCell : MonoBehaviour
{
    public HexCoordinates coordinates;
    public RectTransform uiRect;

    public HexGridChunk chunk;

    //本单元格是否有流入流出的河流与河流的流入/出方向
    bool hasIncomingRiver, hasOutgoingRiver;
    HexDirection incomingRiver, outgoingRiver;

    //本单元格如果有河床那么河床绝对高度应该是多少
    public float StreamBedY
    {
        get
        {
            return
                (elevation + HexMetrics.streamBedElevationOffset) * HexMetrics.elevationStep;
        }
    }

    public bool HasIncomingRiver
    {
        get
        {
            return hasIncomingRiver;
        }
        set
        {
            hasIncomingRiver = value;
        }
    }

    public bool HasOutgoingRiver
    {
        get
        {
            return hasOutgoingRiver;
        }
        set
        {
            hasOutgoingRiver = value;
        }
    }
    public HexDirection IncomingRiver
    {
        get
        {
            return incomingRiver;
        }
        set
        {
            incomingRiver = value;
        }
    }

    public HexDirection OutgoingRiver
    {
        get
        {
            return outgoingRiver;
        }
        set
        {
            outgoingRiver = value;
        }
    }

    //单元格是否有河流
    public bool HasRiver
    {
        get
        {
            return hasIncomingRiver || hasOutgoingRiver;
        }
    }

    public bool HasRiverBeginOrEnd
    {
        get
        {
            return hasIncomingRiver ^ hasOutgoingRiver;
        }
    }

    //河流是否流过本方向
    public bool HasRiverThroughEdge(HexDirection direction)
    {
        return
            hasIncomingRiver && incomingRiver == direction ||
            hasOutgoingRiver && outgoingRiver == direction;
    }

    //获取流域内河流夹角，如果不存在两条河流，则返回None
    public riverState RiverState
    {
        get
        {
            if (hasIncomingRiver && hasOutgoingRiver)
            {
                int bigger = (int)incomingRiver >= (int)outgoingRiver ? (int)incomingRiver : (int)outgoingRiver;
                int smaller = (int)incomingRiver < (int)outgoingRiver ? (int)incomingRiver : (int)outgoingRiver;
                int x = bigger - smaller;
                if (x == 1 || x == 5)
                {
                    return (riverState)1;
                }else if(x == 2 || x == 4){
                    return (riverState)2;
                }
                else
                {
                    return (riverState)3;
                }
            }
            return riverState.None;
        }
    }
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


            //在改变高度时修正所有不正常的河流
            //检测四周流入六边形，若自身高度大于周围，则将流入河流截断

            if(hasOutgoingRiver && elevation < GetNeighbor(outgoingRiver).Elevation)
            {
                RemoveOutgoingRiver();
            }
            if(hasIncomingRiver && elevation > GetNeighbor(incomingRiver).Elevation)
            {
                RemoveIncomingRiver();
            }

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

    //给河流使用，仅刷新自身单元格（因为添加河流时不需要改变周围单元格）
    public void RefreshSelfOnly()
    {
        chunk.Refresh();
    }

    public void RemoveRiver()
    {
        RemoveIncomingRiver();
        RemoveOutgoingRiver(); 
    }


    //移除流出河流且将对应相邻单元格河流截断
    public void RemoveOutgoingRiver()
    {
        if (!hasOutgoingRiver) return;
        HasOutgoingRiver = false;
        RefreshSelfOnly();
        HexCell neighbor = GetNeighbor(outgoingRiver);
        if (neighbors == null) return;
        neighbor.HasIncomingRiver = false;
        neighbor.RefreshSelfOnly();
    }
    
    //移出流入河流并将对应相邻单元格河流截断
    public void RemoveIncomingRiver()
    {
        if (!hasIncomingRiver) return;
        HasIncomingRiver = false;
        RefreshSelfOnly();
        HexCell neighbor = GetNeighbor(incomingRiver);
        if (neighbors == null) return;
        //移出邻居的流出河流
        neighbor.HasOutgoingRiver = false;
        neighbor.RefreshSelfOnly();
    }


    //添加一条流入的河流，考虑所有情况
    //仅会处理自己，同时检测外部，外部有问题则不会执行
   

    public void SetOutgoingRiver(HexDirection direction)
    {
        //排除掉不用工作的情况
        if(hasOutgoingRiver && outgoingRiver == direction)
        {
            return;
        }

        HexCell outCell = GetNeighbor(direction);
        //排除掉不能添加流出河流的情况
        if (outCell == null || outCell.Elevation > elevation) 
        {
            return;
        }
        
        //如果有流入河流且流出河流占用了流入河流，则移出流入河流
        if(hasIncomingRiver && direction == incomingRiver)
        {
            RemoveIncomingRiver();
        }
        //为本单元格添加流出河流
        RemoveOutgoingRiver();
        HasOutgoingRiver = true;
        outgoingRiver = direction;
        RefreshSelfOnly();

        //为对应方向单元格添加流入河流
        //由于肯定会流入河流，那么原来的incomingRiver可以移除了
        outCell.RemoveIncomingRiver();
        outCell.HasIncomingRiver = true;
        outCell.incomingRiver = direction.Opposite();
        outCell.RefreshSelfOnly();
    }
    
    // 靠扩散很难解决问题，主要是时间复杂度
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

