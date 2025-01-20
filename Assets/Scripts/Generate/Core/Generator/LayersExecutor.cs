using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using System.Linq;
using System.IO;

namespace Generate.Core
{
  internal class LayersExecutor
  {
    public async UniTask Execute(
        LayersContainer layers,
        LayerContext context,
        ProgressExecuteLayers progress,
        IProgressWithStage progressReporter,
        CancellationToken token)
    {
      if(!ValidateLayers(layers)) return;

      try
      {
        var activeLayers = layers.GetActiveLayers();
        var (progressFrom, progressTo) = progress.GetProgress();
        float layerProgress = (progressTo - progressFrom) / activeLayers.Length;

        for(int i = 0; i < activeLayers.Length; i++)
        {
          token.ThrowIfCancellationRequested();
          var layer = activeLayers[i];
          try
          {
            await layer.InitLayer(context);
            progressReporter?.UpdateStage(
                $"Generate {progress.GetProgressInfo()} ({progress.Current}/{progress.Total}): " +
                $"{layer.name.Replace("Layer", "")} ({i + 1}/{activeLayers.Length})"
            );
            progressReporter?.Report(progressFrom + (i * layerProgress));
            await UniTask.Yield(PlayerLoopTiming.TimeUpdate, token);
            await layer.GenerateLayer();
          }
          catch(OperationCanceledException)
          {
            Debug.LogWarning("Выполнение отменено");
            throw;
          }
          catch(Exception ex)
          {
            var st = new System.Diagnostics.StackTrace(ex, true);
            var frame = st.GetFrames()?.FirstOrDefault(f =>
              f.GetFileName() != null &&
              f.GetFileName().Contains(layer.name)
            );

            var location = frame != null
              ? $"(строка {frame.GetFileLineNumber()} в {Path.GetFileName(frame.GetFileName())})"
              : "";

            var solution = GetErrorSolution(ex, layer);

            Debug.LogError(
              $"Ошибка выполнения слоя {layer.name} {location}:\n" +
              $"- Сообщение: {ex.Message}\n" +
              $"- Тип ошибки: {ex.GetType().Name}\n" +
              $"- Возможное решение: {solution}\n" +
              $"- Стек вызовов:\n{ex.StackTrace}"
            );

            throw new LayerExecutionException(layer.name, ex);
          }
        }
      }
      catch(Exception ex) when(!(ex is OperationCanceledException || ex is LayerExecutionException))
      {
        Debug.LogError($"Непредвиденная ошибка при выполнении слоев: {ex.Message}");
        throw new LayerExecutionException("Unknown", ex);
      }
    }

    private bool ValidateLayers(LayersContainer layers)
    {
      if(layers == null)
      {
        Debug.LogWarning("Не назначен контейнер слоев");
        return false;
      }

      if(!layers.Validate(out var error))
      {
        Debug.LogError(error);
        return false;
      }

      return true;
    }

    private string GetErrorSolution(Exception ex, ProcessLayer layer)
    {
      return ex switch
      {
        NullReferenceException => "Проверьте инициализацию объектов в слое. " +
          "Возможно, не установлены необходимые ссылки в инспекторе или " +
          "отсутствуют данные в GeneratorData",

        InvalidOperationException when ex.Message.Contains("GetData") =>
          "Отсутствуют необходимые данные. " +
          "Убедитесь что все требуемые данные добавлены в GeneratorData",

        ArgumentException => "Проверьте параметры, передаваемые в методы. " +
          "Возможно, некорректные значения в инспекторе",

        // Можно добавить другие типичные ошибки

        _ => "Проверьте логику работы слоя и корректность входных данных"
      };
    }
  }

  public class LayerExecutionException : Exception
  {
    public string LayerName { get; }

    public LayerExecutionException(string layerName, Exception innerException)
        : base($"Ошибка выполнения слоя {layerName}", innerException)
    {
      LayerName = layerName;
    }
  }
}