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

using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using MermaidCli.Browser.Helpers.Json;

namespace MermaidCli.Browser
{
    /// <summary>
    /// Target type.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumMemberConverter<TargetType>))]
    [DefaultEnumValue((int)Other)]
    public enum TargetType
    {
        /// <summary>
        /// The other.
        /// </summary>
        [EnumMember(Value = "other")]
        Other,

        /// <summary>
        /// Target type page.
        /// </summary>
        [EnumMember(Value = "page")]
        Page,

        /// <summary>
        /// Target type service worker.
        /// </summary>
        [EnumMember(Value = "service_worker")]
        ServiceWorker,

        /// <summary>
        /// Target type browser.
        /// </summary>
        [EnumMember(Value = "browser")]
        Browser,

        /// <summary>
        /// Target type background page.
        /// </summary>
        [EnumMember(Value = "background_page")]
        BackgroundPage,

        /// <summary>
        /// Target type worker.
        /// </summary>
        [EnumMember(Value = "worker")]
        Worker,

        /// <summary>
        /// Target type javascript.
        /// </summary>
        [EnumMember(Value = "javascript")]
        Javascript,

        /// <summary>
        /// Target type network.
        /// </summary>
        [EnumMember(Value = "network")]
        Network,

        /// <summary>
        /// Target type deprecation.
        /// </summary>
        [EnumMember(Value = "deprecation")]
        Deprecation,

        /// <summary>
        /// Target type security.
        /// </summary>
        [EnumMember(Value = "security")]
        Security,

        /// <summary>
        /// Target type recommendation.
        /// </summary>
        [EnumMember(Value = "recommendation")]
        Recommendation,

        /// <summary>
        /// Target type shared worker.
        /// </summary>
        [EnumMember(Value = "shared_worker")]
        SharedWorker,

        /// <summary>
        /// Target type iFrame.
        /// </summary>
        [EnumMember(Value = "iframe")]
        IFrame,

        /// <summary>
        /// Target type rendering.
        /// </summary>
        [EnumMember(Value = "rendering")]
        Rendering,

        /// <summary>
        /// Webview.
        /// </summary>
        [EnumMember(Value = "webview")]
        Webview,

        /// <summary>
        /// Target type tab.
        /// </summary>
        [EnumMember(Value = "tab")]
        Tab,
    }
}
