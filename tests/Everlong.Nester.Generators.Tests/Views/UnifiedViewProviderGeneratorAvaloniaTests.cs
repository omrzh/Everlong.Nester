using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using System.Reflection;
using System.Text.RegularExpressions;
using Everlong.Nester.Tests.Helpers;
using Everlong.Nester.Generators.Views;
using static VerifyXunit.Verifier;

namespace Everlong.Nester.Tests.Views;

public class UnifiedViewProviderGeneratorAvaloniaTests
{
  private static readonly MetadataReference CorlibReference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
  private static readonly MetadataReference SystemRuntimeReference = MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location);
  private static readonly MetadataReference SystemCollectionsReference = MetadataReference.CreateFromFile(Assembly.Load("System.Collections").Location);
  private static readonly MetadataReference NetStandardReference = MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location);

  // Fake Everlong.Nester.Generators.Avalonia reference
  private static readonly MetadataReference NesterAvaloniaReference = MetadataReference.CreateFromFile(typeof(UnifiedViewProviderGeneratorAvaloniaTests).Assembly.Location)
      .WithProperties(new MetadataReferenceProperties(aliases: System.Collections.Immutable.ImmutableArray.Create("Everlong.Nester.Generators.Avalonia")));

  [Fact]
  public Task GeneratesAvaloniaUnifiedProvider()
  {
    var source = @"
using Everlong.Nester.Presentation;
using Everlong.Nester.Routing;
using Everlong.Nester.Dialog;
using System;

namespace Everlong.Nester.Presentation
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ViewLocatorAttribute : Attribute {}
}

namespace Everlong.Nester.Presentation
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ViewForAttribute<T> : Attribute {}
}

namespace Everlong.Nester.Presentation
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MappingAttribute<TVm, TV> : Attribute {}
}

namespace Everlong.Nester.Routing
{
    public interface IRoute {}
    [AttributeUsage(AttributeTargets.Class)]
    public class RoutableAttribute : Attribute {}

    [AttributeUsage(AttributeTargets.Class)]
    public class RoutableAttribute<T> : Attribute {}
}

namespace Everlong.Nester.Dialog
{
    public class DialogSessionBase {}
}

namespace TestApp.ViewModels
{
    public class MainViewModel {}
    public class SettingsViewModel {}

    // No [ViewFor] declaration points at this one — the provider must not claim it.
    [Routable]
    public class HomeViewModel {}

    public class MyDialogViewModel : DialogSessionBase {}
}

namespace Avalonia
{
    public class Application {}
}

namespace Avalonia.Controls
{
    public class Control {}
}

namespace TestApp.Views
{
    [ViewFor<TestApp.ViewModels.MainViewModel>]
    public class MainView {}

    public class SettingsView {} // Convention

    public class HomeView {} // Convention
}

namespace TestApp
{
    [ViewLocator]
    [Mapping<TestApp.ViewModels.MyDialogViewModel, TestApp.Views.MainView>] // Explicit mapping for dialog
    public partial class AppViewProvider {}
}
";
    // Create Everlong.Nester.Generators.Avalonia compilation to get a valid reference
    var avaloniaComp = CSharpCompilation.Create("Everlong.Nester.Generators.Avalonia");
    var avaloniaRef = avaloniaComp.ToMetadataReference();

    var compilation = CSharpCompilation.Create(
        "TestApp",
        new[] { CSharpSyntaxTree.ParseText(source, cancellationToken: TestContext.Current.CancellationToken) },
        new[] {
                CorlibReference,
                SystemRuntimeReference,
                SystemCollectionsReference,
                NetStandardReference,
                avaloniaRef // This should trigger Avalonia framework detection
        },
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
    );

    var generator = new ViewLocatorGenerator();
    GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

    driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

    var runResult = driver.GetRunResult();

    if (runResult.Diagnostics.Length > 0)
    {
      foreach (var diag in runResult.Diagnostics)
      {
        System.Console.WriteLine(diag.ToString());
      }
    }

    Assert.NotEmpty(runResult.GeneratedTrees);

    return Verify(SnapshotBuilder.Build(runResult)).UseDirectory("Snapshots");
  }

  /// <summary>
  ///   A trigger with no declarations still generates a provider — one that claims nothing: there is no
  ///   view model to claim and no view to build for one.
  /// </summary>
  [Fact]
  public Task GeneratesAvaloniaUnifiedProviderWithoutDeclarations()
  {
    var source = @"
using Everlong.Nester.Presentation;
using System;

namespace Everlong.Nester.Presentation
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ViewLocatorAttribute : Attribute {}
}

