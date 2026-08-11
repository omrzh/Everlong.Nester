# Interaction — dialogs, feedback, messaging, auth, settings, i18n

> The consumer surface of the extension domains. Mechanism per domain: `docs/design/dialog.md`,
> `notice.md`, `messaging.md`, `auth.md`. Everything here is exercised by the template's
> `Pages/Labs/InteractionLabPageModel.cs`.

## 1. Dialogs

A dialog is an ordinary derived-router presentation: a dimmer slot below it, a result channel, and a
dismissal rule. The built-in APIs are ready-made sessions; the mechanism is the same for your own.

```csharp
bool ok      = await Router.ConfirmAsync(Lang.Pages.DeleteConfirm);          // → bool
await Router.AlertAsync(Lang.Pages.Saved);                                   // → bool (notification)
DialogChoiceResult choice = await Router.ChooseAsync(Lang.Pages.PickOne);     // → DialogChoiceResult
PostDetail? picked = await Router.SelectFromOptionsAsync(posts, Lang.Pages.PickOne);
decimal? amount = await Router.InputNumberAsync(Lang.Pages.Amount, new NumpadOptions { Min = 0m, Max = 5000m });
ValueColor? color = await Router.PickColorAsync();
DateTime? when  = await Router.PickDateTimeAsync();
IPAddress? ip   = await Router.ComposeIpv4Async(new Ipv4ComposerOptions { InitialIpAddress = ip0 });
SignNameResult? signature = await Router.SignHandwrittenNameAsync();
await Router.PreviewAsync(ImagePreviewInput.FromPathOrUrl(url, httpClient));
```

| API | Returns | Options type |
|---|---|---|
| `AlertAsync(message[, title, buttonText])` | `bool` | — (never dismissible by the backdrop) |
| `ConfirmAsync(message, options?, timeProvider?)` | `bool` | `ConfirmOptions` (`Title`, `ConfirmText`, `CancelText`, `CountdownSeconds`) |
| `ChooseAsync(message, title?, options?)` | `DialogChoiceResult` | `ChoiceOptions` |
| `SelectFromOptionsAsync<TOption>(options, message, title?, options?)` | `TOption?` | `SelectOptions` |
| `InputNumberAsync(title?, options?)` | `decimal?` | `NumpadOptions` |
| `PickColorAsync(title?, message?, options?)` | `ValueColor?` | `ColorPickerOptions` |
| `PickDateTimeAsync(title?, message?, options?)` | `DateTime?` | `DateTimePickerOptions` |
| `ComposeIpv4Async(options?)` | `IPAddress?` | `Ipv4ComposerOptions` |
| `SignHandwrittenNameAsync(title?, message?, options?)` | `SignNameResult?` | `SignaturePadOptions` |
| `PreviewAsync(input)` | `Task` | `ImagePreviewInput` |
| `CreateWait(title, options?)` | `WaitDialogSession` | `WaitDialogOptions` |

Every `title` and `message` in that table reaches the screen: no session carries a heading it does not
show, and none accepts a body it drops.

WPF adds `PickPlatformColorAsync(...)` returning the WPF `Color?`.

Progress uses the session directly, or the work overload that owns the progress reporting:

```csharp
using var wait = Router.CreateWait(Lang.Pages.Processing, new WaitDialogOptions { Maximum = 100 });
wait.Report(Lang.Pages.StepOne);              // Report(string) writes the caption
wait.Advance(50);                             // Report(double) / Advance(double) drive the bar

// or let the session drive it:
await wait.RunAsync(async (progress, ct) => { /* progress.Report(...) */ }, background: true);
```

### Dismissal

A dialog is dismissible from outside its own content — a backdrop tap, an external back — only while the
dimmer it is shown under is light-dismiss. That dimmer is what the sugar builds from the options, so the
call site states the concern on the options POCO and nothing else:

```csharp
await Router.ConfirmAsync(Lang.Pages.DeleteConfirm, new ConfirmOptions { IsMandatory = true });
```

An options type declares only what it adds: `DialogOptions` carries the pair once (`IDialogDimmerOptions`)
and `ToDimmer` turns it into the dimmer the session is presented under. A refused attempt shakes while the
dimmer's `ShakeVetoedDismissal` is set (the default). A session may read its own options type where it
needs one; `ShowAsync(model)` without a dimmer presents the default, light-dismiss.

None of this is a contract to satisfy. A dialog that is acknowledged rather than dismissed
(`AlertDialogSession`) is shown under a non-light-dismiss dimmer by its own factory, and for the dimmer
itself, hand one to `ShowAsync` — it is used as given:

```csharp
await Router.ShowAsync<bool>(new MySession(), new DefaultDimmerModel { LightDismiss = false });
```

A dialog that draws its own way out draws it as **Close** — `CloseText` is the label and `CloseCommand`
dismisses without a result. That is what separates it from `ConfirmOptions.CancelText` (`false`) or
`ChoiceOptions.CancelText` (`DialogChoiceResult.Cancel`), whose buttons settle the dialog with an answer
rather than abandoning the question.

### Custom sessions

