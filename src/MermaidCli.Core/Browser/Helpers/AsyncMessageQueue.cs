/*
 * MIT License
 *
 * Copyright (c) 2017 Darío Kondratiuk
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MermaidCli.Browser.Cdp.Messaging;

namespace MermaidCli.Browser.Helpers
{
    /// <summary>
    /// Provides an async queue for responses for <see cref="CDPSession.SendAsync"/>, so that responses can be handled
    /// async without risk callers causing a deadlock.
    /// </summary>
    internal sealed class AsyncMessageQueue : IDisposable
    {
        private readonly List<MessageTask> _pendingTasks = new();
        private readonly bool _enqueueAsyncMessages;
        private readonly ILogger _logger;
        private bool _disposed;

        public AsyncMessageQueue(bool enqueueAsyncMessages, ILogger? logger = null)
        {
            _enqueueAsyncMessages = enqueueAsyncMessages;
            _logger = logger ?? NullLogger.Instance;
        }

        public void Enqueue(MessageTask callback, ConnectionResponse obj)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            if (!_enqueueAsyncMessages)
            {
                HandleAsyncMessage(callback, obj);
                return;
            }

            // Keep a ref to this task until it completes. If it can't finish by the time we dispose this queue,
            // then we'll find it and cancel it.
            lock (_pendingTasks)
            {
                _pendingTasks.Add(callback);
            }

            var task = Task.Run(() => HandleAsyncMessage(callback, obj));

            // Unhandled error handler
            task.ContinueWith(
                t =>
                {
                    _logger.LogError(t.Exception, "Failed to complete async handling of SendAsync for {Method}", callback.Method);
                    callback.TaskWrapper.TrySetException(t.Exception!); // t.Exception is available since this runs only on faulted
                },
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);

            // Always remove from the queue when done, regardless of outcome.
            task.ContinueWith(
                _ =>
                {
                    lock (_pendingTasks)
                    {
                        _pendingTasks.Remove(callback);
                    }
                },
                TaskScheduler.Default);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            // Ensure all tasks are finished since we're disposing now. Any pending tasks will be canceled.
            MessageTask[] pendingTasks;
            lock (_pendingTasks)
            {
                pendingTasks = _pendingTasks.ToArray();
                _pendingTasks.Clear();
            }

            foreach (var pendingTask in pendingTasks)
            {
                pendingTask.TaskWrapper.TrySetCanceled();
            }

            _disposed = true;
        }

        private static void HandleAsyncMessage(MessageTask callback, ConnectionResponse obj)
        {
            if (obj.Error != null)
            {
                callback.TaskWrapper.TrySetException(new MessageException(callback, obj.Error));
            }
            else
            {
                callback.TaskWrapper.TrySetResult(obj.Result);
            }
        }
    }
}
