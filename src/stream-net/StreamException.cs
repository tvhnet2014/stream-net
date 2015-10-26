using Newtonsoft.Json;
using RestSharp;
using System;

namespace Stream
{
    [Serializable]
    public class StreamException : Exception
    {
        internal StreamException(ExceptionState state, string content)
            : base(message: state.Detail + "\r\n" + content)
        {
        
        }

        internal class ExceptionState
        {
            public int Code { get; set; }
            public String Detail { get; set; }
            public String Exception { get; set; }

            [Newtonsoft.Json.JsonProperty("status_code")]
            public int HttpStatusCode { get; set; }
        }

        internal static StreamException FromResponse(IRestResponse response)
        {
            //{"code": 6, "detail": "The following feeds are not configured: 'secret'", "duration": "4ms", "exception": "FeedConfigException", "status_code": 400}
            var content = response.Content;
            var state = JsonConvert.DeserializeObject<ExceptionState>(content);
            
            throw new StreamException(state, content);
        }
    }
}
