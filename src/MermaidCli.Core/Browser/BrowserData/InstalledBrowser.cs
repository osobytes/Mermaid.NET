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

namespace MermaidCli.Browser.BrowserData
{
    /// <summary>
    /// Installed browser info.
    /// </summary>
    public class InstalledBrowser
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InstalledBrowser"/> class.
        /// </summary>
        /// <param name="cache">Cache.</param>
        /// <param name="browser">Browser.</param>
        /// <param name="buildId">BuildId.</param>
        /// <param name="platform">Platform.</param>
        internal InstalledBrowser(Cache cache, SupportedBrowser browser, string buildId, Platform platform)
        {
            Cache = cache;
            Browser = browser;
            BuildId = buildId;
            Platform = platform;
        }

        /// <summary>
        /// Browser.
        /// </summary>
        public SupportedBrowser Browser { get; set; }

        /// <summary>
        /// Gets or sets the buildID.
        /// </summary>
        public string BuildId { get; set; }

        /// <summary>
        /// Revision platform.
        /// </summary>
        public Platform Platform { get; set; }

        /// <summary>
        /// Whether the permissions have been fixed in the browser.
        /// If Puppeteer executed the command to fix the permissions, this will be true.
        /// If Puppeteer failed to fix the permissions, this will be false.
        /// If the platform does not require permissions to be fixed, this will be null.
        /// </summary>
        public bool? PermissionsFixed { get; internal set; }

        /// <summary>
        /// Revision platform.
        /// </summary>
        internal Cache Cache { get; set; }

        /// <summary>
        /// Get executable path.
        /// </summary>
        /// <returns>executable path.</returns>
        /// <exception cref="ArgumentException">For not supported <see cref="Platform"/>.</exception>
        public string GetExecutablePath()
        {
            var installationDir = Cache.GetInstallationDir(Browser, Platform, BuildId);
            return Path.Combine(
                installationDir,
                GetExecutablePath(Browser, Platform, BuildId));
        }

        private static string GetExecutablePath(SupportedBrowser browser, Platform platform, string buildId) => browser switch
        {
            SupportedBrowser.Chrome => Chrome.RelativeExecutablePath(platform, buildId),
            // SupportedBrowser.ChromeHeadlessShell => ChromeHeadlessShell.RelativeExecutablePath(platform, buildId),
            // SupportedBrowser.Chromium => Chromium.RelativeExecutablePath(platform, buildId),
            // SupportedBrowser.Firefox => Firefox.RelativeExecutablePath(platform, buildId),
            _ => throw new NotSupportedException(),
        };
    }
}
