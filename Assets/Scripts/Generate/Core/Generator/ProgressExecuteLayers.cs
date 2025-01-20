using System;

namespace Generate.Core
{
  /// <summary>
  /// Управляет прогрессом выполнения слоев в процессе генерации
  /// </summary>
  public class ProgressExecuteLayers
  {
    private readonly string processName;

    /// <summary>
    /// Общее количество выполнений
    /// </summary>
    public int Total { get; }

    /// <summary>
    /// Текущее выполнение
    /// </summary>
    public int Current { get; private set; }

    /// <summary>
    /// Завершены ли все выполнения
    /// </summary>
    public bool IsCompleted => Current == Total;

    /// <summary>
    /// Создает новый прогресс выполнения
    /// </summary>
    /// <param name="total">Общее количество выполнений</param>
    /// <param name="processName">Название процесса</param>
    /// <exception cref="ArgumentException">Если total <= 0</exception>
    public ProgressExecuteLayers(int total, string processName)
    {
      if(total <= 0)
        throw new ArgumentException("Общее количество выполнений должно быть положительным", nameof(total));

      Total = total;
      Current = 0;
      this.processName = processName;
    }

    /// <summary>
    /// Получает диапазон прогресса для текущего выполнения
    /// </summary>
    /// <returns>Кортеж (from, to) с диапазоном прогресса</returns>
    /// <exception cref="InvalidOperationException">Если превышено количество выполнений</exception>
    public (float from, float to) GetProgress()
    {
      if(Current >= Total)
      {
        throw new InvalidOperationException(
          $"Некорректное количество выполнений в {processName}:\n" +
          $"- Указано выполнений: {Total}\n" +
          $"- Текущее выполнение: {Current + 1}\n" +
          "Убедитесь что в ExecuteLayers передано корректное значение totalExecutes"
        );
      }

      Current++;
      float stepSize = 1f / Total;
      return ((Current - 1) * stepSize, Current * stepSize);
    }

    /// <summary>
    /// Получает информацию о текущем прогрессе
    /// </summary>
    public string GetProgressInfo() => processName;
  }
}