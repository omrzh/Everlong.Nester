using System.Collections.Immutable;
using Everlong.Nester.Generators.Constants;
using Everlong.Nester.Generators.Extensions;
using Everlong.Nester.Generators.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;

namespace Everlong.Nester.Generators.Views.Executors;

internal sealed class UnifiedViewProviderExecutor
{
  public void Execute(SourceProductionContext context, UnifiedCollectionResult collection)
  {
    IExecutionBranch? branch = collection.UiFramework switch
    {
      UiFramework.Avalonia => new AvaloniaViewProviderBranch(),
      UiFramework.Wpf => new WpfViewProviderBranch(),
      _ => null
    };

    if (branch is null)
    {
      return;
    }

    BranchResult result = branch.Execute(collection.Pairs, collection.Registration);

    var baseTypeList = new List<BaseTypeSyntax>();
    var seenBaseTypes = new HashSet<string>();
    foreach (var baseType in result.BaseTypes)
    {
      if (seenBaseTypes.Add(baseType.ToString()))
      {
        baseTypeList.Add(baseType);
      }
    }

    // C# requires the base class to appear before interfaces in the base list.
    // Sort: types not starting with 'I' (classes) before those starting with 'I' (interfaces).
    var sortedBaseTypes = baseTypeList
      .OrderBy(bt => bt.ToString().TrimStart().StartsWith("I") ? 1 : 0)
      .ToArray();

    // The file belongs to the hierarchy model: it restates the trigger's shape, wraps every enclosing
    // type and attaches the generated-code header.  The exact emitted shape is pinned by
    // UnifiedViewProviderGeneratorAvaloniaTests.GeneratesAvaloniaUnifiedProvider.
    var compilationUnit = collection.Registration.Hierarchy.GetPartialHalf(
      result.Usings,
      result.Members,
      sortedBaseTypes.ToImmutableArray());

    // The hint is the trigger's metadata name: two triggers in one assembly cannot collide, and the
    // `.g.cs` suffix is what marks the file as generated to Roslyn's own analysis.
    context.AddSource($"{collection.Registration.Hierarchy.FilenameHint}{Conventions.GeneratorSuffix}", compilationUnit);
  }
}
