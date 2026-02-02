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

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MermaidCli.Browser
{
    /// <summary>
    /// Provides a method to launch a Chromium instance.
    /// </summary>
    public static class Puppeteer
    {
        /// <summary>
        /// The method launches a browser instance with given arguments.
        /// </summary>
        /// <param name="options">Options for launching Chrome.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <returns>A connected browser.</returns>
        public static Task<IBrowser> LaunchAsync(LaunchOptions options, ILoggerFactory loggerFactory = null)
            => new Launcher(loggerFactory).LaunchAsync(options);
            
        /// <summary>
        /// Creates the browser fetcher.
        /// </summary>
        /// <returns>The browser fetcher.</returns>
        public static IBrowserFetcher CreateBrowserFetcher() => new BrowserFetcher();

        /// <summary>
        /// Creates the browser fetcher.
        /// </summary>
        /// <param name="options">Options.</param>
        /// <returns>The browser fetcher.</returns>
        public static IBrowserFetcher CreateBrowserFetcher(BrowserFetcherOptions options) => new BrowserFetcher(options);
    }
}
