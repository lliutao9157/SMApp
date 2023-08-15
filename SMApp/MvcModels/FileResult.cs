namespace SMApp.MvcModels
{
    public class FileResult : ActionResult
    {
        public override string Name { get; set; } = "FileResult";
        public byte[] Data { get; set; }
        public string ContentType { get; set; }
        public int? HttpCode { get; set; }
        public bool IsCompress { get; set; } = false;
    }
}
