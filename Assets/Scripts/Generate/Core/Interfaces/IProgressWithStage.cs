namespace Generate.Core
{
  /// <summary>
  /// Интерфейс для отслеживания прогресса с этапами.
  /// Позволяет отслеживать как числовой прогресс, так и текстовое описание этапа.
  /// </summary>
  public interface IProgressWithStage
  {
    /// <summary>
    /// Текущее значение прогресса (от 0 до 1)
    /// </summary>
    float CurrentProgress { get; }

    /// <summary>
    /// Текущий этап операции
    /// </summary>
    string CurrentStage { get; }

    /// <summary>
    /// Сообщает о прогрессе операции
    /// </summary>
    /// <param name="progress">Значение прогресса от 0 до 1</param>
    void Report(float progress);

    /// <summary>
    /// Обновляет текущий этап операции
    /// </summary>
    /// <param name="stage">Описание текущего этапа</param>
    void UpdateStage(string stage);
  }
}