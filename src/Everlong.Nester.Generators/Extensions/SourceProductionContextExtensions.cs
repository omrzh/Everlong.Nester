using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
namespace Everlong.Nester.Generators.Extensions;

/// <summary>
/// Extension methods for the <see cref="SourceProductionContext"/> type.
/// </summary>
internal static class SourceProductionContextExtensions
{
  /// <summary>
  /// Adds a new source file to a target <see cref="SourceProductionContext"/> instance.
  /// </summary>
  /// <param name="context">The input <see cref="SourceProductionContext"/> instance to use.</param>
  /// <param name="name">The name of the source file to add.</param>
  /// <param name="compilationUnit">The <see cref="CompilationUnitSyntax"/> instance representing the syntax tree to add.</param>
  /// <remarks>
  ///   The hint keeps the metadata name's <c>+</c> and backtick.  This package is compiled against one
  ///   Roslyn version and the host is at least that one, and a hint name may carry those characters since
  ///   4.3.1 — mangling them into <c>.</c> and <c>_</c> would make a nested type's hint collide with a
  ///   top-level type's of the same dotted name.
  /// </remarks>
  public static void AddSource(this SourceProductionContext context, string name, CompilationUnitSyntax compilationUnit)
    // Add the UTF8 text for the input compilation unit
    => context.AddSource(name, compilationUnit.GetText(Encoding.UTF8));
}
