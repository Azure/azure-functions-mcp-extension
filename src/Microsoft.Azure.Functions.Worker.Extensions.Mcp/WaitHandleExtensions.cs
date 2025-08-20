// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// Extension methods for WaitHandle
/// </summary>
internal static class WaitHandleExtensions
{
    /// <summary>
    /// Asynchronously waits for a WaitHandle to be signaled
    /// </summary>
    /// <param name="waitHandle">The wait handle</param>
    /// <param name="millisecondsTimeout">The timeout in milliseconds</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A task that completes when the wait handle is signaled or timeout occurs</returns>
    public static Task<bool> WaitOneAsync(this WaitHandle waitHandle, int millisecondsTimeout, CancellationToken cancellationToken = default)
    {
        if (waitHandle == null)
            throw new ArgumentNullException(nameof(waitHandle));

        var tcs = new TaskCompletionSource<bool>();
        var rwh = ThreadPool.RegisterWaitForSingleObject(waitHandle,
            (state, timedOut) => ((TaskCompletionSource<bool>)state!).TrySetResult(!timedOut),
            tcs,
            millisecondsTimeout,
            executeOnlyOnce: true);

        var task = tcs.Task;
        
        task.ContinueWith((antecedent, state) => ((RegisteredWaitHandle)state!).Unregister(null), rwh, TaskScheduler.Default);
        
        if (cancellationToken.CanBeCanceled)
        {
            cancellationToken.Register(() => tcs.TrySetCanceled());
        }
        
        return task;
    }
}
