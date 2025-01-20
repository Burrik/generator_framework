using UnityEditor;
using UnityEngine;
using Generate.Core;

namespace Generate.Core.Attributes
{
[CustomPropertyDrawer(typeof(OptionalLayersAttribute))]
public class OptionalLayersDrawer : PropertyDrawer
{
  public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
  {
    return EditorGUIUtility.singleLineHeight;
  }

  public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
  {
    var defaultColor = GUI.color;
    if(property.objectReferenceValue == null)
      GUI.color = new Color(1, 1, 1, 0.6f);

    EditorGUI.PropertyField(position, property, label);

    if(property.objectReferenceValue == null)
    {
      var iconRect = position;
      iconRect.x += EditorGUIUtility.labelWidth - EditorGUIUtility.singleLineHeight;
      iconRect.width = EditorGUIUtility.singleLineHeight;

      var tooltipContent = new GUIContent(
        EditorGUIUtility.IconContent("console.infoicon").image,
        "Optional: This process can work without layers");

      EditorGUI.LabelField(iconRect, tooltipContent);
    }

    GUI.color = defaultColor;
  }
}
}