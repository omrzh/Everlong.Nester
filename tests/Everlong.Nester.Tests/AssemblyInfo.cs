using Xunit;

// The 3.2.2 line's only assembly-level parallelization knob. The 4.x
// `Parallelization(Mode = ParallelMode.None)` replaces it, and the two cannot
// live in one file, so a test head declares its own and this one is not shared.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
