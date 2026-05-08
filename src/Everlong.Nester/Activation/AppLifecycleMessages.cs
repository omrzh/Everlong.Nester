using Everlong.Nester.Messaging;

namespace Everlong.Nester.Activation;

/// <summary>
///   The application left the background state.
/// </summary>
public sealed record AppResumedMessage : IMessage;

/// <summary>
///   The application entered the background state.
/// </summary>
public sealed record AppBackgroundMessage : IMessage;

/// <summary>
///   The platform asked the application to reopen.
/// </summary>
public sealed record AppReopenMessage : IMessage;
