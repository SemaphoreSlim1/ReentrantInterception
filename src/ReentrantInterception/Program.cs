using System;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extras.DynamicProxy;
using Polly;
using Polly.Retry;
using ReentrantInterception.Interceptors;

namespace ReentrantInterception
{
    class Program
    {
        static async Task Main(string[] args)
        {
            ExecuteTest("Synchronous void", Sync_Void);

            ExecuteTest("Synchronous with return value", Sync_Value);

            await ExecuteTestAsync("Async void", Async_Void);

            await ExecuteTestAsync("Async with return value", Async_Value);


            Console.WriteLine("Done. Press Enter to exit.");
            Console.ReadLine();
        }

        private static void ExecuteTest(string header, Action test)
        {
            Console.WriteLine(header);
            Console.WriteLine("------------------");

            test();

            Console.WriteLine();
            Console.WriteLine();
        }

        private static async Task ExecuteTestAsync(string header, Func<Task> test)
        {
            Console.WriteLine(header);
            Console.WriteLine("------------------");

            await test();

            Console.WriteLine();
            Console.WriteLine();
        }

        private static IBusinessClass CreateBusinessClass()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<ConsoleInterceptor>().AsSelf();

            builder.RegisterType<FirstTimeFailBusinessClass>().As<IBusinessClass>()
                   .EnableInterfaceInterceptors()
                   .InterceptedBy(typeof(ConsoleInterceptor));

            var container = builder.Build();

            var businessClass = container.Resolve<IBusinessClass>();

            return businessClass;
        }

        private static RetryPolicy CreateRetryPolicy(bool asyncPolicy = false)
        {
            var builder = Policy.Handle<CustomException>()
                                .Or<OtherCustomException>();

            if(asyncPolicy)
            {
                return builder.RetryForeverAsync(ex => Console.WriteLine($"{Environment.NewLine}Experienced failure. Retrying.{Environment.NewLine}"));
            }
            else
            {
                return builder.RetryForever(ex => Console.WriteLine($"{Environment.NewLine}Experienced failure. Retrying.{Environment.NewLine}"));
            }
        }

        private static void Sync_Void()
        {
            var businessClass = CreateBusinessClass();
            var retryPolicy = CreateRetryPolicy();

            retryPolicy.Execute(businessClass.SomethingImportant);
        }

        private static void Sync_Value()
        {
            var businessClass = CreateBusinessClass();
            var retryPolicy = CreateRetryPolicy();

            var result = retryPolicy.ExecuteAndCapture(businessClass.FetchSomething);

            Console.WriteLine($"Captured result: {result.Result}");
        }

        private static async Task Async_Void()
        {
            var businessClass = CreateBusinessClass();
            var retryPolicy = CreateRetryPolicy(true);

            await retryPolicy.ExecuteAsync(businessClass.SomethingImportantAsync);
        }

        private static async Task Async_Value()
        {
            var businessClass = CreateBusinessClass();
            var retryPolicy = CreateRetryPolicy(true);

            var result = await retryPolicy.ExecuteAndCaptureAsync(businessClass.FetchSomethingAsync);

            Console.WriteLine($"Captured result: {result.Result}");
        }
    }
}
