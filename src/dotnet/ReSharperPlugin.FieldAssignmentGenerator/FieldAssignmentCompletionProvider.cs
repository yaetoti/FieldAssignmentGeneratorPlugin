using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi.Cpp.Lookup;
using JetBrains.ReSharper.Psi.Cpp.Resolve;
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
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Psi.Cpp.Expressions;
using JetBrains.ReSharper.Psi.Cpp.Types;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.UI.Icons;
using JetBrains.UI.RichText;
using JetBrains.Util;
using JetBrains.Util.Media;

namespace ReSharperPlugin.FieldAssignmentGenerator;

// References:
// CppItemCollector::ConfigureCompleteDesignatedInitializationPlacement()
// CppCompleteDesignatedInitializationItemsProvider
// CppItemsProviderOnReferenceExpression - calls add items of designated
// CppCompletionTreeUtil - gets designated initialization members
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
    var node = GetNodeUnderCaret();
    if (node is null) {
      TcpLogger.SLog("node is null");
      return;
    }

    var nodeContainer = node.GetContainingNode<MemberAccessExpression>();
    if (nodeContainer is null) {
      TcpLogger.SLog("nodeContainer is null");
      return;
    }
    
    // TODO get desired text / range
    TcpLogger.SLog($"- Desired text:\n{nodeContainer.GetText()}");

    if (nodeContainer.Qualifier is not ICppAnyResolvedReferenceExpression qualifier) {
      TcpLogger.SLog("Qualifier is not ICppAnyResolvedReferenceExpression");
      return;
    }

    if (qualifier.GetResolvedReference().GetPrimaryEntity() is not ICppVariableDeclaratorResolveEntity resolveEntity) {
      TcpLogger.SLog("GetPrimaryEntityIfStatusIsOk() as ICppVariableDeclaratorResolveEntity is null");
      return;
    }
    
    var cppType = resolveEntity.GetCppType();
    var classResolveEntity = cppType.InternalAs<CppClassResolveEntity>();
    if (classResolveEntity is null) {
      TcpLogger.SLog("Internal type is null");
      return;
    }

    var classKey = classResolveEntity.GetKey();
    if (classKey != CppClassKey.CLASS && classKey != CppClassKey.STRUCT) {
      TcpLogger.SLog("Internal type is not class or struct");
      return;
    }
    
    // TODO debug
    TcpLogger.SLogType("CppType", cppType);
    TcpLogger.SLogType("Internal Type", classResolveEntity);
    TcpLogger.SLogType("Class key", classKey);

    // Find suitable fields
    var suitableFields = new List<string>();
    foreach (var classChild in classResolveEntity.GetChildren()) {
      if (classChild is not CppDeclaratorResolveEntityPack classChildPack) {
        continue;
      }
      
      foreach (var variable in classChildPack.GetGroupedVariables()) {
        if (!variable.IsNonStaticField()) {
          continue;
        }
        
        // TODO check relative accessibility
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
    int start = nodeContainer.Qualifier.GetDocumentRange().StartOffset.Offset;
    int end = m_context.BasicContext.CaretDocumentOffset.Offset;

    string qualifierText = nodeContainer.Qualifier.GetText();
    string signText = nodeContainer.Sign.GetText();

    TcpLogger.SLog($"QualifierText: {qualifierText}");
    TcpLogger.SLog($"SignText: {signText}");
    TcpLogger.SLog($"Range: {start} - {end}");

    // Build text
    var sb = new StringBuilder();
    foreach (var field in suitableFields) {
      sb.Append(qualifierText);
      sb.Append(signText);
      sb.Append(field);
      sb.Append(" = ;\n");
      // TODO add hotspot offset
      // TODO get indents and spaces from config
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
    return true;
  }
  
  public override EvaluationMode SupportedEvaluationMode => EvaluationMode.Light;
  protected override LookupFocusBehaviour GetLookupFocusBehaviour(CppCodeCompletionContext context) {
    //return base.GetLookupFocusBehaviour(context);
    return LookupFocusBehaviour.Soft;
  }
}