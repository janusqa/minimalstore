using System.Net;

namespace MinimalVilla.Models.Dto
{
    public class ApiResponse
    {
        public HttpStatusCode StatusCode { get; set; }
        public bool IsSuccess { get; set; }
        public object? Result { get; set; }
        public List<string> ErrorMessages { get; set; }

        public ApiResponse()
        {
            ErrorMessages = [];
        }
    }
}