using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Cpp.CodeCompletion;
using JetBrains.ReSharper.Psi.Cpp.Expressions;
using JetBrains.ReSharper.Psi.Cpp.Resolve;
using JetBrains.ReSharper.Psi.Cpp.Symbols;
using JetBrains.ReSharper.Psi.Cpp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using ReSharperPlugin.FieldAssignmentGenerator;

namespace DefaultNamespace;

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
      //TcpLogger.SLog("node is null");
      return null;
    }
    
    // Filter context (expression)
    var expressionStatement = node.GetContainingNode<ExpressionStatement>();
    if (expressionStatement is null) {
      //TcpLogger.SLog("Not inside ExpressionStatement");
      return null;
    }

    if (node.Parent is not MemberAccessExpression accessExpression) {
      //TcpLogger.SLog("nodeContainer is null");
      return null;
    }
    
    // Getting the class type
    if (accessExpression.Qualifier is not ICppAnyResolvedReferenceExpression qualifier) {
      //TcpLogger.SLog("Qualifier is not ICppAnyResolvedReferenceExpression");
      return null;
    }
    
    if (qualifier.GetResolvedReference().GetPrimaryEntityIfStatusIsOk() is not ICppDeclaratorResolveEntity resolveEntity) {
      //TcpLogger.SLog("GetPrimaryEntityIfStatusIsOk() as ICppDeclaratorResolveEntity is null");
      return null;
    }

    var cppType = resolveEntity.GetCppType();
    if (cppType.InternalType is not ICppClassResolveEntity classResolveEntity) {
      //TcpLogger.SLog("Internal type is null");
      return null;
    }
    
    // TODO classResolveEntity.GetBases() for base classes
    
    // Filter struct and class
    var classKey = classResolveEntity.GetKey();
    if (classKey != CppClassKey.CLASS && classKey != CppClassKey.STRUCT) {
      //TcpLogger.SLog("Internal type is not class or struct");
      return null;
    }

    result.nodeUnderCaret = node;
    result.expressionStatement = expressionStatement;
    result.accessExpression = accessExpression;
    result.classResolveEntity = classResolveEntity;
    return result;
  }
}