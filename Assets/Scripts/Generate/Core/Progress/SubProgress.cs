using UnityEngine;

namespace Generate.Core
{

  public class SubProgress : IProgressWithStage
  {
    [SerializeField, Range(0, 1)]
    private float from;

    [SerializeField, Range(0, 1)]
    private float to;

    private readonly IProgressWithStage parent;
    private float currentProgress;
    private string currentStage;

    public float CurrentProgress => currentProgress;
    public string CurrentStage => currentStage;

    public SubProgress(IProgressWithStage parent, float from, float to)
    {
      this.parent = parent;
      this.from = Mathf.Clamp01(from);
      this.to = Mathf.Clamp01(to);
    }

    public void Report(float progress)
    {
      currentProgress = progress;
      var scaledProgress = from + (to - from) * progress;
      parent?.Report(scaledProgress);
    }

    public void UpdateStage(string stage)
    {
      currentStage = stage;
      parent?.UpdateStage(stage);
    }
  }
}