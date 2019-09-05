using UnityEngine;
using UnityEditor;

//说实话，这到底在写什么，我一点也不知道
//我只知道，结果是，它会有一个更好的显示

[CustomPropertyDrawer(typeof(HexCoordinates))]
public class HexCoordinatesDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        int x = property.FindPropertyRelative("x").intValue;
        int z = property.FindPropertyRelative("z").intValue;
        HexCoordinates coordinates = new HexCoordinates(x, z);

        position = EditorGUI.PrefixLabel(position, label);
        GUI.Label(position, coordinates.ToString());
    }
}