namespace Generate.Core.Template
{
  public struct FloorContextData : ILayerContextData
  {
    public int FloorIndex;
    public float Height;
    public bool Windows;

    public bool Validate() => Height > 0;
  }
}