using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.DTOs
{
    public enum ResponseStatus
    {
        Success,
        NotFound,
        ValidationError,
        Unauthorized,
        Forbidden,
        Conflict,
        Error
    }

    public class Response<T>
    {
        public bool Succeeded { get; set; }
        public ResponseStatus Status { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new();

        public static Response<T> Success(T data, string? message = null) =>
            new() { Succeeded = true, Status = ResponseStatus.Success, Data = data, Message = message };

        public static Response<T> Fail(ResponseStatus status, string message, List<string>? errors = null) =>
            new() { Succeeded = false, Status = status, Message = message, Errors = errors ?? new() };
    }
}
