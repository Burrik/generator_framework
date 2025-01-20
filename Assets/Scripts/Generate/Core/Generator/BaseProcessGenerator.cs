using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;
using Generate.Core.Attributes;

namespace Generate.Core
{
  /// <summary>
  /// Базовый класс для процессов генерации. Управляет жизненным циклом процесса:
  /// инициализация -> генерация -> регенерация.
  /// </summary>
  public abstract class BaseProcessGenerator : MonoBehaviour, IGeneratorProcess
  {
    [SerializeField, OptionalLayers] private LayersContainer layers;
    [SerializeField, HideInInspector] private bool isEnabled = true;
    [SerializeField, HideInInspector] private bool canBeRegenerated = true;
    [SerializeField, HideInInspector] private GeneratorData data;
    [SerializeReference, HideInInspector] private IGenerator generator;

    private readonly LayersExecutor layersExecutor = new();
    private ProgressExecuteLayers progressExecute;
    private IProgressWithStage progress;
    private CancellationToken token;

    /// <summary>Текущие данные генератора</summary>
    public GeneratorData Data => data;

    /// <summary>Отображаемое имя процесса в редакторе</summary>
    public abstract string DisplayName { get; }

    /// <summary>Определяет, активен ли процесс</summary>
    public bool IsEnabled { get => isEnabled; set => isEnabled = value; }

    /// <summary>Определяет, влияет ли регенерация на последующие процессы</summary>
    public bool CanBeRegenerated => canBeRegenerated;

    async UniTask IGeneratorProcess.Initialize(GeneratorData data, IGenerator generator, CancellationToken token)
    {
      try
      {
        this.data = data;
        this.token = token;
        this.generator = generator;
        token.ThrowIfCancellationRequested();
        await OnInitialize();
      }
      catch(OperationCanceledException)
      {
        HandleCancellation("initialization");
        throw;
      }
      finally
      {
        this.token = default;
      }
    }

    async UniTask IGeneratorProcess.Generate(IProgressWithStage progress, CancellationToken token)
    {
      try
      {
        this.token = token;

        // Добавляем валидацию данных
        if(!ValidateProcessData())
        {
          throw new InvalidOperationException(
            $"[{DisplayName}] Ошибка валидации данных перед запуском процесса");
        }

        await ExecuteWithProgress(OnGenerate, progress, token);
      }
      catch(OperationCanceledException)
      {
        HandleCancellation("generation");
        throw;
      }
      finally
      {
        this.token = default;
      }
    }

    async UniTask IGeneratorProcess.InitRegeneration(CancellationToken token)
    {
      try
      {
        this.token = token;
        token.ThrowIfCancellationRequested();
        await OnInitRegeneration();
      }
      catch(OperationCanceledException)
      {
        HandleCancellation("regeneration initialization");
        throw;
      }
      finally
      {
        this.token = default;
      }
    }

    async UniTask IGeneratorProcess.Regeneration(IProgressWithStage progress, CancellationToken token)
    {
      try
      {
        this.token = token;

        // Добавляем валидацию данных
        if(!ValidateProcessData())
        {
          throw new InvalidOperationException(
            $"[{DisplayName}] Ошибка валидации данных перед регенерацией");
        }

        await ExecuteWithProgress(OnRegeneration, progress, token);
      }
      catch(OperationCanceledException)
      {
        HandleCancellation("regeneration");
        throw;
      }
      finally
      {
        this.token = default;
      }
    }

    /// <summary>Вызывается при инициализации процесса</summary>
    protected virtual async UniTask OnInitialize() => await UniTask.CompletedTask;

    /// <summary>Основная логика генерации</summary>
    protected virtual async UniTask OnGenerate() => await UniTask.CompletedTask;

    /// <summary>Подготовка к регенерации</summary>
    protected virtual async UniTask OnInitRegeneration() => await OnInitialize();

    /// <summary>Основная логика регенерации</summary>
    protected virtual async UniTask OnRegeneration() => await OnGenerate();

