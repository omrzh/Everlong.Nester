// pollyfills exemption from all rules
#pragma warning disable

namespace System.Diagnostics.CodeAnalysis
{
  [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
  internal sealed class UnscopedRefAttribute : Attribute { }
}
