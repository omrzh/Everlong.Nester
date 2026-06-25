using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using Everlong.Nester.Generators.Auth;
using Everlong.Nester.Tests.Helpers;
using static VerifyXunit.Verifier;

namespace Everlong.Nester.Tests.Auth;

public class AuthRegistryGeneratorTests
{
  private static readonly MetadataReference CorlibReference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
  private static readonly MetadataReference SystemRuntimeReference = MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location);
  private static readonly MetadataReference SystemCollectionsReference = MetadataReference.CreateFromFile(Assembly.Load("System.Collections").Location);
  private static readonly MetadataReference NetStandardReference = MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location);

  private const string Stub = """
using System;
using Everlong.Nester.Auth;

namespace Everlong.Nester.Auth
{
    public record AuthDescriptor(string? Roles = null, string? Policy = null);

    public interface IAuthRegistry
    {
        AuthDescriptor? GetDescriptor(Type type);
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class AuthRegistryAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ConcatAttribute<TRegistry> : Attribute where TRegistry : IAuthRegistry { }

    [AttributeUsage(AttributeTargets.Class)]
    public class AuthorizeAttribute : Attribute
    {
        public string? Roles { get; set; }
        public string? Policy { get; set; }
    }
}

""";

  [Fact]
  public Task GeneratesRegistryWithDeclaredRequirements()
  {
    var source = Stub + """
namespace TestApp.Pages
{
    [AuthRegistry]
    public partial class AppAuthRegistry { }

    [Authorize(Roles = "Admin")]
    public partial class AdminPageModel { }

    [Authorize(Policy = "VipOnly")]
    public partial class VipPageModel { }

    [Authorize]
    public partial class MemberAreaPageModel { }
}

namespace TestApp.Layouts
{
    [Authorize(Roles = "Admin", Policy = "Audited")]
    public partial class BackOfficeLayoutModel { }
}

""";
    return RunAndVerify(source);
  }

  [Fact]
  public Task GeneratesEmptyRegistryWithoutDeclarations()
  {
    var source = Stub + """
namespace TestApp.Pages
{
    [AuthRegistry]
    public partial class AppAuthRegistry { }

    public partial class OpenPageModel { }
}

""";
    return RunAndVerify(source);
  }

  /// <summary>
  ///   A trigger in the global namespace: the registry emits neither usings nor a namespace, so the
  ///   header leads the declaration itself — and must not swallow its <c>/// &lt;inheritdoc/&gt;</c>.
  /// </summary>
  [Fact]
  public Task GeneratesRegistryWithoutNamespace()
  {
    var source = Stub + """
[AuthRegistry]
public partial class AppAuthRegistry { }

[Authorize(Roles = "Admin")]
public partial class AdminPageModel { }

""";
    return RunAndVerify(source);
  }

  [Fact]
  public Task MergesConcatenatedRegistry()
  {
    var source = Stub + """
namespace TestLib
{
    public partial class LibAuthRegistry : global::Everlong.Nester.Auth.IAuthRegistry
    {
        // A hand-written registry — the whole contract is GetDescriptor.
        public global::Everlong.Nester.Auth.AuthDescriptor? GetDescriptor(Type type)
            => type == typeof(global::TestLib.LibOnlyPage)
                ? new global::Everlong.Nester.Auth.AuthDescriptor(Roles: "LibAdmin")
                : null;
    }
}

namespace TestLib
{
    public partial class LibOnlyPage { }
}

namespace TestApp
{
    [AuthRegistry]
    [Concat<global::TestLib.LibAuthRegistry>]
    public partial class AppAuthRegistry { }

    [Authorize(Roles = "User")]
    public partial class ProfilePageModel { }
}

""";
    return RunAndVerify(source);
  }

  [Fact]
  public Task ReportsMultipleTriggers()
  {
    var source = Stub + """
namespace TestApp
{
    [AuthRegistry]
    public partial class FirstAuthRegistry { }

    [AuthRegistry]
    public partial class SecondAuthRegistry { }
}

""";
    return RunAndVerify(source);
  }

  /// <summary>
  ///   A nested trigger generates like any other: the generated half is wrapped in its enclosing
  ///   types, which is why every one of them must be partial (PartialKeywordAnalyzer, NSTR0008).
  /// </summary>
  [Fact]
  public Task GeneratesRegistryForNestedTrigger()
  {
    var source = Stub + """
namespace TestApp
{
    public partial class Host
    {
        [AuthRegistry]
        public partial class NestedAuthRegistry { }
    }

    [Authorize(Roles = "Admin")]
    public partial class AdminPageModel { }
}

""";
    return RunAndVerify(source);
  }

  /// <summary>A trigger that is not partial generates nothing — the partial precondition is owned by
  ///   PartialKeywordAnalyzer (NSTR0001/NSTR0008), not by this generator.</summary>
  [Fact]
  public void SkipsNonPartialTrigger()
  {
    var source = Stub + """
namespace TestApp
{
    [AuthRegistry]
    public class AppAuthRegistry { }
}

""";

    var syntaxTree = CSharpSyntaxTree.ParseText(source, cancellationToken: TestContext.Current.CancellationToken);
    var compilation = CSharpCompilation.Create(
      "AuthRegistryTest",
      [syntaxTree],
      [CorlibReference, SystemRuntimeReference, SystemCollectionsReference, NetStandardReference],
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    GeneratorDriver driver = CSharpGeneratorDriver.Create(new AuthRegistryGenerator());
    driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
    var runResult = driver.GetRunResult();

    Assert.Empty(runResult.GeneratedTrees);
    Assert.Empty(runResult.Diagnostics);
  }

  /// <summary>
  ///   The transform's snapshot is what makes this pipeline cacheable: a change that does not alter the
  ///   snapshot — a comment in the trigger's file — must leave the output step cached, while a declaration
  ///   that does alter it must re-run it.  A transform that handed the generation phase the
  ///   <c>GeneratorAttributeSyntaxContext</c> it was given would never compare equal, and both halves of
  ///   this test would fail.
  /// </summary>
  [Fact]
  public void CachesTheOutputWhenOnlyCommentsChange()
  {
    var source = Stub + """
namespace TestApp.Pages
{
    [AuthRegistry]
    public partial class AppAuthRegistry { }

    [Authorize(Roles = "Admin")]
    public partial class AdminPageModel { }
}

""";

    GeneratorDriver driver = CSharpGeneratorDriver.Create(
      [new AuthRegistryGenerator().AsSourceGenerator()],
      driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None,
                                                trackIncrementalGeneratorSteps: true));

    driver = driver.RunGenerators(CreateCompilation(source), TestContext.Current.CancellationToken);
    driver = driver.RunGenerators(
      CreateCompilation(source + "// a comment changes the tree, not the model\n"),
      TestContext.Current.CancellationToken);

    Assert.Equal(new[] { IncrementalStepRunReason.Cached }, OutputReasons(driver));

    driver = driver.RunGenerators(
      CreateCompilation(source + "[Authorize(Roles = \"New\")]\npublic partial class NewPageModel { }\n"),
      TestContext.Current.CancellationToken);

    Assert.Equal(new[] { IncrementalStepRunReason.Modified }, OutputReasons(driver));
  }

  /// <summary>The reasons the generator's output steps ran with on the last run.</summary>
  private static IncrementalStepRunReason[] OutputReasons(GeneratorDriver driver)
    => driver.GetRunResult().Results[0].TrackedOutputSteps
      .SelectMany(static step => step.Value)
      .SelectMany(static step => step.Outputs)
      .Select(static output => output.Reason)
      .Distinct()
      .ToArray();

  private static CSharpCompilation CreateCompilation(string source)
    => CSharpCompilation.Create(
      "AuthRegistryTest",
      [CSharpSyntaxTree.ParseText(source, cancellationToken: TestContext.Current.CancellationToken)],
      [CorlibReference, SystemRuntimeReference, SystemCollectionsReference, NetStandardReference],
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

  private static Task RunAndVerify(string source)
  {
    var syntaxTree = CSharpSyntaxTree.ParseText(source);
    var compilation = CSharpCompilation.Create(
      "AuthRegistryTest",
      [syntaxTree],
      [CorlibReference, SystemRuntimeReference, SystemCollectionsReference, NetStandardReference],
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    GeneratorDriver driver = CSharpGeneratorDriver.Create(new AuthRegistryGenerator());
    driver = driver.RunGenerators(compilation);
    var runResult = driver.GetRunResult();
    return Verify(SnapshotBuilder.Build(runResult))
      .UseDirectory("Snapshots");
  }
}
