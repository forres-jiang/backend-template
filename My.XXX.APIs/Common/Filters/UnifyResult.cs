using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Localization;
using My.XXX.Infra;
using System.Collections.Generic;
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
                var declaredType = objectResult?.DeclaredType;
                var typeList = new List<string>()
                {
                    typeof(BaseResult).Name,
                    typeof(PwCResult).Name,
                    typeof(PwCResult<>).Name,
                    typeof(LoginResult).Name,
                    typeof(PagesResult).Name
                };

                if (declaredType == null || typeList.Contains(declaredType.Name))
                {
                    return;
                }
                context.Result = new ObjectResult(PwCResult.Success(objectResult?.Value));
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
                if (declaredType == null)
                {
                    await next();
                    return;
                }

                if (declaredType.BaseType.Name is (nameof(BaseResult)) or (nameof(PwCResult)))
                {
                    SetLocalization(objectResult);

                    await next();
                    return;
                }

                var result = PwCResult.Success(objectResult?.Value);
                var message = _resultLocalization[result.Message];
                if (!message.ResourceNotFound)
                {
                    result.Localization(message.Value);
                }

                context.Result = new ObjectResult(result);
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