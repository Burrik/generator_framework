using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Generate.Core.Attributes;

namespace Generate.Core.Utils
{
  /// <summary>
  /// Утилита для динамического создания и фильтрации типов
  /// </summary>
  /// <typeparam name="TInterface">Базовый интерфейс или тип для поиска</typeparam>
  public class TypeCreator<TInterface> where TInterface : class
  {
    /// <summary>
    /// Массив найденных и отфильтрованных типов
    /// </summary>
    public Type[] Types { get; }

    /// <summary>
    /// Флаг, указывающий, что все найденные типы являются наследниками MonoBehaviour
    /// </summary>
    private readonly bool requireMonoBehaviour;

    /// <summary>
    /// Инициализирует новый экземпляр TypeCreator с возможностью дополнительной фильтрации
    /// </summary>
    /// <param name="additionalFilter">Дополнительный предикат для фильтрации типов</param>
    /// <param name="requireMonoBehaviour">Требовать ли наследование от MonoBehaviour</param>
    public TypeCreator(
        Func<Type, bool> additionalFilter = null,
        bool requireMonoBehaviour = true)
    {
      this.requireMonoBehaviour = requireMonoBehaviour;
      Types = FindTypes()
          .Where(t => IsValidType(t) && (additionalFilter?.Invoke(t) ?? true))
          .OrderBy(t => t.Name)
          .ToArray();
    }

    /// <summary>
    /// Находит все типы в сборке, реализующие указанный интерфейс
    /// </summary>
    private Type[] FindTypes()
    {
      var assemblies = AppDomain.CurrentDomain.GetAssemblies()
          .Where(assembly =>
          {
            string assemblyName = assembly.GetName().Name;
            return assemblyName.StartsWith("Assembly-CSharp") ||
                     assemblyName.StartsWith("Mart.") ||
                     assemblyName.StartsWith("Assets");
          });

      var types = assemblies
          .SelectMany(assembly =>
          {
            try
            {
              return assembly.GetTypes();
            }
            catch(Exception ex)
            {
              Debug.LogError($"Error loading types from assembly {assembly.GetName().Name}: {ex.Message}");
              return Enumerable.Empty<Type>();
            }
          })
          .Where(type =>
              type != null &&
              type.IsClass &&
              !type.IsAbstract &&
              typeof(TInterface).IsAssignableFrom(type) &&
              (!requireMonoBehaviour || typeof(MonoBehaviour).IsAssignableFrom(type))
          )
          .ToArray();

      return types;
    }

    /// <summary>
    /// Проверяет валидность типа
    /// </summary>
    /// <param name="type">Проверяемый тип</param>
    /// <returns>True, если тип валиден, иначе false</returns>
    private bool IsValidType(Type type)
    {
      if(type == null || !type.IsClass || type.IsAbstract)
        return false;

      if(!typeof(TInterface).IsAssignableFrom(type))
        return false;

      if(requireMonoBehaviour && !typeof(MonoBehaviour).IsAssignableFrom(type))
        return false;

      return true;
    }

    /// <summary>
    /// Добавляет компонент указанного типа к целевому GameObject
    /// </summary>
    /// <param name="type">Тип добавляемого компонента</param>
    /// <param name="target">GameObject, к которому добавляется компонент</param>
    /// <returns>Добавленный компонент или null, если компонент уже существует</returns>
    public Component AddComponent(Type type, GameObject target)
    {
      if(!Types.Contains(type))
        throw new ArgumentException($"Type {type.Name} is not valid for {typeof(TInterface).Name}");

      if(target.GetComponent(type) != null)
        return null;

      return target.AddComponent(type);
    }
  }
}