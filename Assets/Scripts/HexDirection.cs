using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum HexDirection
{
    NE,E,SE,SW,W,NW                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      
}

//扩展方法的第一个参数之前必须有this关键字
//这个特性允许我们在任何东西上添加方法
public static class HexDirectionExtensions
{
    public static HexDirection Opposite(this HexDirection direction)
    {
        return (int)direction < 3 ? (direction + 3) : (direction - 3);
    }

    public static HexDirection Previous(this HexDirection direction)
    {
        return direction == HexDirection.NE ? HexDirection.NW : (direction - 1);
    }

    public static HexDirection Next(this HexDirection direction)
    {
        return direction == HexDirection.NW ? HexDirection.NE : (direction + 1);
    }
    public static HexDirection Next2(this HexDirection direction)
    {
        if (direction == HexDirection.NW)
        {
            return HexDirection.E;
        }else if (direction == HexDirection.W)
        {
            return HexDirection.NE;
        }
        else
        {
            return (direction + 2);
        }
    }
    public static HexDirection Previous2(this HexDirection direction)
    {
        if (direction == HexDirection.NE)
        {
            return HexDirection.W;
        }else if (direction == HexDirection.E)
        {
            return HexDirection.NW;
        }
        else
        {
            return direction - 2;
        }
    }
}