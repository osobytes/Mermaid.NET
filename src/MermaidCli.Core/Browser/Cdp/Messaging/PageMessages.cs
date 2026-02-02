namespace MermaidCli.Browser.Cdp.Messaging
{
    internal class TargetCreateTargetRequest
    {
        public string Url { get; set; }
        public string BrowserContextId { get; set; }
    }

    internal class TargetCreateTargetResponse
    {
        public string TargetId { get; set; }
    }

    internal class PageNavigateRequest
    {
        public string Url { get; set; }
    }

    internal class RuntimeEvaluateRequest
    {
        public string Expression { get; set; }
        public bool ReturnByValue { get; set; }
        public bool AwaitPromise { get; set; }
    }
}
