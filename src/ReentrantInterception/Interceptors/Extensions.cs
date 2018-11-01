using System;
using System.Reflection;
using Castle.DynamicProxy;

namespace ReentrantInterception.Interceptors
{
    public static class IInvocationExtensions
    {
        private static FieldInfo _resetInvocationInterceptorsCall;
        private static FieldInfo _execIndexField = typeof(AbstractInvocation).GetField("currentInterceptorIndex", BindingFlags.Instance | BindingFlags.NonPublic);

        public static int GetExecIndex( this IInvocation invocation)
        {
            if(!(invocation is AbstractInvocation abstractInvocation))
            { throw new InvalidCastException($"Only works if target is {typeof(AbstractInvocation)}"); }

            return (int)_execIndexField.GetValue(invocation);
        }

        public static void SetExecIndex(this IInvocation invocation, int value)
        {
            if (!(invocation is AbstractInvocation abstractInvocation))
            { throw new InvalidCastException($"Only works if target is {typeof(AbstractInvocation)}"); }

            _execIndexField.SetValue(invocation,value);
        }

        public static void Reset(this IInvocation invocation)
        {
            if (_resetInvocationInterceptorsCall == null)
            {
                Type invoc = FindBaseType(invocation.GetType(), typeof(AbstractInvocation));
                if (invoc == null)
                { throw new InvalidOperationException("IInvocationExtensions - Cannot find AbstractInvocation as base class."); }

                _resetInvocationInterceptorsCall = invoc.GetField("currentInterceptorIndex", BindingFlags.Instance | BindingFlags.NonPublic);
            }
            // reinitialize the index of execution, so when we call Proceed() again
            // the whole chain of interceptors start again from the first element
            _resetInvocationInterceptorsCall.SetValue(invocation, -1);
        }

        private static Type FindBaseType(Type src, Type lookingFor)
        {
            while (!(src == typeof(object)) && (src != lookingFor))
            {
                src = src.BaseType;
            }
            if (src == lookingFor)
                return src;
            return null;
        }
    }

}
