using UnityEngine;
using Cysharp.Threading.Tasks;
using Generate.Core.Attributes;

namespace Generate.Core.Template
{
  /// <summary>
  /// Пример слоя для генерации этажей здания
  /// </summary>
  [GeneratorProcess(typeof(ExampleGenerator))]
  public class FloorsProcess : BaseProcessGenerator, IRegeneratable
  {
    [SerializeField] private int floorCount = 3;

    public override string DisplayName => "Floors Process";

    protected override async UniTask OnGenerate()
    {
      for(int i = 0; i < floorCount; i++)
      {
        var context = LayerContext.CreateContext(Data,
            new FloorContextData
            {
              FloorIndex = i,
              Height = 3f,
              Windows = true
            });

        await ExecuteLayers(context, floorCount);
      }
    }
  }
}