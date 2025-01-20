using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Generate.Core.Utils;
using Generate.Core.Attributes;

namespace Generate.Core.Editor
{
  /// <summary>
  /// Базовый редактор для всех генераторов.
  /// Реализует общую логику работы с процессами и генерацией.
  /// </summary>
  public abstract class GeneratorEditor<T> : UnityEditor.Editor where T : BaseGenerator
  {
    protected T generator;
    protected TypeCreator<IGeneratorProcess> typeCreator;
    protected string[] processNames;
    protected Vector2 dragStartPosition;
    protected int draggedProcessIndex = -1;
    protected int selectedProcessIndex = -1;
    protected GeneratorProgressUI progressUI;

    // Кэшируем список доступных процессов для конкретного генератора
    private List<Type> availableProcessTypes;

    protected virtual void OnEnable()
    {
      generator = (T)target;
      progressUI = new GeneratorProgressUI(generator, Repaint);

      // Создаем TypeCreator с предикатом фильтрации
      typeCreator = new TypeCreator<IGeneratorProcess>(
          additionalFilter: IsProcessValidForGenerator
      );

      // Получаем имена процессов из отфильтрованных типов
      processNames = typeCreator.Types
          .Select(t =>
          {
            try
            {
              return ((IGeneratorProcess)Activator.CreateInstance(t)).DisplayName;
            }
            catch(Exception ex)
            {
              Debug.LogError($"Error creating instance of {t.Name}: {ex.Message}");
              return t.Name;
            }
          })
          .ToArray();

      progressUI = new GeneratorProgressUI(generator, Repaint);

      EditorApplication.hierarchyChanged += RefreshProcesses;
      Undo.undoRedoPerformed += RefreshProcesses;

      EnsureProcessesUnderGenerator();
    }

    protected virtual void OnDisable()
    {
      EditorApplication.hierarchyChanged -= RefreshProcesses;
      Undo.undoRedoPerformed -= RefreshProcesses;
    }

    private void RefreshProcesses()
    {
      if(generator != null)
      {
        if(!generator.gameObject.scene.isLoaded) return;

        generator.OnValidate();
        EnsureProcessesUnderGenerator();
        Repaint();
      }
    }

    private void EnsureProcessesUnderGenerator()
    {
      if(generator == null || generator.ProcessesList == null || generator.ProcessesList.Count == 0)
        return;

      var allComponents = generator.gameObject.GetComponents<Component>();
      int generatorIndex = Array.IndexOf(allComponents, generator);

      int maxIterations = allComponents.Length * 2;
      int currentIteration = 0;

      foreach(var process in generator.ProcessesList)
      {
        var component = process as Component;
        if(component == null) continue;

        int processIndex = Array.IndexOf(allComponents, component);
        if(processIndex <= generatorIndex)
        {
          while(Array.IndexOf(generator.gameObject.GetComponents<Component>(), component) <= generatorIndex)
          {
            currentIteration++;
            if(currentIteration > maxIterations)
            {
              Debug.LogWarning("Prevented infinite loop in EnsureProcessesUnderGenerator");
              return;
            }

            UnityEditorInternal.ComponentUtility.MoveComponentDown(component);
          }
        }
      }
    }

    public override void OnInspectorGUI()
    {

      DrawDefaultInspector();
      EditorGUILayout.Space(10);
      DrawCustomGUI();
      EditorGUILayout.Space(10);
      DrawAddProcessSection();
      DrawProcessesSection();
      DrawGenerateSection();

      DrawDraggedProcess();
      HandleDragAndDrop();
    }

    protected virtual void DrawCustomGUI() { }

    private void DrawAddProcessSection()
    {
      DrawHeader("Add Process");
      DrawProcessTypeSelector();
      EditorGUILayout.Space(10);
    }

    private void DrawProcessTypeSelector()
    {
      EditorGUILayout.BeginHorizontal();
      selectedProcessIndex = EditorGUILayout.Popup("Process Type", selectedProcessIndex, processNames);

      var addButtonContent = new GUIContent(" Add", GeneratorEditorStyles.Icons.Add.image, "Add selected process type");

      using(new EditorGUI.DisabledGroupScope(selectedProcessIndex < 0))
      {
        if(GUILayout.Button(addButtonContent, GUILayout.Width(50)))
        {
          TryAddProcess(selectedProcessIndex);
          selectedProcessIndex = -1;
        }
      }
      EditorGUILayout.EndHorizontal();
    }

    private void DrawProcessesSection()
    {
      var processes = generator.ProcessesList;
      if(processes.Count > 0)
      {
        DrawHeader("Generation Processes");
        DrawProcessesList();
        EditorGUILayout.Space(5);
      }
    }

