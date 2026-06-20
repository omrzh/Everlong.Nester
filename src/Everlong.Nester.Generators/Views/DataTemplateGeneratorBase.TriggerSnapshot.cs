using Everlong.Nester.Generators.Constants;
using Everlong.Nester.Generators.Helpers;
using Everlong.Nester.Generators.Models;
using Microsoft.CodeAnalysis;
namespace Everlong.Nester.Generators.Views;

public abstract partial class DataTemplateGeneratorBase
{
  private TriggerSnapshot? CreateTriggerSnapshot(GeneratorAttributeSyntaxContext context)
  {
    if (context.TargetSymbol is not INamedTypeSymbol symbol)
    {
      return null;
    }

    var contracts = new Dictionary<TypeName, MappingModel>();

    foreach (var attr in symbol.GetAttributes())
    {
      var attributeClass = attr.AttributeClass;
      if (attributeClass == null)
      {
        continue;
      }

      AddMappingContract(attributeClass, contracts);
    }

    var registration = new RegistrationModel(
      TypeHierarchy.From(symbol),
      contracts.Values.ToEquatableArray()
    );

    return new TriggerSnapshot(
      registration,
      LocationInfo.CreateFrom(symbol.Locations.FirstOrDefault()));
  }

  private static void AddMappingContract(
    INamedTypeSymbol attributeClass,
    Dictionary<TypeName, MappingModel> contracts)
  {
    if (attributeClass is
      { Name: Attributes.Mapping, TypeArguments: [INamedTypeSymbol vmSymbol, INamedTypeSymbol vSymbol] }
        && ViewGeneratorSymbolHelper.IsMappingNamespace(attributeClass.ContainingNamespace))
    {
      contracts[TypeName.FromNamedTypeSymbol(vmSymbol)] = MappingModel.From(vmSymbol, vSymbol);
    }
  }
}
