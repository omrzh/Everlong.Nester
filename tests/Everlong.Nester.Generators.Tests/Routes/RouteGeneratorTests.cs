using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using Everlong.Nester.Generators.Routes;
using Everlong.Nester.Tests.Helpers;
using static VerifyXunit.Verifier;

namespace Everlong.Nester.Tests.Routes;

public class RouteGeneratorTests
{
  private static readonly MetadataReference CorlibReference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
  private static readonly MetadataReference SystemRuntimeReference = MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location);
  private static readonly MetadataReference SystemCollectionsReference = MetadataReference.CreateFromFile(Assembly.Load("System.Collections").Location);
  private static readonly MetadataReference NetStandardReference = MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location);

  private const string Stub = """
using Everlong.Nester.Routing;

namespace Everlong.Nester.Routing
{
    public interface ITarget
    {
        Type Type { get; }
        IArgs Args { get; }
        object? Instance { get; }
    }

    public interface IArgs
    {
    }

    public record Args : IArgs
    {
        public static readonly Args Empty = new();
    }

    public sealed record Target : ITarget
    {
        public Target(Type type, IArgs? args = null, object? instance = null)
        {
            Type = type;
            Args = args;
            Instance = instance;
        }

        public Type Type { get; }

        public IArgs? Args { get; }

        public object? Instance { get; }

        public static Target Of(Type type, IArgs? args = null, object? instance = null)
            => new(type, args, instance);
    }

    public class Locator
    {
        public Locator(IReadOnlyList<ITarget> targets)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class RoutableAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class RoutableAttribute<TArgs> : Attribute where TArgs : Args { }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class LayoutAttribute<TLayout> : Attribute { }
}

""";

  [Fact]
  public Task GeneratesRouteForBarePage()
  {
    var source = Stub + """
namespace TestApp.Pages
{
    [Routable]
    public partial class AdminPageModel { }
}
""";
    return RunAndVerify(source);
  }

  [Fact]
  public Task GeneratesRouteWithLayoutChain()
  {
    var source = Stub + """
namespace TestApp.Pages
{
    public partial class MainLayoutModel { }

    [Layout<MainLayoutModel>]
    public partial class AdminLayoutModel { }

    [Layout<AdminLayoutModel>]
    [Routable]
    public partial class AdminPageModel { }
}
""";
    return RunAndVerify(source);
  }

  [Fact]
  public Task GeneratesParameterizedRoute()
  {
    var source = Stub + """
namespace TestApp.Pages
{
    public sealed record DetailArgs(string Id) : Args;

    [Routable<DetailArgs>]
    public partial class PostPageModel { }
}
""";
    return RunAndVerify(source);
  }

  [Fact]
  public Task GeneratesRouteWithParameterProjection()
  {
    var source = Stub + """
namespace TestApp.Pages
{
    public partial class MainLayoutModel { }

    public sealed record AdminArgs(string Name) : Args;

    public sealed record AccountArgs(string Code) : Args;

    [Layout<MainLayoutModel>]
    [Routable<AdminArgs>]
    public partial class AdminLayoutModel { }

    [Layout<AdminLayoutModel>]
    [Routable<AccountArgs>]
    public partial class AccountPageModel { }
}
""";
    return RunAndVerify(source);
  }

  [Fact]
  public Task GeneratesViewModelSuffixedRouteName()
  {
    var source = Stub + """
namespace TestApp.Pages
{
    [Routable]
    public partial class SettingsBasicViewModel { }
}
""";
    return RunAndVerify(source);
  }

  private static Task RunAndVerify(string source)
  {
    var syntaxTree = CSharpSyntaxTree.ParseText(source);
    var compilation = CSharpCompilation.Create(
      "RouteTest",
      [syntaxTree],
      [CorlibReference, SystemRuntimeReference, SystemCollectionsReference, NetStandardReference],
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    GeneratorDriver driver = CSharpGeneratorDriver.Create(new RouteGenerator());
    driver = driver.RunGenerators(compilation);
    var runResult = driver.GetRunResult();
    Assert.NotEmpty(runResult.GeneratedTrees);
    return Verify(SnapshotBuilder.Build(runResult))
      .UseDirectory("Snapshots");
  }

}
