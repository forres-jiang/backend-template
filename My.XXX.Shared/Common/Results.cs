using System.Collections.Generic;
using System.Net;
namespace My.XXX.Shared
{
    public class Paged<T>
    {
        public int Total { get; protected set; }

        public List<T> List { get; protected set; }

        public Paged(List<T> data, int total)
        {
            Total = total;
            List = data;
        }

        public static Paged<T> Create(List<T> data, int Total)
        {
            return new Paged<T>(data, Total);
        }
    }

    public class DataValidator<T>
    {
        public DataValidator()
        { }

        public DataValidator(bool isValid, T data)
        {
            IsValid = isValid;
            Data = data;
        }

        public DataValidator(bool isValid, string msg, T data)
        {
            Message = msg;
            IsValid = isValid;
            Data = data;
        }

        public bool IsValid { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }

        public DataValidator<T> Valid(T data)
        {
            return new DataValidator<T>(true, data);
        }

        public DataValidator<T> InValid()
        {
            return new DataValidator<T>(false, default);
        }

        public DataValidator<T> InValid(string msg)
        {
            return new DataValidator<T>(false, msg, default);
        }
    }

    public class Validator<T>
    {
        public Validator()
        { }

        public Validator(bool isValid, T data)
        {
            IsValid = isValid;
            Data = data;
        }

        public Validator(bool isValid, string msg, T data)
        {
            Message = msg;
            IsValid = isValid;
            Data = data;
        }

        public Validator(bool isValid, string msg, T data, HttpStatusCode statusCode)
        {
            StatusCode = statusCode;
            Message = msg;
            IsValid = isValid;
            Data = data;
        }

        public bool IsValid { get; set; }
        public string Message { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public T Data { get; set; }

        public Validator<T> Valid(T data)
        {
            return new Validator<T>(true, data);
        }

        public Validator<T> InValid()
        {
            return new Validator<T>(false, default);
        }

        public Validator<T> InValid(string msg)
        {
            return new Validator<T>(false, msg, default);
        }

        public Validator<T> InValid(HttpStatusCode statusCode)
        {
            return new Validator<T>(false, statusCode.ToString(), default, statusCode);
        }
    }
}