namespace Everlong.Nester.Presentation
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class MappingAttribute<TVm, TV> : Attribute {}
}

namespace Avalonia
{
    public class Application {}
}

namespace Avalonia.Controls
{
    public class Control {}
}

namespace TestApp
{
    [ViewLocator]
    public partial class AppViewProvider
    {
    }
}
";
    // Create Everlong.Nester.Generators.Avalonia compilation to get a valid reference
    var avaloniaComp = CSharpCompilation.Create("Everlong.Nester.Generators.Avalonia");
    var avaloniaRef = avaloniaComp.ToMetadataReference();

    var compilation = CSharpCompilation.Create(
        "TestApp",
        new[] { CSharpSyntaxTree.ParseText(source, cancellationToken: TestContext.Current.CancellationToken) },
        new[] {
                CorlibReference,
                SystemRuntimeReference,
                SystemCollectionsReference,
                NetStandardReference,
                avaloniaRef // This should trigger Avalonia framework detection
        },
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
    );

    var generator = new ViewLocatorGenerator();
    GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

    driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

    var runResult = driver.GetRunResult();

    Assert.NotEmpty(runResult.GeneratedTrees);
    Assert.Empty(runResult.Diagnostics);

    return Verify(SnapshotBuilder.Build(runResult)).UseDirectory("Snapshots");
  }

  /// <summary>
  ///   A trigger in the global namespace: the file declares no namespace, so the header's leading trivia
  ///   lands on the declaration itself — and must not swallow the declaration's own
  ///   <c>/// &lt;inheritdoc/&gt;</c>.
  /// </summary>
  [Fact]
  public Task GeneratesAvaloniaUnifiedProviderWithoutNamespace()
  {
    var source = @"
using Everlong.Nester.Presentation;
using System;

namespace Everlong.Nester.Presentation
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ViewLocatorAttribute : Attribute {}
}

namespace Everlong.Nester.Presentation
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class MappingAttribute<TVm, TV> : Attribute {}
}

namespace Avalonia
{
    public class Application {}
}

namespace Avalonia.Controls
{
    public class Control {}
}

public class PlainViewModel {}

public class PlainView : Avalonia.Controls.Control {}

[ViewLocator]
[Mapping<PlainViewModel, PlainView>]
public partial class GlobalViewProvider
{
}
";
    // Create Everlong.Nester.Generators.Avalonia compilation to get a valid reference
    var avaloniaComp = CSharpCompilation.Create("Everlong.Nester.Generators.Avalonia");
    var avaloniaRef = avaloniaComp.ToMetadataReference();

    var compilation = CSharpCompilation.Create(
        "TestApp",
        new[] { CSharpSyntaxTree.ParseText(source, cancellationToken: TestContext.Current.CancellationToken) },
        new[] {
                CorlibReference,
                SystemRuntimeReference,
                SystemCollectionsReference,
                NetStandardReference,
                avaloniaRef // This should trigger Avalonia framework detection
        },
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
    );

    var generator = new ViewLocatorGenerator();
    GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

    driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

    var runResult = driver.GetRunResult();

    Assert.NotEmpty(runResult.GeneratedTrees);
    Assert.Empty(runResult.Diagnostics);

    return Verify(SnapshotBuilder.Build(runResult)).UseDirectory("Snapshots");
  }

  [Fact]
  public Task GeneratesAvaloniaUnifiedProviderWithMultipleViewForOnSingleView()
  {
    var source = @"
using Everlong.Nester.Presentation;
using System;

namespace Everlong.Nester.Presentation
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ViewLocatorAttribute : Attribute {}
}

namespace Everlong.Nester.Presentation
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ViewForAttribute<T> : Attribute {}
}

namespace Everlong.Nester.Routing
{
    public interface IRoute {}
}

namespace Avalonia
{
    public class Application {}
}

namespace Avalonia.Controls
{
    public class Control {}
}

namespace TestApp.ViewModels
{
    public class MainViewModel {}
    public class DetailsViewModel {}
}

namespace TestApp.Views
{
    [ViewFor<TestApp.ViewModels.MainViewModel>]
    [ViewFor<TestApp.ViewModels.DetailsViewModel>]
    public class SharedView : Avalonia.Controls.Control {}
}

