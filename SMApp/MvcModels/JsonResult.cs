namespace SMApp
{
    public class JsonResult : ActionResult
    {
        public override string Name { get; set; } = "JsonResult";
        public object Data { get; set; }
        public JsonResult(object data)
        {
            Data = data;
        }
        public JsonResult()
        {

        }
    }
}
