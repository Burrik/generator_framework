using UnityEngine;
using UnityEditor;

namespace Generate.Core.Editor
{
  /// <summary>
  /// Стандартные иконки, используемые в редакторах генератора.
  /// </summary>
  public static class GeneratorEditorStyles
  {
    /// <summary>
    /// Цветовая палитра для UI элементов.
    /// </summary>
    public static class Colors
    {
      public static readonly Color Separator = new Color(0.5f, 0.5f, 0.5f, 0.5f);
      public static readonly Color Hover = new Color(0.7f, 0.7f, 0.7f, 0.1f);
      public static readonly Color Selected = new Color(0.3f, 0.5f, 0.9f, 0.3f);
      public static readonly Color Warning = new Color(0.9f, 0.6f, 0.1f, 0.5f);
      public static readonly Color Error = new Color(0.9f, 0.3f, 0.2f, 0.5f);
      public static readonly Color ProcessBackground = new Color(0.2f, 0.2f, 0.2f, 1f);
      public static readonly Color DisabledBackground = new Color(0.15f, 0.15f, 0.15f, 1f);
      public static readonly Color ConnectionLine = new Color(0.7f, 0.7f, 0.7f, 1f);
    }

    /// <summary>
    /// Иконки для UI элементов.
    /// </summary>
    public static class Icons
    {
      public static readonly GUIContent Up = EditorGUIUtility.IconContent("d_ScrollUp");
      public static readonly GUIContent Down = EditorGUIUtility.IconContent("d_ScrollDown");
      public static readonly GUIContent Delete = EditorGUIUtility.IconContent("d_TreeEditor.Trash");
      public static readonly GUIContent Add = EditorGUIUtility.IconContent("d_CreateAddNew");
      public static readonly GUIContent Generate = EditorGUIUtility.IconContent("d_PlayButton");
      public static readonly GUIContent Refresh = EditorGUIUtility.IconContent("d_Refresh");
      public static readonly GUIContent Settings = EditorGUIUtility.IconContent("d_Settings");
      public static readonly GUIContent EyeVisible = EditorGUIUtility.IconContent("d_scenevis_visible_hover");
      public static readonly GUIContent EyeHidden = EditorGUIUtility.IconContent("d_scenevis_hidden_hover");
      public static readonly GUIContent LinkedProcesses = EditorGUIUtility.IconContent("d_Linked");
      public static readonly GUIContent UnlinkedProcesses = EditorGUIUtility.IconContent("d_Unlinked");

      private static Texture2D link;
      public static Texture2D Link => link ??= EditorGUIUtility.FindTexture("d_Link Icon");
    }

    /// <summary>
    /// Предопределенные стили UI элементов.
    /// </summary>
    public static class Styles
    {
      private static GUIStyle s_Header;
      public static GUIStyle Header => s_Header ??= new GUIStyle(EditorStyles.boldLabel)
      {
        fontSize = 12,
        margin = new RectOffset(0, 0, 10, 5)
      };

      private static GUIStyle s_Button;
      public static GUIStyle Button => s_Button ??= new GUIStyle(GUI.skin.button)
      {
        padding = new RectOffset(10, 10, 5, 5)
      };

      private static GUIStyle s_IconButton;
      public static GUIStyle IconButton => s_IconButton ??= new GUIStyle(EditorStyles.miniButton)
      {
        padding = new RectOffset(2, 2, 2, 2)
      };

      private static GUIStyle s_GenerateViewLabel;
      public static GUIStyle GenerateViewLabel => s_GenerateViewLabel ??= new GUIStyle(EditorStyles.label)
      {
        alignment = TextAnchor.MiddleLeft,
        fixedHeight = 25
      };

      private static GUIStyle s_ToolbarButton;
      public static GUIStyle ToolbarButton => s_ToolbarButton ??= new GUIStyle(EditorStyles.miniButton)
      {
        fixedWidth = 30,
        fixedHeight = 25,
        padding = new RectOffset(2, 2, 2, 2)
      };
    }

    public static void DrawSeparator()
    {
      var rect = EditorGUILayout.GetControlRect(false, 1);
      EditorGUI.DrawRect(rect, Colors.Separator);
      EditorGUILayout.Space(5);
    }

    public static void DrawHeader(string title)
    {
      EditorGUILayout.LabelField(title, Styles.Header);
      DrawSeparator();
    }

    public static void DrawWarningBox(string message)
    {
      var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight * 2);
      EditorGUI.DrawRect(rect, Colors.Warning);
      EditorGUI.LabelField(rect, message, EditorStyles.wordWrappedLabel);
    }

    public static void DrawErrorBox(string message)
    {
      var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight * 2);
      EditorGUI.DrawRect(rect, Colors.Error);
      EditorGUI.LabelField(rect, message, EditorStyles.wordWrappedLabel);
    }
  }
}