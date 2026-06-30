using Everlong.Nester.Auth;

namespace NesterApp.Services;

/// <summary>
///   The templates' authorization registry trigger — the generator fills
///   this partial class with the compiling assembly's <c>[Authorize]</c>
///   declarations (see the emitted <c>AppAuthRegistry.AuthRegistry.g.cs</c>).
///   One per assembly: the shared source compiles into each GUI template and
///   the runtime-test assembly, each getting its own registry.
/// </summary>
[AuthRegistry]
public partial class AppAuthRegistry;
