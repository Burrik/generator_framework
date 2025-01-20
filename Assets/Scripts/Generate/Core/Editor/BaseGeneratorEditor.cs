using UnityEditor;

namespace Generate.Core.Editor
{
  /// <summary>
  /// Стандартный редактор для BaseGenerator.
  /// Предоставляет базовую функциональность "из коробки".
  /// </summary>
  [CustomEditor(typeof(BaseGenerator), true)]
  public class BaseGeneratorEditor : GeneratorEditor<BaseGenerator>
  {
    // Базовая реализация уже содержит всю необходимую функциональность
    // Можно добавить специфичные методы при необходимости
  }
}