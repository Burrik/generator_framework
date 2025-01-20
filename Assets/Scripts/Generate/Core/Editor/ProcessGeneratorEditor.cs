using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

namespace Generate.Core.Editor
{
  /// <summary>
  /// Базовый редактор для всех процессов генерации.
  /// Реализует общую логику работы с регенерацией и UI процесса.
  /// </summary>
  public abstract class ProcessGeneratorEditor<T> : UnityEditor.Editor where T : BaseProcessGenerator
  {
    protected T process;
    protected IRegeneratable regeneratableProcess;
    protected BaseGenerator generator;
    protected GeneratorProgressUI progressUI;

    protected virtual void OnEnable()
    {
      process = target as T;
      if(process == null) return;

      regeneratableProcess = process as IRegeneratable;
      generator = process.GetComponentInParent<BaseGenerator>();
      progressUI = new GeneratorProgressUI(generator, Repaint);
    }

    protected virtual void OnDisable()
    {
      // Базовая реализация пустая
    }

    public override void OnInspectorGUI()
    {
      serializedObject.Update();
      DrawDefaultInspector();
      DrawCustomGUI();

      if(IsValidSetup())
      {
        DrawProcessTools();
      }
    }

    #region Drawing Methods
    /// <summary>
    /// Отрисовка кастомных элементов UI для конкретного процесса.
    /// Переопределите этот метод для добавления специфичных элементов управления.
    /// </summary>
    protected virtual void DrawCustomGUI() { }

    /// <summary>
    /// Отрисовка панели инструментов процесса.
    /// Включает в себя кнопки управления видимостью и регенерацией.
    /// </summary>
    protected virtual void DrawProcessTools()
    {
      EditorGUILayout.LabelField("Process Tools", EditorStyles.boldLabel);
      DrawGenerateViewToolbar();

      if(regeneratableProcess != null)
      {
        if(generator.IsGenerating)
        {
          progressUI.DrawProgress();
        }
        else
        {
          DrawRegenerationButton();
        }
      }
    }

    /// <summary>
    /// Отрисовка панели Generate view с кнопками управления процессом.
    /// </summary>
    private void DrawGenerateViewToolbar()
    {
      DrawSeparator();
      EditorGUILayout.BeginHorizontal();
      {
        DrawGenerateViewLabel();
        GUILayout.FlexibleSpace();
        DrawVisibilityButton();
        DrawProcessLinkButton();
      }
      EditorGUILayout.EndHorizontal();
      DrawSeparator();
    }
    #endregion

    #region UI Helper Methods
    private void DrawSeparator()
    {
      var rect = EditorGUILayout.GetControlRect(false, 1);
      EditorGUI.DrawRect(rect, GeneratorEditorStyles.Colors.Separator);
    }

    private void DrawGenerateViewLabel()
    {
      EditorGUILayout.LabelField("Generate view", GeneratorEditorStyles.Styles.GenerateViewLabel);
    }

    private void DrawVisibilityButton()
    {
      var eyeIcon = process.IsEnabled ?
          GeneratorEditorStyles.Icons.EyeVisible :
          GeneratorEditorStyles.Icons.EyeHidden;

      if(GUILayout.Button(eyeIcon, GeneratorEditorStyles.Styles.ToolbarButton))
      {
        process.IsEnabled = !process.IsEnabled;
        EditorUtility.SetDirty(process);
      }
    }

    private void DrawProcessLinkButton()
    {
      var linkIcon = process.CanBeRegenerated ?
          GeneratorEditorStyles.Icons.LinkedProcesses :
          GeneratorEditorStyles.Icons.UnlinkedProcesses;

      if(GUILayout.Button(linkIcon, GeneratorEditorStyles.Styles.ToolbarButton))
      {
        var serializedObject = new SerializedObject(process);
        var property = serializedObject.FindProperty("canBeRegenerated");
        property.boolValue = !property.boolValue;
        serializedObject.ApplyModifiedProperties();
      }
    }
    #endregion

    #region Validation and Messages
    /// <summary>
    /// Проверка корректности настройки процесса.
    /// </summary>
    protected virtual bool IsValidSetup()
    {
      if(process == null || generator == null)
      {
        EditorGUILayout.HelpBox("Process must be a child of BaseGenerator", MessageType.Warning);
        return false;
      }
      return true;
    }

    /// <summary>
    /// Отрисовка кнопки регенерации процесса и сопутствующих сообщений.
    /// </summary>
    protected virtual void DrawRegenerationButton()
    {
      var layersProperty = serializedObject.FindProperty("layers");
      bool hasLayers = layersProperty.objectReferenceValue != null;
      bool hasNoActiveLayers = hasLayers && !HasEnabledLayers();

      // Проверяем, есть ли "сломанный" процесс ниже по цепочке
      bool hasBrokenProcessBelow = false;
      if(generator != null)
      {
        var processes = generator.GetComponentsInChildren<BaseProcessGenerator>();
        var currentIndex = Array.IndexOf(processes, process);

        // Теперь проверяем процессы НИЖЕ текущего
        for(int i = currentIndex + 1; i < processes.Length; i++)
        {
          var p = processes[i];
          var pLayers = new SerializedObject(p).FindProperty("layers").objectReferenceValue as LayersContainer;
          if(p.IsEnabled && p.CanBeRegenerated &&
             pLayers != null &&
             !pLayers.GetActiveLayersEnumerable().Any())
          {
            hasBrokenProcessBelow = true;
            break;
          }
        }
      }

      bool canRegenerate = process.Data != null &&
                           !generator.IsGenerating &&
                           process.IsEnabled &&
                           (!hasLayers || !hasNoActiveLayers) && // Проверка слоев только если они есть
                           !hasBrokenProcessBelow;

      GUI.enabled = canRegenerate;

      var buttonContent = new GUIContent(
        " Regenerate Process",
        GeneratorEditorStyles.Icons.Generate.image);

      if(GUILayout.Button(buttonContent, GUILayout.Height(30)))
      {
        progressUI.StartGeneration((progress, token) =>
            generator.RequestProcessRegeneration(process, progress, token)).Forget();
      }

      GUI.enabled = true;
      DrawRegenerationMessages();
    }

    /// <summary>
    /// Отображение информационных сообщений о состоянии регенерации.
    /// </summary>
    protected virtual void DrawRegenerationMessages()
    {
      if(!process.IsEnabled || !process.CanBeRegenerated)
      {
        EditorGUILayout.HelpBox("Процесс отключен. Включите его для регенерации.", MessageType.Info);
      }
      else if(process.Data == null)
      {
        EditorGUILayout.HelpBox("Сначала необходимо выполнить генерацию.", MessageType.Info);
      }
      else if(generator.IsGenerating)
      {
        EditorGUILayout.HelpBox("Регенерация недоступна во время основной генерации.", MessageType.Info);
      }
      else if(!HasEnabledLayers())
      {
        var layersProperty = serializedObject.FindProperty("layers");
        if(layersProperty.objectReferenceValue != null)
        {
          EditorGUILayout.HelpBox("В процессе нет активных слоев. Включите хотя бы один слой для регенерации.", MessageType.Info);
        }
      }
    }

    private bool HasEnabledLayers()
    {
      var layersProperty = serializedObject.FindProperty("layers");
      var layers = layersProperty.objectReferenceValue as LayersContainer;
      return layers != null && layers.GetActiveLayersEnumerable().Any();
    }
    #endregion

  }
}