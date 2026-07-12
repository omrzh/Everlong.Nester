using Everlong.Nester.Presentation;

namespace Everlong.Nester.Routing;

/// <summary>
///   The Terminal.Gui router — its navigation view is a
///   <see cref="TerminalNavigationHost" /> that assembles the resolved
///   chain into real views and switches them per body at the reveal.
/// </summary>
public class TerminalRouter(IServiceProvider services)
  : RouterBase(services, new TerminalPlatformStackModel(services));
