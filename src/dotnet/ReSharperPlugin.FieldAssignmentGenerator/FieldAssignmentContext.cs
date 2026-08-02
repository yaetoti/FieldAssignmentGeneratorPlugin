using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Cpp.CodeCompletion;
using JetBrains.ReSharper.Psi.Cpp.Symbols;
using JetBrains.ReSharper.Psi.Cpp.Tree;
using JetBrains.ReSharper.Psi.Cpp.Types;
using JetBrains.ReSharper.Psi.Tree;

namespace ReSharperPlugin.FieldAssignmentGenerator;

// Reference:
// CppDeclaredElementLookupItem
// CppMemberAccessExpressionUtil
class FieldAssignmentContext {
  public ITreeNode nodeUnderCaret;
  public ExpressionStatement expressionStatement;
  public MemberAccessExpression accessExpression;
  public ICppClassResolveEntity classResolveEntity;

  private static ICppClassResolveEntity ResolveQualifierClass(MemberAccessExpression memAccess) {
    // Resolve pointers
    if (memAccess.GetLookupScopeResolveResult().GetPrimaryEntity() is ICppDeclaratorResolveEntity resolvedEntity) {
      if (resolvedEntity.GetCppType().InternalType is CppFunctionType functionType) {
        if (functionType.ReturnType.InternalType is ICppClassResolveEntity classResolveEntity) {
          return classResolveEntity;
        }
      }
    }
    
    // Resolve references and values
    if (memAccess.GetResolvedLeftArgument() is not ICppExpressionNode resolvedLeftArgument) {
      return null;
    }
    
    CppTypeAndCategory typeAndCategory = resolvedLeftArgument.GetTypeAndCategory();
    if (typeAndCategory.Category != CppValueCategory.L_VALUE) {
      return null;
    }
    
    CppQualType t = typeAndCategory.Type;
    return t.InternalAs<ICppClassResolveEntity>();
  }
  
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

    var classResolveEntity = ResolveQualifierClass(accessExpression);
    if (classResolveEntity is null) {
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