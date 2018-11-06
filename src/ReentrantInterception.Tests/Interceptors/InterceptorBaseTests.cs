using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extras.DynamicProxy;
using Castle.DynamicProxy;
using Moq;
using Moq.Protected;
using ReentrantInterception.Interceptors;
using Xunit;

namespace ReentrantInterception.Tests.Interceptors
{
    public class ActionBusinessClass : IBusinessClass
    {
        public Func<object> FetchSomethingInvocation { get; set; }
        public Func<Task<object>> FetchSomethingAsyncInvocation { get; set; }

        public Action SomethingImportantInvocation { get; set; }
        public Func<Task> SomethingImportantAsyncInvocation { get; set; }

        public object FetchSomething()
        {
            return FetchSomethingInvocation();
        }

        public Task<object> FetchSomethingAsync()
        {
            return FetchSomethingAsyncInvocation();
        }

        public Task<int> FetchSomethingSpecificAsync()
        {
            return FetchSomethingSpecificAsync();
        }

        public void SomethingImportant()
        {
            SomethingImportantInvocation();
        }

        public Task SomethingImportantAsync()
        {
            return SomethingImportantAsyncInvocation();
        }
    }

    public class InterceptorBaseTests
    {
        private enum ExceptionMode
        {
            DontThrow,
            Throw,
            ThrowBeforeAwait,
            ThrowAfterAwait
        }

        private const string Pre = nameof(Pre);
        private const string PostSuccess = nameof(PostSuccess);
        private const string PostError = nameof(PostError);
        private const string TransformException = nameof(TransformException);

        private readonly object ExpectedFetchedValue = new object();
        private readonly List<string> _invocationList;

        private bool _PreReturnValue = true;

        private readonly ActionInterceptor _actionInterceptor;
        private readonly IDictionary<ExceptionMode, IBusinessClass> _businessClasses;

        public InterceptorBaseTests()
        {
            _invocationList = new List<string>();

            //using concrete classes because interception did NOT play well with Moq
            _actionInterceptor = new ActionInterceptor()
            {
                PreInvocation = context => { _invocationList.Add(Pre); return _PreReturnValue; },
                PostSuccessInvocation = context => { _invocationList.Add(PostSuccess); },
                PostErrorInvocation = (ex, context) => { _invocationList.Add(PostError); },
                TransformExceptionInvocation = (ex, context) => { _invocationList.Add(TransformException); return ex; }
            };

            _businessClasses = new Dictionary<ExceptionMode, IBusinessClass>();

            foreach (var mode in Enum.GetValues(typeof(ExceptionMode)).Cast<ExceptionMode>())
            {
                _businessClasses[mode] = new ActionBusinessClass()
                                        {
                                            SomethingImportantInvocation = () => { RecordInvocation(nameof(IBusinessClass.SomethingImportant), mode); },
                                            SomethingImportantAsyncInvocation = () => { return RecordInvocationAsync(nameof(IBusinessClass.SomethingImportantAsync), mode); },

                                            FetchSomethingInvocation = () => { RecordInvocation(nameof(IBusinessClass.FetchSomething), mode); return ExpectedFetchedValue; },
                                            FetchSomethingAsyncInvocation = () => { return RecordInvocationAsync(nameof(IBusinessClass.FetchSomethingAsync), mode, ExpectedFetchedValue); }
                                        };

            }
        }

        private void RecordInvocation(string name, ExceptionMode exceptionMode = ExceptionMode.DontThrow)
        {
            _invocationList.Add(name);

            if(exceptionMode != ExceptionMode.DontThrow)
            {
                throw new MonkeyWrench();
            }
        }

        private async Task<object> RecordInvocationAsync(string name, ExceptionMode exceptionMode = ExceptionMode.DontThrow, object returnValue = null)
        {
            _invocationList.Add(name);

            switch (exceptionMode)
            {
                case ExceptionMode.Throw:
                case ExceptionMode.ThrowBeforeAwait:
                    throw new MonkeyWrench();
                case ExceptionMode.ThrowAfterAwait:
                    await Task.CompletedTask;
                    throw new MonkeyWrench();
            }

            return returnValue;
        }

        private IBusinessClass GetBusinessClass(ExceptionMode mode)
        {
            //register the types
            var builder = new ContainerBuilder();
            builder.RegisterInstance(_actionInterceptor).As<ActionInterceptor>();

            builder.RegisterInstance(_businessClasses[mode]).As<IBusinessClass>()
                   .EnableInterfaceInterceptors()
                   .InterceptedBy(typeof(ActionInterceptor));

            //build the container and resolve the intercepted business class
            var container = builder.Build();
            var businessClass = container.Resolve<IBusinessClass>();

            return businessClass;
        }

        [Fact]
        public void SynchronousVoid_HappyPath()
        {
            var businessClass = GetBusinessClass(ExceptionMode.DontThrow);

            //act
            businessClass.SomethingImportant();

            Assert.Equal(3, _invocationList.Count);
            Assert.Equal(Pre, _invocationList[0]);
            Assert.Equal(nameof(IBusinessClass.SomethingImportant), _invocationList[1]);
            Assert.Equal(PostSuccess, _invocationList[2]);
        }

