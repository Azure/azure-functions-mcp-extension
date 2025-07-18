// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal static class SemaphoreSlimExtensions
{
    public static async ValueTask<DisposableLock> LockAsync(this SemaphoreSlim semaphore, int millisecondsTimeout = -1, CancellationToken cancellationToken = default)
    {
        var lockAcquired = await semaphore.WaitAsync(millisecondsTimeout, cancellationToken).ConfigureAwait(false);

        if (!lockAcquired)
        {
            throw new TimeoutException("Failed to acquire semaphore within the timeout period.");
        }

        return new(semaphore);
    }

    public readonly struct DisposableLock(SemaphoreSlim semaphore) : IDisposable
    {
        public void Dispose() => semaphore.Release();
    }
}

