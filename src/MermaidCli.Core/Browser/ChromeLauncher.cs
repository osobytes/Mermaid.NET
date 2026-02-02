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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MermaidCli.Browser.Helpers;

namespace MermaidCli.Browser
{
    /// <summary>
    /// Represents a Chromium process and any associated temporary user data directory that have created
    /// by Puppeteer and therefore must be cleaned up when no longer needed.
    /// </summary>
    public class ChromeLauncher : LauncherBase
    {
        private const string UserDataDirArgument = "--user-data-dir";

        /// <summary>
        /// Initializes a new instance of the <see cref="ChromeLauncher"/> class.
        /// </summary>
        /// <param name="executable">Full path of executable.</param>
        /// <param name="options">Options for launching Chromium.</param>
        public ChromeLauncher(string executable, LaunchOptions options)
            : base(executable, options)
        {
            (var chromiumArgs, TempUserDataDir) = PrepareChromiumArgs(options);

            Process.StartInfo.Arguments = string.Join(" ", chromiumArgs);
        }

        public override Task StartAsync()
        {
            Process.Start();
            Process.BeginErrorReadLine();
            return WaitForEndpoint();
        }

        private async Task WaitForEndpoint()
        {
            var tcs = new TaskCompletionSource<string>();
            DataReceivedEventHandler handler = null;
            handler = (sender, e) =>
            {
                if (e.Data != null)
                {
                    var match = Regex.Match(e.Data, @"DevTools listening on (ws:\/\/.*)");
                    if (match.Success)
                    {
                        EndPoint = match.Groups[1].Value;
                        tcs.TrySetResult(EndPoint);
                        Process.ErrorDataReceived -= handler;
                    }
                }
            };

            Process.ErrorDataReceived += handler;
            
            // Simple timeout for startup
            await tcs.Task.WithTimeout(30_000); 
        }

        internal static string[] GetDefaultArgs(LaunchOptions options)
        {
            var args = options.Args ?? [];
            
            // Simplified default args for MermaidCli use case
            var chromiumArguments = new List<string>(
            [
                "--allow-pre-commit-input",
                "--disable-background-networking",
                "--disable-background-timer-throttling",
                "--disable-backgrounding-occluded-windows",
                "--disable-breakpad",
                "--disable-client-side-phishing-detection",
                "--disable-component-extensions-with-background-pages",
                "--disable-component-update",
                "--disable-default-apps",
                "--disable-dev-shm-usage",
                "--disable-extensions",
                "--disable-field-trial-config",
                "--disable-hang-monitor",
                "--disable-infobars",
                "--disable-ipc-flooding-protection",
                "--disable-popup-blocking",
                "--disable-prompt-on-repost",
                "--disable-renderer-backgrounding",
                "--disable-search-engine-choice-screen",
                "--disable-sync",
                "--enable-automation",
                "--enable-blink-features=IdleDetection",
                "--export-tagged-pdf",
                "--generate-pdf-document-outline",
                "--force-color-profile=srgb",
                "--metrics-recording-only",
                "--no-first-run",
                "--password-store=basic",
                "--use-mock-keychain",
            ]);

            if (!string.IsNullOrEmpty(options.UserDataDir))
            {
                chromiumArguments.Add($"{UserDataDirArgument}={options.UserDataDir.Quote()}");
            }

            if (options.Devtools)
            {
                chromiumArguments.Add("--auto-open-devtools-for-tabs");
            }

            if (options.Headless)
            {
                chromiumArguments.AddRange(new[]
                {
                    "--headless=new", // Default to new headless
                    "--hide-scrollbars",
                    "--mute-audio",
                });
            }

            if (args.All(arg => arg.StartsWith("-", StringComparison.Ordinal)))
            {
                chromiumArguments.Add("about:blank");
            }

            chromiumArguments.AddRange(args);
            return chromiumArguments.ToArray();
        }

        private static (List<string> ChromiumArgs, TempDirectory TempUserDataDirectory) PrepareChromiumArgs(LaunchOptions options)
        {
            var chromiumArgs = new List<string>();

            if (!options.IgnoreDefaultArgs)
            {
                chromiumArgs.AddRange(GetDefaultArgs(options));
            }
            else if (options.IgnoredDefaultArgs?.Length > 0)
            {
                // Simplified: ignoring sophisticated array diffing for now
                chromiumArgs.AddRange(options.Args);
            }
            else
            {
                chromiumArgs.AddRange(options.Args);
            }

            TempDirectory tempUserDataDirectory = null;

            if (!chromiumArgs.Any(argument => argument.StartsWith("--remote-debugging-", StringComparison.Ordinal)))
            {
                chromiumArgs.Add("--remote-debugging-port=0");
            }

            var userDataDirOption = chromiumArgs.FirstOrDefault(i => i.StartsWith(UserDataDirArgument, StringComparison.Ordinal));
            if (string.IsNullOrEmpty(userDataDirOption))
            {
                tempUserDataDirectory = new TempDirectory();
                chromiumArgs.Add($"{UserDataDirArgument}={tempUserDataDirectory.Path.Quote()}");
            }

            return (chromiumArgs, tempUserDataDirectory);
        }
    }
}
