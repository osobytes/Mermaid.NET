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

using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using MermaidCli.Browser.BrowserData;

namespace MermaidCli.Browser
{
    /// <summary>
    /// BrowserFetcher can download and manage different versions of Chromium.
    /// </summary>
    public interface IBrowserFetcher
    {
        /// <summary>
        /// A download host to be used.
        /// </summary>
        string BaseUrl { get; set; }

        /// <summary>
        /// Determines the path to download browsers to.
        /// </summary>
        string CacheDir { get; set; }

        /// <summary>
        /// Gets the platform.
        /// </summary>
        Platform Platform { get; set; }

        /// <summary>
        /// Gets the browser.
        /// </summary>
        SupportedBrowser Browser { get; set; }

        /// <summary>
        /// Proxy used by the HttpClient in <see cref="DownloadAsync()"/>, <see cref="DownloadAsync(string)"/> and <see cref="CanDownloadAsync"/>.
        /// </summary>
        IWebProxy WebProxy { get; set; }

        /// <summary>
        /// The method initiates a HEAD request to check if the revision is available.
        /// </summary>
        /// <returns>Whether the version is available or not.</returns>
        /// <param name="buildId">A build to check availability.</param>
        Task<bool> CanDownloadAsync(string buildId);

        /// <summary>
        /// Downloads the revision.
        /// </summary>
        /// <returns>Task which resolves to the completed download.</returns>
        Task<InstalledBrowser> DownloadAsync();

        /// <summary>
        /// Downloads the revision.
        /// </summary>
        /// <param name="tag">Browser tag.</param>
        /// <returns>Task which resolves to the completed download.</returns>
        Task<InstalledBrowser> DownloadAsync(BrowserTag tag);

        /// <summary>
        /// Downloads the revision.
        /// </summary>
        /// <returns>Task which resolves to the completed download.</returns>
        /// <param name="revision">Revision.</param>
        Task<InstalledBrowser> DownloadAsync(string revision);

        /// <summary>
        /// A list of all browsers available locally on disk.
        /// </summary>
        /// <returns>The available browsers.</returns>
        IEnumerable<InstalledBrowser> GetInstalledBrowsers();

        /// <summary>
        /// Removes a downloaded browser.
        /// </summary>
        /// <param name="buildId">Browser to remove.</param>
        void Uninstall(string buildId);

        /// <summary>
        /// Gets the executable path.
        /// </summary>
        /// <param name="buildId">Browser buildId.</param>
        /// <returns>The executable path.</returns>
        string GetExecutablePath(string buildId);
    }
}
