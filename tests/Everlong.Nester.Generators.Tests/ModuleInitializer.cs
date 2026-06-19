using System.Runtime.CompilerServices;
using DiffEngine;

public static class ModuleInitializer
{
  [ModuleInitializer]
  public static void Init()
  {
    // No diff-tool popups on snapshot mismatch — the received file is the diff.
    DiffRunner.Disabled = true;
  }
}
