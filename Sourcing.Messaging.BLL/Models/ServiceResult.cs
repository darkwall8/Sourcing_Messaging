using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sourcing.Messaging.BLL.Models
{
    public class ServiceResult<T>
    {
        public int StatusCode { get; set; } // 👈 Code HTTP à envoyer
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }

        public static ServiceResult<T> Ok(T data, string message = "Opération réussie") =>
            new ServiceResult<T> { StatusCode = 200, Message = message, Data = data };

        public static ServiceResult<T> BadRequest(string message) =>
            new ServiceResult<T> { StatusCode = 400, Message = message };

        public static ServiceResult<T> NotFound(string message) =>
            new ServiceResult<T> { StatusCode = 404, Message = message };

        public static ServiceResult<T> ServerError(string message) =>
            new ServiceResult<T> { StatusCode = 500, Message = message };
    }
}
