using Everlong.Nester.Generators.Constants;
using Everlong.Nester.Generators.Models;
using Microsoft.CodeAnalysis;
namespace Everlong.Nester.Generators.Views;

[Generator]
public sealed class WpfViewLocatorGenerator : DataTemplateGeneratorBase
{
  protected override string TriggerAttribute => Attributes.WpfViewLocatorFull;
  protected override UiFramework TargetFramework => UiFramework.Wpf;
}
