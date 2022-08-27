namespace SMApp
{
    public class ContentResult : ActionResult
    {

        public override string Name { get; set; } = "ContentResult";
        public string Data { get; set; }
        public ContentResult(string data)
        {
            Data = data;
        }
        public ContentResult()
        {

        }
    }
}
