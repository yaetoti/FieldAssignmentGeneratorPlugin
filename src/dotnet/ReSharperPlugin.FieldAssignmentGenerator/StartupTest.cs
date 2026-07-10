using System;
using System.IO;
using JetBrains.Application.Parts;
using JetBrains.Application.Threading;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.Rd.Base;
using JetBrains.Rd.Tasks;
using JetBrains.RdBackend.Common.Env;
using JetBrains.RdBackend.Common.Features.Documents;
using JetBrains.ReSharper.Feature.Services.Protocol;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Rider.Model;
using JetBrains.Util;

namespace ReSharperPlugin.FieldAssignmentGenerator;

public class MyLogger {
  private string m_content = "";
  
  public MyLogger Append(string message) {
    m_content += message;
    return this;
  }
  
  public MyLogger Log(string message) {
    m_content += message + "\n";
    return this;
  }

  public MyLogger Dump(string file) {
    File.WriteAllText(@"C:\temp\" + file, m_content);
    return this;
  }

  public MyLogger Clear() {
    m_content = "";
    return this;
  }
}

[SolutionComponent(Instantiation.ContainerAsyncPrimaryThread)]
public class MyTestFrameworkBackendExt2 {
  public MyTestFrameworkBackendExt2(Lifetime lifetime, ILogger logger, ISolution solution, IShellLocks locks, MyPluginModel model) {
    new MyLogger().Log("Ext2 started").Dump("2_Init.txt");
    
    new MyLogger().Log($"{model}").Dump("3_Error.txt");
    
    //var model = solution.<MyPluginModel>(() => new MyPluginModel(lifetime, protocolSolution.Protocol));
    
    if (model == null) {
      new MyLogger().Log("no model").Dump("2_Error.txt");
    }
    
    model.GetTestExecutionCommand.SetSync((line) => {
      var l = new MyLogger();
      l.Log("Was called");
      l.Dump("2_Log.txt");
      
      using (locks.UsingReadLock()) {

      }
      return "aaa";
    });
  }
}

[SolutionComponent(Instantiation.ContainerAsyncPrimaryThread)]
public class MyTestFrameworkBackendExt {
  private static String CONTENT = "";

  static void Log(String message) {
    CONTENT += message;
  }
  
  static void LogLine(String message) {
    CONTENT += message + "\n";
  }

  static void DumpLog(String file) {
    File.WriteAllText(file, CONTENT);
    CONTENT = "";
  }
  
  public MyTestFrameworkBackendExt(Lifetime lifetime, ILogger logger, ISolution solution, IShellLocks locks) {
    File.WriteAllText(@"C:\temp\loaded.txt", "Hello");
    String content = "";

    logger.Info("KOKOKO 0");
    using (locks.UsingWriteLock()) {
      logger.Info("KOKOKO 1");
      
      // var type = solution.SolutionProject.GetType();
      // logger.Info(type.FullName);
      // foreach (var i in type.GetInterfaces())
      //   logger.Info(i.FullName);
      
      LogLine("Solution name:");
      LogLine(solution.Name);
      
      LogLine("Start enumerating:");
      foreach (var project in solution.GetAllProjects()) {
        LogLine($"{project.Name}:");
        
        LogLine($"Project files:");
        foreach (var file in project.GetAllProjectFiles()) {
          LogLine($"- Name: {file.Name}");
          LogLine($"- LanguageType: {file.LanguageType}");
          LogLine($"- file.ToSourceFile(): {file.GetPsiModule()}");
        }
        
        LogLine($"Project PSI modules:");
        foreach (var module in project.GetPsiModules()) {
          LogLine($"- {module.Name}");
        }
      }
      

      
      logger.Info("KOKOKO 2");
    }
    
    
    DumpLog(@"C:\temp\log.txt");
  }
}

// [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
// public class MyTestFrameworkBackendExt {
//   public MyTestFrameworkBackendExt(ISolution solution) {
//     var protocolSolution = solution.GetProtocolSolution();
//     var model = protocolSolution.GetExtension<MyPluginModel>(nameof(MyPluginModel));
//     
//     model.GetTestExecutionCommand.SetSync(request => $"Command for {request}");
//   }
// }