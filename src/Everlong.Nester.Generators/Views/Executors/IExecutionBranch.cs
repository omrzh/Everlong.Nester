using System.Collections.Immutable;
using Everlong.Nester.Generators.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;
namespace Everlong.Nester.Generators.Views.Executors;

internal readonly record struct BranchResult(
    ImmutableArray<MemberDeclarationSyntax> Members,
    ImmutableArray<BaseTypeSyntax> BaseTypes,
    ImmutableArray<string> Usings);

internal interface IExecutionBranch
{
  /// <summary>Emits the framework's provider half for the resolved view-model to view bindings.</summary>
  BranchResult Execute(EquatableArray<PairedInfo> pairs, RegistrationModel registration);
}
