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
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MermaidCli.Browser.BrowserData;
using MermaidCli.Browser.Cdp;

namespace MermaidCli.Browser
{
    /// <summary>
    /// Launcher controls the creation of processes or the connection remote ones.
    /// </summary>
    public class Launcher
    {
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="Launcher"/> class.
        /// </summary>
        /// <param name="loggerFactory">Logger factory.</param>
        public Launcher(ILoggerFactory loggerFactory = null) => _loggerFactory = loggerFactory ?? new LoggerFactory();

        /// <summary>
        /// The method launches a browser instance with given arguments. The browser will be closed when the Browser is disposed.
        /// </summary>
        /// <param name="options">Options for launching the browser.</param>
        /// <returns>A connected browser.</returns>
        public async Task<IBrowser> LaunchAsync(LaunchOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var executable = options.ExecutablePath;
            if (executable == null)
            {
                var buildId = Chrome.DefaultBuildId;
                // Simple logic: default to Chrome
                var browserFetcher = new BrowserFetcher(SupportedBrowser.Chrome);
                // We assume it's downloaded. The original logic checks if it's downloaded or resolves path.
                // Here we get the executable path using the fetcher logic
                executable = browserFetcher.GetExecutablePath(buildId);
            }

            var process = new ChromeLauncher(executable, options);

            try
            {
                await process.StartAsync().ConfigureAwait(false);

                var connection = await Connection.Create(process.EndPoint, options, _loggerFactory);
                return CdpBrowser.Create(connection, process);
            }
            catch
            {
                await process.KillAsync().ConfigureAwait(false);
                throw;
            }
        }
    }
}
