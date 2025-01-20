using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Compilation;
using Mono.Cecil;


namespace Generate.Core
{
  public static class LayerDependencyAnalyzer
  {
    private static readonly Dictionary<Type, LayerAnalysisCache> layerCache = new();
    private static readonly Dictionary<System.Reflection.Assembly, ModuleDefinition> moduleCache = new();
    private static readonly Dictionary<(Type Type, string Method), MethodDefinition> stateMachineCache = new();

    static LayerDependencyAnalyzer()
    {
      CompilationPipeline.compilationFinished += _ =>
      {
        layerCache.Clear();
        stateMachineCache.Clear();
        foreach(var module in moduleCache.Values)
        {
          module.Dispose();
        }
        moduleCache.Clear();
      };
    }

    private static ModuleDefinition GetModuleForAssembly(System.Reflection.Assembly assembly)
    {
      if(!moduleCache.TryGetValue(assembly, out var module))
      {
        module = ModuleDefinition.ReadModule(assembly.Location);
        moduleCache[assembly] = module;
      }
      return module;
    }

    public static (bool RequiresContext, string LayerName, string[] MissingDataTypes) AnalyzeContainer(
      LayersContainer container,
      GeneratorData generatorData,
      LayerContext context)
    {
      var totalSw = System.Diagnostics.Stopwatch.StartNew();
      if(container == null) return (false, null, null);

      foreach(var layer in container.GetActiveLayersEnumerable())
      {
        var type = layer.GetType();
        LayerAnalysisCache analysis;

        if(!layerCache.TryGetValue(type, out analysis))
        {
          var hasContext = HasTryGetContext(type);
          var dataTypes = FindRequiredDataTypes(type);
          var contextTypes = hasContext ? FindContextTypes(type) : new List<Type>();

          analysis = new LayerAnalysisCache(hasContext, dataTypes, contextTypes);
          layerCache[type] = analysis;
        }

        if(analysis.DataTypes.Any())
        {
          if(generatorData == null)
          {
            return (false, type.Name, analysis.DataTypes.Select(t => t.Name).ToArray());
          }
          if(!generatorData.ValidateRequiredData(analysis.DataTypes))
          {
            return (false, type.Name, analysis.DataTypes
              .Where(t => !generatorData.HasData(t))
              .Select(t => t.Name)
              .ToArray());
          }
        }

        if(analysis.HasContext && analysis.ContextTypes.Any())
        {
          if(context != null)
          {
            var missingContextTypes = analysis.ContextTypes
              .Where(t => !context.HasContext(t));
            if(missingContextTypes.Any())
            {
              return (true, type.Name, missingContextTypes.Select(t => t.Name).ToArray());
            }
          }
          else
          {
            return (true, type.Name, analysis.ContextTypes.Select(t => t.Name).ToArray());
          }
        }
      }

      totalSw.Stop();
      return (false, null, null);
    }

    private static bool HasTryGetContext(Type type)
    {
      return type.GetMethod("TryGetContext",
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy) != null;
    }

    private static List<Type> FindRequiredDataTypes(Type layerType)
    {
      var dataTypes = new HashSet<Type>();

      try
      {
        var assembly = System.Reflection.Assembly.GetAssembly(layerType);
        var module = GetModuleForAssembly(assembly);
        var moveNext = FindStateMachine(module, layerType, "OnGenerate");

        if(moveNext != null)
        {
          var visitedMethods = new HashSet<string>();
          AnalyzeMethodRecursive(moveNext, dataTypes, "GetData", assembly, visitedMethods);
        }
      }
      catch(Exception e)
      {
        Debug.LogError($"Error: {e}");
      }

      return dataTypes.ToList();
    }

    private static List<Type> FindContextTypes(Type type)
    {
      var contextTypes = new HashSet<Type>();

      try
      {
        var assembly = System.Reflection.Assembly.GetAssembly(type);
        var module = GetModuleForAssembly(assembly);
        var moveNext = FindStateMachine(module, type, "OnInit");

        if(moveNext != null)
        {
          var visitedMethods = new HashSet<string>();
          AnalyzeMethodRecursive(moveNext, contextTypes, "TryGetContext", assembly, visitedMethods);
        }
      }
      catch(Exception e)
      {
        Debug.LogError($"Ошибка анализа типов контекста: {e}");
      }

      return contextTypes.ToList();
    }

    private static void AnalyzeMethodRecursive(
      MethodDefinition method,
      HashSet<Type> types,
      string targetMethodName,
      System.Reflection.Assembly assembly,
      HashSet<string> visitedMethods)
    {
      if(method?.Body == null) return;

      var methodId = $"{method.DeclaringType.FullName}.{method.Name}";
      if(!visitedMethods.Add(methodId)) return;

      AnalyzeMethodInstructions(method, types, targetMethodName, assembly);

      foreach(var instruction in method.Body.Instructions)
      {
        if(instruction.Operand is MethodReference calledMethodRef)
        {
          try
          {
            var calledMethod = calledMethodRef.Resolve();
            if(calledMethod != null)
            {
              AnalyzeMethodRecursive(calledMethod, types, targetMethodName, assembly, visitedMethods);
            }
          }
          catch(Exception)
          {
            // Игнорируем ошибки резолва методов из других сборок
          }
        }
      }
    }

    private static void AnalyzeMethodInstructions(
      MethodDefinition method,
      HashSet<Type> types,
      string targetMethodName,
      System.Reflection.Assembly assembly)
    {
      if(method?.Body == null) return;

      var instructions = method.Body.Instructions;

      for(int i = 0; i < instructions.Count; i++)
      {
        var genericMethod = instructions[i].Operand as GenericInstanceMethod;
        if(genericMethod?.Name == targetMethodName)
        {
          foreach(var genericArg in genericMethod.GenericArguments)
          {
            var typeName = genericArg.FullName;
            var paramType = Type.GetType($"{typeName}, {assembly.GetName().Name}");
            if(paramType != null)
              types.Add(paramType);
          }
        }
      }
    }

    private static MethodDefinition FindStateMachine(ModuleDefinition module, Type type, string methodName)
    {
      var key = (type, methodName);

      if(!stateMachineCache.TryGetValue(key, out var moveNext))
      {
        var stateMachineType = module.GetTypes()
          .FirstOrDefault(t => t.FullName.Contains($"{type.FullName}/<{methodName}>"));

        moveNext = stateMachineType?.Methods
          .FirstOrDefault(m => m.Name == "MoveNext");

        stateMachineCache[key] = moveNext;
      }

      return moveNext;
    }

    private class LayerAnalysisCache
    {
      public bool HasContext { get; }
      public IReadOnlyList<Type> DataTypes { get; }
      public IReadOnlyList<Type> ContextTypes { get; }

      public LayerAnalysisCache(bool hasContext, List<Type> dataTypes, List<Type> contextTypes)
      {
        HasContext = hasContext;
        DataTypes = dataTypes;
        ContextTypes = contextTypes;
      }
    }
  }
}