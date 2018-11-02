using System;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extras.DynamicProxy;
using ReentrantInterception.Interceptors;

namespace ReentrantInterception
{
    class Program
    {
        static void Main(string[] args)
        {
            ShowcaseConsoleInterceptor();

            Console.WriteLine("Done. Press Enter to exit.");
            Console.ReadLine();
        }

        private static void ShowcaseConsoleInterceptor()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<ConsoleInterceptor>().AsSelf();

            builder.RegisterType<SimpleBusinessClass>().As<IBusinessClass>()
                   .EnableInterfaceInterceptors()
                   .InterceptedBy(typeof(ConsoleInterceptor));

            var container = builder.Build();

            var businessClass = container.Resolve<IBusinessClass>();

            businessClass.SomethingImportant();
        }
    }
}
