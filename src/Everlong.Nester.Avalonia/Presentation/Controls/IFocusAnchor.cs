// NOTE: Single-source file — the WPF project compiles this exact file via
// <Compile Include> in Everlong.Nester.Wpf.csproj.  Edit it here only;
// never create a WPF-side copy (the two builds would drift).

using Everlong.Nester.Layer;

namespace Everlong.Nester.Presentation;

/// <summary>
///   Focusable layer content that also carries an anchor — the interaction
///   origin a transition crosses layers with.
/// </summary>
/// <remarks>
///   The anchor is the surface's own, not a value a view parks: the surface
///   records the element the interaction inside it rested on as the
///   interaction happens — focus arriving on a control, or a control's
///   activation — and falls back to the element focus it held when it last
///   gave up layer focus.  It drops both once it holds layer focus again, so
///   the anchor is readable exactly while the surface is in the background —
///   the window a cross-layer transition runs in.  A director reads the
///   counterpart layer's anchor off <see cref="TransitionContext.Counterpart" />.
/// </remarks>
public interface IFocusAnchor : IFocusableContent
{
  /// <summary>
  ///   The interaction origin this surface holds: the element the last
  ///   interaction inside it rested on, or, when it saw no interaction, the
  ///   element focus it held when it last gave up layer focus.
  ///   <see langword="null" /> while it holds layer focus, or when it has
  ///   neither to hold on to.
  /// </summary>
  object? Anchor { get; }
}
