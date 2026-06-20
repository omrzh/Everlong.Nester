using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using Everlong.Nester.Tests.Helpers;
using Everlong.Nester.Generators.Views;
using VerifyTests;
using VerifyXunit;

namespace Everlong.Nester.Tests.DataTemplates;

public class ViewLocatorGeneratorSnapshotTests
{
  [Fact]
  public Task GeneratesViewLocatorForWpf()
  {
    var source = @"
using Everlong.Nester.Presentation;
using Everlong.Nester.Routing;
using Everlong.Nester.ControlsModels;
using System.Windows;
using System.Windows.Controls;
using System;

namespace Everlong.Nester.Presentation
{
    [AttributeUsage(AttributeTargets.Class)]
    public class WpfViewLocatorAttribute : Attribute {}
}

namespace Everlong.Nester.Presentation
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ViewForAttribute<T> : Attribute {}
}

namespace Everlong.Nester.ControlsModels
{
    public class ViewModelBase {}
}

namespace Everlong.Nester.Routing
{
    [AttributeUsage(AttributeTargets.Class)]
    public class RoutableAttribute : Attribute {}

    [AttributeUsage(AttributeTargets.Class)]
    public class RoutableAttribute<T> : Attribute {}
}

namespace System.Windows
{
    public class Application {}
    public class FrameworkElement {}
    public class ResourceDictionary {}
    public class DataTemplate
    {
        public DataTemplate(object dataType) {}
    }
    public class FrameworkElementFactory
    {
        public FrameworkElementFactory(Type type) {}
    }
}

namespace System.Windows.Controls
{
    public class Control : FrameworkElement {}
    public class UserControl : Control {}
    public class TextBlock : Control { public string Text { get; set; } }
}

namespace TestApp.ViewModels
{
    public class HomePageViewModel : ViewModelBase {}
    public class SettingsViewModel : ViewModelBase {}
}

namespace TestApp.Views
{
    [ViewFor<TestApp.ViewModels.HomePageViewModel>]
    public partial class HomePageView : UserControl
    {
        public HomePageView() {}
    }

    [ViewFor<TestApp.ViewModels.SettingsViewModel>]
    public partial class SettingsView : UserControl
    {
        public SettingsView() {}
    }
}

namespace TestApp
{
    [WpfViewLocator]
    public partial class AppViewLocator {}
}
";

    return Verify(source);
  }

  [Fact]
  public Task GeneratesViewLocatorWithMultipleViewForOnSingleView()
  {
    var source = @"
using Everlong.Nester.Presentation;
using Everlong.Nester.Mappings;
using System.Windows;
using System.Windows.Controls;
using System;

namespace Everlong.Nester.Presentation
{
    [AttributeUsage(AttributeTargets.Class)]
    public class WpfViewLocatorAttribute : Attribute {}
}

namespace Everlong.Nester.Presentation
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ViewForAttribute<T> : Attribute {}
}

namespace System.Windows
{
    public class Application {}
    public class FrameworkElement {}
    public class ResourceDictionary {}
    public class DataTemplate
    {
        public DataTemplate(object dataType) {}
        public object VisualTree { get; set; }
        public void Seal() {}
    }
    public class DataTemplateModel
    {
        public DataTemplateModel(Type type) {}
    }
    public class FrameworkElementFactory
    {
        public FrameworkElementFactory(Type type) {}
    }
}

namespace System.Windows.Controls
{
    public class Control : FrameworkElement {}
    public class UserControl : Control {}
    public class TextBlock : Control
    {
        public string Text { get; set; }
    }
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
    public partial class SharedView : UserControl
    {
        public SharedView() {}
    }
}

namespace TestApp
{
    [WpfViewLocator]
    public partial class AppViewLocator {}
}
";

    return Verify(source);
  }


  [Fact]
  public void GeneratesViewLocatorForRecordModelMappedByViewFor()
  {
    var source = @"
using Everlong.Nester.Presentation;
using System.Windows;
using System.Windows.Controls;
using System;

namespace Everlong.Nester.Presentation
{
    [AttributeUsage(AttributeTargets.Class)]
    public class WpfViewLocatorAttribute : Attribute {}
}

namespace Everlong.Nester.Presentation
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ViewForAttribute<T> : Attribute {}
}

