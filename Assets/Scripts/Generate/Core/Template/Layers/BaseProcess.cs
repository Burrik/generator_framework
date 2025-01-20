using UnityEngine;
using Cysharp.Threading.Tasks;
using Generate.Core.Attributes;

namespace Generate.Core.Template
{
  /// <summary>
  /// Пример слоя для генерации основания здания
  /// </summary>
  [GeneratorProcess(typeof(ExampleGenerator))]
  public class BaseProcess : BaseProcessGenerator, IRegeneratable
  {
    public override string DisplayName => "Base Process";

    protected override async UniTask OnGenerate()
    {
      // Имитация работы
      await UniTask.Delay(1000);
      Debug.Log("Base generated!");
    }
  }
}