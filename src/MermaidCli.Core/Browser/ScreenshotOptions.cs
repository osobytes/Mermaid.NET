namespace MermaidCli.Browser
{
    public enum ScreenshotType { Png, Jpeg, Webp }

    public class ScreenshotOptions
    {
        public bool FullPage { get; set; }
        public bool OmitBackground { get; set; }
        public ScreenshotType? Type { get; set; }
        public int? Quality { get; set; }
        public bool CaptureBeyondViewport { get; set; } = true;
        public bool? FromSurface { get; set; }
    }
}
