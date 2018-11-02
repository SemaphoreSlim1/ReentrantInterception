using Castle.DynamicProxy;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ReentrantInterception.Interceptors
{
    /// <summary>
    /// Intercepts interface methods, normalizing the synchronous and asynchronous version
    /// into easily implemented lifetime events
    /// </summary>
    public abstract class InterceptorBase : IInterceptor
    {

        public void Intercept(IInvocation invocation)
        {
            var invocationContext = new Dictionary<string, object>();

            //Pre might optimize us out of invocation, so honor that
            if (Pre(invocation, invocationContext) == false)
            { return; }

            try
            {
                //invoke the method being intercepted
                invocation.Proceed();
            }
            catch (System.Exception ex)
            {
                PostError(invocation, ex, invocationContext);
                var transformedException = TransformException(ex, invocationContext);

                if (transformedException == ex)
                { throw; } //preserve the call stack

                if (transformedException != null)
                { throw transformedException; }

                return;
            }

            //determine if the return value of the intercepted method is a Task.
            //if it is, then we need to wait for the task to complete before invoking post
            //otherwise, we can invoke post right now.
            if (invocation.ReturnValue is Task returnedTask)
            {
                var returnedTaskType = returnedTask.GetType();
                if(returnedTaskType.IsGenericType) //if it's generic, then it's going to have a return value
                {
                    invocation.ReturnValue = WatchForResultAsync(returnedTask, returnedTaskType, invocation, invocationContext);
                }
                else
                {
                    invocation.ReturnValue = WatchForCompletionAsync(returnedTask, invocation, invocationContext);
                }
            }
            else
            {
                PostSuccess(invocation, invocationContext);
            }
        }

        /// <summary>
        /// Awaits execution of the task, and then invokes the post function once the task has completed.
        /// </summary>
        /// <param name="invocation">the target invocation</param>
        /// <param name="invocationContext">Data from the pre invocation</param>
        /// <returns>The task that the caller of the target invocation will now await upon</returns>
        private async Task WatchForCompletionAsync(Task task, IInvocation invocation, IDictionary<string, object> invocationContext)
        {
            try
            {
                await task;
            }
            catch(Exception ex)
            {
                PostError(invocation, ex, invocationContext);
                var transformedException = TransformException(ex, invocationContext);

                if(transformedException == ex)
                { throw; }

                if (transformedException != null)
                { throw transformedException; }

                return;
            }

            PostSuccess(invocation, invocationContext);
        }

        private async Task<object> WatchForResultAsync(Task task, Type taskType, IInvocation invocation, IDictionary<string, object> invocationContext)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                PostError(invocation, ex, invocationContext);
                var transformedException = TransformException(ex, invocationContext);

                if (transformedException == ex)
                { throw; }

                if (transformedException != null)
                { throw transformedException; }

                //at this point in the execution, we are suppressing the rethrow.
                //return the default for the return type
                var returnType = taskType.GetGenericParameterConstraints()[0];
                return returnType.IsValueType ? Activator.CreateInstance(returnType) : null;
            }

            PostSuccess(invocation, invocationContext);

            //we need to extract the return value since we awaited earlier
            var resultProperty = taskType.GetProperty(nameof(Task<object>.Result));
            var resultValue = resultProperty.GetValue(task);
            return resultValue;
        }



      

        /// <summary>
        /// The method executed prior to the target method
        /// </summary>
        /// <param name="invocation">The target of invocation</param>
        /// <param name="invocationContext">Context data that will persist to the post methods</param>
        /// <returns>True, if the interceptor should proceed with invocation</returns>
        protected virtual bool Pre(IInvocation invocation, IDictionary<string, object> invocationContext)
        {
            return true;
        }

        /// <summary>
        /// The method executed after the target method has completed successfully
        /// </summary>
        /// <param name="invocation">The target of invocation</param>
        /// <param name="invocationContext">Contextual data from this invocation</param>
        protected virtual void PostSuccess(IInvocation invocation, IDictionary<string, object> invocationContext)
        { }

        /// <summary>
        /// The method executed after the target method has thrown an exception
        /// </summary>
        /// <param name="invocation">The target of invocation</param>
        /// <param name="ex">The exception thrown by the target invocation</param>
        /// <param name="invocationContext">Contextual data from this invocation</param>
        protected virtual void PostError(IInvocation invocation, Exception ex, IDictionary<string, object> invocationContext)
        { }

        /// <summary>
        /// Transforms the errored exception before throwing it back to the caller.
        /// Note: Returning null will suppress the exception.
        /// </summary>
        /// <param name="ex">The original exception</param>
        /// <param name="invocationContext">Contextual data from this invocation</param>
        /// <returns>The transformed exception</returns>
        protected virtual Exception TransformException(Exception ex, IDictionary<string, object> invocationContext)
        { return ex; }
    }
}