namespace TestApp
{
    [ViewLocator]
    public partial class AppViewProvider {}
}
";
    var avaloniaComp = CSharpCompilation.Create("Everlong.Nester.Generators.Avalonia");
    var avaloniaRef = avaloniaComp.ToMetadataReference();

    var compilation = CSharpCompilation.Create(
        "TestApp",
        new[] { CSharpSyntaxTree.ParseText(source, cancellationToken: TestContext.Current.CancellationToken) },
        new[] {
                CorlibReference,
                SystemRuntimeReference,
                SystemCollectionsReference,
                NetStandardReference,
                avaloniaRef
        },
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
    );

    var generator = new ViewLocatorGenerator();
    GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
    driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

    var runResult = driver.GetRunResult();
    Assert.NotEmpty(runResult.GeneratedTrees);

    return Verify(SnapshotBuilder.Build(runResult)).UseDirectory("Snapshots");
  }

  /// <summary>
  ///   A nested trigger generates like any other: C# instantiates nested types, so the generated
  ///   half is wrapped in its enclosing types instead of being rejected. Each enclosing type must be
  ///   partial — <c>PartialKeywordAnalyzer</c> reports it otherwise.
  /// </summary>
  [Fact]
  public Task GeneratesProviderForNestedTrigger()
  {
    var source = @"
using Everlong.Nester.Presentation;
using System;

namespace Everlong.Nester.Presentation
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ViewLocatorAttribute : Attribute {}
}

namespace Everlong.Nester.Presentation
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ViewForAttribute<T> : Attribute {}
}

namespace Avalonia
{
    public class Application {}
}

namespace Avalonia.Controls
{
    public class Control {}
}

namespace TestApp.ViewModels
{
    public class MainViewModel {}
}

namespace TestApp.Views
{
    [ViewFor<TestApp.ViewModels.MainViewModel>]
    public class MainView {}
}

namespace TestApp
{
    public partial class Container
    {
        [ViewLocator]
        public partial class AppViewProvider {}
    }
}
";

    return VerifyGenerated(source);
  }

  /// <summary>
  ///   The generated half restates the arity of the type it completes and of every type enclosing
  ///   it: a missing type parameter list would silently declare a *different* type instead of
  ///   failing to compile.
  /// </summary>
  [Fact]
  public Task GeneratesProviderForNestedGenericTrigger()
  {
    var source = @"
using Everlong.Nester.Presentation;
using System;

namespace Everlong.Nester.Presentation
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ViewLocatorAttribute : Attribute {}
}

namespace Everlong.Nester.Presentation
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ViewForAttribute<T> : Attribute {}
}

namespace Avalonia
{
    public class Application {}
}

namespace Avalonia.Controls
{
    public class Control {}
}

namespace TestApp.ViewModels
{
    public class MainViewModel {}
}

namespace TestApp.Views
{
    [ViewFor<TestApp.ViewModels.MainViewModel>]
    public class MainView {}
}

namespace TestApp
{
    public partial class Container<T>
    {
        [ViewLocator]
        public partial class AppViewProvider<TItem> {}
    }
}
";

    return VerifyGenerated(source);
  }

  private static Task VerifyGenerated(string source)
  {
    var avaloniaRef = CSharpCompilation.Create("Everlong.Nester.Generators.Avalonia").ToMetadataReference();

    var compilation = CSharpCompilation.Create(
        "TestApp",
        new[] { CSharpSyntaxTree.ParseText(source) },
        new[] {
                CorlibReference,
                SystemRuntimeReference,
                SystemCollectionsReference,
                NetStandardReference,
                avaloniaRef
        },
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
    );

    GeneratorDriver driver = CSharpGeneratorDriver.Create(new ViewLocatorGenerator());
    driver = driver.RunGenerators(compilation);

    var runResult = driver.GetRunResult();
    Assert.NotEmpty(runResult.GeneratedTrees);
    Assert.Empty(runResult.Diagnostics);

    return Verify(SnapshotBuilder.Build(runResult)).UseDirectory("Snapshots");
  }

  /// <summary>
  ///   The claim set and the build set are the same set — <c>Match</c> may only answer true
  ///   for a view model the provider can actually present.
  /// </summary>
  [Fact]
  public void ClaimsOnlyWhatItCanBuild()
  {
    var source = @"
using Everlong.Nester.Presentation;
using Everlong.Nester.Routing;
using System;

namespace Everlong.Nester.Presentation
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ViewLocatorAttribute : Attribute {}
}

namespace Everlong.Nester.Presentation
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ViewForAttribute<T> : Attribute {}
}

namespace Everlong.Nester.Routing
{
    [AttributeUsage(AttributeTargets.Class)]
    public class RoutableAttribute : Attribute {}
}

namespace Avalonia.Controls
{
    public class Control {}
}

namespace TestApp.ViewModels
{
    public class PairedViewModel {}

    // Declared as a route without a view — no [ViewFor] points at it.
    [Routable]
    public class UnpairedViewModel {}
}

