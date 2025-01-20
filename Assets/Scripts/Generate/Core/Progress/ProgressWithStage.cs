using System;
using UnityEngine;

namespace Generate.Core
{
  /// <summary>
  /// Реализация прогресса с этапами.
  /// Позволяет отслеживать прогресс и текущий этап генерации.
  /// </summary>
  public class ProgressWithStage : IProgressWithStage
  {
    private readonly Action<float> onProgress;
    private readonly Action<string> onStage;
    private float currentProgress;
    private string currentStage;

    /// <summary>
    /// Текущее значение прогресса (от 0 до 1)
    /// </summary>
    public float CurrentProgress => currentProgress;

    /// <summary>
    /// Текущий этап операции
    /// </summary>
    public string CurrentStage => currentStage;

    /// <summary>
    /// Создает новый экземпляр прогресса с этапами
    /// </summary>
    /// <param name="onProgress">Callback для обновления значения прогресса</param>
    /// <param name="onStage">Callback для обновления текущего этапа (опционально)</param>
    public ProgressWithStage(Action<float> onProgress, Action<string> onStage = null)
    {
      this.onProgress = onProgress ?? throw new ArgumentNullException(nameof(onProgress));
      this.onStage = onStage;
      this.currentProgress = 0f;
      this.currentStage = string.Empty;
    }

    /// <summary>
    /// Сообщает о прогрессе операции
    /// </summary>
    /// <param name="progress">Значение прогресса от 0 до 1</param>
    public void Report(float progress)
    {
      currentProgress = Mathf.Clamp01(progress);
      onProgress?.Invoke(currentProgress);
    }

    /// <summary>
    /// Обновляет текущий этап операции
    /// </summary>
    /// <param name="stage">Описание текущего этапа</param>
    public void UpdateStage(string stage)
    {
      currentStage = stage ?? string.Empty;
      onStage?.Invoke(currentStage);
    }
  }
}