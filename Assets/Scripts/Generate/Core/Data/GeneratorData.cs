using UnityEngine;
using System;
using System.Collections.Generic;


namespace Generate.Core
{
  /// <summary>
  /// Контейнер для данных генерации.
  /// Хранит различные типы данных, используемые в процессе генерации.
  /// </summary>
  [CreateAssetMenu(fileName = "GeneratorData", menuName = "Generate/GeneratorData")]
  public class GeneratorData : ScriptableObject, ISerializationCallbackReceiver
  {
    [SerializeField]
    private GeneratorDataContainer container = new();

    /// <summary>
    /// Очищает все данные и создает новый контейнер
    /// </summary>
    public void Clear()
    {
      container = new GeneratorDataContainer();

#if UNITY_EDITOR
      // Помечаем ассет как "грязный" для сохранения
      UnityEditor.EditorUtility.SetDirty(this);
      // Сохраняем все ассеты
      UnityEditor.AssetDatabase.SaveAssets();
#endif
    }

    /// <summary>
    /// Добавляет новые данные в контейнер
    /// </summary>
    /// <param name="data">Данные для добавления</param>
    /// <returns>true если данные успешно добавлены, false если данные null или такой тип уже существует</returns>
    public bool TryAddData(IGeneratorData data) => container.TryAddData(data);

    /// <summary>
    /// Получает данные указанного типа
    /// </summary>
    /// <typeparam name="T">Тип данных, реализующий IGeneratorData</typeparam>
    /// <returns>Данные указанного типа или default если данные не найдены</returns>
    public T GetData<T>() where T : IGeneratorData => container.GetData<T>();

    /// <summary>
    /// Проверяет наличие данных указанного типа
    /// </summary>
    /// <typeparam name="T">Тип данных, реализующий IGeneratorData</typeparam>
    /// <returns>true если данные существуют, false если нет</returns>
    public bool HasData<T>() where T : IGeneratorData => container.HasData<T>();

    /// <summary>
    /// Проверяет наличие данных указанного типа
    /// </summary>
    /// <param name="type">Тип данных</param>
    /// <returns>true если данные существуют, false если нет</returns>
    public bool HasData(Type type) => container.HasData(type);

    /// <summary>
    /// Возвращает все хранимые данные
    /// </summary>
    /// <returns>Коллекция всех данных</returns>
    public IEnumerable<IGeneratorData> GetAllData() => container.GetAllData();

    /// <summary>
    /// Пытается получить данные указанного типа
    /// </summary>
    /// <typeparam name="T">Тип данных, реализующий IGeneratorData</typeparam>
    /// <param name="data">Полученные данные</param>
    /// <returns>true если данные найдены, false если данные не существуют</returns>
    public bool TryGetData<T>(out T data) where T : IGeneratorData
    {
      var type = typeof(T);
      if(container.HasData(type))
      {
        data = container.GetData<T>();
        return true;
      }
      data = default;
      return false;
    }

    /// <summary>
    /// Проверяет наличие всех требуемых типов данных
    /// </summary>
    /// <param name="requiredTypes">Список требуемых типов</param>
    /// <returns>true если все требуемые типы присутствуют, false если какой-то тип отсутствует</returns>
    public bool ValidateRequiredData(IEnumerable<Type> requiredTypes)
    {
      foreach(var type in requiredTypes)
      {
        if(!container.HasData(type))
        {
          Debug.LogError($"Отсутствует необходимый тип данных: {type.Name}");
          return false;
        }
      }
      return true;
    }

    void ISerializationCallbackReceiver.OnBeforeSerialize() => container.OnBeforeSerialize();
    void ISerializationCallbackReceiver.OnAfterDeserialize() => container.OnAfterDeserialize();
  }
}