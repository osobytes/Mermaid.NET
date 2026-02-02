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
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MermaidCli.Browser.Cdp;
using MermaidCli.Browser.Helpers.Json;

namespace MermaidCli.Browser
{
    /// <inheritdoc/>
    public abstract class CDPSession : ICDPSession
    {
        /// <inheritdoc/>
        public event EventHandler<MessageEventArgs> MessageReceived;

        /// <inheritdoc/>
        public event EventHandler Disconnected;

        /// <inheritdoc/>
        public event EventHandler<SessionEventArgs> SessionAttached;

        /// <inheritdoc/>
        public event EventHandler<SessionEventArgs> SessionDetached;

        internal event EventHandler<SessionEventArgs> Ready;

        internal event EventHandler<SessionEventArgs> Swapped;

        /// <inheritdoc/>
        public string Id { get; init; }

        /// <summary>
        /// Gets the target identifier.
        /// </summary>
        public string TargetId { get; internal set; }

        /// <inheritdoc/>
        public string CloseReason { get; protected set; }

        /// <inheritdoc/>
        public ILoggerFactory LoggerFactory => Connection.LoggerFactory;

        internal Connection Connection { get; set; }

        // internal Target Target { get; set; } // Commented out Target for now as we didn't port Target.cs yet (circular/complex dependency)

        internal abstract CDPSession ParentSession { get; }

        /// <inheritdoc/>
        public async Task<T> SendAsync<T>(string method, object args = null, CommandOptions options = null)
        {
            var content = await SendAsync(method, args, true, options).ConfigureAwait(false);
            Debug.Assert(content != null, nameof(content) + " != null");
            return content.Value.ToObject<T>();
        }

        /// <inheritdoc/>
        public abstract Task<JsonElement?> SendAsync(string method, object args = null, bool waitForCallback = true, CommandOptions options = null);

        /// <inheritdoc/>
        public abstract Task DetachAsync();

        internal void OnSessionReady(CDPSession session) => Ready?.Invoke(this, new SessionEventArgs(session));

        internal abstract void Close(string closeReason);

        internal void OnSessionAttached(CDPSession session)
            => SessionAttached?.Invoke(this, new SessionEventArgs(session));

        internal void OnSessionDetached(CDPSession session)
            => SessionDetached?.Invoke(this, new SessionEventArgs(session));

        internal void OnSwapped(CDPSession session) => Swapped?.Invoke(this, new SessionEventArgs(session));

        /// <summary>
        /// Emits <see cref="MessageReceived"/> event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected void OnMessageReceived(MessageEventArgs e) => MessageReceived?.Invoke(this, e);

        /// <summary>
        /// Emits <see cref="Disconnected"/> event.
        /// </summary>
        protected void OnDisconnected() => Disconnected?.Invoke(this, EventArgs.Empty);
    }
}
