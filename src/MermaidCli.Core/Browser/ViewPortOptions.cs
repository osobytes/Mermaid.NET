namespace MermaidCli.Browser
{
    public record ViewPortOptions
    {
        public static ViewPortOptions Default => new() { Width = 800, Height = 600 };
        public int Width { get; set; }
        public int Height { get; set; }
        public bool IsMobile { get; set; }
        public double DeviceScaleFactor { get; set; } = 1;
        public bool IsLandscape { get; set; }
        public bool HasTouch { get; set; }
    }
}
