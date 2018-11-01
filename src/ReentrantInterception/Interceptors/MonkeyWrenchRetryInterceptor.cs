using System;
using System.Collections.Generic;
using Castle.DynamicProxy;

namespace ReentrantInterception.Interceptors
{
    /// <summary>
    /// Interceptor that forces monkey wrench exceptions to be retried
    /// </summary>
    public class MonkeyWrenchRetryInterceptor : InterceptorBase
    {
        protected override void PostSuccess(IInvocation invocation, IDictionary<string, object> invocationContext)
        {
            invocationContext.Remove("ShouldRetry");
        }

        protected override Exception TransformException(Exception ex, IDictionary<string, object> invocationContext)
        {
            if (ex is MonkeyWrench monkeyWrench)
            {
                invocationContext["ShouldRetry"] = true;
                return null;
            }
            else
            {
                invocationContext.Remove("ShouldRetry");
                return ex;
            }
        }
    }
}