namespace TestApp.Views
{
    [ViewFor<TestApp.ViewModels.PairedViewModel>]
    public class PairedView {}
}

namespace TestApp
{
    [ViewLocator]
    public partial class AppViewProvider {}
}
";
    // Create Everlong.Nester.Generators.Avalonia compilation to get a valid reference
    var avaloniaComp = CSharpCompilation.Create("Everlong.Nester.Generators.Avalonia");
    var avaloniaRef = avaloniaComp.ToMetadataReference();

    var compilation = CSharpCompilation.Create(
        "TestApp",
        new[] { CSharpSyntaxTree.ParseText(source, cancellationToken: TestContext.Current.CancellationToken) },
        new[] {
                CorlibReference,
                SystemRuntimeReference,
                SystemCollectionsReference,
                NetStandardReference,
                avaloniaRef
        },
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
    );

    GeneratorDriver driver = CSharpGeneratorDriver.Create(new ViewLocatorGenerator());
    driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

    var generated = string.Join(
      Environment.NewLine,
      driver.GetRunResult().GeneratedTrees.Select(t => t.GetText().ToString()));

    var claimed = Matches(generated, @"^\s*typeof\(global::TestApp\.ViewModels\.(\w+)\)", RegexOptions.Multiline);
    var built = Matches(generated, @"if \(type == typeof\(global::TestApp\.ViewModels\.(\w+)\)\)", RegexOptions.None);

    Assert.Equal(["PairedViewModel"], claimed);
    Assert.Equal(claimed, built);
  }

  /// <summary>
  ///   A view nested in a non-generic type is a candidate like any other — the provider names it
  ///   through its enclosing type.  A view the locator cannot name (generic, or nested in a generic
  ///   type) is skipped and left to <c>ViewInstantiationAnalyzer</c> (NSTR1004) to report.
  /// </summary>
  [Fact]
  public void GeneratesProviderForNestedView_AndSkipsUnnameable()
  {
    var source = @"
using Everlong.Nester.Presentation;
using System;

namespace Everlong.Nester.Presentation
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ViewLocatorAttribute : Attribute {}
}

namespace Everlong.Nester.Presentation
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ViewForAttribute<T> : Attribute {}
}

namespace Avalonia.Controls
{
    public class Control {}
}

namespace TestApp.ViewModels
{
    public class MainViewModel {}
    public class OtherViewModel {}
    public class ThirdViewModel {}
}

namespace TestApp.Views
{
    public class ViewHost
    {
        [ViewFor<TestApp.ViewModels.MainViewModel>]
        public class InnerView {}
    }

    [ViewFor<TestApp.ViewModels.OtherViewModel>]
    public class GenericView<T> {}

    public class GenericHost<T>
    {
        [ViewFor<TestApp.ViewModels.ThirdViewModel>]
        public class NestedInGeneric {}
    }
}

namespace TestApp
{
    [ViewLocator]
    public partial class AppViewProvider {}
}
";

    var generated = RunGenerator(source);

    Assert.Contains("new global::TestApp.Views.ViewHost.InnerView()", generated);

    var claimed = Matches(generated, @"^\s*typeof\(global::TestApp\.ViewModels\.(\w+)\)", RegexOptions.Multiline);
    var built = Matches(generated, @"if \(type == typeof\(global::TestApp\.ViewModels\.(\w+)\)\)", RegexOptions.None);

    Assert.Equal(["MainViewModel"], claimed);
    Assert.Equal(claimed, built);
  }

  /// <summary>Runs the Avalonia generator over <paramref name="source" /> and returns its generated text.</summary>
  private static string RunGenerator(string source)
  {
    var avaloniaRef = CSharpCompilation.Create("Everlong.Nester.Generators.Avalonia").ToMetadataReference();

    var compilation = CSharpCompilation.Create(
        "TestApp",
        new[] { CSharpSyntaxTree.ParseText(source) },
        new[] {
                CorlibReference,
                SystemRuntimeReference,
                SystemCollectionsReference,
                NetStandardReference,
                avaloniaRef
        },
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
    );

    GeneratorDriver driver = CSharpGeneratorDriver.Create(new ViewLocatorGenerator());
    driver = driver.RunGenerators(compilation);

    return string.Join(
      Environment.NewLine,
      driver.GetRunResult().GeneratedTrees.Select(t => t.GetText().ToString()));
  }

  private static string[] Matches(string text, string pattern, RegexOptions options)
    => Regex.Matches(text, pattern, options)
      .Select(m => m.Groups[1].Value)
      .Distinct()
      .Order()
      .ToArray();
}
