using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HexMapEditor : MonoBehaviour
{
    public Color[] colors;
    public HexGrid hexGrid;
    //当前选中的颜色
    private Color activeColor;
    //当前选中的海拔高度
    private int activeElevation;

    public Slider slider;


    private void Awake()
    {
        colors = new Color[] { Color.white, Color.red, Color.yellow, Color.blue };
        SelectColor(0);
    }

    private void Update()
    {
        //Debug.Log(EventSystem.current.IsPointerOverGameObject());
        if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject()) 
        {
            HandleInput();
        }
    }
    void HandleInput()
    {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition); //在鼠标点击位置画一条射线
        RaycastHit hit; //接收射线与物体碰撞信息
        if(Physics.Raycast(inputRay,out hit)) //out表示引用
        {
            EditCell(hexGrid.GetCell(hit.point));

        }
    }

    public void SelectColor(int index)
    {
        activeColor = colors[index];
    }

    public void SetElevation()
    {
        Debug.Log(slider.value);
        activeElevation = (int)slider.value;
    }

    void EditCell(HexCell cell)
    {
        cell.color = activeColor;
        cell.Elevation = activeElevation;
        hexGrid.Refresh();
    }
}