namespace System.Windows
{
    public class Application {}
    public class FrameworkElement {}
    public class ResourceDictionary {}
    public class DataTemplate
    {
        public DataTemplate(object dataType) {}
        public object VisualTree { get; set; }
        public void Seal() {}
    }
    public class DataTemplateModel
    {
        public DataTemplateModel(Type type) {}
    }
    public class FrameworkElementFactory
    {
        public FrameworkElementFactory(Type type) {}
    }
}

namespace System.Windows.Controls
{
    public class Control : FrameworkElement {}
    public class UserControl : Control {}
    public class TextBlock : Control
    {
        public string Text { get; set; }
    }
}

namespace TestApp.Models
{
    public record TextMessage(string Content);
}

namespace TestApp.Views
{
    [ViewFor<TestApp.Models.TextMessage>]
    public partial class TextMessageView : UserControl
    {
        public TextMessageView() {}
    }
}

namespace TestApp
{
    [WpfViewLocator]
    public partial class AppViewLocator {}
}
";

    var generated = GenerateOutput(source);
    // Assert.Contains("(data is global::TestApp.Models.TextMessage)", generated);
    Assert.Contains("global::TestApp.Views.TextMessageView", generated);
  }

  [Fact]
  public Task GeneratesViewLocatorWithConditionalMapping()
  {
    var source = @"
using Everlong.Nester.Presentation;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System;

namespace Everlong.Nester.Presentation
{
    [AttributeUsage(AttributeTargets.Class)]
    public class WpfViewLocatorAttribute : Attribute {}
}

namespace Everlong.Nester.Presentation
{
    public interface IViewLocator
    {
    }
}

namespace System.Windows
{
    public class Application {}
    public class FrameworkElement {}
    public class ResourceDictionary
    {
        public object this[object key]
        {
            get => null;
            set {}
        }
    }
    public class DataTemplate
    {
        public DataTemplate(object dataType) {}
        public object VisualTree { get; set; }
        public void Seal() {}
    }
    public class DataTemplateKey
    {
        public DataTemplateKey(Type type) {}
    }
    public class FrameworkElementFactory
    {
        public FrameworkElementFactory(Type type) {}
    }
}

namespace System.Windows.Controls
{
    public class Control : FrameworkElement {}
    public class UserControl : Control {}
    public class ContentControl : Control {}
    public class TextBlock : Control
    {
        public string Text { get; set; }
    }
}

namespace TestApp.Models
{
    public class PointBase {}
}

namespace TestApp
{
    [WpfViewLocator]
    public partial class AppViewLocator {}
}
";

    return Verify(source);
  }

  private Task Verify(string source)
  {
    var syntaxTree = CSharpSyntaxTree.ParseText(source);

    var references = AppDomain.CurrentDomain.GetAssemblies()
        .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
        .Select(a => MetadataReference.CreateFromFile(a.Location))
        .Distinct()
        .ToList();

    var compilation = CSharpCompilation.Create(
        "TestApp",
        [syntaxTree],
        references,
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
    );

    //
    var generator = new WpfViewLocatorGenerator();
    GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

    driver = driver.RunGenerators(compilation);

    var settings = new VerifySettings();
    settings.UseDirectory("Snapshots");
    var runResult = driver.GetRunResult();
    return Verifier.Verify(SnapshotBuilder.Build(runResult), settings);
  }

  private static string GenerateOutput(string source)
  {
    var syntaxTree = CSharpSyntaxTree.ParseText(source);

    var references = AppDomain.CurrentDomain.GetAssemblies()
      .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
      .Select(a => MetadataReference.CreateFromFile(a.Location))
      .Distinct()
      .ToList();

    var compilation = CSharpCompilation.Create(
      "TestApp",
      [syntaxTree],
      references,
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
    );

    var generator = new WpfViewLocatorGenerator();
    GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
    driver = driver.RunGenerators(compilation);
    var generatedTrees = driver.GetRunResult().GeneratedTrees;

    return string.Join(
      Environment.NewLine,
      generatedTrees.Select(t => t.GetText().ToString()));
  }

  private static GeneratorDriverRunResult RunGenerator(string source)
  {
    var syntaxTree = CSharpSyntaxTree.ParseText(source);

    var references = AppDomain.CurrentDomain.GetAssemblies()
      .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
      .Select(a => MetadataReference.CreateFromFile(a.Location))
      .Distinct()
      .ToList();

    var compilation = CSharpCompilation.Create(
      "TestApp",
      [syntaxTree],
      references,
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    var generator = new WpfViewLocatorGenerator();
    GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
    driver = driver.RunGenerators(compilation);
    return driver.GetRunResult();
  }
}
