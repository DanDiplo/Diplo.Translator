using System.Net;

namespace Diplo.Translator.Models
{
    /// <summary>
    /// Represents a model that wraps a response object
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ResponseWrapper<T> where T : class
    {
        public bool IsSuccess { get; set; }

        public HttpStatusCode StatusCode { get; set; }

        public string Message { get; set; }

        public T Model { get; set; }
    }
}
