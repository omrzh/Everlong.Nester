using Avalonia.Controls.Templates;

namespace Everlong.Nester.Presentation;

/// <summary>
///   The platform's view resolution — the view for a data instance, resolved
///   from the tree the asking control sits in.
/// </summary>
/// <remarks>
///   The platform's own template table is the translation table: the nearest
///   ancestor's templates first, the application's own last.  There is no
///   separate locator contract to consult — an Avalonia locator is an
///   <see cref="IDataTemplate" />, and the lookup already reaches it.
/// </remarks>
internal static class ViewResolution
{
  /// <summary>Builds the view <paramref name="data" /> resolves to, or <see langword="null" /> when nothing claims it.</summary>
  internal static PControl? Build(PControl from, object? data)
    => from.FindDataTemplate(data)?.Build(data);
}
