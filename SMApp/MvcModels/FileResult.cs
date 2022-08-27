namespace SMApp
{
    public class FileResult : ActionResult
    {
        public override string Name { get; set; } = "FileResult";
        public byte[] Data { get; set; }
        public string ContentType { get; set; }
    }
}
