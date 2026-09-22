using System.Windows;

namespace Everlong.Nester.Presentation;

/// <summary>
///   Abstract base class for WPF leaf view locators. Inherits <see cref="ResourceDictionary"/>;
///   registered templates are available to the WPF resource-lookup pipeline.
/// </summary>
/// <remarks>
///   <see cref="AddTemplate{TData,TView}"/> registers a static binding;
///   <see cref="AddDynamicTemplate{TData,TViewLocator}"/> registers a presenter-based dispatch and
///   wraps each item in one <see cref="ImperativeContentControl{TLocator}"/>.
/// </remarks>
public abstract class ViewLocatorBase : ResourceDictionary, IViewLocator
{

  /// <inheritdoc/>
  /// <remarks>
  ///   Looks up a <see cref="DataTemplate"/> in this
  ///   <see cref="ResourceDictionary"/> by <see cref="DataTemplateKey"/> and loads its content.
  /// </remarks>
  public virtual FrameworkElement? Build(object? data)
  {
    if (data is null)
      return null;
    var key = new DataTemplateKey(data.GetType());
    return this[key] is DataTemplate t ? t.LoadContent() as FrameworkElement : null;
  }

  /// <inheritdoc/>
  /// <remarks>
  ///   Claims exactly the data types this dictionary holds a
  ///   <see cref="DataTemplate"/> for, so <see cref="Match(object?)"/> and
  ///   <see cref="Build(object?)"/> agree on the default path.
  /// </remarks>
  public virtual bool Match(object? data)
  {
    if (data is null)
      return false;
    return this[new DataTemplateKey(data.GetType())] is DataTemplate;
  }

  /// <summary>
  ///   Registers a static <see cref="DataTemplate"/> that maps <typeparamref name="TData"/>
  ///   to <typeparamref name="TView"/> into this locator's resource dictionary.
  /// </summary>
  /// <remarks>
  ///   The resulting template is WPF-native and does not require a presenter wrapper.
  /// </remarks>
  protected void AddTemplate<TData, TView>()
    where TView : PControl, new()
  {
    EnsureConcreteTemplateKeyType<TData>();
    var template = new DataTemplate(typeof(TData));
    var factory = new FrameworkElementFactory(typeof(TView));
    template.VisualTree = factory;
    template.Seal();
    this[new DataTemplateKey(typeof(TData))] = template;
  }

  /// <summary>
  ///   Registers a dynamic <see cref="DataTemplate"/> for <typeparamref name="TData"/>
  ///   that delegates view creation to <typeparamref name="TViewLocator"/> at runtime.
  /// </summary>
  /// <remarks>
  ///   The template wraps the result in an
  ///   <see cref="ImperativeContentControl{TLocator}"/>.
  /// </remarks>
  protected void AddDynamicTemplate<TData, TViewLocator>()
    where TData : class
    where TViewLocator : IViewLocator, new()
  {
    AddTemplate<TData, ImperativeContentControl<TViewLocator>>();
  }

  /// <summary>
  ///   Enables dynamic locating through <typeparamref name="TLocator" /> — the locator
  ///   <see cref="AddDynamicTemplate{TData}()" /> dispatches to.
  /// </summary>
  /// <param name="locator">The locator instance to consult.</param>
  /// <exception cref="ArgumentException"><typeparamref name="TLocator" /> is not the derived locator type.</exception>
  /// <exception cref="InvalidOperationException">A dynamic locator is already enabled.</exception>
  protected void EnableDynamicLocating<TLocator>(TLocator locator)
    where TLocator : IViewLocator, new()
  {
    if (typeof(TLocator) != GetType())
    {
      throw new ArgumentException("The TLocator should be of type " + GetType().FullName);
    }

    if (_dynamicContentControlType != null)
    {
      throw new InvalidOperationException("A dynamic locator has already been registered. Only one is allowed per ViewLocator.");
    }

    DynamicContentControl<TLocator>.Locator = locator;
    _dynamicContentControlType = typeof(DynamicContentControl<TLocator>);
  }

  private Type? _dynamicContentControlType;

  /// <summary>
  /// Registers a <see cref = "DataTemplate"/> for <typeparamref name = "TData"/> into the provided ResourceDictionary using
  /// <see cref = "ImperativeContentControl{T}"/> as the view, delegating view creation to <see cref = "ViewLocatorBase.Build(object?)"/>
  /// </summary>
  // Not completed... reflection inevitable?
  protected void AddDynamicTemplate<TData>()
    where TData : class
  {
    if (_dynamicContentControlType is null)
    {
      throw new InvalidOperationException("Call EnableDynamicLocating(this) first.");
    }

    EnsureConcreteTemplateKeyType<TData>();
    var template = new DataTemplate(typeof(TData));
    var factory = new FrameworkElementFactory(_dynamicContentControlType);
    template.VisualTree = factory;
    template.Seal();
    this[new DataTemplateKey(typeof(TData))] = template;
  }

  private static void EnsureConcreteTemplateKeyType<TViewModel>()
  {
    EnsureConcreteTemplateKeyType(typeof(TViewModel));
  }

  private static void EnsureConcreteTemplateKeyType(Type dataType)
  {
    if (!dataType.IsClass || dataType.IsAbstract)
    {
      throw new ArgumentException(
        $"`{dataType.FullName}` cannot be used as a DataTemplate key. Use a concrete, non-abstract class.",
        nameof(dataType));
    }
  }
}
