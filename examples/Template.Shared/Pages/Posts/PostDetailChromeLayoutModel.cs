using Everlong.Nester.ComponentModel;
using Everlong.DI;

namespace NesterApp.Pages.Posts;

/// <summary>
///   Floating (overlay) chrome for the post detail page — the panel shell
///   shown when PostDetail opens in the "post-detail" overlay layer.
///   Bearing-surface orthogonal shell decision: the ground shell
///   (MainLayout) never hosts overlay content; the overlay shell comes from
///   the shell table's overlay column, and the layer default shell
///   (Dimmer) backs it.
/// </summary>
[Transient]
public partial class PostDetailChromeLayoutModel : RoutableModel;
