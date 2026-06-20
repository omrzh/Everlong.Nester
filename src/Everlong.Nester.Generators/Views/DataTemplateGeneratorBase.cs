using Everlong.Nester.Generators.Helpers;
using Everlong.Nester.Generators.Models;
using Everlong.Nester.Generators.Views.Executors;
using Microsoft.CodeAnalysis;

namespace Everlong.Nester.Generators.Views;

/// <summary>
///   The view provider pipeline: find the assembly's trigger, resolve the declared view-model to view
///   bindings, and emit the provider half.
/// </summary>
public abstract partial class DataTemplateGeneratorBase : IIncrementalGenerator
{
  // Trigger rules:
  //   - The trigger is the assembly's one [ViewLocator] / [WpfViewLocator] class.  A trigger that is not
  //     `partial` never reaches the plan (the query is filtered on the syntax) and is the analyzer's to
  //     report; a second trigger zeroes the plan instead of being skipped (NSTR2002 / NSTR2003).
  //   - Nesting is legal: the half is wrapped in its enclosing types, so every enclosing type must be
  //     `partial` as well — that cascade belongs to the analyzer (NSTR0008).
  //   - The trigger's [Mapping<TModel, TView>] attributes are collected as the explicit override.
  protected abstract string TriggerAttribute { get; }
  protected abstract UiFramework TargetFramework { get; }

  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    // Phase 1: collect the trigger types (ViewLocator / WpfViewLocator) as an array for the later steps.
    IncrementalValueProvider<EquatableArray<TriggerSnapshot>> triggers =
      context.SyntaxProvider.ForAttributeWithMetadataName(
          TriggerAttribute,
          predicate: PredicateHelper.IsPartialClassDecl,
          transform: (ctx, _) => CreateTriggerSnapshot(ctx)
        )
        .Where(static s => s is not null)
        .Select(static (s, _) => s!)
        .Collect()
        .Select(static (arr, _) => arr.AsEquatableArray());

    // Phase 2: normalize the trigger snapshot into a TriggerPlan (no trigger / multiple triggers, and their diagnostics).
    IncrementalValueProvider<TriggerPlan> triggerPlans
      = triggers.Select((snapshots, _) => BuildTriggerPlan(snapshots));

    // Phase 3: pre-filter this assembly's view declarations through a SyntaxProvider ([ViewFor<TViewModel>] only).
    IncrementalValueProvider<EquatableArray<ViewCandidate>> views
      = context.SyntaxProvider
        .CreateSyntaxProvider(
          static (node, _) => CandidateFactory.IsCandidateSyntax(node),
          static (ctx, token) => CandidateFactory.Create(ctx, token))
        .Where(static view => view is not null)
        .Select(static (view, _) => view!)
        .Collect()
        .Select(static (arr, _) => arr.AsEquatableArray());

    // Phase 4: merge the trigger plan with the view declarations into the final unified result.
    IncrementalValueProvider<UnifiedCollectionResult?> input
      = triggerPlans.Combine(views)
        .Select((tuple, _) => BuildUnifiedResult(tuple.Left, tuple.Right));

    // Phase 6: report the diagnostics or emit the generated code.
    context.RegisterSourceOutput(input, (ctx, result) =>
    {
      if (result == null)
      {
        return;
      }

      foreach (var diag in result.Diagnostics)
      {
        ctx.ReportDiagnostic(diag.ToDiagnostic());
      }

      if (result.UiFramework == UiFramework.Error)
      {
        return;
      }

      var executor = new UnifiedViewProviderExecutor();
      executor.Execute(ctx, result);
    });
  }

  private TriggerPlan BuildTriggerPlan(EquatableArray<TriggerSnapshot> snapshots)
  {
    using var diagnostics = ImmutableArrayBuilder<DiagnosticInfo>.Rent();

    if (snapshots.IsEmpty)
    {
      return new TriggerPlan(null, diagnostics.ToImmutable());
    }

    if (snapshots.Length > 1)
    {
      foreach (var snapshot in snapshots)
      {
        diagnostics.Add(TargetFramework == UiFramework.Avalonia
                          ? CreateDiagnostic(Descriptors.MultipleViewLocators, snapshot!.Location)
                          : CreateDiagnostic(Descriptors.MultipleViewDictionaries, snapshot!.Location));
      }

      return new TriggerPlan(null, diagnostics.ToImmutable());
    }

    var trigger = snapshots[0]!;
    return new TriggerPlan(trigger, diagnostics.ToImmutable());
  }
}
