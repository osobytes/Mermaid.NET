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
#nullable disable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace MermaidCli.Browser.Helpers
{
    internal class AsyncDictionaryHelper<TKey, TValue>
    {
        private readonly string _timeoutMessage;
        // private readonly MultiMap<TKey, TaskCompletionSource<TValue>> _pendingRequests = new(); // Simplifying: Assume standard Dictionary/ConcurrentDictionary for now or implement MultiMap if critical. 
        // MultiMap is used for multiple waiters on same key. I will implement a simple version of logic or need MultiMap.
        // Wait, MultiMap is likely another helper. I didn't check for it.
        // To be safe and minimal, I'll use a Dictionary<TKey, List<TaskCompletionSource<TValue>>> logic locally or just a ConcurrentDictionary of lists.
        
        private readonly ConcurrentDictionary<TKey, List<TaskCompletionSource<TValue>>> _pendingRequests = new();
        private readonly ConcurrentDictionary<TKey, TValue> _dictionary = new();

        public AsyncDictionaryHelper(string timeoutMessage)
        {
            _timeoutMessage = timeoutMessage;
        }

        internal ICollection<TValue> Values => _dictionary.Values;

        internal async Task<TValue> GetItemAsync(TKey key)
        {
            var tcs = new TaskCompletionSource<TValue>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pendingRequests.AddOrUpdate(key, 
                k => new List<TaskCompletionSource<TValue>> { tcs },
                (k, list) => { lock(list) { list.Add(tcs); } return list; });

            if (_dictionary.TryGetValue(key, out var item))
            {
                RemovePending(key, tcs);
                return item;
            }

            // WithTimeout needs an extension method. I'll need to implement that too or use standard WaitAsync.
            // For now, simple timeout logic.
            using var cts = new CancellationTokenSource(1000); // 1s timeout hardcoded in original
            try
            {
                var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(-1, cts.Token)).ConfigureAwait(false);
                if (completedTask == tcs.Task)
                {
                    return await tcs.Task.ConfigureAwait(false);
                }
                throw new TimeoutException();
            }
            catch (TimeoutException)
            {
                 throw new PuppeteerException(string.Format(CultureInfo.CurrentCulture, _timeoutMessage, key));
            }
            finally
            {
                 RemovePending(key, tcs);
            }
        }

        internal async Task<TValue> TryGetItemAsync(TKey key)
        {
             var tcs = new TaskCompletionSource<TValue>(TaskCreationOptions.RunContinuationsAsynchronously);
             _pendingRequests.AddOrUpdate(key, 
                k => new List<TaskCompletionSource<TValue>> { tcs },
                (k, list) => { lock(list) { list.Add(tcs); } return list; });

            if (_dictionary.TryGetValue(key, out var item))
            {
                RemovePending(key, tcs);
                return item;
            }

             using var cts = new CancellationTokenSource(1000); 
            try
            {
                var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(-1, cts.Token)).ConfigureAwait(false);
                if (completedTask == tcs.Task)
                {
                    return await tcs.Task.ConfigureAwait(false);
                }
                return default;
            }
            catch (TaskCanceledException)
            {
                 return default;
            }
            finally
            {
                 RemovePending(key, tcs);
            }
        }

        private void RemovePending(TKey key, TaskCompletionSource<TValue> tcs)
        {
            if (_pendingRequests.TryGetValue(key, out var list))
            {
                lock(list) { list.Remove(tcs); }
            }
        }

        internal void AddItem(TKey key, TValue value)
        {
            _dictionary[key] = value;
            if (_pendingRequests.TryGetValue(key, out var list))
            {
                lock(list)
                {
                    foreach (var tcs in list)
                    {
                        tcs.TrySetResult(value);
                    }
                    list.Clear();
                }
            }
        }

        internal bool TryRemove(TKey key, out TValue value)
        {
            var result = _dictionary.TryRemove(key, out value);
            _pendingRequests.TryRemove(key, out _);
            return result;
        }

        internal void Clear()
        {
            _dictionary.Clear();
            _pendingRequests.Clear();
        }

        internal TValue GetValueOrDefault(TKey key)
            => _dictionary.TryGetValue(key, out var val) ? val : default;

        internal bool TryGetValue(TKey key, out TValue value)
            => _dictionary.TryGetValue(key, out value);

        internal bool ContainsKey(TKey key)
            => _dictionary.ContainsKey(key);
    }
}
