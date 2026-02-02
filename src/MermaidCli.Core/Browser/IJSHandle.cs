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
using System.Threading.Tasks;
using System.Text.Json;
using MermaidCli.Browser.Cdp.Messaging;

namespace MermaidCli.Browser
{
    /// <summary>
    /// IJSHandle represents an in-page JavaScript object.
    /// </summary>
    public interface IJSHandle : IAsyncDisposable
    {
        /// <summary>
        /// Gets a value indicating whether this <see cref="IJSHandle"/> is disposed.
        /// </summary>
        /// <value><c>true</c> if disposed; otherwise, <c>false</c>.</value>
        bool Disposed { get; }

        /// <summary>
        /// Gets the remote object.
        /// </summary>
        /// <value>The remote object.</value>
        RemoteObject RemoteObject { get; }

        // Simplified IJSHandle, commented out methods that require Page/Frame context for now to avoid cascading dependencies
        // Can be uncommented when Page/Frame are ported
        
        /*
        Task<JsonElement?> EvaluateFunctionAsync(string script, params object[] args);
        Task<T> EvaluateFunctionAsync<T>(string script, params object[] args);
        Task<IJSHandle> EvaluateFunctionHandleAsync(string pageFunction, params object[] args);
        Task<Dictionary<string, IJSHandle>> GetPropertiesAsync();
        Task<IJSHandle> GetPropertyAsync(string propertyName);
        */

        /// <summary>
        /// Returns a JSON representation of the object.
        /// </summary>
        /// <returns>Task.</returns>
        Task<object> JsonValueAsync();

        /// <summary>
        /// Returns a JSON representation of the object.
        /// </summary>
        /// <typeparam name="T">A strongly typed object to parse to.</typeparam>
        /// <returns>Task.</returns>
        Task<T> JsonValueAsync<T>();
    }
}
