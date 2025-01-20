using UnityEngine;

namespace Generate.Core.Template
{
  /// <summary>
  /// Пример компонента данных для генератора.
  /// Показывает базовое использование IGeneratorData.
  /// </summary>
  [System.Serializable]
  public class ExampleData : IGeneratorData
  {
    [SerializeField] private string id = "example";
    [SerializeField] private float size = 1f;
    [SerializeField] private Color color = Color.white;

    public string Id => id;
    public float Size => size;
    public Color Color => color;

    public bool Validate()
    {
      return !string.IsNullOrEmpty(id) && size > 0f;
    }


    public IGeneratorData Clone()
    {
      return new ExampleData
      {
        id = this.id,
        size = this.size,
        color = this.color
      };
    }
  }
}