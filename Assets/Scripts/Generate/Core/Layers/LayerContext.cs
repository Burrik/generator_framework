using UnityEngine;
using System;
using System.Collections.Generic;

namespace Generate.Core
{
  /// <summary>
  /// Контекст выполнения слоя, содержит все необходимые данные
  /// </summary>
  public class LayerContext
  {
    private readonly Dictionary<Type, ILayerContextData> contextData = new();

    /// <summary>
    /// Основные данные генерации
    /// </summary>
    public GeneratorData Data { get; }

    private LayerContext(GeneratorData data)
    {
      Data = data;
    }

    /// <summary>
    /// Создает новый контекст с указанными данными генерации
    /// </summary>
    /// <param name="data">Данные генерации</param>
    public static LayerContext CreateContext(GeneratorData data)
    {
      return new LayerContext(data);
    }

    /// <summary>
    /// Создает новый контекст с указанными данными генерации
    /// </summary>
    /// <param name="data">Данные генерации</param>
    /// <param name="contextData">Данные контекста</param>
    /// <returns>Текущий контекст для chain вызовов</returns>
    public static LayerContext CreateContext(GeneratorData data, params ILayerContextData[] contextData)
    {
      var context = new LayerContext(data);

      foreach(var item in contextData)
      {
        context.Add(item);
      }

      return context;
    }

    /// <summary>
    /// Добавляет данные в контекст. Выполняет валидацию и проверку на дубликаты.
    /// </summary>
    /// <typeparam name="T">Тип данных контекста</typeparam>
    /// <param name="context">Данные контекста</param>
    /// <returns>Текущий контекст для chain вызовов</returns>
    public LayerContext Add<T>(T context) where T : struct, ILayerContextData
    {
      var type = typeof(T);

      if(contextData.ContainsKey(type))
      {
        Debug.LogError($"Контекст типа {type.Name} уже существует");
        return this;
      }

      if(!context.Validate())
      {
        Debug.LogError($"Ошибка валидации контекста для {type.Name}");
        return this;
      }

      contextData[type] = context;
      return this;
    }

    /// <summary>
    /// Добавляет данные в контекст. Выполняет валидацию и проверку на дубликаты.
    /// </summary>
    /// <param name="context">Данные контекста</param>
    /// <returns>Текущий контекст для chain вызовов</returns>
    private LayerContext Add(ILayerContextData context)
    {
      var type = context.GetType();

      if(contextData.ContainsKey(type))
      {
        Debug.LogError($"Контекст типа {type.Name} уже существует");
        return this;
      }

      if(!context.Validate())
      {
        Debug.LogError($"Ошибка валидации контекста для {type.Name}");
        return this;
      }

      contextData[type] = context;
      return this;
    }

    /// <summary>
    /// Пытается получить данные указанного типа из контекста
    /// </summary>
    /// <typeparam name="T">Тип данных контекста</typeparam>
    /// <param name="data">Полученные данные</param>
    /// <returns>true если данные найдены, иначе false</returns>
    public bool TryGetContext<T>(out T data) where T : ILayerContextData
    {
      if(contextData.TryGetValue(typeof(T), out var value))
      {
        data = (T)value;
        return true;
      }
      data = default;
      return false;
    }

    /// <summary>
    /// Проверяет наличие контекстных данных указанного типа
    /// </summary>
    /// <typeparam name="T">Тип контекстных данных</typeparam>
    /// <returns>true если данные найдены, иначе false</returns>
    public bool HasContext<T>() where T : ILayerContextData
    {
      return contextData.ContainsKey(typeof(T));
    }

    /// <summary>
    /// Проверяет наличие контекстных данных указанного типа
    /// </summary>
    /// <param name="type">Тип контекстных данных</param>
    /// <returns>true если данные найдены, иначе false</returns>
    public bool HasContext(Type type)
    {
      return contextData.ContainsKey(type);
    }
  }
}