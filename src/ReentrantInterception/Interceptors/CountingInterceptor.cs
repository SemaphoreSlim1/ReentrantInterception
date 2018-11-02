using System;
using System.Collections.Generic;
using Castle.DynamicProxy;
using ReentrantInterception.Interceptors;

namespace ReentrantInterception.Interceptors
{
    public class CountingInterceptor : InterceptorBase
    {
        public int PreExecutionCount { get; private set; }
        public int PostSuccessExecutionCount { get; private set; }
        public int PostErrorExecutionCount { get; private set; }
        public int TransformExceptionExecutionCount { get; private set; }

        protected override bool Pre(IInvocation invocation, IDictionary<string, object> invocationContext)
        {
            PreExecutionCount++;
            return true;
        }

        protected override void PostSuccess(IInvocation invocation, IDictionary<string, object> invocationContext)
        {
            PostSuccessExecutionCount++;
        }

        protected override void PostError(IInvocation invocation, Exception ex, IDictionary<string, object> invocationContext)
        {
            PostErrorExecutionCount++;
        }

        protected override Exception TransformException(Exception ex, IDictionary<string, object> invocationContext)
        {
            TransformExceptionExecutionCount++;
            return ex;
        }
    }
}
