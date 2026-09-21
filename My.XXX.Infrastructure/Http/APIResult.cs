using My.XXX.Contracts.DTOs;
using System.Net;
namespace My.XXX.Infrastructure
{
    public class APIResult
    {
        public Users Data { get; set; }
        public string Message { get; set; }
        public int Status { get; set; }
    }

    public class APIResult<T>
    {
        public T Data { get; set; }
        public string Message { get; set; }
        public int Status { get; set; }
        public HttpStatusCode HttpStatusCode { get; set; }
    }

}
