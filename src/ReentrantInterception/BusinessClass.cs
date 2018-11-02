using System;
using System.Threading.Tasks;
using Autofac.Features.AttributeFilters;
using Polly;
using Polly.Retry;

namespace ReentrantInterception
{
    public interface IBusinessClass
    {
        void SomethingImportant();
        object FetchSomething();

        Task SomethingImportantAsync();
        Task<object> FetchSomethingAsync();
    }

    /// <summary>
    /// Simple business class that does business stuff
    /// </summary>
    public class SimpleBusinessClass : IBusinessClass
    {
        protected void ThrowException()
        {
            Console.WriteLine("Throwing an exception");
            throw new CustomException();
        }

        public virtual void SomethingImportant()
        {
            Console.WriteLine("Really. Important. Stuff.");
        }

        public virtual object FetchSomething()
        {
            Console.WriteLine("Fetching Something");
            return 1;
        }

        public virtual async Task SomethingImportantAsync()
        {
            //simulate a long running task or wire call
            await Task.Delay(TimeSpan.FromSeconds(5));
            SomethingImportant();
        }

        public virtual async Task<object> FetchSomethingAsync()
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            return FetchSomething();
        }
    }

    /// <summary>
    /// Provides an implementation of IBusinessClass that throws an exception the first time any method is executed
    /// </summary>
    public class FirstTimeFailBusinessClass : SimpleBusinessClass
    {
        private int InvokeCount = 0;

        public override void SomethingImportant()
        {
            if (++InvokeCount < 2)
            {
                ThrowException();
            }

            base.SomethingImportant();
        }

        public override object FetchSomething()
        {
            if (++InvokeCount < 2)
            {
                ThrowException();
            }

            return base.FetchSomething();
        }

        public override Task SomethingImportantAsync()
        {
            if (++InvokeCount < 2)
            {
                ThrowException();
            }

            return base.SomethingImportantAsync();
        }

        public override Task<object> FetchSomethingAsync()
        {
            if (++InvokeCount < 2)
            {
                ThrowException();
            }

            return base.FetchSomethingAsync();
        }
    }

    /// <summary>
    /// Provides a poor-man's interception around a business class, applying a polly retry policy to the public methods
    /// </summary>
    /// <remarks>
    /// Using a wrapper class like this is considered to be a poor-man's implementation of interception.
    /// while this approach works, it's not scalable; use your judgement before using
    /// </remarks>
    public class PollyBusinessClass : IBusinessClass
    {
        private readonly IBusinessClass _target;

        public PollyBusinessClass(IBusinessClass target)
        {
            _target = target;
        }

        public object FetchSomething()
        {
            var policy = CreateRetryPolicy();
            var result = policy.ExecuteAndCapture(() => _target.FetchSomething());
            return result.Result;
        }

        public async Task<object> FetchSomethingAsync()
        {
            var policy = CreateRetryPolicy(true);
            var result = await policy.ExecuteAndCaptureAsync(() => _target.FetchSomethingAsync());

            return result.Result;
        }

        public void SomethingImportant()
        {
            var policy = CreateRetryPolicy();
            policy.Execute(() => _target.SomethingImportant());
        }

        public Task SomethingImportantAsync()
        {
            var policy = CreateRetryPolicy(true);
            return policy.ExecuteAsync(() => _target.SomethingImportantAsync());
        }

        private static RetryPolicy CreateRetryPolicy(bool asyncPolicy = false)
        {
            var builder = Policy.Handle<CustomException>()
                                .Or<OtherCustomException>();

            if (asyncPolicy)
            {
                return builder.RetryForeverAsync(ex => Console.WriteLine($"{Environment.NewLine}Experienced failure. Retrying.{Environment.NewLine}"));
            }
            else
            {
                return builder.RetryForever(ex => Console.WriteLine($"{Environment.NewLine}Experienced failure. Retrying.{Environment.NewLine}"));
            }
        }
    }
}
