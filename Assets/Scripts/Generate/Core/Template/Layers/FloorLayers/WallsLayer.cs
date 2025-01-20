using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Generate.Core.Template
{
  /// <summary>
  /// ВНИМАНИЕ: Это шаблонный класс для демонстрации архитектуры.
  /// </summary>
  [CreateAssetMenu(fileName = "WallsLayer", menuName = "Generate/Layers/Template/WallsLayer")]
  public class WallsLayer : ProcessLayer
  {
    private FloorContextData floorData;

    protected override async UniTask OnInit()
    {
      if(!TryGetContext<FloorContextData>(out floorData))
      {
        Debug.LogError("Нет данных об этаже в контексте!");
        return;
      }
      await UniTask.Yield();
    }
    protected override async UniTask OnGenerate()
    {
      Debug.Log($"Generating walls for floor {floorData.FloorIndex}");

      // Симуляция генерации стен
      for(int i = 0; i < 4; i++)
      {
        await UniTask.Delay(100);
      }

      Debug.Log($"Walls generated for floor {floorData.FloorIndex}");
    }
  }
}