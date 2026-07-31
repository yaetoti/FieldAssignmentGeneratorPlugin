using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Cpp.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.Cpp.Tree;
using JetBrains.ReSharper.Psi.Cpp.Symbols;
using JetBrains.ReSharper.Psi.Cpp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace ReSharperPlugin.FieldAssignmentGenerator;

// Reference:
// CppDeclaredElementLookupItem
class FieldAssignmentContext {
  public ITreeNode nodeUnderCaret;
  public ExpressionStatement expressionStatement;
  public MemberAccessExpression accessExpression;
  public ICppClassResolveEntity classResolveEntity;

  [CanBeNull]
  public static FieldAssignmentContext Create(CppCodeCompletionContext ctx) {
    var result = new FieldAssignmentContext();
    
    // Getting node is better because ctx.MemberAccessExpression's offsets are inconsistent. They may be correct and may be 0
    var node = ctx.BasicContext.File.FindNodeAt(ctx.BasicContext.CaretTreeOffset);
    if (node is null) {
      return null;
    }
    
    // Filter context (expression) TODO improve
    var expressionStatement = node.GetContainingNode<ExpressionStatement>();
    if (expressionStatement is null) {
      return null;
    }

    if (node.Parent is not MemberAccessExpression accessExpression) {
      return null;
    }

    // Allows resolving class from pointers, functions, parentheses, etc. TODO handle rvalue
    var resolvedQualifier = accessExpression.GetResolvedClass();
    if (resolvedQualifier is not ICppClassResolveEntity classResolveEntity) {
      return null;
    }
    
    // TODO classResolveEntity.GetBases() for base classes
    
    // Filter struct and class
    var classKey = classResolveEntity.GetKey();
    if (classKey != CppClassKey.CLASS && classKey != CppClassKey.STRUCT) {
      return null;
    }

    result.nodeUnderCaret = node;
    result.expressionStatement = expressionStatement;
    result.accessExpression = accessExpression;
    result.classResolveEntity = classResolveEntity;
    return result;
  }
}