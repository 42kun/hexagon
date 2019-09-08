using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class HexMapEditor : MonoBehaviour
{
    public Color[] colors;
    public HexGrid hexGrid;
    private Color activeColor;

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
            hexGrid.ColorCell(hit.point, activeColor);
        }
    }

    public void SelectColor(int index)
    {
        activeColor = colors[index];
    }
}
