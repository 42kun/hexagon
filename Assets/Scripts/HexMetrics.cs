using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum HexEdgeType
{
    Flat, Slope, Cliff
}
public static class HexMetrics
{
    //外径
    public const float outerRadius = 10f;
    //内径
    public const float innerRadius = outerRadius * 0.866025404f;

    //内部非混合部分
    public const float solidFactor = 0.8f;
    //内部混合部分
    public const float blendFactor = 1 - solidFactor;

    //标准高度
    public const float elevationStep = 3f;
    //每个斜坡插值数量（梯形数量）
    public const int terracesPerSlope = 2;
    //每个斜坡由于插值被划分的数量（每个梯形占两个部分，最后一个尖角占一个部分）
    public const int terraceSteps = terracesPerSlope * 2 + 1;
    //每步水平方向上的比例
    public const float horizontalTerraceStepSize = 1f / terraceSteps;
    //每步垂直方向上的比例
    public const float verticalTerraceStepSize = 1f / (terracesPerSlope + 1);

    //噪声贴图
    public static Texture2D noiseSource;
    //扰动强度
    public const float cellPerturbStrength = 4f;
    //缩放采样比例
    public const float noiseScale = 0.003f;
    //单元格海拔高度扰动
    public const float elevationPerturbStrength = 1.5f;

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

    /// <summary>
    /// 生成插值每步的坐标
    /// </summary>
    /// <param name="a">插值起点</param>
    /// <param name="b">插值终点</param>
    /// <param name="step">插值时的步数</param>
    /// <returns></returns>
    public static Vector3 TerraceLerp(Vector3 a,Vector3 b,int step)
    {
        float h = step * HexMetrics.horizontalTerraceStepSize;
        a.x += (b.x - a.x) * h;
        a.z += (b.z - a.z) * h;
        //只在奇数步上将y之提高
        float v = ((step + 1) / 2) * HexMetrics.verticalTerraceStepSize;
        a.y += (b.y - a.y) * v;
        return a;
    }

    /// <summary>
    /// 生成插值每步的颜色
    /// </summary>
    /// <param name="a">起点颜色</param>
    /// <param name="b">终点颜色</param>
    /// <param name="step">步数</param>
    /// <returns></returns>
    public static Color TerraceLerp(Color a,Color b,int step)
    {
        float h = step * HexMetrics.horizontalTerraceStepSize;
        return Color.Lerp(a, b, h);
    }

    //输入两个高度，返回其相对边界类型
    public static HexEdgeType GetHexEdgeType(int elevation1,int elevation2)
    {
        int delta = System.Math.Abs(elevation1 - elevation2);
        switch (delta)
        {
            case 0:return HexEdgeType.Flat;
            case 1:return HexEdgeType.Slope;
            default:return HexEdgeType.Cliff;
        }
    }

    //双线性过滤
    public static Vector4 SampleNoise(Vector3 position)
    {
        return noiseSource.GetPixelBilinear(position.x * noiseScale, position.z * noiseScale);
    }


}
