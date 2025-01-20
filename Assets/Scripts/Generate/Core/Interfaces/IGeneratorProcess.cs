using Cysharp.Threading.Tasks;
using System.Threading;

namespace Generate.Core
{
  /// <summary>
  /// Базовый интерфейс для процесса генерации.
  /// Определяет основные методы и свойства для работы со процессом.
  /// </summary>
  public interface IGeneratorProcess
  {
    /// <summary>
    /// Отображаемое имя процесса в редакторе
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Флаг, указывающий включен ли процесс в процесс генерации
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Флаг, указывающий может ли процесс участвовать в регенерации
    /// </summary>
    bool CanBeRegenerated { get; }

    /// <summary>
    /// Данные генератора
    /// </summary>
    GeneratorData Data { get; }

    /// <summary>
    /// Инициализация процесса
    /// </summary>
    /// <param name="data">Данные для инициализации</param>
    /// <param name="generator">Генератор</param>
    /// <param name="cancellationToken">Токен отмены</param>
    UniTask Initialize(GeneratorData data, IGenerator generator, CancellationToken cancellationToken);

    /// <summary>
    /// Генерация процесса
    /// </summary>
    /// <param name="progress">Прогресс генерации</param>
    /// <param name="cancellationToken">Токен отмены</param>
    UniTask Generate(IProgressWithStage progress, CancellationToken cancellationToken);

    /// <summary>
    /// Инициализация состояния для регенерации
    /// По умолчанию использует обычную инициализацию
    /// </summary>
    UniTask InitRegeneration(CancellationToken cancellationToken);

    /// <summary>
    /// Выполняет регенерацию процесса
    /// ВАЖНО: Не вызывайте этот метод напрямую! Используйте generator.RequestProcessRegeneration или generator.RequestProcessRegenerateOnly
    /// для корректной работы флага IsGenerating
    /// </summary>
    /// <param name="progress">Прогресс генерации</param>
    /// <param name="cancellationToken">Токен отмены</param>
    UniTask Regeneration(IProgressWithStage progress, CancellationToken cancellationToken);
  }
}