using Everlong.Nester.Presentation;
using Everlong.Nester.Tests.Controls;

namespace Everlong.Nester.Tests;

/// <summary>
///   The test assembly's single <c>[ViewLocator]</c> trigger — one locator per
///   assembly is the app-level truth, so every suite's mapping is declared here:
///   the busy-indicator probe view through its own <c>[ViewFor]</c>, and the
///   view-resolution pins through <c>[Mapping]</c>.
/// </summary>
[ViewLocator]
[Mapping<LocatorFirstModel, LocatorFirstView>]
[Mapping<LocatorSecondModel, LocatorSecondView>]
public partial class TestViewLocator;
