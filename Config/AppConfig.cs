namespace EcommerceMiddleware.Config
{

    public class DefaultConfigs
    {
        public static readonly int STATUS_SUCCESS = 1;
        public static readonly int STATUS_ERROR = 2;
        public static readonly int STATUS_FAIL = 0;
        public static readonly string DEFAULT_DATE_FORMAT = "yyyy-MM-dd";
        public static readonly string DEFAULT_DATE_TIME_FORMAT = "yyyy-MM-dd HH:mm:ss";


        public struct DefaultResponse
        {
            public int status { get; set; }
            public string message { get; set; }
            public bool res { get; set; }
            public string return_token { get; set; }
            public object data { get; set; }

            public DefaultResponse(int status, string message, string return_token, object data = null, bool res = false)
            {
                this.status = status;
                this.message = message;
                this.return_token = return_token;
                this.data = data;
                this.res = res;
            }
        }



    }
}