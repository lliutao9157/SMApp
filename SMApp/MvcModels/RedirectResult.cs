namespace SMApp.MvcModels
{
    public class RedirectResult : ActionResult
    {
        public override string Name { get; set; } = "RedirectResult";
        public string Url { get; set; }
        public RedirectResult(string url)
        {
            Url = url;
        }
    }
}