        [Fact]
        public void SynchronousVoid_ExceptionPath()
        {
            var businessClass = GetBusinessClass(ExceptionMode.Throw);

            //act
            Assert.Throws<MonkeyWrench>(()=>businessClass.SomethingImportant());


            Assert.Equal(4, _invocationList.Count);
            Assert.Equal(Pre, _invocationList[0]);
            Assert.Equal(nameof(IBusinessClass.SomethingImportant), _invocationList[1]);
            Assert.Equal(PostError, _invocationList[2]);
            Assert.Equal(TransformException, _invocationList[3]);
        }



        [Fact]
        public void SynchronousResult_HappyPath()
        {
            var businessClass = GetBusinessClass(ExceptionMode.DontThrow);

            //act
            var returnedValue = businessClass.FetchSomething();

            Assert.Equal(3, _invocationList.Count);
            Assert.Equal(Pre, _invocationList[0]);
            Assert.Equal(nameof(IBusinessClass.FetchSomething), _invocationList[1]);
            Assert.Equal(PostSuccess, _invocationList[2]);

            //ensure the intended returned value was preserved
            Assert.Same(ExpectedFetchedValue, returnedValue);
        }

        [Fact]
        public void SynchronousResult_ExceptionPath()
        {
            var businessClass = GetBusinessClass(ExceptionMode.Throw);

            //act
            Assert.Throws<MonkeyWrench>(() => businessClass.FetchSomething());

            Assert.Equal(4, _invocationList.Count);
            Assert.Equal(Pre, _invocationList[0]);
            Assert.Equal(nameof(IBusinessClass.FetchSomething), _invocationList[1]);
            Assert.Equal(PostError, _invocationList[2]);
            Assert.Equal(TransformException, _invocationList[3]);
        }

        [Fact]
        public async Task AsyncVoid_HappyPath()
        {
            var businessClass = GetBusinessClass(ExceptionMode.DontThrow);

            //act
            await businessClass.SomethingImportantAsync();

            Assert.Equal(3, _invocationList.Count);
            Assert.Equal(Pre, _invocationList[0]);
            Assert.Equal(nameof(IBusinessClass.SomethingImportantAsync), _invocationList[1]);
            Assert.Equal(PostSuccess, _invocationList[2]);
        }

        [Fact]
        public async Task AsyncVoid_ExceptionBeforeAwait()
        {
            var businessClass = GetBusinessClass(ExceptionMode.ThrowBeforeAwait);

            //act
            await Assert.ThrowsAsync<MonkeyWrench>(() => businessClass.SomethingImportantAsync());

            Assert.Equal(4, _invocationList.Count);
            Assert.Equal(Pre, _invocationList[0]);
            Assert.Equal(nameof(IBusinessClass.SomethingImportantAsync), _invocationList[1]);
            Assert.Equal(PostError, _invocationList[2]);
            Assert.Equal(TransformException, _invocationList[3]);
        }

        [Fact]
        public async Task AsyncVoid_ExceptionAfterAwait()
        {
            var businessClass = GetBusinessClass(ExceptionMode.ThrowBeforeAwait);

            //act
            await Assert.ThrowsAsync<MonkeyWrench>(() => businessClass.SomethingImportantAsync());

            Assert.Equal(4, _invocationList.Count);
            Assert.Equal(Pre, _invocationList[0]);
            Assert.Equal(nameof(IBusinessClass.SomethingImportantAsync), _invocationList[1]);
            Assert.Equal(PostError, _invocationList[2]);
            Assert.Equal(TransformException, _invocationList[3]);
        }

        [Fact]
        public async Task AsyncResult_HappyPath()
        {
            var businessClass = GetBusinessClass(ExceptionMode.DontThrow);

            //act
            var result = await businessClass.FetchSomethingAsync();

            Assert.Equal(3, _invocationList.Count);
            Assert.Equal(Pre, _invocationList[0]);
            Assert.Equal(nameof(IBusinessClass.FetchSomethingAsync), _invocationList[1]);
            Assert.Equal(PostSuccess, _invocationList[2]);

            Assert.Same(ExpectedFetchedValue, result);
        }

        [Fact]
        public async Task AsyncResult_ExceptionBeforeAwait()
        {
            var businessClass = GetBusinessClass(ExceptionMode.ThrowBeforeAwait);

            //act
            await Assert.ThrowsAsync<MonkeyWrench>(() => businessClass.FetchSomethingAsync());

            Assert.Equal(4, _invocationList.Count);
            Assert.Equal(Pre, _invocationList[0]);
            Assert.Equal(nameof(IBusinessClass.FetchSomethingAsync), _invocationList[1]);
            Assert.Equal(PostError, _invocationList[2]);
            Assert.Equal(TransformException, _invocationList[3]);
        }

        [Fact]
        public async Task AsyncResult_ExceptionAfterAwait()
        {
            var businessClass = GetBusinessClass(ExceptionMode.ThrowAfterAwait);

            //act
            await Assert.ThrowsAsync<MonkeyWrench>(() => businessClass.FetchSomethingAsync());

            Assert.Equal(4, _invocationList.Count);
            Assert.Equal(Pre, _invocationList[0]);
            Assert.Equal(nameof(IBusinessClass.FetchSomethingAsync), _invocationList[1]);
            Assert.Equal(PostError, _invocationList[2]);
            Assert.Equal(TransformException, _invocationList[3]);
        }


    }
}
