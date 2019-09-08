using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(HexCoordinates))] //指定显示位置？
public class HexCoordinatesDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        int x = property.FindPropertyRelative("x").intValue;
        int z = property.FindPropertyRelative("z").intValue;
        HexCoordinates coordinates = new HexCoordinates(x, z); //生成HexCoordinates类，以便于调用ToString方法
        position = EditorGUI.PrefixLabel(position, label); //添加标签名
        GUI.Label(position, coordinates.ToString());
    }
}
