using Xunit;

// The 3.2.2 line's only assembly-level parallelization knob, and this head needs
// it: the locale suite reassigns the assembly's process-wide string provider.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
