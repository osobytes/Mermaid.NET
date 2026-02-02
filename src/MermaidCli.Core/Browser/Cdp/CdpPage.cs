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
using System.Threading.Tasks;
using System.Text.Json;
using MermaidCli.Browser.Cdp.Messaging;
using MermaidCli.Browser.Helpers.Json;

namespace MermaidCli.Browser.Cdp
{
    internal class CdpPage : Page
    {
        private readonly CdpCDPSession _session;
        private readonly Connection _connection;

        public CdpPage(CdpCDPSession session, Connection connection)
        {
            _session = session;
            _connection = connection;
            
            _session.MessageReceived += (sender, e) => {
                // Simplified event routing
                if (e.MessageID == "Runtime.consoleAPICalled") NotifyConsole();
                if (e.MessageID == "Runtime.exceptionThrown") NotifyPageError();
            };
        }

        public override async Task GoToAsync(string url, NavigationOptions options = null)
        {
            await _session.SendAsync("Page.navigate", new PageNavigateRequest { Url = url }).ConfigureAwait(false);
        }

        public override Task SetContentAsync(string html, NavigationOptions options = null)
        {
            return EvaluateFunctionAsync(@"html => {
                document.open();
                document.write(html);
                document.close();
            }", html);
        }

        public override async Task<T> EvaluateFunctionAsync<T>(string script, params object[] args)
        {
            // Even more simplified: assume script is expression or wrap it
            var expression = args is { Length: > 0 } 
                ? $"({script})({JsonSerializer.Serialize(args[0])})" // Very limited support for 1 arg for now
                : $"({script})()";

            var response = await _session.SendAsync("Runtime.evaluate", new RuntimeEvaluateRequest
            {
                Expression = expression,
                ReturnByValue = true,
                AwaitPromise = true
            }).ConfigureAwait(false);

            if (response != null && response.Value.TryGetProperty("result", out var result))
            {
                if (result.TryGetProperty("value", out var value))
                {
                    return value.ToObject<T>();
                }
            }

            return default;
        }

        public override async Task EvaluateFunctionAsync(string script, params object[] args)
        {
             await EvaluateFunctionAsync<JsonElement>(script, args).ConfigureAwait(false);
        }

        public override async Task<byte[]> PdfDataAsync(PdfOptions options = null)
        {
            var response = await _session.SendAsync("Page.printToPDF", options ?? new PdfOptions()).ConfigureAwait(false);
            if (response != null && response.Value.TryGetProperty("data", out var data))
            {
                return Convert.FromBase64String(data.GetString());
            }
            return Array.Empty<byte>();
        }

        public override async Task<byte[]> ScreenshotDataAsync(ScreenshotOptions options = null)
        {
            var response = await _session.SendAsync("Page.captureScreenshot", options ?? new ScreenshotOptions()).ConfigureAwait(false);
             if (response != null && response.Value.TryGetProperty("data", out var data))
            {
                return Convert.FromBase64String(data.GetString());
            }
            return Array.Empty<byte>();
        }

        public override Task SetViewportAsync(ViewPortOptions viewport)
        {
            return _session.SendAsync("Emulation.setDeviceMetricsOverride", new
            {
                width = viewport.Width,
                height = viewport.Height,
                deviceScaleFactor = viewport.DeviceScaleFactor,
                mobile = viewport.IsMobile
            });
        }

        public override Task CloseAsync()
        {
            return _connection.SendAsync("Target.closeTarget", new { targetId = _session.TargetId });
        }

        public override async Task<IElementHandle> AddScriptTagAsync(AddTagOptions options)
        {
            await EvaluateFunctionAsync(@"(url) => {
                const script = document.createElement('script');
                script.src = url;
                return new Promise((resolve, reject) => {
                    script.onload = resolve;
                    script.onerror = reject;
                    document.head.appendChild(script);
                });
            }", options.Url);
            return null; // Simplified
        }

        public override async Task<IElementHandle> AddStyleTagAsync(AddTagOptions options)
        {
             await EvaluateFunctionAsync(@"(url) => {
                const link = document.createElement('link');
                link.rel = 'stylesheet';
                link.href = url;
                document.head.appendChild(link);
            }", options.Url);
            return null; // Simplified
        }
    }
}