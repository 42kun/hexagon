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
        colors = new Color[] { Color.white, Color.black, Color.yellow, Color.blue };
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
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if(Physics.Raycast(inputRay,out hit))
        {
            hexGrid.ColorCell(hit.point, activeColor);
        }
    }

    public void SelectColor(int index)
    {
        activeColor = colors[index];
    }
}
