using System.Collections.Generic;
using System.Text;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems.Impl;
using JetBrains.ReSharper.Feature.Services.Cpp.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.Cpp.CodeStyle;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Templates;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Psi.Cpp.Symbols;
using JetBrains.ReSharper.Psi.Format;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
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
// CppFormattingSettingsKey - settings
public class FieldAssignmentLookupItem : TextLookupItemBase {
  public const string NAME = "<generate-field-assignments>";

  private readonly CppCodeCompletionContext m_context;
  
  public FieldAssignmentLookupItem(CppCodeCompletionContext context) {
    m_context = context;
    Placement = new LookupItemPlacement("000000a");
    Placement.Relevance |= (ulong)CppCompletionRanks.CompleteDesignatedInitialization;
    Ranges = context.CompletionRanges;
  }

  public override IconId Image => ServicesThemedIcons.LiveTemplate.Id;
  protected override RichText GetDisplayName() => new RichText(NAME, new TextStyle(JetFontStyles.Italic, TextColor, new JetRgbaColor()));
  
  public override void Accept(ITextControl textControl, DocumentRange nameRange, LookupItemInsertType insertType, Suffix suffix, ISolution solution, bool keepCaretStill) {
    var ctx = FieldAssignmentContext.Create(m_context);
    if (ctx is null) {
      return;
    }
    
    // Find suitable fields
    var suitableFields = ctx.GetSuitableFields();
    if (suitableFields.Count == 0) {
      return;
    }
    
    // Extract useful info
    var document = m_context.BasicContext.Document;
    int start = ctx.accessExpression.Qualifier.GetDocumentRange().StartOffset.Offset;
    int end = m_context.BasicContext.CaretDocumentOffset.Offset;

    string qualifierText = ctx.accessExpression.Qualifier.GetText();
    string signText = ctx.accessExpression.Sign.GetText();

    // Extract settings
    var settings = m_context.BasicContext.ContextBoundSettingsStore;
    var indentStyle = settings.GetValue<CppFormattingSettingsKey, IndentStyle>(key => key.INDENT_STYLE);
    var indentSize = settings.GetValue<CppFormattingSettingsKey, int>(key => key.INDENT_SIZE);
    var spaceAroundAssignment = settings.GetValue<CppFormattingSettingsKey, bool>(key => key.SPACE_AROUND_ASSIGNMENT_OPERATOR);
    var spaceAroundDot = settings.GetValue<CppFormattingSettingsKey, bool>(key => key.SPACE_AROUND_DOT);
    
    var indentText = DocumentIndentUtils.GetLineIndent(document, new DocumentOffset(document, start).ToDocumentCoords().Line);
    
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
      IHotspotExpression expression = new HotspotExpressionBase();
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
}