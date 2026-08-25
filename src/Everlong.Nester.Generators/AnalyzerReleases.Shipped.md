; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 0.1.7

### New Rules
Rule ID | Category | Severity | Notes
--------|----------|----------|-------
NSTR0001 | Usage | Error | Class must be partial for code generation
NSTR0008 | Usage | Error | Enclosing type must be partial for code generation
NSTR1001 | View | Info | Conflicting Mapping and ViewFor mappings
NSTR1002 | View | Info | Mapping is redundant given ViewFor mapping
NSTR1003 | View | Warning | View requires a parameterless constructor
NSTR1004 | View | Warning | View cannot be instantiated by the view locator
NSTR2002 | Configuration | Error | Multiple ViewLocator attributes
NSTR2003 | Configuration | Error | Multiple WpfViewLocator attributes
NSTR2004 | Configuration | Error | Multiple AuthRegistry attributes
NSTR9998 | Transform | Error | Generator internal error at transform phase
NSTR9999 | Generator | Error | Source Generator Exception
