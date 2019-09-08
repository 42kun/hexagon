using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexCell : MonoBehaviour
{
    public HexCoordinates coordinates;
    public Color color;

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


}