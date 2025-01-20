using UnityEngine;
using Cysharp.Threading.Tasks;
using Generate.Core.Attributes;

namespace Generate.Core.Template
{
  /// <summary>
  /// Пример слоя для генерации крыши здания
  /// </summary>
  /// 
  [GeneratorProcess(typeof(ExampleGenerator))]
  public class RoofProcess : BaseProcessGenerator, IRegeneratable
  {
    public override string DisplayName => "Roof Process";

    protected override async UniTask OnGenerate()
    {
      // Имитация работы
      await UniTask.Delay(800);
      Debug.Log("Roof generated!");
    }
  }
}