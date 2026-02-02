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
 
using System.Text.Json.Serialization;
using MermaidCli.Browser.Helpers.Json;

namespace MermaidCli.Browser
{
    public enum PaperFormat { Letter, Legal, Tabloid, Ledger, A0, A1, A2, A3, A4, A5, A6 }

    public record MarginOptions
    {
        public string Top { get; set; } = string.Empty;
        public string Left { get; set; } = string.Empty;
        public string Bottom { get; set; } = string.Empty;
        public string Right { get; set; } = string.Empty;
    }

    public record PdfOptions
    {
        public decimal Scale { get; set; } = 1;
        public bool DisplayHeaderFooter { get; set; }
        public string HeaderTemplate { get; set; } = string.Empty;
        public string FooterTemplate { get; set; } = string.Empty;
        public bool PrintBackground { get; set; }
        public bool Landscape { get; set; }
        public string PageRanges { get; set; } = string.Empty;
        public PaperFormat Format { get; set; }
        /// <summary>
        /// Paper width, accepts values labeled with units.
        /// </summary>
        [JsonConverter(typeof(PrimitiveTypeConverter))]
        public object Width { get; set; } = null!;

        /// <summary>
        /// Paper height, accepts values labeled with units.
        /// </summary>
        [JsonConverter(typeof(PrimitiveTypeConverter))]
        public object Height { get; set; } = null!;

        public MarginOptions MarginOptions { get; set; } = new();
        public bool PreferCSSPageSize { get; set; }
        public bool OmitBackground { get; set; }
    }
}
