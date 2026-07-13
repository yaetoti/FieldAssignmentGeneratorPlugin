using System;
using System.IO;
using JetBrains.Application.Parts;
using JetBrains.Application.Threading;
using JetBrains.Application.UI.Icons.FeaturesIntellisenseThemedIcons;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Cpp.Language;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Rider.Model;
using JetBrains.Util;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.Cpp.CodeCompletion;
using JetBrains.UI.Icons;

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

public class MyLookupItemBase : CppNoHotspotsLookupItem {
  // TODO replace with live template completion icon. Idk where tf it is
  public override IconId Image => FeaturesIntellisenseThemedIcons.EditorOptionsPage.Id;
  public override string Text => m_text;

  private string m_text;
  
  public MyLookupItemBase(CppCodeCompletionContext context) {
    // Input
    var text = "Generate field assignments";
    var rank = CppCompletionRanks.GenerateImplementationEntity;

    // Constructor
    m_text = text;
    
    var placement = new LookupItemPlacement(m_text, rank.ToRank(), PlacementLocation.Top);
    placement.Relevance = (ulong) ((CppCompletionRanks) placement.Relevance | rank);
    Placement = placement;
    
    var ranges = context.CompletionRanges;
    Ranges = ranges;
    VisualReplaceRangeMarker = ranges.CreateVisualReplaceRangeMarker();
  }
}

[Language(typeof(CppLanguage))]
public class MyCppCompletionProvider : ItemsProviderOfSpecificContext<CppCodeCompletionContext> {
  protected override bool IsAvailable(CppCodeCompletionContext context) {
    new MyLogger().Log("completion").Dump("completion.txt");
    return context.BasicContext.CodeCompletionType == CodeCompletionType.BasicCompletion ||
           context.BasicContext.CodeCompletionType == CodeCompletionType.SmartCompletion;
  }

  protected override bool AddLookupItems(CppCodeCompletionContext context, IItemsCollector collector) {
    collector.Add(new MyLookupItemBase(context));
    return true;
  }
}

[SolutionComponent(Instantiation.ContainerAsyncPrimaryThread)]
public class MyTestFrameworkBackendExt2 {
  public MyTestFrameworkBackendExt2(Lifetime lifetime, ILogger logger, ISolution solution, IShellLocks locks, MyPluginModel model) {
    new MyLogger().Log("Ext2 started").Dump("2_Init.txt");
    if (model == null) {
      new MyLogger().Log("no model").Dump("2_Error.txt");
    }
    
    new MyLogger().Log($"{model}").Dump("3_Model.txt");
    
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