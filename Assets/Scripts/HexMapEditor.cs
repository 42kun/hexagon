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

    //检测拖拽相关
    /// <summary>
    /// 关于拖拽，即在保持鼠标点击的情况下，单元格沿鼠标方向变化即为拖拽
    /// 那么首先，鼠标如果离开单元格，拖拽停止，isDrag=false
    /// 鼠标在单元格时，先记录previousCell，然后检测，如果目前的cell不等于previousCell，那么我们即可认为发生了拖拽，isDrag=True
    /// 每帧只会进行一次拖拽判定
    /// </summary>
    //上一个点击到的单元格
    HexCell previousCell;
    //拖拽方向
    HexDirection dragDirection;

    [SerializeField]
    //在鼠标已经点击地图的情况下，这个鼠标动作是否为拖拽
    bool isDrag = false;


    //笔刷大小
    public int brushSize = 1;

    enum OptionalToggle
    {
        Ignore,Yes,No
    }
    OptionalToggle riverMode;
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
        else
        {
            //某时刻鼠标并没有点击屏幕，则将拖拽变量全部关闭
            previousCell = null;
            isDrag = false;
        }
    }

    //对于鼠标点击的处理
    void HandleInput()
    {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition); //在鼠标点击位置画一条射线
        RaycastHit hit; //接收射线与物体碰撞信息
        if(Physics.Raycast(inputRay,out hit)) //out表示引用
        {
            HexCell currenCell = hexGrid.GetCell(hit.point);
            //拖拽检测，首先如果存在上一个六边形，且上一个不是这一个，那么就认为发生了拖拽
            if(previousCell && previousCell != currenCell)
            {
                //如果鼠标一直点在屏幕上，且在某时刻切换了一次点击六边形，那么就进行拖拽判定
                //如果判定成功，那么就可以通过previousCell与dragDirection获取河流走向
                ValidateDrag(currenCell);
            }
            else
            {
                //如果只是一直点击一个六边形而不是拖拽，那么isDrag为Fasle
                isDrag = false;
            }
            EditCells(currenCell);
            previousCell = currenCell;
        }
        else
        {
            //一旦没有点击到正确的六边形就将上一个六边形赋值为空
            previousCell = null;
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
        if(riverMode == OptionalToggle.No)
        {
            cell.RemoveRiver();
        }
        if (previousCell && isDrag && riverMode == OptionalToggle.Yes)
        {
            //如果判定为正在拖拽且开启了河流编辑，那么就在dragDirection方向上创造出一条流出的河流
            previousCell.SetOutgoingRiver(dragDirection);

            //一些似乎没什么意义的代码。河流为什么需要笔刷！
            HexCell otherCell = cell.GetNeighbor(dragDirection.Opposite());
            if (otherCell)
            {
                otherCell.SetOutgoingRiver(dragDirection);
            }
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

    //通过UI选项，设置笔刷大小
    public void SetRiverMode(int mode)
    {
        riverMode = (OptionalToggle)mode;
    }

    //拖拽处理
    public void ValidateDrag(HexCell currentCell)
    {
        for(int i = 0; i < 6; i++)
        {
            if (previousCell.GetNeighbor((HexDirection)i) == currentCell)
            {
                dragDirection = (HexDirection)i;
                isDrag = true;
                return;
            }
        }
        isDrag = false;
    }
}
