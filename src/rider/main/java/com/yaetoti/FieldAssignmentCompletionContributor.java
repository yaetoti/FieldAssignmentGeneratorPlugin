package com.yaetoti;

import com.intellij.clion.langSwitcher.CLionBackendEngineCollector;
import com.intellij.codeInsight.completion.*;
import com.intellij.codeInsight.lookup.LookupElementBuilder;
import com.intellij.openapi.command.WriteCommandAction;
import com.intellij.patterns.PlatformPatterns;
import com.intellij.psi.PsiElement;
import com.intellij.psi.codeStyle.CodeStyleManager;
import com.intellij.util.ProcessingContext;
import com.jetbrains.rdclient.services.IdeBackend;
import org.jetbrains.annotations.NotNull;
import org.jspecify.annotations.NonNull;

public class FieldAssignmentCompletionContributor extends CompletionContributor {
  public FieldAssignmentCompletionContributor() {
    extend(
      CompletionType.BASIC,
      PlatformPatterns.psiElement().afterLeaf("."),
      new CompletionProvider<CompletionParameters>() {
        @Override
        protected void addCompletions(@NonNull CompletionParameters params, @NotNull ProcessingContext ctx, @NotNull CompletionResultSet result) {
          PsiElement element = params.getPosition();

          PrintPsiHierarchy1(element.getParent());

          System.out.println(params.getPosition().getContainingFile().getClass());
          System.out.println(params.getOriginalFile().getClass());

          System.out.println(params.getPosition().getContainingFile() == params.getOriginalFile());

          result.addElement(LookupElementBuilder
            .create("")
            .withPresentableText("Generate assignments...")
            .withTailText("(Public fields)")
            .withInsertHandler((context, item) -> {
              executeAssignmentGeneration(context);
            })
          );
        }
      }
    );
  }

  private void executeAssignmentGeneration(InsertionContext ctx) {
    var document = ctx.getDocument();
    var project = ctx.getProject();
    var file = ctx.getFile();

    String fileText = document.getText();
    int caretOffset = ctx.getStartOffset();

    int variableStart = -1;
    int variableEnd = -1;
    int dotOffset = -1;

    // TODO Test
    System.out.println("Start: " + ctx.getStartOffset());
    System.out.println("End: " + ctx.getTailOffset());

    {
      int symbolOffset = caretOffset - 1;

      // Find dot offset (We can write v1   .     <alt+space>)
      for (; symbolOffset >= 0; --symbolOffset) {
        char symbol = fileText.charAt(symbolOffset);

        // Found
        if (symbol == '.') {
          dotOffset = symbolOffset;
          break;
        }

        // Skip whitespaces
        if (Character.isWhitespace(symbol)) {
          continue;
        }

        // Do not allow anything except whitespaces
        break;
      }

      // Can't find the dot (should never happen)
      if (dotOffset == -1) {
        System.out.println("[BUG]: Dot not found");
        return;
      }

      for (--symbolOffset; symbolOffset >= 0; --symbolOffset) {
        char symbol = fileText.charAt(symbolOffset);

        // Found
        if (Character.isJavaIdentifierPart(symbol)) {
          variableEnd = symbolOffset + 1;
          break;
        }

        // Skip whitespaces
        if (Character.isWhitespace(symbol)) {
          continue;
        }

        break;
      }

      // Can't find the variable end (should never happen)
      if (variableEnd == -1) {
        System.out.println("[BUG]: varEnd not found");
        return;
      }

      // Find variable start
      variableStart = variableEnd - 1;
      for (--symbolOffset; symbolOffset >= 0 && Character.isJavaIdentifierPart(fileText.charAt(symbolOffset)); --symbolOffset) {
        variableStart = symbolOffset;
      }
    }

    // Get variable name
    String variableName = fileText.substring(variableStart, variableEnd);

    var element = file.findElementAt(variableStart);
    var reference = file.findReferenceAt(variableStart);

    // Perform the code insertion
    String sampleGeneration =
      "a = ;\n" +
        variableName + ".b = ;\n" +
        variableName + ".c = ;\n";

    int start = ctx.getStartOffset();
    WriteCommandAction.runWriteCommandAction(ctx.getProject(), () -> {
      document.insertString(start, sampleGeneration);
      CodeStyleManager.getInstance(ctx.getProject()).reformatText(ctx.getFile(), start, start + sampleGeneration.length());
    });
  }

  private void PrintPsiHierarchy(PsiElement e) {
    while (e != null) {
      System.out.println(e.getClass().getName() + " : " + e.getText());
      e = e.getParent();
    }
  }

  private PsiElement FindAbsoluteParent(PsiElement e) {
    while (e.getParent() != null) {
      e = e.getParent();
    }

    return e;
  }

  private void PrintPsiHierarchy1(PsiElement e) {
    PrintPsiHierarchy1(e, 0);
  }

  private void PrintPsiHierarchy1(PsiElement e, int depth) {
    if (e == null) return;

    String indent = "  ".repeat(depth);
    System.out.println(indent + e.getClass().getSimpleName());

    for (PsiElement child : e.getChildren()) {
      PrintPsiHierarchy1(child, depth + 1);
    }
  }

  private void checkCppType(PsiElement position) {
    PsiElement parent = position.getParent();
    if (parent != null) {
      System.out.println("Parent element text: " + parent.getText());
    }
  }
}
