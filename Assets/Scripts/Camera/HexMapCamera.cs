using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexMapCamera : MonoBehaviour
{
    public HexGrid hexGrid;


    /// <summary>
    /// swivel 调整角度
    /// stick 调整视距
    /// </summary>
    Transform swivel, stick;


    /// <summary>
    /// 最远角度，最近角度
    /// </summary>
    public float swivelMinZoom = 90, swivelMaxZoom = 45;

    // 记录视距，0为最远，1为最近
    float zoom = 1f;
    //最远距离，最近距离
    public float stickMinZoom = -250, StickMaxZoom = -45;

    // 最远速度，最近速度
    public float moveSpeedMinZoom = 400, moveSpeedMaxZoom = 100;


    // 旋转速度
    public float rotationSpeed = 100;
    //记录旋转角度
    public float rotationAngle;

    private void Awake()
    {
        swivel = transform.GetChild(0);
        stick = swivel.GetChild(0);
    }

    void Start()
    {
        
    }


    void Update()
    {

        //获取鼠标滚轮输入，设置缩放距离，同时控制缩放速度与角度
        float zoomDelta = Input.GetAxis("Mouse ScrollWheel");
        if(Mathf.Abs(zoomDelta) >= 0.01f)
        {
            AdjustZoom(zoomDelta);
        }

        //获取方向键输入，控制摄像机x,z坐标
        float xDelta = Input.GetAxis("Horizontal");
        float zDelta = Input.GetAxis("Vertical");
        if(Mathf.Abs(xDelta)>=0.01 || Mathf.Abs(zDelta) >= 0.01)
        {
            AdjustPosition(xDelta, zDelta);
        }

        //获取Q，E输入，调整角度
        float rotationDelta = Input.GetAxis("Rotation");
        AdjustRotation(rotationDelta);

    }

    /// <summary>
    /// 接收增量，调整视距，然后把它限制在0-1之间
    /// 调整镜头距离
    /// 调整角度
    /// </summary>
    /// <param name="delta"></param>
    void AdjustZoom(float delta)
    {
        zoom = Mathf.Clamp01(zoom + delta);
        float distance = Mathf.Lerp(stickMinZoom, StickMaxZoom, zoom);
        stick.localPosition = new Vector3(0f, 0f, distance);

        float angle = Mathf.Lerp(swivelMinZoom, swivelMaxZoom, zoom);
        swivel.localRotation = Quaternion.Euler(angle, 0f, 0f);
    }


    /// <summary>
    /// 控制前后左右移动（暂不考虑旋转）
    /// </summary>
    /// <param name="xDelta"></param>
    /// <param name="zDelta"></param>
    void AdjustPosition(float xDelta,float zDelta)
    {

        Vector3 direction = transform.localRotation * new Vector3(xDelta, 0, zDelta).normalized;
        Vector3 position = transform.localPosition;
        float speed = Mathf.Lerp(moveSpeedMinZoom, moveSpeedMaxZoom, zoom);
        //阻尼系数，用来平滑移动
        float damping = Mathf.Max(Mathf.Abs(xDelta), Mathf.Abs(zDelta));
        //每帧移动向量
        position += direction * speed * damping * Time.deltaTime;
        transform.localPosition = ClampPosition(position);
    }

    /// <summary>
    /// 限制相机在地图内部移动
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    Vector3 ClampPosition(Vector3 position)
    {
        float xMax = (hexGrid.cellCountX - 1f) * HexMetrics.innerRadius * 2f;
        float zMax = (hexGrid.cellCountZ - 1f) * HexMetrics.outerRadius * 1.5f;

        position.x = Mathf.Clamp(position.x, 0, xMax);
        position.z = Mathf.Clamp(position.z, 0, zMax);
        return position;
    }

    /// <summary>
    /// 控制地图旋转
    /// </summary>
    /// <param name="delta"></param>
    void AdjustRotation(float delta) {
        rotationAngle += delta * rotationSpeed * Time.deltaTime;//旋转的角度
        rotationAngle = rotationAngle % 360;//将角度限制在0-360中
        transform.localRotation = Quaternion.Euler(0, rotationAngle, 0);
    }
}
