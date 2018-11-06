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

            await ExecuteTestAsync("Async with reference return value", Async_Value);

            await ExecuteTestAsync("Async with value return value type", Async_SpecificValue);


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

            //----
            //Pay special attention to what's going on here.
            //----
            //we're registering our target business class on this line, with our (non-retrying) interceptors,
            //and then naming the registration so that it won't be resolved unless specifically requested by name
            builder.RegisterType<FirstTimeFailBusinessClass>().Named<IBusinessClass>("Intercepted")
                   .EnableInterfaceInterceptors()
                   .InterceptedBy(typeof(ConsoleInterceptor));

            //Here, we're registering the wrapper class, that also conforms to the interface
            //and it's resolving the named version (with the interceptors) for internal use.
            //Since this version is NOT named/keyed, this will be the version that resolvers recieve.
            builder.Register(c => new PollyBusinessClass(c.ResolveNamed<IBusinessClass>("Intercepted")))
                   .As<IBusinessClass>();

            var container = builder.Build();

            var businessClass = container.Resolve<IBusinessClass>();

            return businessClass;
        }

        private static void Sync_Void()
        {
            var businessClass = CreateBusinessClass();
            businessClass.SomethingImportant();
        }

        private static void Sync_Value()
        {
            var businessClass = CreateBusinessClass();
            var result = businessClass.FetchSomething();

            Console.WriteLine($"Captured result: {result}");
        }

        private static async Task Async_Void()
        {
            var businessClass = CreateBusinessClass();
            await businessClass.SomethingImportantAsync();
        }

        private static async Task Async_Value()
        {
            var businessClass = CreateBusinessClass();
            var result = await businessClass.FetchSomethingAsync();

            Console.WriteLine($"Captured result: {result}");
        }

        private static async Task Async_SpecificValue()
        {
            var businessClass = CreateBusinessClass();
            var result = await businessClass.FetchSomethingSpecificAsync();

            Console.WriteLine($"Captured result: {result}");
        }
    }
}
