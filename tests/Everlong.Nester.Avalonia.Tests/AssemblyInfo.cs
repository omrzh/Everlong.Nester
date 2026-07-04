using Xunit;

// The 3.2.2 line's only assembly-level parallelization knob.  Every head declares
// its own: the 4.x `Parallelization(Mode = ParallelMode.None)` does not exist on
// this line, and the two cannot live in one file.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
