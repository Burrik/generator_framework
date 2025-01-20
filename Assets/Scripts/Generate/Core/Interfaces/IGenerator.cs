using Cysharp.Threading.Tasks;
using System.Threading;
using System;

namespace Generate.Core
{
  /// <summary>
  /// Интерфейс для взаимодействия с генератором.
  /// Предоставляет базовый функционал для процессов генерации.
  /// </summary>
  public interface IGenerator
  {
    /// <summary>
    /// Флаг, указывающий что генерация в процессе
    /// </summary>
    bool IsGenerating { get; }

    /// <summary>
    /// Безопасно выполняет операции генерации последовательно
    /// </summary>
    UniTask Request(params Func<UniTask>[] actions);

    /// <summary>
    /// Запускает регенерацию процесса с последующими процессами
    /// </summary>
    UniTask RequestProcessRegeneration(
      IGeneratorProcess process,
      IProgressWithStage progress = null,
      CancellationToken cancellationToken = default);
  }
}