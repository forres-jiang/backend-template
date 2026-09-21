using FluentValidation.Results;
using My.XXX.Shared;
using System.Collections.Generic;
using System.Linq;

namespace My.XXX.APIs.Models
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

    public class MyResult : BaseResult
    {
        public object Data { get; protected set; }

        public MyResult(int statusCode, string msg) : base(statusCode, msg)
        {
        }

        public MyResult(int statusCode, string msg, object data) : base(statusCode, msg)
        {
            Data = data;
        }

        public static MyResult Successed
        {
            get
            {
                return new MyResult(_successStatus, _successMessage);
            }
        }

        public new static MyResult Success()
        {
            return new MyResult(_successStatus, _successMessage);
        }

        public static MyResult Success(object data)
        {
            return new MyResult(_successStatus, _successMessage, data);
        }

        public new static MyResult Fail(string message)
        {
            return new MyResult(_failStatus, message);
        }

        public static MyResult Fail(string message, int statusCode)
        {
            return new MyResult(statusCode, message);
        }

        public static MyResult Fail(IList<ValidationFailure> errors)
        {
            var msg = string.Join(",", errors.Select(m => m.ErrorMessage).ToList());
            return new MyResult(_failStatus, msg);
        }
    }

    public class MyResult<T> : BaseResult
    {
        public T Data { get; protected set; }

        public MyResult(int statusCode, string msg) : base(statusCode, msg)
        {
        }

        public MyResult(int statusCode, string msg, T data) : base(statusCode, msg)
        {
            Data = data;
        }

        public new static MyResult Success()
        {
            return new MyResult(_successStatus, _successMessage);
        }

        public static MyResult<T> Success(T data)
        {
            return new MyResult<T>(_successStatus, _successMessage, data);
        }

        public new static MyResult Fail(string message)
        {
            return new MyResult(_failStatus, message);
        }

        public static MyResult Fail(IList<ValidationFailure> errors)
        {
            var msg = string.Join(",", errors.Select(m => m.ErrorMessage).ToList());
            return new MyResult(_failStatus, msg);
        }
    }

    public sealed class LoginResult : MyResult
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

    public sealed class PagesResult : MyResult
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

}
