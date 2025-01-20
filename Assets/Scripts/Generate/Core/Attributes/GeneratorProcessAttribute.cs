using System;

namespace Generate.Core.Attributes
{
  /// <summary>
  /// Атрибут для привязки процесса генерации к конкретному генератору
  /// </summary>
  [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
  public class GeneratorProcessAttribute : Attribute
  {
    /// <summary>
    /// Тип генератора, к которому привязан процесс
    /// </summary>
    public Type GeneratorType { get; }

    /// <summary>
    /// Создает новый экземпляр атрибута
    /// </summary>
    /// <param name="generatorType">Тип генератора</param>
    public GeneratorProcessAttribute(Type generatorType)
    {
      GeneratorType = generatorType;
    }
  }
}