    /// <summary>
    /// Выполняет генерацию слоев из контейнера
    /// </summary>
    /// <param name="context">Контекст для слоев</param>
    /// <param name="totalExecutes">Общее количество вызовов ExecuteLayers</param>
    protected async UniTask ExecuteLayers(LayerContext context = null, int totalExecutes = 1)
    {
      // 1. Проверяем наличие контекста
      if(context == null)
      {
        context = LayerContext.CreateContext(Data);
      }

      // 2. Проверяем наличие слоев
      if(layers == null)
      {
        throw new InvalidOperationException(
          $"[{DisplayName}] Не назначен контейнер слоев! " +
          $"Убедитесь, что слои назначены в инспекторе для {gameObject.name}");
      }

      // 3. Проверяем валидность слоев
      if(!layers.Validate(out string error))
      {
        throw new InvalidOperationException(
          $"[{DisplayName}] Ошибка валидации контейнера слоев: {error}");
      }

      // 4. Проверяем наличие всех необходимых типов данных через анализатор
      var (requiresContext, layerName, missingTypes) = LayerDependencyAnalyzer.AnalyzeContainer(layers, data, context);
      if(missingTypes?.Length > 0)
      {
        var errorMessage = requiresContext
          ? $"[{DisplayName}] {layerName} отсутствуют необходимые типы контекста: {string.Join(", ", missingTypes)}"
          : $"[{DisplayName}] {layerName} отсутствуют необходимые типы данных: {string.Join(", ", missingTypes)}";

        throw new InvalidOperationException(errorMessage);
      }

      // 5. Проверяем валидность данных
      if(!ValidateProcessData())
      {
        throw new InvalidOperationException($"[{DisplayName}] Ошибка валидации данных");
      }

      // 6. Выполняем слои
      if(progressExecute == null)
      {
        progressExecute = new ProgressExecuteLayers(totalExecutes, DisplayName);
      }
      await layersExecutor.Execute(layers, context, progressExecute, progress, token);
    }

    private void HandleCancellation(string operation)
    {
      Debug.Log($"{DisplayName} {operation} cancelled");
      progress?.Report(0f);
      progress?.UpdateStage($"{DisplayName} {operation} cancelled");
    }

    private async UniTask ExecuteWithProgress(
        Func<UniTask> action,
        IProgressWithStage progress,
        CancellationToken token)
    {
      try
      {
        this.progress = progress;
        progressExecute = null;

        progress?.Report(0f);
        progress?.UpdateStage($"Generate {DisplayName}...");
        Debug.Log($"[{DisplayName}] Starting generation...");
        token.ThrowIfCancellationRequested();

        await action();

        progress?.Report(1f);
        progress?.UpdateStage($"Generate {DisplayName} completed");
        Debug.Log($"[{DisplayName}] Generation completed");
      }
      finally
      {
        this.progress = null;
        progressExecute = null;
      }
    }

    private bool ValidateProcessData()
    {
      if(data == null)
      {
        Debug.LogError($"[{DisplayName}] GeneratorData не установлен");
        return false;
      }

      var allData = data.GetAllData();

      foreach(var item in allData)
      {
        if(item == null)
        {
          Debug.LogError($"[{DisplayName}] Найден null в GeneratorData");
          return false;
        }

        if(!item.Validate())
        {
          Debug.LogError($"[{DisplayName}] Ошибка валидации {item.GetType().Name}");
          return false;
        }
      }
      return true;
    }

    protected async UniTask Request(Func<UniTask> action)
    {
      if(!ValidateProcessData())
      {
        throw new InvalidOperationException($"[{DisplayName}] Ошибка валидации данных");
      }
      Debug.Log($"[{DisplayName}] Processing request...");
      await generator.Request(action);
      Debug.Log($"[{DisplayName}] Request completed");
    }

    protected async UniTask RequestRegeneration(
      IProgressWithStage progress = null,
      CancellationToken token = default)
    {
      if(!ValidateProcessData())
      {
        throw new InvalidOperationException($"[{DisplayName}] Ошибка валидации данных");
      }
      Debug.Log($"[{DisplayName}] Starting regeneration...");
      await generator.RequestProcessRegeneration(this, progress, token);
      Debug.Log($"[{DisplayName}] Regeneration completed");
    }
  }
}