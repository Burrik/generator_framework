using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Generate.Core
{
  /// <summary>
  /// Контейнер для хранения и управления данными генерации
  /// </summary>
  [Serializable]
  public class GeneratorDataContainer
  {
    private Dictionary<Type, IGeneratorData> dataMap = new();

    [SerializeReference]
    private List<IGeneratorData> dataList = new();

    public void Clear()
    {
      dataMap.Clear();
      dataList.Clear();
    }

    /// <summary>
    /// Добавляет новые данные в контейнер
    /// </summary>
    /// <param name="data">Данные для добавления</param>
    /// <returns>true если данные успешно добавлены, false если данные null или такой тип уже существует</returns>
    public bool TryAddData(IGeneratorData data)
    {
      if(data == null) return false;
      if(data is UnityEngine.Object)
      {
        Debug.LogError($"Невозможно добавить UnityEngine.Object как IGeneratorData: {data.GetType()}");
        return false;
      }
      return dataMap.TryAdd(data.GetType(), data);
    }

    /// <summary>
    /// Получает данные указанного типа
    /// </summary>
    /// <typeparam name="T">Тип данных, реализующий IGeneratorData</typeparam>
    /// <returns>Данные указанного типа или default если данные не найдены</returns>
    public T GetData<T>() where T : IGeneratorData
    {
      var type = typeof(T);
      return dataMap.TryGetValue(type, out var value) ? (T)value : default;
    }

    /// <summary>
    /// Проверяет наличие данных указанного типа
    /// </summary>
    public bool HasData<T>() where T : IGeneratorData => dataMap.ContainsKey(typeof(T));

    /// <summary>
    /// Проверяет наличие данных указанного типа
    /// </summary>
    public bool HasData(Type type) => dataMap.ContainsKey(type);

    /// <summary>
    /// Возвращает все хранимые данные
    /// </summary>
    public IEnumerable<IGeneratorData> GetAllData() => dataMap.Values;

    /// <summary>
    /// Получает список всех данных для сериализации
    /// </summary>
    public List<IGeneratorData> GetDataList() => dataMap.Values.ToList();

    /// <summary>
    /// Устанавливает данные из десериализованного списка
    /// </summary>
    public void SetDataList(List<IGeneratorData> list)
    {
      dataMap = new Dictionary<Type, IGeneratorData>();
      foreach(var data in list)
      {
        if(data != null && !(data is UnityEngine.Object))
        {
          dataMap[data.GetType()] = data;
        }
      }
    }

    public void OnBeforeSerialize()
    {
      dataList = dataMap.Values.ToList();
    }

    public void OnAfterDeserialize()
    {
      dataMap = new Dictionary<Type, IGeneratorData>();
      foreach(var data in dataList)
      {
        if(data != null && !(data is UnityEngine.Object))
        {
          dataMap[data.GetType()] = data;
        }
      }
    }
  }
}