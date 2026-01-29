using Xunit;

// Prevent cross-test interference via shared environment variables (e.g., FUNCTIONS_APPLICATION_DIRECTORY).
[assembly: CollectionBehavior(DisableTestParallelization = true)]
