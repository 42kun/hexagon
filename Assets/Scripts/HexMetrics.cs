using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HexMetrics
{
    //外径
    public const float outerRadius = 10f;
    //内径
    public const float innerRadius = outerRadius * 0.866025404f;

    //内部非混合部分
    public const float solidFactor = 0.75f;
    //内部混合部分
    public const float blendFactor = 1 - solidFactor;


    //六顶点坐标
    static Vector3[] corners =
    {
        new Vector3(0f,0f,outerRadius),
        new Vector3(innerRadius,0f,0.5f * outerRadius),
        new Vector3(innerRadius,0f,-0.5f * outerRadius),
        new Vector3(0f,0f,-outerRadius),
        new Vector3(-innerRadius,0f,-0.5f * outerRadius),
        new Vector3(-innerRadius,0f,0.5f * outerRadius),
        new Vector3(0f,0f,outerRadius)
    };

    //获取当前三角形上顶点
    public static Vector3 GetFirstCornet(HexDirection direction)
    {
        return corners[(int)direction];
    }

    //获取当前三角形下顶点
    public static Vector3 GetSecondCornet(HexDirection direction)
    {
        return corners[(int)direction+1];
    }


    //获取三角形内部上顶点
    public static Vector3 GetFirstSolidCornet(HexDirection direction)
    {
        return corners[(int)direction] * solidFactor;
    }

    //获取三角形内部下顶点
    public static Vector3 GetSecondSolidCornet(HexDirection direction)
    {
        return corners[(int)direction + 1] * solidFactor;
    }

    //获取连接桥向量
    public static Vector3 GetBirdge(HexDirection d)
    {
        return (corners[(int)d] + corners[(int)d + 1]) * blendFactor;
    }



}
