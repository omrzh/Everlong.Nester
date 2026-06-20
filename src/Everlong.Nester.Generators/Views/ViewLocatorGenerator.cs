using Everlong.Nester.Generators.Constants;
using Everlong.Nester.Generators.Models;
using Microsoft.CodeAnalysis;
namespace Everlong.Nester.Generators.Views;

[Generator]
public sealed class ViewLocatorGenerator : DataTemplateGeneratorBase
{
  protected override string TriggerAttribute => Attributes.ViewLocatorFull;
  protected override UiFramework TargetFramework => UiFramework.Avalonia;
}
