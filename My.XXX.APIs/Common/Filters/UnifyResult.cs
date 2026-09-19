using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Localization;
using My.XXX.Infra;
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
                if (objectResult.Value is BaseResult || objectResult.DeclaredType == null)
                {
                    return;
                }
                objectResult.Value = MyResult.Success(objectResult.Value);
                objectResult.DeclaredType = objectResult.Value.GetType();
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
                var declaredType = objectResult?.DeclaredType;
                if (declaredType == null && objectResult.Value is not BaseResult)
                {
                    await next();
                    return;
                }

                if (objectResult.Value is BaseResult)
                {
                    SetLocalization(objectResult);

                    await next();
                    return;
                }

                var result = MyResult.Success(objectResult?.Value);
                var message = _resultLocalization[result.Message];
                if (!message.ResourceNotFound)
                {
                    result.Localization(message.Value);
                }

                objectResult.Value = result;
                objectResult.DeclaredType = result.GetType();
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