Inherit `DialogSessionBase<TResult>` and close with the result — `ShowAsync` returns it, and a
dismissal yields `null`:

```csharp
public partial class MyConfirmDialogSession : DialogSessionBase<bool>
{
  [Inject] private partial IRouter Router { get; }
  public required string? Message { get; set; }

  [RelayCommand] private void Yes() => Close(true);
  [RelayCommand] private void No()  => Close(false);

  [RelayCommand] private async Task OpenNested()
    => await Router.ShowAsync<object?>(new NestedConfirmDialogSession { Message = "Nested!" });
}

bool? answer = await Router.ShowAsync<bool>(new MyConfirmDialogSession { Message = "Sure?" });
```

Sessions are models like any other — routable, arrival-hooked, releasable — and their state lives on
the model, never on the view.

The built-in dialogs are an example, not the set: your own options type derives from `DialogOptions` for
the dismissal pair, and `ToDimmer()` — the same dispatch the built-in factories use — derives the dimmer
the session is presented under:

```csharp
public sealed class MyOptions : DialogOptions
{
  public string? Message { get; init; }
}

public static class MyDialogExtensions
{
  public static Task<bool> AskAsync(this IRouter router, MyOptions? options = null)
  {
    options ??= new MyOptions();
    return router.ShowAsync<bool>(new MyConfirmDialogSession { Message = options.Message },
                                  options.ToDimmer());
  }
}
```

### Floating pages

A page presented over the dimmer and its own chrome chain, with no independent history — a
presentation, not a stack entry:

```csharp
IRouter result = Router.Derive();
await result.RouteAsync(new Locator(
[
  new DefaultDimmerModel { LightDismiss = true },
  Target.Of(typeof(PostDetailChromeLayoutModel)),
  Target.Of(typeof(PostDetailPageModel), new PostDetailArgs(post))
]));
```

## 2. Feedback

One service, three channels. `INoticeService : IToastService, ISnackbarService, INotificationService`;
inject it and call it from any thread.

```csharp
[Inject] private partial INoticeService Notice { get; }

Notice.Toast(Lang.Pages.Uploaded, ToastLevel.Success);                       // passive, auto-dismisses
SnackbarResult r = await Notice.ShowAsync(Lang.Pages.Deleted, Lang.Pages.Undo);
Notice.Notify(Lang.Pages.Build, Lang.Pages.Finished, NotificationLevel.Success);   // titled banner
NotificationResult n = await Notice.NotifyAsync(title, message, level, actionText: Lang.Pages.Retry);
```

| Channel | Fire-and-forget | Awaitable | Result |
|---|---|---|---|
| Toast | `Toast(message, ToastLevel, duration?)` | — | — |
| Snackbar | `Show(message, actionText?, duration?)` | `ShowAsync(...)` | `SnackbarResult` (`TimedOut` / `ActionInvoked` / `Dismissed`) |
| Notification | `Notify(title, message, NotificationLevel, duration?, actionText?)` | `NotifyAsync(...)` | `NotificationResult` |

`NoticePosition` (`TopLeft` … `BottomRight`) and the default durations come from
`NoticeServiceOptions`, registered by `AddNesterNotice`. The service is a layer tenant, not a
renderer: it rents the notice band on the first show, and when the lease is evicted it drops its host
and discards later shows rather than faulting the caller.

## 3. Messaging

A synchronous, broadcast-only hub: no request/reply, no weak references, no type registry. A fact is
a sealed record; a recipient declares what it accepts.

```csharp
public sealed record OrderShipped(string OrderId) : IMessage;

public sealed partial class OrdersViewModel : IMessageRecipient<OrderShipped>
{
  public void Receive(OrderShipped message) => Reload(message.OrderId);
}

var token = Messages.Register(this);      // reference identity; a duplicate registration throws
Messages.Publish(new OrderShipped(id));   // registration order, synchronous, roster fixed at start
token.Dispose();                          // idempotent; Unregister(this) is the other form
```

| Rule | Behavior |
|---|---|
| `CanReceive` | Pure; may filter on the payload. A throw aborts the broadcast. |
| `Receive` | Runs on the publisher's thread; the message instance is shared and read-only. A throw is isolated per recipient and reported as `MessageReceiveException` after the broadcast. |
| Ordering | Registration order; nested broadcasts settle inner-first. |
| Threading | No main-thread affinity — any thread may publish and receive. |

Typed arities 1..6 exist; `IMessageRecipient` alone is the untyped form (what the template's
`MainLayoutModel` implements). The hub's scope is user-owned — the framework registers none. Put a
`MessageHub` in the process container and bridge it into each window. Application lifecycle facts
(`AppResumedMessage`, `AppBackgroundMessage`, `AppReopenMessage`) are published by the activation
agents that take a hub.

## 4. Authorization

Policies are configured where the identity lives — the process container:

```csharp
services.AddNesterAuth(options => options.AddPolicy("Admin", p => p.RequireRole("Admin")));
```

```csharp
[Authorize(Roles = "Admin", Policy = "Admin")]
[Layout<AuthLayoutModel>]
public partial class AdminPageModel : RoutableModel;
```

