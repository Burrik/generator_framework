using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Generate.Core
{
  /// <summary>
  /// Базовый класс для всех слоев генерации
  /// </summary>
  public abstract class ProcessLayer : ScriptableObject
  {
    /// <summary>
    /// Флаг активности слоя
    /// </summary>
    public bool isEnabled = true;

    private LayerContext context;

    /// <summary>
    /// Основные данные генерации
    /// </summary>
    protected GeneratorData Data => context.Data;

    /// <summary>
    /// Пытается получить данные указанного типа из контекста
    /// </summary>
    /// <typeparam name="T">Тип данных контекста</typeparam>
    /// <param name="data">Полученные данные</param>
    /// <returns>true если данные найдены, иначе false</returns>
    protected bool TryGetContext<T>(out T data) where T : ILayerContextData =>
        context.TryGetContext(out data);

    /// <summary>
    /// Инициализирует слой контекстом
    /// </summary>
    /// <param name="layerContext">Контекст выполнения</param>
    internal async UniTask InitLayer(LayerContext layerContext)
    {
      context = layerContext;
      await OnInit();
    }

    /// <summary>
    /// Запускает генерацию слоя
    /// </summary>
    /// <param name="progress">Прогресс генерации</param>
    /// <param name="token">Токен отмены</param>
    public async UniTask GenerateLayer()
    {
      if(!isEnabled) return;
      await OnGenerate();
    }

    /// <summary>
    /// Вызывается при инициализации слоя
    /// </summary>
    protected virtual UniTask OnInit() => UniTask.CompletedTask;

    /// <summary>
    /// Выполняет генерацию слоя
    /// </summary>
    /// <param name="progress">Прогресс генерации</param>
    /// <param name="token">Токен отмены</param>
    protected abstract UniTask OnGenerate();
  }
}