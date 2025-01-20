using UnityEngine;

namespace Generate.Core
{
  public interface ILayerContextData
  {
    /// <summary>
    /// Валидация данных контекста
    /// </summary>
    bool Validate() => true;
  }
}