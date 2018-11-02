using System;
using System.Collections.Generic;
using Castle.DynamicProxy;

namespace ReentrantInterception.Interceptors
{
    public class ActionInterceptor : InterceptorBase
    {
        public Func<IDictionary<string, object>, bool> PreInvocation { get; set; }
        public Action<IDictionary<string, object>> PostSuccessInvocation { get; set; }
        public Action<Exception, IDictionary<string, object>> PostErrorInvocation { get; set; }
        public Func<Exception, IDictionary<string, object>, Exception> TransformExceptionInvocation { get; set; }

        protected override bool Pre(IInvocation invocation, IDictionary<string, object> invocationContext)
        {
            return PreInvocation?.Invoke(invocationContext) ?? true;
        }

        protected override void PostSuccess(IInvocation invocation, IDictionary<string, object> invocationContext)
        {
            PostSuccessInvocation?.Invoke(invocationContext);
        }

        protected override void PostError(IInvocation invocation, Exception ex, IDictionary<string, object> invocationContext)
        {
            PostErrorInvocation?.Invoke(ex, invocationContext);
        }

        protected override Exception TransformException(Exception ex, IDictionary<string, object> invocationContext)
        {
            if (TransformExceptionInvocation == null)
            { return ex; }

            return TransformExceptionInvocation.Invoke(ex, invocationContext);
        }
    }
}
