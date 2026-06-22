using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Everlong.Nester.Generators;

namespace Everlong.Nester.CodeFixers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PartialKeywordCodeFixProvider)), Shared]
public sealed class PartialKeywordCodeFixProvider : CodeFixProvider
{
  public sealed override ImmutableArray<string> FixableDiagnosticIds =>
    ImmutableArray.Create(Descriptors.ClassPartialId, Descriptors.EnclosingTypePartialId);

  public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

  public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
  {
    var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
    if (root == null)
      return;

    // NSTR0001 / NSTR0008: a type that must be partial — the generation target itself, or a type
    // enclosing it.
    var partialDiag = context.Diagnostics.FirstOrDefault(
      d => d.Id is Descriptors.ClassPartialId or Descriptors.EnclosingTypePartialId);
    if (partialDiag != null)
    {
      var typeDecl = root.FindToken(partialDiag.Location.SourceSpan.Start).Parent?
        .AncestorsAndSelf()
        .OfType<TypeDeclarationSyntax>()
        .FirstOrDefault();

      if (typeDecl != null)
      {
        context.RegisterCodeFix(
          CodeAction.Create(
            title: "Make type partial",
            createChangedDocument: c => MakeTypePartialAsync(context.Document, typeDecl, c),
            equivalenceKey: "MakeClassPartial"),
          partialDiag);
      }
    }

  }

  private static async Task<Document> MakeTypePartialAsync(Document document,
                                                       TypeDeclarationSyntax typeDecl,
                                                       CancellationToken cancellationToken)
  {
    var root = await document.GetSyntaxRootAsync(cancellationToken);
    if (root == null)
      return document;

    if (typeDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
      return document;

    var partialToken = SyntaxFactory.Token(SyntaxKind.PartialKeyword).WithTrailingTrivia(SyntaxFactory.Space);
    var newModifiers = typeDecl.Modifiers.Add(partialToken);
    var newTypeDecl = typeDecl.WithModifiers(newModifiers);

    var newRoot = root.ReplaceNode(typeDecl, newTypeDecl);
    return document.WithSyntaxRoot(newRoot);
  }


}
