using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi.Cpp.Symbols;
using JetBrains.ReSharper.Psi.Cpp.Tree;
using JetBrains.TextControl;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Cpp.Language;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems.Impl;
using JetBrains.ReSharper.Feature.Services.Cpp.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.Cpp.CodeStyle;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Templates;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Psi.Cpp.Expressions;
using JetBrains.ReSharper.Psi.Cpp.Resolve;
using JetBrains.ReSharper.Psi.Format;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.UI.Icons;
using JetBrains.UI.RichText;
using JetBrains.Util;
using JetBrains.Util.dataStructures.TypedIntrinsics;
using JetBrains.Util.Media;

namespace ReSharperPlugin.FieldAssignmentGenerator;

// Stolen from CppCompleteDesignatedInitializationItemsProvider
internal class HotspotExpression : IHotspotExpression {
  public string EvaluateQuickResult(IHotspotContext context) => "";

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

  public HotspotItems GetLookupItems(IHotspotContext context) => HotspotItems.Empty;

  public object Clone() => throw new NotImplementedException();

  public string Serialize() => throw new NotImplementedException();
}

// References:
// CppItemCollector::ConfigureCompleteDesignatedInitializationPlacement()
// CppCompleteDesignatedInitializationItemsProvider
// CppItemsProviderOnReferenceExpression - calls add items of designated
// CppCompletionTreeUtil - gets designated initialization members
// CppFormattingSettingsKey - settings
public class MyLookupItemBase : TextLookupItemBase {
  public const string NAME = "<generate-field-assignments>";
  
  public override IconId Image => ServicesThemedIcons.LiveTemplate.Id;

  private readonly CppCodeCompletionContext m_context;
  
  public MyLookupItemBase(CppCodeCompletionContext context) {
    m_context = context;
    Placement = new LookupItemPlacement("000000a");
    Placement.Relevance |= (ulong)CppCompletionRanks.CompleteDesignatedInitialization;
    Ranges = context.CompletionRanges;
  }

  protected override RichText GetDisplayName() {
    return new RichText(NAME, new TextStyle(JetFontStyles.Italic, TextColor, new JetRgbaColor()));
  }

