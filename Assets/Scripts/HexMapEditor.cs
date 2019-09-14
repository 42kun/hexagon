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

    public Slider elevationSlider;
    public Slider brushSlider;

    //是否显示标签
    public bool showUI = false;
    //是否开启高度编辑功能
    public bool editElevation = true;
    //是否开启颜色编辑功能
    public bool editColor = true;

    public Toggle labelSwitch;
    public Toggle elevationSwitch;


    //笔刷大小
    public int brushSize = 1;


    private void Awake()
    {
        colors = new Color[] { Color.white, Color.red, Color.yellow, Color.blue };
        SelectColor(0);
    }

    private void Update()
    {
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
            EditCells(hexGrid.GetCell(hit.point));

        }
    }

    public void SelectColor(int index)
    {
        if (index >= 0)
        {
            editColor = true;
            activeColor = colors[index];
        }
        else
        {
            editColor = false;
        }
    }

    public void SetElevation()
    {
        if (editElevation)
        {
            activeElevation = (int)elevationSlider.value;
        }
    }

    // 编辑网格，根据从点击坐标通过hexGrid.GetCell推算出的网格来更新
    void EditCells(HexCell cell)
    {
        //根据笔刷大小，批量更新网格
        //cell.EditCell(brushSize, activeElevation, activeColor);

        int centerX = cell.coordinates.X;
        int centerZ = cell.coordinates.Z;
        int weight = brushSize - 1;

        for(int i = 0; i <= weight; i++)
        {
            for(int j = 0; j <= weight; j++)
            {
                if (Mathf.Abs(i + j) <= weight) EditCell(hexGrid.GetCell(centerX + i, centerZ + j));
                if (Mathf.Abs(-i + j) <= weight)  EditCell(hexGrid.GetCell(centerX - i, centerZ + j));
                if (Mathf.Abs(-i - j) <= weight) EditCell(hexGrid.GetCell(centerX - i, centerZ - j));
                if (Mathf.Abs(i - j) <= weight) EditCell(hexGrid.GetCell(centerX + i, centerZ - j));
            }
        }
    }

    void EditCell(HexCell cell)
    {
        if (cell == null)
        {
            return;
        }
        if (editElevation)
        {
            cell.Elevation = activeElevation;
        }
        if (editColor)
        {
            cell.Color = activeColor;
        }
    }

    // 设置UI是否显示
    public void ShouUI()
    {
        showUI = labelSwitch.isOn;
        hexGrid.ShowUI(showUI);
    }

    // 设置是否开启地形编辑
    public void EditElevation()
    {
        editElevation = elevationSwitch.isOn;
        activeElevation = (int)elevationSlider.value;
    }

    //设置笔刷大小
    public void SetBrushSize()
    {
        brushSize = (int)brushSlider.value;
    }
}
