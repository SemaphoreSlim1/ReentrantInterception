using System;
using System.Collections.Generic;
using Castle.DynamicProxy;

namespace ReentrantInterception.Interceptors
{
    public class ConsoleInterceptor : InterceptorBase
    {
        protected override bool Pre(IInvocation invocation, IDictionary<string, object> invocationContext)
        {
            var methodName = invocation.Method.Name;
            Console.WriteLine($"About to execute {methodName}");

            var shouldExecuteTarget = true;
            return shouldExecuteTarget;
        }

        protected override void PostSuccess(IInvocation invocation, IDictionary<string, object> invocationContext)
        {
            var methodName = invocation.Method.Name;
            Console.WriteLine($"Successfully executed {methodName}");
            Console.WriteLine();
        }

        protected override void PostError(IInvocation invocation, Exception ex, IDictionary<string, object> invocationContext)
        {
            var methodName = invocation.Method.Name;
            Console.WriteLine($"Failure during execution of {methodName} : {ex.Message}");
            Console.WriteLine();
        }
    }
}