  public override void Accept(ITextControl textControl, DocumentRange nameRange, LookupItemInsertType insertType, Suffix suffix, ISolution solution, bool keepCaretStill) {
    Test0(textControl, nameRange, insertType, suffix, solution, keepCaretStill);
    
    // Getting node is better because m_context.MemberAccessExpression's offsets are inconsistent. They may be correct and may be 0
    var node = GetNodeUnderCaret();
    if (node is null) {
      TcpLogger.SLog("node is null");
      return;
    }
    
    // Filter context (expression)
    if (node.GetContainingNode<ExpressionStatement>() is null) {
      TcpLogger.SLog("Not inside ICppExpressionNode");
      return;
    }

    if (node.Parent is not MemberAccessExpression accessExpression) {
      TcpLogger.SLog("nodeContainer is null");
      return;
    }
    
    // Getting the class type
    if (accessExpression.Qualifier is not ICppAnyResolvedReferenceExpression qualifier) {
      TcpLogger.SLog("Qualifier is not ICppAnyResolvedReferenceExpression");
      return;
    }
    
    if (qualifier.GetResolvedReference().GetPrimaryEntityIfStatusIsOk() is not ICppDeclaratorResolveEntity resolveEntity) {
      TcpLogger.SLog("GetPrimaryEntityIfStatusIsOk() as ICppDeclaratorResolveEntity is null");
      return;
    }

    var cppType = resolveEntity.GetCppType();
    var classResolveEntity = cppType.InternalAs<ICppClassResolveEntity>();
    if (classResolveEntity is null) {
      TcpLogger.SLog("Internal type is null");
      return;
    }
    
    // TODO classResolveEntity.GetBases() for base classes
    
    // Filter struct and class
    var classKey = classResolveEntity.GetKey();
    if (classKey != CppClassKey.CLASS && classKey != CppClassKey.STRUCT) {
      TcpLogger.SLog("Internal type is not class or struct");
      return;
    }
    
    // TODO replace classResolveEntity.GetAggregateMembers()
    // Find suitable fields
    var suitableFields = new List<string>();
    foreach (var classChild in classResolveEntity.GetChildren()) {
      if (classChild is not CppDeclaratorResolveEntityPack classChildPack) {
        continue;
      }
      
      foreach (var variable in classChildPack.GetGroupedVariables()) {
        // Filter non-static fields
        if (!variable.IsNonStaticField()) {
          continue;
        }

        // TODO check relative accessibility
        // Filter accessibility
        if (variable.GetAccessibility() != CppAccessibility.PUBLIC) {
          continue;
        }
        
        suitableFields.Add(variable.Name.ToString());
        TcpLogger.SLog($"Good variable: {variable.Name}");
      }
    }

    if (suitableFields.Count == 0) {
      TcpLogger.SLog($"No suitable fields");
      return;
    }
    
    // Extract useful info
    var document = m_context.BasicContext.Document;
    int start = accessExpression.Qualifier.GetDocumentRange().StartOffset.Offset;
    int end = m_context.BasicContext.CaretDocumentOffset.Offset;

    string qualifierText = accessExpression.Qualifier.GetText();
    string signText = accessExpression.Sign.GetText();

    // Extract settings
    var settings = m_context.BasicContext.ContextBoundSettingsStore;
    var indentStyle = settings.GetValue<CppFormattingSettingsKey, IndentStyle>(key => key.INDENT_STYLE);
    var indentSize = settings.GetValue<CppFormattingSettingsKey, int>(key => key.INDENT_SIZE);
    var spaceAroundAssignment = settings.GetValue<CppFormattingSettingsKey, bool>(key => key.SPACE_AROUND_ASSIGNMENT_OPERATOR);
    var spaceAroundDot = settings.GetValue<CppFormattingSettingsKey, bool>(key => key.SPACE_AROUND_DOT);
    
    var indentText = DocumentIndentUtils.GetLineIndent(document, new DocumentOffset(document, start).ToDocumentCoords().Line);
    
    TcpLogger.SLog($"QualifierText: {qualifierText}");
    TcpLogger.SLog($"SignText: {signText}");
    TcpLogger.SLog($"Range: {start} - {end}");
    
    // TODO add hotspot offset
    // Build text
    var hotspotOffsets = new List<int>();
    var sb = new StringBuilder();
    for (int i = 0; i < suitableFields.Count; ++i) {
      var field = suitableFields[i];
      
      sb.Append(qualifierText);
        
      if (spaceAroundDot) sb.Append(' ');
      sb.Append(signText);
      if (spaceAroundDot) sb.Append(' ');
        
      sb.Append(field);
        
      if (spaceAroundAssignment) sb.Append(' ');
      sb.Append("=");
      if (spaceAroundAssignment) sb.Append(' ');
      
      hotspotOffsets.Add(start + sb.Length);
      
      sb.Append(";\n");
      sb.Append(indentText);
    }

    var text = sb.ToString();
    
    // Compute ranges
    var insertRange = new DocumentRange(document, new TextRange(start));
    var replaceRange = new DocumentRange(document, new TextRange(start, end));
    Ranges = new TextLookupRanges(insertRange, replaceRange);
    
    // Append text
    try {
      Text = text;
      base.Accept(textControl, nameRange, insertType, suffix, solution, keepCaretStill);
    }
    finally {
      Text = string.Empty;
    }
    
    // Start hotspot session
    var hotspotInfoList = new List<HotspotInfo>(hotspotOffsets.Count);
    foreach (int offset in hotspotOffsets) {
      var name = $"hotspot#{offset}";
      var documentRange = new DocumentRange(document, offset);
      //IHotspotExpression expression = new TextHotspotExpression(new List<string>([string.Empty]));
      IHotspotExpression expression = new HotspotExpression();
      hotspotInfoList.Add(new HotspotInfo(new TemplateField(name, expression, 0), documentRange));
    }
    
    LiveTemplatesManager.Instance.CreateHotspotSessionAtopExistingText(
      m_context.BasicContext.Solution,
      new DocumentOffset(document, start + text.Length),
      textControl,
      LiveTemplatesManager.EscapeAction.LeaveTextAndCaret,
      hotspotInfoList.ToArray()
    ).ExecuteAndForget();
  }

