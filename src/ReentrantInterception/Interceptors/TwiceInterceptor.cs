using System.Collections.Generic;
using Castle.DynamicProxy;

namespace ReentrantInterception.Interceptors
{
    /// <summary>
    /// Interceptor that forces invocation of the next element in the invocation stack to twice
    /// </summary>
    public class TwiceInterceptor : InterceptorBase
    {
        int invocationCount = 0;

        protected override bool Pre(IInvocation invocation, IDictionary<string, object> invocationContext)
        {
            var shouldRetry = ++invocationCount <= 2;

            invocationContext["ShouldRetry"] = shouldRetry;

            var shouldExecuteTarget = shouldRetry;
            return shouldExecuteTarget;
        }
    }
}
