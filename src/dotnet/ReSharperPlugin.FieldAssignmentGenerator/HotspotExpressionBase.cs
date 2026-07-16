using System;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Templates;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.TextControl;
using JetBrains.Util;

namespace ReSharperPlugin.FieldAssignmentGenerator;

// Stolen from CppCompleteDesignatedInitializationItemsProvider
internal class HotspotExpressionBase : IHotspotExpression {
  public string EvaluateQuickResult(IHotspotContext context) => "";
  public HotspotItems GetLookupItems(IHotspotContext context) => HotspotItems.Empty;
  public object Clone() => throw new NotImplementedException();
  public string Serialize() => throw new NotImplementedException();
  
  public void HandleExpansion(IHotspotContext context) {
    ITextControl openedTextControl = context.GetOpenedTextControl();
    if (openedTextControl == null) {
      return;
    }
    
    ITextControlSelection selection = openedTextControl.Selection;
    DocumentRange expressionRange = context.ExpressionRange;
    TextRange textRange = expressionRange.TextRange;
    selection.SetRange(textRange);
    ISolution solution = context.SessionContext.Solution;
    expressionRange = context.ExpressionRange;
    if (expressionRange.Document.GetPsiSourceFile(solution) == null) {
      return;
    }
    
    solution.GetComponent<ICodeCompletionSessionManager>().ExecuteManualCompletion(CodeCompletionType.BasicCompletion, openedTextControl, solution, EmptyAction.Instance, EvaluationMode.Light, AutoAcceptBehaviour.DoNotAutoAccept, LookupReplaceBehaviour.AlwaysReplace, LookupFocusBehaviour.Soft);
  }
}