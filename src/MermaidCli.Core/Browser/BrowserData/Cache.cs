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
using System.IO;
using System.Linq;
using MermaidCli.Browser.Helpers;

namespace MermaidCli.Browser.BrowserData
{
    internal class Cache
    {
        private readonly string _rootDir;

        public Cache() => _rootDir = BrowserFetcher.GetBrowsersLocation();

        public Cache(string rootDir) => _rootDir = rootDir;

        public string GetBrowserRoot(SupportedBrowser browser) => Path.Combine(_rootDir, browser.ToString());

        public string GetInstallationDir(SupportedBrowser browser, Platform platform, string buildId)
            => Path.Combine(GetBrowserRoot(browser), $"{platform}-{buildId}");

        public IEnumerable<InstalledBrowser> GetInstalledBrowsers()
        {
            var rootInfo = new DirectoryInfo(_rootDir);

            if (!rootInfo.Exists)
            {
                return Array.Empty<InstalledBrowser>();
            }

            var browserNames = EnumHelper.GetNames<SupportedBrowser>().Select(browser => browser.ToUpperInvariant());
            var browsers = rootInfo.GetDirectories().Where(browser => browserNames.Contains(browser.Name.ToUpperInvariant()));

            return browsers.SelectMany(browser =>
            {
                var browserEnum = EnumHelper.Parse<SupportedBrowser>(browser.Name, ignoreCase: true);
                var dirInfo = new DirectoryInfo(GetBrowserRoot(browserEnum));
                var dirs = dirInfo.GetDirectories();

                return dirs.Select(dir =>
                {
                    var result = ParseFolderPath(dir);

                    if (result == null)
                    {
                        return null;
                    }

                    var platformEnum = EnumHelper.Parse<Platform>(result.Value.Platform, ignoreCase: true);
                    return new InstalledBrowser(this, browserEnum, result.Value.BuildId, platformEnum);
                })
                .Where(item => item != null);
            });
        }

        public void Uninstall(SupportedBrowser browser, Platform platform, string buildId)
        {
            var dir = new DirectoryInfo(GetInstallationDir(browser, platform, buildId));
            if (dir.Exists)
            {
                dir.Delete(true);
            }
        }

        public void Clear() => new DirectoryInfo(_rootDir).Delete(true);

        private (string Platform, string BuildId)? ParseFolderPath(DirectoryInfo directory)
        {
            var name = directory.Name;
            var splits = name.Split('-');

            if (splits.Length != 2)
            {
                return null;
            }

            return (splits[0], splits[1]);
        }
    }
}
