using UnityEngine;
using UnityEditor;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Generate.Core.Editor
{
  /// <summary>
  /// UI компонент для отображения прогресса генерации в редакторе Unity
  /// с поддержкой отмены генерации.
  /// </summary>
  public class GeneratorProgressUI
  {
    private readonly ProgressWithStage progress;
    private readonly Action repaint;
    private readonly BaseGenerator generator;

    public GeneratorProgressUI(BaseGenerator generator, Action repaintAction)
    {
      this.generator = generator ?? throw new ArgumentNullException(nameof(generator));
      this.repaint = repaintAction ?? throw new ArgumentNullException(nameof(repaintAction));
      progress = new ProgressWithStage(
          progress => repaintAction(),
          stage => repaintAction()
      );
    }

    public void DrawProgress()
    {
      var rect = EditorGUILayout.GetControlRect(false, 30);
      EditorGUI.ProgressBar(rect, progress.CurrentProgress, progress.CurrentStage ?? "Generating...");

      if(GUILayout.Button("Cancel", GUILayout.Height(25)))
      {
        if(EditorUtility.DisplayDialog("Cancel Generation",
            "Are you sure you want to cancel the generation?",
            "Yes", "No"))
        {
          generator.CancelGeneration();
        }
      }
    }

    public async UniTaskVoid StartGeneration(Func<IProgressWithStage, CancellationToken, UniTask> generationTask)
    {
      if(generationTask == null) throw new ArgumentNullException(nameof(generationTask));

      progress.Report(0f);
      progress.UpdateStage("Запуск генерации...");

      try
      {
        await generationTask(progress, generator.GetCancellationToken());
      }
      catch(OperationCanceledException)
      {
        progress.UpdateStage("Генерация отменена");
        Debug.Log("Generation was cancelled");
      }
      finally
      {
        progress.Report(0);
        progress.UpdateStage(string.Empty);
        repaint();
      }
    }
  }
}