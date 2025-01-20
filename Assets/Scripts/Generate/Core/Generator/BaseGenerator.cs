using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System;
using UnityEditor;

namespace Generate.Core
{
  public abstract class BaseGenerator : MonoBehaviour
  {
    [SerializeField] protected GeneratorData data;
    [SerializeField, HideInInspector] private GeneratorCore core;

    public GeneratorData Data => data;
    public bool IsGenerating => core.IsGenerating;
    public IReadOnlyList<IGeneratorProcess> ProcessesList => core.ProcessesList;

    public event Action OnGenerationStarted;
    public event Action<bool> OnGenerationCompleted;

    protected virtual async UniTask OnInitialize() => await UniTask.Yield();
    protected virtual async UniTask OnGenerate() => await UniTask.Yield();

    public async UniTask Generate(IProgressWithStage progress, CancellationToken token = default)
    {
      try
      {
        OnGenerationStarted?.Invoke();
        await OnInitialize();
        await OnGenerate();
        await UniTask.Yield(PlayerLoopTiming.TimeUpdate, token);
        await core.Generate(data, progress, token);
        OnGenerationCompleted?.Invoke(true);
      }
      catch
      {
        OnGenerationCompleted?.Invoke(false);
      }
    }

    protected virtual void OnEnable()
    {
      EnsureCore();
      core.Initialize(GetComponents<IGeneratorProcess>());
    }

    private void EnsureCore()
    {
      core ??= new GeneratorCore();
    }

    public void OnValidate()
    {
      if(!enabled || !gameObject.activeInHierarchy) return;

      EnsureCore();
      core.Initialize(GetComponents<IGeneratorProcess>());
    }

    public async UniTask RequestProcessRegeneration(
        IGeneratorProcess process,
        IProgressWithStage progress = null,
        CancellationToken token = default)
    {
      await core.RequestProcessRegeneration(process, progress, token);
    }

    public virtual bool ValidateGenerator(out string error)
    {
      error = "Generator validation failed";
      return true;
    }

    public void CancelGeneration()
    {
      if(core.IsGenerating)
      {
        Debug.Log("Cancelling generation...");
        core.CancelGeneration();
        EditorUtility.SetDirty(this);
      }
    }


    public CancellationToken GetCancellationToken() => core.GetCancellationToken();
  }
}