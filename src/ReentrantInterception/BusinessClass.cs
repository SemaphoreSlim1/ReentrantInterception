using System;
using System.Threading.Tasks;

namespace ReentrantInterception
{
    public interface IBusinessClass
    {
        void SomethingImportant();
        object FetchSomething();

        Task SomethingImportantAsync();
        Task<object> FetchSomethingAsync();
    }

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
}
