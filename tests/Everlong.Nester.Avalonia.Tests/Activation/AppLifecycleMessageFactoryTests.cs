using Avalonia.Controls.ApplicationLifetimes;
using Everlong.Nester.Activation;
using Xunit;

namespace Everlong.Nester.Tests.Activation;

public class AppLifecycleMessageFactoryTests
{
  [Fact]
  public void FromActivation_Reopen_MapsToReopenMessage()
  {
    var message = AppLifecycleMessageFactory.FromActivation(new ActivatedEventArgs(ActivationKind.Reopen));

    Assert.IsType<AppReopenMessage>(message);
  }

  [Fact]
  public void FromActivation_Background_MapsToResumedMessage()
  {
    var message = AppLifecycleMessageFactory.FromActivation(new ActivatedEventArgs(ActivationKind.Background));

    Assert.IsType<AppResumedMessage>(message);
  }

  [Theory]
  [InlineData(ActivationKind.File)]
  [InlineData(ActivationKind.OpenUri)]
  public void FromActivation_NonLifecycleKind_CarriesNoLifecycleFact(ActivationKind kind)
  {
    Assert.Null(AppLifecycleMessageFactory.FromActivation(new ActivatedEventArgs(kind)));
  }

  [Fact]
  public void FromDeactivation_MapsToBackgroundMessage()
  {
    Assert.IsType<AppBackgroundMessage>(AppLifecycleMessageFactory.FromDeactivation());
  }
}
