using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Localization;
using My.XXX.Shared;
using System.Linq;
using System.Threading.Tasks;

namespace My.XXX.APIs.Common
{
    public class UnifyResult : ResultFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext context)
        {
            var controllerActionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;
            var action = controllerActionDescriptor?.MethodInfo.GetCustomAttributes(typeof(NonUnifyResult), false).FirstOrDefault();
            var controller = controllerActionDescriptor?.ControllerTypeInfo.GetCustomAttributes(typeof(NonUnifyResult), false).FirstOrDefault();

            if (action != null || controller != null)
            {
                return;
            }

            if (context.Result is ObjectResult)
            {
                var objectResult = context.Result as ObjectResult;
                ResponseNormalizer.Normalize(objectResult);
            }
        }
    }

    internal static class ResponseNormalizer
    {
        internal static void Normalize(ObjectResult response)
        {
            if (response.Value is FluentResults.ResultBase)
                throw new System.InvalidOperationException("Convert business results with ToApiResult before returning them from a controller.");
            if (response.Value is not BaseResult)
            {
                response.Value = MyResult.Success(response.Value);
                response.DeclaredType = response.Value.GetType();
            }
        }
    }

    public class UnifyResultAsync : IAsyncResultFilter
    {
        private readonly IStringLocalizer<ResultResource> _resultLocalization;

        public UnifyResultAsync(IStringLocalizer<ResultResource> resultLocalization)
        {
            _resultLocalization = resultLocalization;
        }

        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            var controllerActionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;
            var action = controllerActionDescriptor?.MethodInfo
                .GetCustomAttributes(typeof(NonUnifyResult), false).FirstOrDefault();
            var controller = controllerActionDescriptor?.ControllerTypeInfo
                .GetCustomAttributes(typeof(NonUnifyResult), false).FirstOrDefault();

            if (action != null || controller != null)
            {
                await next();
                return;
            }

            if (context.Result is ObjectResult)
            {
                var objectResult = context.Result as ObjectResult;
                ResponseNormalizer.Normalize(objectResult);
                SetLocalization(objectResult);
            }

            await next();
        }

        private void SetLocalization(ObjectResult objectResult)
        {
            if (objectResult.Value is not BaseResult result)
            {
                return;
            }

            var message = _resultLocalization[result.Message];
            if (!message.ResourceNotFound)
            {
                result.Localization(message.Value);
            }
        }
    }

    public class NonUnifyResult : ResultFilterAttribute
    { }
}
