using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Generate.Core.Template
{
  /// <summary>
  /// Пример реализации генератора.
  /// Показывает базовое использование BaseGenerator.
  /// </summary>
  public class ExampleGenerator : BaseGenerator
  {
    private ExampleData exampleData;

    protected override async UniTask OnInitialize()
    {
      // Проверяем наличие данных
      if(!Data.TryGetData<ExampleData>(out exampleData))
      {
        Debug.LogError("ExampleData not found in GeneratorData!");
        return;
      }

      // Проверяем валидность данных
      if(!exampleData.Validate())
      {
        Debug.LogError("ExampleData validation failed!");
        return;
      }

      await UniTask.Yield();
    }

    protected override async UniTask OnGenerate()
    {
      // Тут можно выполнить подготовку перед запуском процессов
      await UniTask.Yield();
    }
  }
}