using Microsoft.CodeAnalysis;
namespace Everlong.Nester.Generators;

// The diagnostic IDs and categories are fixed: an allocated ID is never reused, and a category is
// never hardcoded.  A title and message may be literal text.
internal static class Descriptors
{
  // 0xxx: generation preconditions.
  //   NSTR0001 ClassPartial — the generation target must be partial.
  //   NSTR0008 EnclosingTypePartial — every enclosing type of the target must be partial in cascade.
  internal const string ClassPartialId = "NSTR0001";
  internal const string EnclosingTypePartialId = "NSTR0008";

  // 1xxx: view mapping — the generator's output rules and the view-resolution surface.
  //   NSTR1001/1002 MappingConflict / MappingRedundant — [Mapping] against [ViewFor].
  //   NSTR1003 ViewNeedsParameterlessConstructor — the parameterless-constructor rule [ViewFor] anchors.
  //   NSTR1004 ViewNotInstantiable — the locator names the view type, so the view must be nameable
  //           (non-generic, non-static, reachable by the assembly).
  internal const string MappingConflictId = "NSTR1001";
  internal const string MappingRedundantId = "NSTR1002";
  internal const string ViewNeedsParameterlessConstructorId = "NSTR1003";
  internal const string ViewNotInstantiableId = "NSTR1004";

  // 2xxx: declaration configuration — a declaration allowed once per assembly.
  //   NSTR2002/2003 MultipleViewLocators / MultipleViewDictionaries — one view locator per assembly.
  internal const string MultipleViewLocatorsId = "NSTR2002";
  internal const string MultipleViewDictionariesId = "NSTR2003";

  // 9xxx: internal generator faults — they do not reach user code.
  internal const string TransformErrorId = "NSTR9998";
  internal const string ExecutionErrorId = "NSTR9999";

  internal static class Category
  {
    internal const string View = "View";
    internal const string Usage = "Usage";
    internal const string Configuration = "Configuration";
    internal const string Transform = "Transform";
    internal const string Generator = "Generator";
  }

  // ── 0xxx: generation preconditions ──

  // NSTR0001 · ClassPartial · Error [Usage] — the generation target must be partial.
  internal static readonly DiagnosticDescriptor TargetPartial = new(
    ClassPartialId,
    "Class must be partial",
    "The target type '{0}' must be partial to allow code generation",
    Category.Usage,
    DiagnosticSeverity.Error,
    isEnabledByDefault: true);

  // NSTR0008 · EnclosingTypePartial · Error [Usage] — the target's enclosing type must be partial in cascade.
  internal static readonly DiagnosticDescriptor EnclosingTypePartial = new(
    EnclosingTypePartialId,
    "Enclosing type must be partial",
    "The enclosing type '{0}' must be partial because it contains the code generation target '{1}'",
    Category.Usage,
    DiagnosticSeverity.Error,
    isEnabledByDefault: true);

  // ── 1xxx: view mapping ──

  // NSTR1001 · MappingConflict · Info — [Mapping] and [ViewFor] conflict; the explicit [Mapping] wins.
  internal static readonly DiagnosticDescriptor MappingConflict = new(
    MappingConflictId,
    "Conflicting Mapping and ViewFor mappings",
    "'{0}' is mapped to '{1}' by [Mapping], which overrides [ViewFor] mapping to '{2}'. Remove one of them.",
    Category.View, DiagnosticSeverity.Info, isEnabledByDefault: true,
    description: "If both [ViewFor] and [Mapping] are present, [Mapping] takes precedence.",
    customTags: WellKnownDiagnosticTags.CompilationEnd);

  // NSTR1002 · MappingRedundant · Info — [Mapping] duplicates [ViewFor].
  internal static readonly DiagnosticDescriptor MappingRedundant = new(
    MappingRedundantId,
    "Redundant Mapping",
    "This [Mapping<{1}, {0}>] is redundant; [ViewFor<{0}>] on '{1}' already provides the same mapping",
    Category.View, DiagnosticSeverity.Info, isEnabledByDefault: true,
    customTags: WellKnownDiagnosticTags.CompilationEnd);

  // NSTR1003 · ViewNeedsParameterlessConstructor · Warning — the [ViewFor<T>] view has no usable parameterless constructor.
  internal static readonly DiagnosticDescriptor ViewNeedsParameterlessConstructor = new(
    ViewNeedsParameterlessConstructorId,
    "View requires a parameterless constructor",
    "View '{0}' declares no public or internal parameterless constructor, so the view locator cannot instantiate it. Add one, or remove [ViewFor<T>].",
    Category.View,
    DiagnosticSeverity.Warning,
    isEnabledByDefault: true);

  // NSTR1004 · ViewNotInstantiable · Warning — the view must be a concrete type the locator can name.
  internal static readonly DiagnosticDescriptor ViewNotInstantiable = new(
    ViewNotInstantiableId,
    "View cannot be instantiated by the view locator",
    "View '{0}' cannot be instantiated by the generated view locator: {1}. The locator names the view type, so it must be a non-generic, non-static class that is accessible to the assembly.",
    Category.View,
    DiagnosticSeverity.Warning,
    isEnabledByDefault: true);

  // ── 2xxx: declaration configuration ──

  internal static readonly DiagnosticDescriptor MultipleViewLocators = new(
    MultipleViewLocatorsId,
    "Multiple ViewLocator attributes",
    "Multiple [ViewLocator] attributes found. Only one ViewLocator is allowed per assembly.",
    Category.Configuration,
    DiagnosticSeverity.Error,
    isEnabledByDefault: true);

  internal static readonly DiagnosticDescriptor MultipleViewDictionaries = new(
    MultipleViewDictionariesId,
    "Multiple WpfViewLocator attributes",
    "Multiple [WpfViewLocator] attributes found. Only one WpfViewLocator is allowed per assembly.",
    Category.Configuration,
    DiagnosticSeverity.Error,
    isEnabledByDefault: true);

  // ── 9xxx: internal generator faults — they do not reach user code ──

  internal static readonly DiagnosticDescriptor TransformError = new(
    TransformErrorId,
    "Transform Error",
    "Generator internal error at transform phase: input={0}; message={1}; stack={2}",
    Category.Transform,
    DiagnosticSeverity.Error,
    isEnabledByDefault: true);

  internal static readonly DiagnosticDescriptor ExecutionError = new(
    ExecutionErrorId,
    "Source Generator Exception",
    "Generator internal error at execution phase: input={0}; message={1}; stack={2}",
    Category.Generator,
    DiagnosticSeverity.Error,
    isEnabledByDefault: true);
}
