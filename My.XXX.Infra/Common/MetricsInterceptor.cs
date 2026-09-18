using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace My.XXX.Infra.Common
{
    public class MetricsAsyncInterceptor : IAsyncInterceptor
    {
        private readonly ILogger<MetricsAsyncInterceptor> _logger;
        public MetricsAsyncInterceptor(ILogger<MetricsAsyncInterceptor> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 同步方法拦截时使用
        /// </summary>
        /// <param name="invocation"></param>
        public void InterceptSynchronous(IInvocation invocation)
        {
            invocation.Proceed();
        }

        /// <summary>
        /// 异步方法返回Task时使用
        /// </summary>
        /// <param name="invocation"></param>
        public void InterceptAsynchronous(IInvocation invocation)
        {
            invocation.ReturnValue = InternalInterceptAsynchronous(invocation);
        }

        /// <summary>
        /// 异步方法返回Task T 时使用
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="invocation"></param>
        public void InterceptAsynchronous<TResult>(IInvocation invocation)
        {
            //调用业务方法
            invocation.ReturnValue = InternalInterceptAsynchronous<TResult>(invocation);
        }

        private async Task InternalInterceptAsynchronous(IInvocation invocation)
        {
            invocation.Proceed();
            await (Task)invocation.ReturnValue;
        }

        private async Task<TResult> InternalInterceptAsynchronous<TResult>(IInvocation invocation)
        {
            //获取执行信息
            var methodName = invocation.Method.Name;
            invocation.Proceed();
            var task = (Task<TResult>)invocation.ReturnValue;
            TResult result = await task;
            _logger.LogError("{methodName} 已执行，返回结果：{result}", methodName, result);
            return result;
        }
    }

    public class MetricsInterceptor : IInterceptor
    {
        private readonly MetricsAsyncInterceptor _interceptor;
        public MetricsInterceptor(MetricsAsyncInterceptor interceptor)
        {
            _interceptor = interceptor;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="invocation"></param>
        public void Intercept(IInvocation invocation)
        {
            _interceptor.ToInterceptor().Intercept(invocation);
        }
    }
}