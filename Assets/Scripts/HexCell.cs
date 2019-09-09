﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexCell : MonoBehaviour
{
    public HexCoordinates coordinates;
    public Color color;
    public RectTransform uiRect;

    //高度，私有变量
    int elevation;

    //设置高度
    public int Elevation
    {
        get
        {
            return elevation;
        }
        set
        {
            elevation = value;
            Vector3 position = transform.localPosition;
            position.y = value * HexMetrics.elevationStep;
            transform.localPosition = position;

            Vector3 uiPosition = uiRect.localPosition;
            uiPosition.z = elevation * -HexMetrics.elevationStep;
            uiRect.localPosition = uiPosition;
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
    public HexMetrics.HexEdgeType GetEdgeType (HexDirection d)
    {
        return HexMetrics.GetHexEdgeType(
            elevation,
            neighbors[(int)d].Elevation);
    }

}