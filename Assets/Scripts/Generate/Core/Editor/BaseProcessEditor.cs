using UnityEditor;

namespace Generate.Core.Editor
{
  /// <summary>
  /// Стандартный редактор для BaseProcessGenerator.
  /// Предоставляет базовую функциональность "из коробки".
  /// </summary>
  [CustomEditor(typeof(BaseProcessGenerator), true)]
  [CanEditMultipleObjects]
  public class BaseProcessEditor : ProcessGeneratorEditor<BaseProcessGenerator>
  {
    // Базовая реализация уже содержит всю необходимую функциональность
  }
}