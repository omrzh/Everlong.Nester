namespace Everlong.Nester.Routing;

/// <summary>
///   Marks a ViewModel as a routable target — the source generator produces
///   the strongly-typed route class for it.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class RoutableAttribute : Attribute;

/// <summary>
///   Marks a routable ViewModel and declares the argument type it consumes.
/// </summary>
/// <typeparam name="TArgs">The argument record type, derived from <see cref="Args" />.</typeparam>
[AttributeUsage(AttributeTargets.Class)]
public sealed class RoutableAttribute<TArgs> : Attribute where TArgs : Args;

/// <summary>
///   Declares a layout for a PageViewModel or a parent layout for a LayoutViewModel, forming a nested layout chain.
/// </summary>
/// <typeparam name="TLayoutViewModel">The type of the LayoutViewModel.</typeparam>
[AttributeUsage(AttributeTargets.Class)]
public sealed class LayoutAttribute<TLayoutViewModel> : Attribute;