    private void DrawProcessesList()
    {
      var processes = generator.ProcessesList;
      for(int i = 0; i < processes.Count; i++)
      {
        DrawProcessItem(processes[i], i, processes.Count);

        if(i < processes.Count - 1)
        {
          EditorGUILayout.Space(2);
        }
      }
    }

    private void DrawProcessItem(IGeneratorProcess process, int index, int totalCount)
    {
      if(process == null) return;

      var rect = EditorGUILayout.GetControlRect(false, 20);
      var itemRect = new Rect(rect);

      if(itemRect.Contains(Event.current.mousePosition))
      {
        EditorGUI.DrawRect(itemRect, GeneratorEditorStyles.Colors.Hover);
      }

      EditorGUILayout.BeginHorizontal(GUILayout.Height(20));

      // Чекбокс процесса
      var toggleRect = new Rect(rect.x, rect.y, 20, rect.height);
      var component = process as MonoBehaviour;
      var enabled = EditorGUI.Toggle(toggleRect, process.IsEnabled);
      if(enabled != process.IsEnabled)
      {
        Undo.RecordObject(component, "Toggle Process Enabled");
        process.IsEnabled = enabled;
        EditorUtility.SetDirty(component);
      }

      // Название процесса
      var labelRect = new Rect(rect.x + 20, rect.y, rect.width - 100, rect.height);
      EditorGUI.LabelField(labelRect, $"{index + 1}. {process.DisplayName}");

      DrawProcessControls(process, index, rect);

      EditorGUILayout.EndHorizontal();

      // Получаем слои через SerializedObject
      if(process is BaseProcessGenerator processGenerator)
      {
        var serializedObject = new SerializedObject(processGenerator);
        var layersProperty = serializedObject.FindProperty("layers");
        var layersContainer = layersProperty.objectReferenceValue as LayersContainer;

        if(layersContainer != null)
        {
          var serializedLayersContainer = new SerializedObject(layersContainer);
          var layersArrayProperty = serializedLayersContainer.FindProperty("layers");

          if(layersArrayProperty != null)
          {
            EditorGUI.indentLevel++;
            for(int i = 0; i < layersArrayProperty.arraySize; i++)
            {
              var layerProperty = layersArrayProperty.GetArrayElementAtIndex(i);
              var layer = layerProperty.objectReferenceValue as ProcessLayer;

              if(layer != null)
              {
                EditorGUILayout.BeginHorizontal();
                var rectLayer = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);

                // Чекбокс процесса
                var toggleRectLayer = new Rect(rectLayer.x, rectLayer.y, 30, rectLayer.height);
                var enabledLayer = EditorGUI.Toggle(toggleRectLayer, layer.isEnabled);

                if(enabledLayer != layer.isEnabled)
                {
                  Undo.RecordObject(layer, "Toggle Layer Enabled");
                  layer.isEnabled = enabledLayer;
                  EditorUtility.SetDirty(layer);
                }

                // Название процесса
                var labelRectLayer = new Rect(rectLayer.x + 20, rectLayer.y, rectLayer.width - 100, rectLayer.height);
                EditorGUI.LabelField(labelRectLayer, layer.name);

                EditorGUILayout.EndHorizontal();
              }
            }
            EditorGUI.indentLevel--;
          }
        }
      }

      if(Event.current.type == EventType.MouseDown && itemRect.Contains(Event.current.mousePosition))
      {
        dragStartPosition = Event.current.mousePosition;
        draggedProcessIndex = index;
        Event.current.Use();
      }
    }

    private void DrawProcessControls(IGeneratorProcess process, int index, Rect rect)
    {
      var buttonsStartX = rect.xMax - 75;
      var buttonY = rect.y + (rect.height - EditorGUIUtility.singleLineHeight) / 2;

      DrawMoveUpButton(process, index, buttonsStartX, buttonY);
      DrawMoveDownButton(process, index, buttonsStartX + 25, buttonY);
      DrawDeleteButton(process, buttonsStartX + 50, buttonY);
    }

    private void DrawMoveUpButton(IGeneratorProcess process, int index, float x, float y)
    {
      GUI.enabled = index > 0;
      var buttonRect = new Rect(x, y, 25, EditorGUIUtility.singleLineHeight);
      if(GUI.Button(buttonRect, GeneratorEditorStyles.Icons.Up))
      {
        UnityEditorInternal.ComponentUtility.MoveComponentUp(process as Component);
        EditorUtility.SetDirty(generator);
      }
    }

    private void DrawMoveDownButton(IGeneratorProcess process, int index, float x, float y)
    {
      GUI.enabled = index < generator.ProcessesList.Count - 1;
      var buttonRect = new Rect(x, y, 25, EditorGUIUtility.singleLineHeight);
      if(GUI.Button(buttonRect, GeneratorEditorStyles.Icons.Down))
      {
        UnityEditorInternal.ComponentUtility.MoveComponentDown(process as Component);
        EditorUtility.SetDirty(generator);
      }
    }

