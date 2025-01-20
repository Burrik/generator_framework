using UnityEngine;
using System.Linq;
using System;
using System.Collections.Generic;

namespace Generate.Core
{
  [CreateAssetMenu(fileName = "LayersContainer", menuName = "Generate/Process/LayersContainer")]
  public class LayersContainer : ScriptableObject
  {
    [SerializeField] private ProcessLayer[] layers;

    public int Count => layers?.Length ?? 0;

    public ProcessLayer[] GetActiveLayers()
    {
      if(layers == null) return Array.Empty<ProcessLayer>();
      return layers.Where(l => l != null && l.isEnabled).ToArray();
    }

    public IEnumerable<ProcessLayer> GetActiveLayersEnumerable()
    {
      if(layers == null) yield break;

      for(int i = 0; i < layers.Length; i++)
      {
        var layer = layers[i];
        if(layer != null && layer.isEnabled)
        {
          yield return layer;
        }
      }
    }

    public bool Validate(out string error)
    {
      error = string.Empty;

      if(layers == null)
      {
        error = "Список слоев пуст";
        return false;
      }

      if(layers.Any(l => l == null))
      {
        error = "В списке слоев есть пустые элементы";
        return false;
      }

      var activeLayers = GetActiveLayers();
      if(activeLayers.Length == 0)
      {
        error = "No active layers found";
        return false;
      }

      return true;
    }
  }
}
