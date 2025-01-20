namespace Generate.Core
{
  /// <summary>
  /// Базовый интерфейс для данных генератора.
  /// Определяет общий контракт для всех типов данных в системе генерации.
  /// </summary>
  public interface IGeneratorData
  {

    /// <summary>
    /// Проверяет валидность данных
    /// </summary>
    /// <returns>true если данные корректны, иначе false</returns>
    bool Validate();
  }
}