    private void DrawDeleteButton(IGeneratorProcess process, float x, float y)
    {
      GUI.enabled = true;
      var buttonRect = new Rect(x, y, 25, EditorGUIUtility.singleLineHeight);

      if(GUI.Button(buttonRect, GeneratorEditorStyles.Icons.Delete))
      {
        if(EditorUtility.DisplayDialog("Delete Process",
            $"Are you sure you want to delete {process.DisplayName}?",
            "Delete", "Cancel"))
        {
          // Отключаем процесс перед удалением
          process.IsEnabled = false;

          // Удаляем компонент без вызова RegenerateProcess
          var component = process as Component;
          if(component != null)
          {
            Undo.DestroyObjectImmediate(component); // Используем Undo для возможности отмены
            EditorUtility.SetDirty(generator);
          }
        }
      }
    }

    private void DrawDraggedProcess()
    {
      if(draggedProcessIndex != -1 && draggedProcessIndex < generator.ProcessesList.Count)
      {
        var process = generator.ProcessesList[draggedProcessIndex];
        var dragRect = new Rect(Event.current.mousePosition.x + 10, Event.current.mousePosition.y - 10, 200, 20);
        EditorGUI.DrawRect(dragRect, new Color(0.2f, 0.3f, 0.7f, 0.8f));
        EditorGUI.LabelField(dragRect, process.DisplayName);
        Repaint();
      }
    }

    private void HandleDragAndDrop()
    {
      var currentEvent = Event.current;

      if(currentEvent.type == EventType.MouseDrag && draggedProcessIndex != -1)
      {
        DragAndDrop.PrepareStartDrag();
        DragAndDrop.SetGenericData("ProcessIndex", draggedProcessIndex);
        DragAndDrop.StartDrag(string.Empty);
        currentEvent.Use();
        Repaint();
      }
      else if(currentEvent.type == EventType.DragUpdated || currentEvent.type == EventType.DragPerform)
      {
        DragAndDrop.visualMode = DragAndDropVisualMode.Move;

        if(currentEvent.type == EventType.DragPerform)
        {
          var draggedIndex = (int)DragAndDrop.GetGenericData("ProcessIndex");
          var targetIndex = GetTargetProcessIndex(currentEvent.mousePosition);

          if(draggedIndex != targetIndex && targetIndex != -1)
          {
            var draggedComponent = generator.ProcessesList[draggedIndex] as Component;
            var targetComponent = generator.ProcessesList[targetIndex] as Component;

            var allComponents = generator.gameObject.GetComponents<Component>();
            int draggedPosition = Array.IndexOf(allComponents, draggedComponent);
            int targetPosition = Array.IndexOf(allComponents, targetComponent);

            // Если перетаскиваем вниз
            if(draggedPosition < targetPosition)
            {
              // Сначала поднимаем целевой компонент на позицию перетаскиваемого + 1
              while(Array.IndexOf(generator.gameObject.GetComponents<Component>(), targetComponent) > draggedPosition + 1)
              {
                UnityEditorInternal.ComponentUtility.MoveComponentUp(targetComponent);
              }

              // Затем опускаем перетаскиваемый компонент на исходную позицию целевого
              while(Array.IndexOf(generator.gameObject.GetComponents<Component>(), draggedComponent) < targetPosition)
              {
                UnityEditorInternal.ComponentUtility.MoveComponentDown(draggedComponent);
              }
            }
            // Если перетаскиваем вверх
            else
            {
              // Сначала поднимаем перетаскиваемый компонент до позиции целевого + 1
              while(Array.IndexOf(generator.gameObject.GetComponents<Component>(), draggedComponent) > targetPosition + 1)
              {
                UnityEditorInternal.ComponentUtility.MoveComponentUp(draggedComponent);
              }

              // Затем опускаем целевой компонент на позицию перетаскиваемого
              while(Array.IndexOf(generator.gameObject.GetComponents<Component>(), targetComponent) < draggedPosition)
              {
                UnityEditorInternal.ComponentUtility.MoveComponentDown(targetComponent);
              }
            }

            EditorUtility.SetDirty(generator);
          }

          draggedProcessIndex = -1;
          DragAndDrop.AcceptDrag();
          DragAndDrop.SetGenericData("ProcessIndex", null);
        }
        currentEvent.Use();
      }
      else if(currentEvent.type == EventType.DragExited || currentEvent.type == EventType.MouseUp)
      {
        draggedProcessIndex = -1;
        DragAndDrop.SetGenericData("ProcessIndex", null);
        Repaint();
        currentEvent.Use();
      }
    }

