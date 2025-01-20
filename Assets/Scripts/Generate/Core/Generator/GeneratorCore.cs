using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System;
using UnityEditor;

namespace Generate.Core
{
  [Serializable]
  public class GeneratorCore : IGenerator
  {
    private List<IGeneratorProcess> processes = new();
    private bool isGenerating;
    private CancellationTokenSource cancellationTokenSource;
    private UniTask currentGenerationTask;

    public bool IsGenerating => isGenerating ||
                               (currentGenerationTask.Status == UniTaskStatus.Pending);
    public IReadOnlyList<IGeneratorProcess> ProcessesList => processes;

    /// <summary>
    /// Инициализирует генератор списком процессов
    /// </summary>
    /// <param name="newProcesses">Список процессов для инициализации</param>
    public void Initialize(IEnumerable<IGeneratorProcess> newProcesses)
    {
      processes = newProcesses?.ToList() ?? new List<IGeneratorProcess>();
    }

    /// <summary>
    /// Запускает генерацию всех активных процессов
    /// </summary>
    /// <param name="data">Данные для генерации</param>
    /// <param name="progress">Отображение прогресса</param>
    /// <param name="token">Токен отмены</param>
    public async UniTask Generate(GeneratorData data, IProgressWithStage progress, CancellationToken token)
    {
      await Request(async () =>
      {
        cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
        try
        {
          var enabledProcesses = ProcessesList
              .Where(p => p.IsEnabled)
              .ToArray();

          await ExecuteProcessesWithProgress(
              enabledProcesses,
              async (process, processProgress) => await process.Initialize(data, this, cancellationTokenSource.Token),
              async (process, processProgress) => await process.Generate(processProgress, cancellationTokenSource.Token),
              progress,
              cancellationTokenSource.Token
          );
        }
        finally
        {
          cancellationTokenSource?.Dispose();
          cancellationTokenSource = null;
        }
      });
    }

    /// <summary>
    /// Запускает регенерацию указанного процесса и всех зависимых от него
    /// </summary>
    /// <param name="process">Процесс для регенерации</param>
    /// <param name="progress">Отображение прогресса</param>
    /// <param name="token">Токен отмены</param>
    public async UniTask RequestProcessRegeneration(
        IGeneratorProcess process,
        IProgressWithStage progress = null,
        CancellationToken token = default)
    {
      var index = ProcessesList.ToList().IndexOf(process);
      if(index == -1) return;

      await Request(async () =>
      {
        // Создаем новый связанный токен
        cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
        try
        {
          var processesToRegenerate = ProcessesList
              .Skip(index)
              .OfType<BaseProcessGenerator>()
              .Where(p => p.IsEnabled && p.CanBeRegenerated)
              .ToArray();

          await ExecuteProcessesWithProgress(
              processesToRegenerate,
              async (process, processProgress) =>
                  await ((IGeneratorProcess)process).InitRegeneration(cancellationTokenSource.Token),
              async (process, processProgress) =>
                  await ((IGeneratorProcess)process).Regeneration(processProgress, cancellationTokenSource.Token),
              progress,
              cancellationTokenSource.Token
          );
        }
        finally
        {
          cancellationTokenSource?.Dispose();
          cancellationTokenSource = null;
        }
      });
    }

    /// <summary>
    /// Безопасно выполняет последовательность асинхронных действий
    /// </summary>
    /// <param name="actions">Действия для выполнения</param>
    public async UniTask Request(params Func<UniTask>[] actions)
    {
      if(IsGenerating)
      {
        Debug.LogError("Попытка запуска генерации во время активной генерации");
        return;
      }

      isGenerating = true;
      try
      {
        foreach(var action in actions)
        {
          currentGenerationTask = action();
          await currentGenerationTask;
        }
        SaveData();
      }
      catch(OperationCanceledException)
      {
        Debug.Log("Генерация отменена");
      }
      catch(Exception ex)
      {
        Debug.LogError($"Ошибка при выполнении генерации: {ex}");
        throw;
      }
      finally
      {
        isGenerating = false;
        currentGenerationTask = default;

        // Принудительно очищаем состояние
        GC.Collect();
        await UniTask.Yield(PlayerLoopTiming.TimeUpdate);
      }
    }

    private void SaveData()
    {
      var data = processes.FirstOrDefault()?.Data;
      if(data != null)
      {
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
      }
    }

    private async UniTask ExecuteProcessesWithProgress<T>(
        T[] processes,
        Func<T, IProgressWithStage, UniTask> initAction,
        Func<T, IProgressWithStage, UniTask> executeAction,
        IProgressWithStage progress,
        CancellationToken token) where T : IGeneratorProcess
    {
      int totalSteps = processes.Length * 2;
      int currentStep = 0;

      // Инициализация
      foreach(var process in processes)
      {
        token.ThrowIfCancellationRequested();
        progress?.UpdateStage($"Initializing {process.DisplayName}...");
        await UniTask.Yield(PlayerLoopTiming.TimeUpdate, token);
        await initAction(process, progress);
        currentStep++;
        progress?.Report((float)currentStep / totalSteps);
      }

      // Выполнение
      foreach(var process in processes)
      {
        token.ThrowIfCancellationRequested();
        var processProgress = CreateProcessProgress(currentStep, totalSteps, progress);
        await executeAction(process, processProgress);
        currentStep++;
        progress?.Report((float)currentStep / totalSteps);
        await UniTask.Yield(PlayerLoopTiming.TimeUpdate, token);
      }
    }

    private IProgressWithStage CreateProcessProgress(int step, int totalSteps, IProgressWithStage progress) =>
        new ProgressWithStage(
            value => progress?.Report((step + value) / totalSteps),
            stage => progress?.UpdateStage(stage)
        );

    public void CancelGeneration()
    {
      if(IsGenerating)
      {
        Debug.Log("Generator core cancelling...");
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
        cancellationTokenSource = null;
        isGenerating = false;
      }
    }

    public CancellationToken GetCancellationToken()
    {
      return cancellationTokenSource?.Token ?? CancellationToken.None;
    }
  }
}