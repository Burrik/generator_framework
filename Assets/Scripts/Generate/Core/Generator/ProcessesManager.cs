using System.Collections.Generic;
using System.Linq;
using System;

namespace Generate.Core
{
  [Serializable]
  public class ProcessesManager
  {
    private List<IGeneratorProcess> processes = new();

    public IReadOnlyList<IGeneratorProcess> ProcessesList => processes;

    public void Initialize(IEnumerable<IGeneratorProcess> newProcesses)
    {
      processes = newProcesses?.ToList() ?? new List<IGeneratorProcess>();
    }
  }
}