    private int GetTargetProcessIndex(Vector2 mousePosition)
    {
      var processes = generator.ProcessesList;
      if(processes.Count == 0) return -1;

      // Вычисляем расстояние от начальной позиции
      float dragDistance = mousePosition.y - dragStartPosition.y;
      float stepSize = 30f; // Размер шага для перемещения на следующий индекс

      // Определяем количество шагов на основе расстояния
      int steps = Mathf.RoundToInt(dragDistance / stepSize);

      // Вычисляем целевой индекс
      return Mathf.Clamp(draggedProcessIndex + steps, 0, processes.Count - 1);
    }

    private void DrawGenerateSection()
    {
      EditorGUILayout.Space(5);

      if(generator.IsGenerating)
      {
        progressUI.DrawProgress();
      }
      else
      {
        var errorMessage = GetGenerateErrorMessage();

        using(new EditorGUI.DisabledGroupScope(!string.IsNullOrEmpty(errorMessage)))
        {
          var buttonContent = new GUIContent(" Generate", GeneratorEditorStyles.Icons.Generate.image);
          if(GUILayout.Button(buttonContent, GUILayout.Height(30)))
          {
            if(!string.IsNullOrEmpty(errorMessage))
            {
              EditorUtility.DisplayDialog("Cannot Generate", errorMessage, "OK");
              return;
            }
            StartGeneration();
          }
        }

        if(!string.IsNullOrEmpty(errorMessage))
        {
          EditorGUILayout.Space(5);
          EditorGUILayout.HelpBox(errorMessage, MessageType.Error);
        }
      }
    }

    protected virtual string GetGenerateErrorMessage()
    {
      // Проверяем валидацию генератора
      if(!generator.ValidateGenerator(out string error))
      {
        return error;
      }

      // Проверяем все процессы на наличие активных слоев
      var processes = generator.GetComponentsInChildren<BaseProcessGenerator>();
      foreach(var process in processes)
      {
        if(process.IsEnabled) // Проверяем только IsEnabled
        {
          var layersProperty = new SerializedObject(process).FindProperty("layers");
          var layers = layersProperty.objectReferenceValue as LayersContainer;

          if(layers != null && !layers.GetActiveLayersEnumerable().Any())
          {
            return $"В процессе '{process.DisplayName}' нет активных слоев. Включите хотя бы один слой для генерации.";
          }
        }
      }

      // Остальные проверки
      if(generator.Data == null)
        return "Generator data is not set";

      if(generator.ProcessesList.Count == 0)
        return "Add at least one generation process";

      if(!generator.ProcessesList.Any(p => p.IsEnabled))
        return "Enable at least one generation process";

      return string.Empty;
    }

    private void StartGeneration()
    {
      progressUI.StartGeneration((progress, token) =>
          generator.Generate(progress, token)).Forget();
    }

    private void DrawHeader(string title)
    {
      GeneratorEditorStyles.DrawHeader(title);
    }

    private void TryAddProcess(int index)
    {
      if(index < 0 || index >= typeCreator.Types.Length) return;

      var selectedType = typeCreator.Types[index];
      var component = typeCreator.AddComponent(selectedType, generator.gameObject);

      if(component == null)
      {
        EditorUtility.DisplayDialog("Warning",
            $"Component {processNames[index]} already exists!", "OK");
      }
      else
      {
        Undo.RegisterCreatedObjectUndo(component, "Add Process");
        EditorUtility.SetDirty(generator);
      }
    }

    /// <summary>
    /// Проверяет, является ли процесс валидным для текущего генератора
    /// </summary>
    private bool IsProcessValidForGenerator(Type processType)
    {
      // Получаем реальный тип генератора, а не тип из дженерика
      Type actualGeneratorType = target.GetType();

      // Проверяем наличие атрибута GeneratorProcess
      var attrs = processType.GetCustomAttributes(typeof(GeneratorProcessAttribute), true);

      if(attrs.Length == 0)
        return false;

      // Проверяем каждый атрибут
      foreach(GeneratorProcessAttribute attr in attrs)
      {
        Type attributeGeneratorType = attr.GeneratorType;

        // Процесс доступен если:
        // 1. Атрибут указывает на BaseGenerator
        if(attributeGeneratorType == typeof(BaseGenerator))
          return true;

        // 2. Атрибут точно указывает на реальный тип генератора
        if(attributeGeneratorType == actualGeneratorType)
          return true;

        // 3. Реальный тип генератора наследуется от типа в атрибуте
        if(attributeGeneratorType.IsAssignableFrom(actualGeneratorType))
          return true;
      }

      return false;
    }
  }
}
