using FluentValidation.Results;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace My.XXX.Infra
{
    public class BaseResult
    {
        protected const string _successMessage = "Success";
        protected const int _successStatus = 1;
        protected const int _failStatus = 0;

        public BaseResult(int statusCode, string msg)
        {
            StatusCode = statusCode;
            Message = msg;
        }

        public string Message { get; protected set; }
        public int StatusCode { get; set; }

        public static BaseResult Success()
        {
            return new BaseResult(_successStatus, _successMessage);
        }

        public static BaseResult Fail(string msg)
        {
            return new BaseResult(_failStatus, msg);
        }

        public virtual void Localization(string message)
        {
            Message = message;
        }
    }

    public class PwCResult : BaseResult
    {
        public object Data { get; protected set; }

        public PwCResult(int statusCode, string msg) : base(statusCode, msg)
        {
        }

        public PwCResult(int statusCode, string msg, object data) : base(statusCode, msg)
        {
            Data = data;
        }

        public static PwCResult Successed
        {
            get
            {
                return new PwCResult(_successStatus, _successMessage);
            }
        }

        public new static PwCResult Success()
        {
            return new PwCResult(_successStatus, _successMessage);
        }

        public static PwCResult Success(object data)
        {
            return new PwCResult(_successStatus, _successMessage, data);
        }

        public new static PwCResult Fail(string message)
        {
            return new PwCResult(_failStatus, message);
        }

        public static PwCResult Fail(string message, int statusCode)
        {
            return new PwCResult(statusCode, message);
        }

        public static PwCResult Fail(IList<ValidationFailure> errors)
        {
            var msg = string.Join(",", errors.Select(m => m.ErrorMessage).ToList());
            return new PwCResult(_failStatus, msg);
        }
    }

    public class PwCResult<T> : BaseResult
    {
        public T Data { get; protected set; }

        public PwCResult(int statusCode, string msg) : base(statusCode, msg)
        {
        }

        public PwCResult(int statusCode, string msg, T data) : base(statusCode, msg)
        {
            Data = data;
        }

        public new static PwCResult Success()
        {
            return new PwCResult(_successStatus, _successMessage);
        }

        public static PwCResult<T> Success(T data)
        {
            return new PwCResult<T>(_successStatus, _successMessage, data);
        }

        public new static PwCResult Fail(string message)
        {
            return new PwCResult(_failStatus, message);
        }

        public static PwCResult Fail(IList<ValidationFailure> errors)
        {
            var msg = string.Join(",", errors.Select(m => m.ErrorMessage).ToList());
            return new PwCResult(_failStatus, msg);
        }
    }

    public sealed class LoginResult : PwCResult
    {
        public string AccessToken { get; private set; }
        public string RefreshToken { get; private set; }
        public int ExpiryInMinutes { get; set; }

        public LoginResult(int state, string msg, object data, string token, string refreshToken = "") : base(state, msg, data)
        {
            AccessToken = token;
            RefreshToken = refreshToken;
        }

        public LoginResult(int state, string msg, int statusCode) : base(state, msg, statusCode)
        {
        }

        public static LoginResult Success(object data, string token, string refreshToken = "")
        {
            return new LoginResult(_successStatus, _successMessage, data, token, refreshToken);
        }

        public static LoginResult Success(object data, string token, string refreshToken, int expiryInMinutes)
        {
            return new LoginResult(_successStatus, _successMessage, data, token, refreshToken)
            {
                ExpiryInMinutes = expiryInMinutes
            };
        }

        public static LoginResult RefreshSuccess(string token, string refreshToken, int expiryInMinutes)
        {
            return new LoginResult(_successStatus, _successMessage, null, token, refreshToken)
            {
                ExpiryInMinutes = expiryInMinutes
            };
        }

        public new static LoginResult Fail(string message = "")
        {
            return new LoginResult(_failStatus, message, null, string.Empty);
        }

        public static LoginResult Exception(string message)
        {
            return new LoginResult(_failStatus, message, 500);
        }
    }

    public sealed class PagesResult : PwCResult
    {
        public int Total { get; private set; }

        public PagesResult(int state, string msg, object data, int total) : base(state, msg, data)
        {
            Total = total;
        }

        public static PagesResult Success<T>(Paged<T> page)
        {
            return new PagesResult(_successStatus, _successMessage, page.List, page.Total);
        }

        public static PagesResult Fail<T>(string msg)
        {
            var list = new List<T>();
            return new PagesResult(_failStatus, msg, list, 0);
        }
    }

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
