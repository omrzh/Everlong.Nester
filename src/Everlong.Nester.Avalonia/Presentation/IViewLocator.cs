using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace Everlong.Nester.Presentation;

/// <summary>
///   Avalonia view-resolution contract.
/// </summary>
/// <remarks>
///   Builds, never reuses: the returned control is built for the given data and is never a control
///   built for earlier data.  A locator answers for the application's whole mapping set, so it
///   cannot judge whether the previous child Avalonia offers a recycling template is still
///   applicable; neither this interface nor an implementation of it implements
///   <c>IRecyclingDataTemplate</c>.
/// </remarks>
public interface IViewLocator : IDataTemplate, IViewLocator<Control>;