  private void Test0(ITextControl textControl, DocumentRange nameRange, LookupItemInsertType insertType, Suffix suffix, ISolution solution, bool keepCaretStill) {
    var node = GetNodeUnderCaret();
    if (node is null) {
      TcpLogger.SLog("node is null");
      return;
    }

    // Filter context (expression)
    if (node.GetContainingNode<ICppExpressionNode>() is null) {
      TcpLogger.SLog("Not inside ICppExpressionNode");
      return;
    }

    var nodeContainer = node.GetContainingNode<MemberAccessExpression>();
    if (nodeContainer is null) {
      TcpLogger.SLog("nodeContainer is null");
      return;
    }
    
    // Start
    TcpLogger.SLog($"Caret offset: {m_context.BasicContext.CaretTreeOffset}");
    TcpLogger.SLog($"\n");
    
    // Experiments
    TcpLogger.SLog($"Experiments:");
    
    TcpLogger.SLog($"Qualifier: {nodeContainer.Qualifier}");
    TcpLogger.SLog($"Qualifier Text: {nodeContainer.Qualifier.GetText()}");
    
    TcpLogger.SLog($"\n");
    
    // Parents
    TcpLogger.SLog($"Caret node parents:");
    TcpLogger.SLog($"- Node: {node}");
    TcpLogger.SLog($"- Text:\n{node.GetText()}");
    for (var nodeParent = node.Parent; nodeParent != null; nodeParent = nodeParent.Parent) {
      TcpLogger.SLog($"- Parent: {nodeParent}");
      TcpLogger.SLog($"- Text:\n{nodeParent.GetText()}");
    }
    TcpLogger.SLog($"\n");

    // Children
    TcpLogger.SLog($"Children:");
    foreach (var treeNode in nodeContainer.Children()) {
      TcpLogger.SLog($"- Child: {treeNode}");
      TcpLogger.SLog($"- Text:\n{treeNode.GetText()}");
    }
    
    TcpLogger.SLog($"\n\n");
  }

  [CanBeNull]
  private ITreeNode GetNodeUnderCaret() {
    return m_context.BasicContext.File.FindNodeAt(m_context.BasicContext.CaretTreeOffset);
  }
}

[Language(typeof(CppLanguage))]
public class FieldAssignmentCompletionProvider : ItemsProviderOfSpecificContext<CppCodeCompletionContext> {
  protected override bool IsAvailable(CppCodeCompletionContext context) {
    if (context.UnterminatedContext.RootNode is PPPragmaDirective) {
      return false;
    }

    if (context.BasicContext.CodeCompletionType != CodeCompletionType.BasicCompletion && context.BasicContext.CodeCompletionType != CodeCompletionType.SmartCompletion) {
      return false;
    }

    // TODO filter context
    
    return true;
  }

  protected override bool AddLookupItems(CppCodeCompletionContext context, IItemsCollector collector) {
    collector.Add(new MyLookupItemBase(context));
    return base.AddLookupItems(context, collector);
  }
  
  public override EvaluationMode SupportedEvaluationMode => EvaluationMode.Light;
  public override CompletionMode SupportedCompletionMode => CompletionMode.Single;
  protected override LookupFocusBehaviour GetLookupFocusBehaviour(CppCodeCompletionContext context) => LookupFocusBehaviour.Soft;
}