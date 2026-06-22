using Everlong.Nester.CodeFixers;
using Everlong.Nester.Generators.Analyzers;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Everlong.Nester.Tests.PartialKeyword;

/// <summary>
///   Covers <see cref="PartialKeywordCodeFixProvider" /> against the real
///   <see cref="PartialKeywordAnalyzer" /> diagnostic: the fix must add the
///   <c>partial</c> keyword without touching anything else, and it must
///   batch correctly under Fix All.
/// </summary>
public class PartialKeywordCodeFixProviderTests
{
  [Fact]
  public async Task AddsPartialToTheAnnotatedType()
  {
    var test = new CSharpCodeFixTest<PartialKeywordAnalyzer, PartialKeywordCodeFixProvider, DefaultVerifier>
    {
      TestCode = """
using Everlong.Nester.Presentation;

namespace TestApp
{
    [ViewLocator]
    public class {|NSTR0001:AppViewProvider|}
    {
        public object Build() => new();
    }
}

namespace Everlong.Nester.Presentation
{
    public class ViewLocatorAttribute : System.Attribute { }
}
""",
      FixedCode = """
using Everlong.Nester.Presentation;

namespace TestApp
{
    [ViewLocator]
    public partial class AppViewProvider
    {
        public object Build() => new();
    }
}

namespace Everlong.Nester.Presentation
{
    public class ViewLocatorAttribute : System.Attribute { }
}
"""
    };

    await test.RunAsync(TestContext.Current.CancellationToken);
  }

  [Fact]
  public async Task AddsPartialToTheEnclosingType()
  {
    var test = new CSharpCodeFixTest<PartialKeywordAnalyzer, PartialKeywordCodeFixProvider, DefaultVerifier>
    {
      TestCode = """
using Everlong.Nester.Presentation;

namespace TestApp
{
    public class {|NSTR0008:Container|}
    {
        [ViewLocator]
        public partial class AppViewProvider
        {
        }
    }
}

namespace Everlong.Nester.Presentation
{
    public class ViewLocatorAttribute : System.Attribute { }
}
""",
      FixedCode = """
using Everlong.Nester.Presentation;

namespace TestApp
{
    public partial class Container
    {
        [ViewLocator]
        public partial class AppViewProvider
        {
        }
    }
}

namespace Everlong.Nester.Presentation
{
    public class ViewLocatorAttribute : System.Attribute { }
}
"""
    };

    await test.RunAsync(TestContext.Current.CancellationToken);
  }

  [Fact]
  public async Task FixAll_MakesEveryAnnotatedTypePartial()
  {
    var test = new CSharpCodeFixTest<PartialKeywordAnalyzer, PartialKeywordCodeFixProvider, DefaultVerifier>
    {
      TestCode = """
using Everlong.Nester.Presentation;

namespace TestApp
{
    [ViewLocator]
    public class {|NSTR0001:AppViewProvider|}
    {
    }

    [ViewLocator]
    public class {|NSTR0001:DialogViewProvider|}
    {
    }
}

namespace Everlong.Nester.Presentation
{
    public class ViewLocatorAttribute : System.Attribute { }
}
""",
      FixedCode = """
using Everlong.Nester.Presentation;

namespace TestApp
{
    [ViewLocator]
    public partial class AppViewProvider
    {
    }

    [ViewLocator]
    public partial class DialogViewProvider
    {
    }
}

namespace Everlong.Nester.Presentation
{
    public class ViewLocatorAttribute : System.Attribute { }
}
""",
      BatchFixedCode = """
using Everlong.Nester.Presentation;

namespace TestApp
{
    [ViewLocator]
    public partial class AppViewProvider
    {
    }

    [ViewLocator]
    public partial class DialogViewProvider
    {
    }
}

namespace Everlong.Nester.Presentation
{
    public class ViewLocatorAttribute : System.Attribute { }
}
"""
    };

    await test.RunAsync(TestContext.Current.CancellationToken);
  }
}
