using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexMapCamera : MonoBehaviour
{
    /// <summary>
    /// swivel 调整角度
    /// stick 调整视距
    /// </summary>
    Transform swivel, stick;

    public float swivelMinZoom = 90, swivelMaxZoom = 45;

    /// <summary>
    /// 记录视距，0为最远，1为最近
    /// </summary>
    float zoom = 1f;
    //最远距离，最近距离
    public float stickMinZoom = -250, StickMaxZoom = -45;

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
        float zoomDelta = Input.GetAxis("Mouse ScrollWheel");
        if(Mathf.Abs(zoomDelta) >= 0.01f)
        {
            AdjustZoom(zoomDelta);
        }
    }

    /// <summary>
    /// 接收增量，调整视距，然后把它限制在0-1之间
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
}
