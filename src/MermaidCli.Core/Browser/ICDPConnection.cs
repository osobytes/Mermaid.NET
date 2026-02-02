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
using System.Text.Json;
using System.Threading.Tasks;
using MermaidCli.Browser.Cdp;

namespace MermaidCli.Browser
{
    /// <summary>
    /// An ICDPConnection is an object able to send and receive messages from the browser.
    /// </summary>
    public interface ICDPConnection
    {
        /// <summary>
        /// Occurs when message received from Chromium.
        /// </summary>
        event EventHandler<MessageEventArgs> MessageReceived;

        /// <summary>
        /// Protocol methods can be called with this method.
        /// </summary>
        /// <param name="method">The method name.</param>
        /// <param name="args">The method args.</param>
        /// <param name="options">The options.</param>
        /// <typeparam name="T">Return type.</typeparam>
        /// <returns>The task.</returns>
        Task<T> SendAsync<T>(string method, object args = null, CommandOptions options = null);

        /// <summary>
        /// Protocol methods can be called with this method.
        /// </summary>
        /// <param name="method">The method name.</param>
        /// <param name="args">The method args.</param>
        /// <param name="waitForCallback">
        /// If <c>true</c> the method will return a task to be completed when the message is confirmed by Chromium.
        /// If <c>false</c> the task will be considered complete after sending the message to Chromium.
        /// </param>
        /// <param name="options">The options.</param>
        /// <returns>The task.</returns>
        /// <exception cref="MermaidCli.Browser.PuppeteerException">If the <see cref="Connection"/> is closed.</exception>
        Task<JsonElement?> SendAsync(string method, object args = null, bool waitForCallback = true, CommandOptions options = null);
    }
}
