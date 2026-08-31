
using System;
using System.Collections.Generic;

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

        public string? MessageKey { get; set; }

        public object[] MessageArguments { get; set; } = Array.Empty<object>();

        public T? Data { get; set; }

        public List<string> Errors { get; set; } = new();

        public static Response<T> Success(T data, string? message = null) =>
            new()
            {
                Succeeded = true,
                Status = ResponseStatus.Success,
                Data = data,
                Message = message
            };

        public static Response<T> Fail(
            ResponseStatus status,
            string message,
            List<string>? errors = null) =>
            new()
            {
                Succeeded = false,
                Status = status,
                Message = message,
                Errors = errors ?? new()
            };

        public static Response<T> SuccessWithKey(
            T data,
            string messageKey,
            params object[] messageArguments) =>
            new()
            {
                Succeeded = true,
                Status = ResponseStatus.Success,
                Data = data,
                MessageKey = messageKey,
                MessageArguments = messageArguments
            };

        public static Response<T> FailWithKey(
            ResponseStatus status,
            string messageKey,
            params object[] messageArguments) =>
            new()
            {
                Succeeded = false,
                Status = status,
                MessageKey = messageKey,
                MessageArguments = messageArguments
            };
    }
}
