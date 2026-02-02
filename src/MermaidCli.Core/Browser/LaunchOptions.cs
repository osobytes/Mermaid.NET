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
using System.Net.WebSockets;
using MermaidCli.Browser.BrowserData;
using MermaidCli.Browser.Cdp;
using MermaidCli.Browser.Transport;

namespace MermaidCli.Browser
{
    /// <summary>
    /// Options for launching the Chrome/ium browser.
    /// </summary>
    public class LaunchOptions : IConnectionOptions
    {
        private string[] _ignoredDefaultArgs;
        private bool _devtools;

        /// <summary>
        /// Whether to ignore HTTPS errors during navigation. Defaults to false.
        /// </summary>
        public bool AcceptInsecureCerts { get; set; }

        /// <summary>
        /// Whether to run browser in headless mode. Defaults to true unless the devtools option is true.
        /// </summary>
        public bool Headless { get; set; } = true;

        /// <summary>
        /// Path to a Chromium or Chrome executable to run instead of bundled Chromium. If executablePath is a relative path, then it is resolved relative to current working directory.
        /// </summary>
        public string ExecutablePath { get; set; }

        /// <summary>
        /// Slows down Puppeteer operations by the specified amount of milliseconds. Useful so that you can see what is going on.
        /// </summary>
        public int SlowMo { get; set; }

        /// <summary>
        /// Additional arguments to pass to the browser instance. List of Chromium flags can be found <a href="http://peter.sh/experiments/chromium-command-line-switches/">here</a>.
        /// </summary>
        public string[] Args { get; set; } = [];

        /// <summary>
        /// Maximum time in milliseconds to wait for the browser instance to start. Defaults to 30000 (30 seconds). Pass 0 to disable timeout.
        /// </summary>
        public int Timeout { get; set; } = 30_000;

        /// <summary>
        ///  Whether to pipe browser process stdout and stderr into process.stdout and process.stderr. Defaults to false.
        /// </summary>
        public bool DumpIO { get; set; }

        /// <summary>
        /// Path to a User Data Directory.
        /// </summary>
        public string UserDataDir { get; set; }

        /// <summary>
        /// Specify environment variables that will be visible to browser. Defaults to Environment variables.
        /// </summary>
        public IDictionary<string, string> Env { get; } = new Dictionary<string, string>();

        /// <summary>
        /// Whether to auto-open DevTools panel for each tab. If this option is true, the headless option will be set false.
        /// </summary>
        public bool Devtools
        {
            get => _devtools;
            set
            {
                _devtools = value;
                if (value)
                {
                    Headless = false;
                }
            }
        }

        /// <summary>
        /// If <c>true</c>, then do not use <see cref="ChromeLauncher.GetDefaultArgs"/>.
        /// Dangerous option; use with care. Defaults to <c>false</c>.
        /// </summary>
        public bool IgnoreDefaultArgs { get; set; }

        /// <summary>
        /// if <see cref="IgnoreDefaultArgs"/> is set to <c>false</c> this list will be used to filter <see cref="ChromeLauncher.GetDefaultArgs"/>.
        /// </summary>
        public string[] IgnoredDefaultArgs
        {
            get => _ignoredDefaultArgs;
            set
            {
                IgnoreDefaultArgs = true;
                _ignoredDefaultArgs = value;
            }
        }

        /// <summary>
        /// Optional factory for <see cref="WebSocket"/> implementations.
        /// If <see cref="Transport"/> is set this property will be ignored.
        /// </summary>
        public WebSocketFactory WebSocketFactory { get; set; }

        /// <summary>
        /// Optional factory for <see cref="IConnectionTransport"/> implementations.
        /// </summary>
        public TransportFactory TransportFactory { get; set; }

        /// <summary>
        /// Gets or sets the default Viewport.
        /// </summary>
        public ViewPortOptions DefaultViewport { get; set; } = ViewPortOptions.Default;

        /// <summary>
        /// If not <see cref="Transport"/> is set this will be use to determine is the default <see cref="WebSocketTransport"/> will enqueue messages.
        /// </summary>
        /// <remarks>
        /// It's set to <c>true</c> by default because it's the safest way to send commands to Chromium.
        /// Setting this to <c>false</c> proved to work in .NET Core but it tends to fail on .NET Framework.
        /// </remarks>
        public bool EnqueueTransportMessages { get; set; } = true;

        /// <summary>
        /// The browser to be used (Chrome, Chromium, Firefox).
        /// </summary>
        public SupportedBrowser Browser { get; set; } = SupportedBrowser.Chrome;

        /// <summary>
        /// Affects how responses to <see cref="CDPSession.SendAsync"/> are returned to the caller. If <c>true</c> (default), the
        /// response is delivered to the caller on its own thread; otherwise, the response is delivered the same way <see cref="CDPSession.MessageReceived"/>
        /// events are raised.
        /// </summary>
        /// <remarks>
        /// This should normally be set to <c>true</c> to support applications that aren't <c>async</c> "all the way up"; i.e., the application
        /// has legacy code that is not async which makes calls into PuppeteerSharp. If you experience issues, or your application is not mixed sync/async use, you
        /// can set this to <c>false</c> (default).
        /// </remarks>
        public bool EnqueueAsyncMessages { get; set; }

        // public Func<Target, bool> TargetFilter { get; set; }

        /// <inheritdoc />
        public int ProtocolTimeout { get; set; } = 180_000;

        // internal Func<Target, bool> IsPageTarget { get; set; }
    }
}
