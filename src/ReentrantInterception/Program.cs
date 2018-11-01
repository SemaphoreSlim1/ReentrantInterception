using System;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extras.DynamicProxy;
using ReentrantInterception.Interceptors;

namespace ReentrantInterception
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await ShowcaseAsyncRetryTwice();

            Console.WriteLine("Done. Press Enter to exit.");
            Console.ReadLine();
        }

        private static async Task ShowcaseAsyncRetryTwice()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<TwiceInterceptor>().AsSelf();
            builder.RegisterType<ConsoleInterceptor>().AsSelf();

            builder.RegisterType<SimpleBusinessClass>().As<IBusinessClass>()
                   .EnableInterfaceInterceptors()
                   .InterceptedBy( typeof(TwiceInterceptor));

            var container = builder.Build();

            var businessClass = container.Resolve<IBusinessClass>();

            await businessClass.SomethingImportantAsync();
        }

        private static void ShowcaseRetryOnMonkeyWrench()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<MonkeyWrenchRetryInterceptor>().AsSelf();
            builder.RegisterType<ConsoleInterceptor>().AsSelf();

            builder.RegisterType<FirstTimeFailBusinessClass>().As<IBusinessClass>()
                   .EnableInterfaceInterceptors()
                   .InterceptedBy(typeof(MonkeyWrenchRetryInterceptor),
                                  typeof(ConsoleInterceptor));

            var container = builder.Build();

            var businessClass = container.Resolve<IBusinessClass>();

            try
            {
                businessClass.SomethingImportant();
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Caught exception of type {ex.GetType()}");
            }
        }

        private static void ShowcaseTwiceInterceptor()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<TwiceInterceptor>().AsSelf();
            builder.RegisterType<ConsoleInterceptor>().AsSelf();

            builder.RegisterType<SimpleBusinessClass>().As<IBusinessClass>()
                   .EnableInterfaceInterceptors()
                   .InterceptedBy(typeof(TwiceInterceptor),
                                    typeof(ConsoleInterceptor));

            var container = builder.Build();

            var businessClass = container.Resolve<IBusinessClass>();

            businessClass.SomethingImportant();
        }
    }
}
