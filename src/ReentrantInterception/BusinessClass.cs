using System;
using System.Threading.Tasks;

namespace ReentrantInterception
{
    public interface IBusinessClass
    {
        void SomethingImportant();
        Task SomethingImportantAsync();
    }

    public abstract class BusinessClassBase
    {
        protected void ThrowMonkeyWrench()
        {
            Console.WriteLine("Throwing a monkey wrench");
            throw new MonkeyWrench();
        }

        protected void DoStuff()
        {
            Console.WriteLine("Really. Important. Stuff.");
        }
    }

    public class SimpleBusinessClass : BusinessClassBase, IBusinessClass
    {
        public void SomethingImportant()
        {
            DoStuff();
        }

        public async Task SomethingImportantAsync()
        {
            //simulate a long running task or wire call
            await Task.Delay(TimeSpan.FromSeconds(5));
            SomethingImportant();
        }
    }

    public class FirstTimeFailBusinessClass : BusinessClassBase, IBusinessClass
    {
        private int InvokeCount = 0;

        public void SomethingImportant()
        {
            if (++InvokeCount < 2)
            {
                ThrowMonkeyWrench();
            }

            DoStuff();
        }

        public async Task SomethingImportantAsync()
        {
            //simulate a long running task or wire call
            await Task.Delay(TimeSpan.FromSeconds(5));
            SomethingImportant();
        }
    }
}
