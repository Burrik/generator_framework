using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Generate.Core.Template
{
  /// <summary>
  /// ВНИМАНИЕ: Это шаблонный класс для демонстрации архитектуры.
  /// </summary>
  [CreateAssetMenu(fileName = "WindowsLayer", menuName = "Generate/Layers/Template/WindowsLayer")]
  public class WindowsLayer : ProcessLayer
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
      if(!floorData.Windows) return;

      Debug.Log($"Adding windows to floor {floorData.FloorIndex}");

      // Симуляция добавления окон
      for(int i = 0; i < 4; i++)
      {
        await UniTask.Delay(50);
      }

      Debug.Log($"Windows added to floor {floorData.FloorIndex}");
    }
  }
}