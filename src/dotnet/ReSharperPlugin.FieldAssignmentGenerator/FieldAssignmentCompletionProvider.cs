using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi.Cpp.Tree;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Psi.Cpp.Language;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.Cpp.CodeCompletion;
using JetBrains.ReSharper.Psi;

namespace ReSharperPlugin.FieldAssignmentGenerator;

[Language(typeof(CppLanguage))]
public class FieldAssignmentCompletionProvider : ItemsProviderOfSpecificContext<CppCodeCompletionContext> {
  protected override bool IsAvailable(CppCodeCompletionContext context) {
    if (context.UnterminatedContext.RootNode is PPPragmaDirective) {
      return false;
    }

    if (context.BasicContext.CodeCompletionType != CodeCompletionType.BasicCompletion && context.BasicContext.CodeCompletionType != CodeCompletionType.SmartCompletion) {
      return false;
    }
    
    var ctx = FieldAssignmentContext.Create(context);
    if (ctx is null) {
      //TcpLogger.SLog("Heavy fail");
      return false;
    }

    var suitableFields = ctx.GetSuitableFields();
    if (suitableFields.Count == 0) {
      //TcpLogger.SLog("Heavy fail");
      return false;
    }
    
    return true;
  }

  protected override bool AddLookupItems(CppCodeCompletionContext context, IItemsCollector collector) {
    //TcpLogger.SLog("AddLookupItems");
    collector.Add(new FieldAssignmentLookupItem(context));
    
    return base.AddLookupItems(context, collector);
  }
  
  public override EvaluationMode SupportedEvaluationMode => EvaluationMode.Light;
  public override CompletionMode SupportedCompletionMode => CompletionMode.Single;
  protected override LookupFocusBehaviour GetLookupFocusBehaviour(CppCodeCompletionContext context) => LookupFocusBehaviour.Soft;
}