Route protection is only half of it: the template's `AuthorizedRouter` (a `IRouter` subclass
registered in the shell container) evaluates every route against the generated `[AuthRegistry]`
binding, so a direct route to a protected page is refused, not merely unlinked.

```xml
<n:AuthorizeView Rules="Admin">
  <n:AuthorizeView.Authorized>   <TextBlock Text="…" /> </n:AuthorizeView.Authorized>
  <n:AuthorizeView.NotAuthorized><TextBlock Text="…" /> </n:AuthorizeView.NotAuthorized>
</n:AuthorizeView>

<Button Content="Admin panel" n:Authorize.Visible="Admin" />
<Button Content="Admin action" n:Authorize.Enable="Admin" />
```

The gates live in `Everlong.Nester.Extensions.Avalonia` / `.Wpf`; the policy engine in
`Everlong.Nester.Extensions`. The rules string mirrors `[Authorize]`:
`"Policy=Admin; Roles=Admin,Owner"`, or the shorthand `"R=Admin,Owner"`. `[Concat<TRegistry>]` appends
a second registry, consulted when the generated one misses.

## 5. Settings

```csharp
[Settings]
public partial class AppSettings : IInjectable
{
  [Setting("en")]  public partial string Language { get; set; }
  [Setting(true)]  public partial bool AutoSave { get; set; }

  // raw value → Range clamp → Coercion method → stored
  [Setting(14), Range(10, 24), Coercion]
  public partial int FontSize { get; set; }

  [Setting] public partial string[] Tags { get; set; }
  private static partial string[] GetTagsDefault() => ["C#", "Java", "Go"];
  private static partial int CoerceFontSize(int value) => value % 2 == 0 ? value : value + 1;

  [Section] public partial AccountSettings Account { get; }
  [Section] public partial SecuritySettings Security { get; }

  public virtual void Inject(IServiceProvider services)
    => (this as ISettingsRoot).Initialize(services.GetRequiredService<ISettingsStore>());
}
```

`[Range]` is `System.ComponentModel.DataAnnotations.RangeAttribute`; `[Coercion]` emits the
`private static partial T CoerceXxx(T)` you implement; a property with no constructor argument takes
its default from a generated `GetXxxDefault()` when you write one.

Persist through your own `ISettingsStore` — subclass `SettingsStoreBase` and register it:

```csharp
[Singleton<ISettingsStore>]
public sealed class JsonSettingsStore : SettingsStoreBase
{
  protected override IEnumerable<KeyValuePair<string, string>> LoadAll() { ... }
  protected override Task PersistAsync(string key, string value, CancellationToken ct) { ... }
}
```

Bind the store once, in the shell's `OnAssembled` (`AppSettings.Default.Inject(Services)`) — the
store is a shell singleton, so no scope is needed. After that, read and write anywhere through
`AppSettings.Default`, and bind sections directly (`{Binding Account.Email}`).

## 6. Internationalization

Locales are JSON files under `Properties/i18n/<locale>/`; `dotnet elg gen` turns them into typed
`Lang` members. Opt in with one property:

```xml
<Elg>true</Elg>   <!-- auto-detects Properties/i18n/i18n.jsonc -->
```

`output.lineEnding` pins the line endings of every file the tool writes — `"lf"` (the default),
`"crlf"`, or `"platform"` (the newline of the running OS).  The template declares `"lf"` to match the
repository's `.editorconfig`, so regenerating on any platform leaves the working tree byte-identical.

```json
// Properties/i18n/en/Pages.json
{ "DashboardTitle": "Nester Feature Hub", "Greeting": "Hello, {name}!" }
```

The member's `/// <summary>` is the JSONC comment written **under** the entry it documents (the first
entry of a module takes the comment above the object); an entry without one is documented by its own
value.

```csharp
Lang.Pages.DashboardTitle          // typed, no dictionary lookup
Lang.Pages.FormatGreeting(user);   // an ICU placeholder generates a typed FormatXxx helper
```

A string containing `{name}` (or `{count, plural, one {{count} item} other {{count} items}}`)
additionally generates `FormatXxx(...)` with one parameter per placeholder — call that rather than
reaching for `IcuMessageFormatter` yourself.  The formatter resolves `=0` / `one` / `other` branches
and `{argument}` placeholders; ICU's `#` shorthand is not implemented, so spell the argument out inside
the branch.

Bootstrap once, then switch at runtime:

```csharp
Lang.Initialize(AppSettings.Default.Language);   // after the first shell is up
Lang.UseLocale("zh-CN");                          // synchronous — existing bindings follow
```

`Lang.Initialize` / `Lang.UseLocale` are generated by the coordinator when
`i18n.jsonc` lists `coordinator.manifests`; it registers every assembly's manifest so one call
switches the app and the framework chrome together. `dotnet elg check` (CI) fails when a translation
falls behind the default locale, and `dotnet elg sync` fills the missing keys.

The `Everlong.Globalization.Tools` CLI (`dotnet tool install --global Everlong.Globalization.Tools`)
provides `init`, `gen`, `add`, `check` and `sync`.
