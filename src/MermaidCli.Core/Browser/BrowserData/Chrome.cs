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
    /// Chrome info.
    /// </summary>
    public static class Chrome
    {
        /// <summary>
        /// Default chrome build.
        /// </summary>
        public static string DefaultBuildId => "138.0.7204.101";

        internal static string ResolveDownloadUrl(Platform platform, string buildId, string baseUrl)
            => $"{baseUrl ?? "https://storage.googleapis.com/chrome-for-testing-public"}/{string.Join("/", ResolveDownloadPath(platform, buildId))}";

        internal static string RelativeExecutablePath(Platform platform, string builId)
            => platform switch
            {
                Platform.MacOS or Platform.MacOSArm64 => Path.Combine(
                    "chrome-" + GetFolder(platform),
                    "Google Chrome for Testing.app",
                    "Contents",
                    "MacOS",
                    "Google Chrome for Testing"),
                Platform.Linux => Path.Combine("chrome-linux64", "chrome"),
                Platform.Win32 or Platform.Win64 => Path.Combine("chrome-" + GetFolder(platform), "chrome.exe"),
                _ => throw new ArgumentException("Invalid platform", nameof(platform)),
            };

        private static string[] ResolveDownloadPath(Platform platform, string buildId)
            => new string[]
            {
                buildId,
                GetFolder(platform),
                $"chrome-{GetFolder(platform)}.zip"
            };

        private static string GetFolder(Platform platform)
            => platform switch
            {
                Platform.Linux => "linux64",
                Platform.MacOSArm64 => "mac-arm64",
                Platform.MacOS => "mac-x64",
                Platform.Win32 => "win32",
                Platform.Win64 => "win64",
                _ => throw new PuppeteerException($"Unknown platform: {platform}"),
            };
    }
}
