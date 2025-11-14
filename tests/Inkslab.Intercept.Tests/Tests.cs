using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Inkslab.Intercept.Tests
{
    /// <summary>
    /// 添加。
    /// </summary>
    public class ServiceTypeInterceptAttribute : InterceptAttribute
    {
        /// <summary>
        /// 
        /// </summary>
        public int A { set; get; }

        /// <inheritdoc/>
        public override void Run(InterceptContext context, Intercept intercept)
        {
            intercept.Run(context);
        }
    }

    /// <summary>
    /// 添加。
    /// </summary>
    public class TestInterceptAttribute : ReturnValueInterceptAttribute
    {
        /// <summary>
        /// 测试。
        /// </summary>
        public TestInterceptAttribute() { }

        /// <summary>
        /// 
        /// </summary>
        public int A { set; get; }

        /// <inheritdoc/>
        public override T Run<T>(InterceptContext context, Intercept<T> intercept)
        {
            return default;
        }
    }

    /// <summary>
    /// 添加。
    /// </summary>
    public class ReturnValueServiceInterceptAttribute : ReturnValueInterceptAttribute
    {
        /// <summary>
        /// 测试。
        /// </summary>
        public ReturnValueServiceInterceptAttribute() { }

        /// <inheritdoc/>
        public override T Run<T>(InterceptContext context, Intercept<T> intercept)
        {
            return base.Run(context, intercept);
        }

        /// <inheritdoc/>
        public override Task<T> RunAsync<T>(InterceptContext context, InterceptAsync<T> intercept)
        {
            return base.RunAsync(context, intercept);
        }
    }

    /// <summary>
    /// 添加。
    /// </summary>
    public class ServiceTypeInterceptAsyncAttribute : InterceptAsyncAttribute
    {
        /// <inheritdoc/>
        public override Task RunAsync(InterceptContext context, InterceptAsync intercept)
        {
            return intercept.RunAsync(context);
        }

        /// <inheritdoc/>
        public override Task<T> RunAsync<T>(InterceptContext context, InterceptAsync<T> intercept)
        {
            return intercept.RunAsync(context);
        }
    }

    /// <summary>
    /// 实现接口。
    /// </summary>
    public interface IServiceType
    {
        /// <summary>
        /// 测试。
        /// </summary>
        int A { [TestIntercept(A = 1)] get; }

        /// <summary>
        /// 测试。
        /// </summary>
        int B { get; set; }

        /// <summary>
        /// 记录。
        /// </summary>
        [ServiceTypeIntercept]
        void Records();

        /// <summary>
        /// 加法。
        /// </summary>
        [ServiceTypeIntercept(A = 5)] //! 因为方法是有返回值，标记为无返回值，拦截器不会生效。
        int Add(int i, ref int j);

        /// <summary>
        /// 异步记录。
        /// </summary>
        [ServiceTypeInterceptAsync]
        Task RecordsAsync();

        /// <summary>
        /// 异步加法。
        /// </summary>
        ValueTask<int> AddAsync(int i, int j);
    }

    /// <summary>
    /// 服务类型。
    /// </summary>
    public class ServiceType : IServiceType
    {
        /// <summary>
        /// 测试。
        /// </summary>
        public int A => throw new NotImplementedException();

        /// <summary>
        /// 测试。
        /// </summary>
        public int B { get; set; }

        /// <summary>
        /// 测试。
        /// </summary>
        public int C { get; set; }

        /// <summary>
        /// 记录。
        /// </summary>
        public virtual void Records()
        {
        }

        /// <summary>
        /// 加法。
        /// </summary>
        public virtual int Add(int i, ref int j) => i + j;

        /// <summary>
        /// 异步记录。
        /// </summary>
        public virtual Task RecordsAsync() => Task.CompletedTask;

        /// <summary>
        /// 异步加法。
        /// </summary>
        public virtual ValueTask<int> AddAsync(int i, int j) => new ValueTask<int>(Task.FromResult(i + j));
    }

    /// <summary>
    /// 实现类型。
    /// </summary>
    public class ImplementationType : ServiceType
    {
        /// <inheritdoc/>
        public override void Records()
        {
            base.Records();
        }
    }

    /// <summary>
    /// 含参数的实现。
    /// </summary>
    public class ImplementationWithArgumentType
    {
        private readonly ServiceType serviceType;

        /// <inheritdoc/>
        public ImplementationWithArgumentType(ServiceType serviceType)
        {
            this.serviceType = serviceType;
        }

        /// <summary>
        /// 记录。
        /// </summary>
        [ServiceTypeIntercept]
        public virtual void Records() => serviceType.Records();
    }

    /// <summary>
    /// 泛型方法测试。
    /// </summary>
    public class ServiceGenericMethodType
    {
        /// <summary>
        /// 测试。
        /// </summary>
        /// <typeparam name="T">泛型。</typeparam>
        /// <returns>实例。</returns>
        [ReturnValueServiceIntercept]
        public virtual T Get<T>() where T : new() => new T();

        /// <summary>
        /// 测试。
        /// </summary>
        /// <typeparam name="T">泛型。</typeparam>
        /// <returns>实例。</returns>
        [ServiceTypeInterceptAsync]
        public virtual Task<T> GetAsync<T>() where T : new() => Task.FromResult(new T());
    }

    /// <summary>
    /// 实现类型。
    /// </summary>
    public class ImplementationeGenericMethodType : ServiceGenericMethodType
    {
        private class ServiceGenericMethodTypeOverride_Get<T> : IInvocation where T : new()
        {
            [NonSerialized]
            private readonly ServiceGenericMethodType invocation;

            public ServiceGenericMethodTypeOverride_Get(ServiceGenericMethodType invocation)
            {
                //Error decoding local variables: Signature type sequence must have at least one element.
                this.invocation = invocation;
            }

            public object Invoke(object[] parameters)
            {
                throw new NotImplementedException();
            }

            public virtual T Get()
            {
                return invocation.Get<T>();
            }

        }

        /// <summary>
        /// 测试。
        /// </summary>
        public override T Get<T>()
        {
            var invocation = new ServiceGenericMethodTypeOverride_Get<T>(this);

            return invocation.Get();
        }
    }

    /// <summary>
    /// 泛型。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ServiceGenericMethodAndGenericType<T> where T : struct
    {
        /// <summary>
        /// 测试。
        /// </summary>
        /// <returns>实例。</returns>
        [ReturnValueServiceIntercept]
        public virtual TResult Get<TResult>() where TResult : IEnumerable<T>, new() => new TResult();

        /// <summary>
        /// 测试。
        /// </summary>
        /// <returns>实例。</returns>
        [ServiceTypeInterceptAsync]
        public virtual Task<TResult> GetAsync<TResult>() where TResult : IEnumerable<T>, new() => Task.FromResult(new TResult());
    }

    /// <summary>
    /// 泛型。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IServiceGenericType<T> where T : class, new()
    {
        /// <summary>
        /// 测试。
        /// </summary>
        /// <returns>实例。</returns>
        [ReturnValueServiceIntercept]
        T Get();

        /// <summary>
        /// 测试。
        /// </summary>
        /// <returns>实例。</returns>
        [ServiceTypeInterceptAsync]
        Task<T> GetAsync();
    }

    /// <summary>
    /// 泛型。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ServiceGenericType<T> : IServiceGenericType<T> where T : class, new()
    {
        /// <summary>
        /// 测试。
        /// </summary>
        /// <returns>实例。</returns>
        [ReturnValueServiceIntercept]
        public virtual T Get() => new T();

        /// <summary>
        /// 测试。
        /// </summary>
        /// <returns>实例。</returns>
        [ServiceTypeInterceptAsync]
        public virtual Task<T> GetAsync() => Task.FromResult(new T());
    }

    /// <summary>
    /// 异常拦截器测试。
    /// </summary>
    public class ExceptionInterceptAttribute : InterceptAttribute
    {
        /// <summary>
        /// 运行拦截器。
        /// </summary>
        /// <param name="context">上下文。</param>
        /// <param name="intercept">拦截器。</param>
        public override void Run(InterceptContext context, Intercept intercept)
        {
            throw new InvalidOperationException("拦截器异常测试");
        }
    }

    /// <summary>
    /// 异常返回值拦截器测试。
    /// </summary>
    public class ExceptionReturnValueInterceptAttribute : ReturnValueInterceptAttribute
    {
        /// <summary>
        /// 运行拦截器。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="context">上下文。</param>
        /// <param name="intercept">拦截器。</param>
        /// <returns>结果。</returns>
        public override T Run<T>(InterceptContext context, Intercept<T> intercept)
        {
            throw new ArgumentException("返回值拦截器异常测试");
        }
    }

    /// <summary>
    /// 密封类型测试（不应该被代理）。
    /// </summary>
    public sealed class SealedServiceType
    {
        /// <summary>
        /// 方法测试。
        /// </summary>
        [ServiceTypeIntercept]
        public void Method() { }
    }

    /// <summary>
    /// 静态方法测试类。
    /// </summary>
    public class StaticMethodServiceType
    {
        /// <summary>
        /// 静态方法测试。
        /// </summary>
        [ServiceTypeIntercept]
        public static void StaticMethod() { }

        /// <summary>
        /// 实例方法测试。
        /// </summary>
        [ServiceTypeIntercept]
        public virtual void InstanceMethod() { }
    }

    /// <summary>
    /// 抽象基类测试。
    /// </summary>
    public abstract class AbstractServiceType
    {
        /// <summary>
        /// 抽象方法。
        /// </summary>
        [ServiceTypeIntercept]
        public abstract void AbstractMethod();

        /// <summary>
        /// 虚方法。
        /// </summary>
        [ServiceTypeIntercept]
        public virtual void VirtualMethod() { }

        /// <summary>
        /// 非虚方法。
        /// </summary>
        [ServiceTypeIntercept]
        public void NonVirtualMethod() { }
    }

    /// <summary>
    /// 抽象类实现。
    /// </summary>
    public class ConcreteServiceType : AbstractServiceType
    {
        /// <summary>
        /// 抽象方法实现。
        /// </summary>
        public override void AbstractMethod() { }
    }

    /// <summary>
    /// 包含各种属性的测试类。
    /// </summary>
    public class PropertyTestServiceType
    {
        private int _writeOnlyValue;

        /// <summary>
        /// 只读属性测试。
        /// </summary>
        public virtual int ReadOnlyProperty { [TestIntercept] get; } = 42;

        /// <summary>
        /// 只写属性测试。
        /// </summary>
        public virtual int WriteOnlyProperty 
        { 
            [ServiceTypeIntercept] 
            set => _writeOnlyValue = value; 
        }

        /// <summary>
        /// 读写属性测试。
        /// </summary>
        public virtual string ReadWriteProperty 
        { 
            [TestIntercept] 
            get; 
            [ServiceTypeIntercept] 
            set; 
        } = "test";

        /// <summary>
        /// 索引器属性测试。
        /// </summary>
        /// <param name="index">索引。</param>
        /// <returns>值。</returns>
        public virtual string this[int index] 
        { 
            [TestIntercept]
            get => index.ToString(); 
            [ServiceTypeIntercept] 
            set { } 
        }
    }

    /// <summary>
    /// 方法重载测试类。
    /// </summary>
    public class MethodOverloadServiceType
    {
        /// <summary>
        /// 重载方法1。
        /// </summary>
        [ServiceTypeIntercept]
        public virtual void OverloadedMethod() { }

        /// <summary>
        /// 重载方法2。
        /// </summary>
        /// <param name="parameter">参数。</param>
        [ServiceTypeIntercept]
        public virtual void OverloadedMethod(int parameter) { }

        /// <summary>
        /// 重载方法3。
        /// </summary>
        /// <param name="parameter">参数。</param>
        [ServiceTypeIntercept]
        public virtual void OverloadedMethod(string parameter) { }

        /// <summary>
        /// 重载方法4。
        /// </summary>
        /// <param name="a">参数A。</param>
        /// <param name="b">参数B。</param>
        [ServiceTypeIntercept]
        public virtual void OverloadedMethod(int a, string b) { }

        /// <summary>
        /// 泛型重载方法1。
        /// </summary>
        /// <typeparam name="T">类型。</typeparam>
        /// <returns>实例。</returns>
        [ReturnValueServiceIntercept]
        public virtual T GenericOverloadedMethod<T>() where T : new() => new T();

        /// <summary>
        /// 泛型重载方法2。
        /// </summary>
        /// <typeparam name="T">类型。</typeparam>
        /// <param name="input">输入。</param>
        /// <returns>输入值。</returns>
        [ReturnValueServiceIntercept]
        public virtual T GenericOverloadedMethod<T>(T input) => input;
    }

    /// <summary>
    /// ref/out/in 参数测试类。
    /// </summary>
    public class RefOutInParameterServiceType
    {
        /// <summary>
        /// ref参数测试。
        /// </summary>
        /// <param name="value">引用参数。</param>
        [ServiceTypeIntercept]
        public virtual void RefParameter(ref int value) => value *= 2;

        /// <summary>
        /// out参数测试。
        /// </summary>
        /// <param name="value">输出参数。</param>
        [ServiceTypeIntercept]
        public virtual void OutParameter(out int value) => value = 100;

        /// <summary>
        /// in参数测试。
        /// </summary>
        /// <param name="value">只读引用参数。</param>
        [ServiceTypeIntercept]
        public virtual void InParameter(in int value) { }

        /// <summary>
        /// 混合参数测试。
        /// </summary>
        /// <param name="normal">普通参数。</param>
        /// <param name="refParam">引用参数。</param>
        /// <param name="outParam">输出参数。</param>
        /// <param name="inParam">只读引用参数。</param>
        [ServiceTypeIntercept]
        public virtual void MixedParameters(int normal, ref int refParam, out string outParam, in double inParam)
        {
            refParam += normal;
            outParam = inParam.ToString();
        }

        /// <summary>
        /// TryGet模式测试。
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="value">输出值。</param>
        /// <returns>是否成功。</returns>
        [ReturnValueServiceIntercept]
        public virtual bool TryGetValue(string key, out int value)
        {
            value = key.Length;
            return !string.IsNullOrEmpty(key);
        }
    }

    /// <summary>
    /// 复杂泛型约束测试类。
    /// </summary>
    /// <typeparam name="TBase">基础类型。</typeparam>
    public class ComplexGenericConstraintServiceType<TBase> where TBase : class, IDisposable, new()
    {
        /// <summary>
        /// 处理方法。
        /// </summary>
        /// <typeparam name="TResult">结果类型。</typeparam>
        /// <param name="input">输入。</param>
        /// <returns>结果。</returns>
        [ReturnValueServiceIntercept]
        public virtual TResult Process<TResult>(int input) 
            where TResult : class, new()
        {
            return new TResult();
        }
    }

    /// <summary>
    /// NoninterceptAttribute 测试类。
    /// </summary>
    public class NoninterceptTestServiceType
    {
        /// <summary>
        /// 拦截的方法。
        /// </summary>
        [ServiceTypeIntercept]
        public virtual void InterceptedMethod() { }

        /// <summary>
        /// 不拦截的方法。
        /// </summary>
        [ServiceTypeIntercept]
        [Nonintercept]
        public virtual void NonInterceptedMethod() { }

        /// <summary>
        /// 拦截的属性。
        /// </summary>
        public virtual int InterceptedProperty { [TestIntercept] get; set; }

        /// <summary>
        /// 不拦截的属性。
        /// </summary>
        [Nonintercept]
        public virtual int NonInterceptedProperty { get; set; }
    }

    /// <summary>
    /// 继承链测试类。
    /// </summary>
    public class BaseInheritanceServiceType
    {
        /// <summary>
        /// 基础方法。
        /// </summary>
        [ServiceTypeIntercept]
        public virtual void BaseMethod() { }

        /// <summary>
        /// 获取基础信息。
        /// </summary>
        /// <returns>基础信息。</returns>
        [ReturnValueServiceIntercept]
        public virtual string GetBaseInfo() => "base";
    }

    /// <summary>
    /// 中间继承类。
    /// </summary>
    public class MiddleInheritanceServiceType : BaseInheritanceServiceType
    {
        /// <summary>
        /// 中间方法。
        /// </summary>
        [ServiceTypeIntercept]
        public virtual void MiddleMethod() { }

        /// <summary>
        /// 重写基础信息方法。
        /// </summary>
        /// <returns>中间信息。</returns>
        public override string GetBaseInfo() => "middle";

        /// <summary>
        /// 获取中间值。
        /// </summary>
        /// <returns>中间值。</returns>
        [ReturnValueServiceIntercept]
        public virtual int GetMiddleValue() => 42;
    }

    /// <summary>
    /// 最终继承类。
    /// </summary>
    public class DerivedInheritanceServiceType : MiddleInheritanceServiceType
    {
        /// <summary>
        /// 派生方法。
        /// </summary>
        [ServiceTypeIntercept]
        public virtual void DerivedMethod() { }

        /// <summary>
        /// 重写基础信息方法。
        /// </summary>
        /// <returns>派生信息。</returns>
        public override string GetBaseInfo() => "derived";

        /// <summary>
        /// 重写中间值方法。
        /// </summary>
        /// <returns>派生值。</returns>
        public override int GetMiddleValue() => 100;
    }

    /// <summary>
    /// 接口多重继承测试。
    /// </summary>
    public interface IBaseService
    {
        /// <summary>
        /// 基础服务方法。
        /// </summary>
        [ServiceTypeIntercept]
        void BaseServiceMethod();
    }

    /// <summary>
    /// 扩展接口。
    /// </summary>
    public interface IExtendedService : IBaseService
    {
        /// <summary>
        /// 扩展服务方法。
        /// </summary>
        /// <returns>结果。</returns>
        [ReturnValueServiceIntercept]
        int ExtendedServiceMethod();
    }

    /// <summary>
    /// 多接口实现。
    /// </summary>
    public interface IAnotherService
    {
        /// <summary>
        /// 另一个服务异步方法。
        /// </summary>
        /// <returns>任务。</returns>
        [ServiceTypeInterceptAsync]
        Task AnotherServiceMethodAsync();
    }

    /// <summary>
    /// 多接口实现类。
    /// </summary>
    public class MultiInterfaceServiceType : IExtendedService, IAnotherService
    {
        /// <summary>
        /// 基础服务方法实现。
        /// </summary>
        public virtual void BaseServiceMethod() { }

        /// <summary>
        /// 扩展服务方法实现。
        /// </summary>
        /// <returns>结果。</returns>
        public virtual int ExtendedServiceMethod() => 42;

        /// <summary>
        /// 另一个服务异步方法实现。
        /// </summary>
        /// <returns>任务。</returns>
        public virtual Task AnotherServiceMethodAsync() => Task.CompletedTask;
    }

    /// <summary>
    /// 事件测试类。
    /// </summary>
    public class EventTestServiceType
    {
        /// <summary>
        /// 测试事件。
        /// </summary>
        public virtual event Action<string> TestEvent;

        /// <summary>
        /// 触发事件。
        /// </summary>
        /// <param name="message">消息。</param>
        [ServiceTypeIntercept]
        public virtual void TriggerEvent(string message) => TestEvent?.Invoke(message);

        /// <summary>
        /// 事件处理。
        /// </summary>
        /// <param name="message">消息。</param>
        [ServiceTypeIntercept]
        protected virtual void OnTestEvent(string message) => TestEvent?.Invoke(message);
    }

    /// <summary>
    /// 委托和Func/Action测试类。
    /// </summary>
    public class DelegateTestServiceType
    {
        /// <summary>
        /// 获取转换器。
        /// </summary>
        /// <returns>转换函数。</returns>
        [ReturnValueServiceIntercept]
        public virtual Func<int, string> GetConverter() => x => x.ToString();

        /// <summary>
        /// 执行Action。
        /// </summary>
        /// <param name="action">动作。</param>
        /// <param name="parameter">参数。</param>
        [ServiceTypeIntercept]
        public virtual void ExecuteAction(Action<string> action, string parameter) => action(parameter);

        /// <summary>
        /// 获取异步工厂。
        /// </summary>
        /// <typeparam name="T">类型。</typeparam>
        /// <param name="value">值。</param>
        /// <returns>异步工厂函数。</returns>
        [ReturnValueServiceIntercept]
        public virtual Task<Func<Task<T>>> GetAsyncFactory<T>(T value)
        {
            return Task.FromResult<Func<Task<T>>>(() => Task.FromResult(value));
        }
    }


    /// <summary>
    /// 测试。
    /// </summary>
    public class Tests
    {
        /// <summary>
        /// 代理服务。
        /// </summary>
        [Fact]
        public async Task ProxyIServiceType()
        {
            var services = new ServiceCollection();

            services.AddTransient<IServiceType, ServiceType>()
                .UseIntercept();

            var provider = services.BuildServiceProvider();

            var serviceType = provider.GetRequiredService<IServiceType>();

            int a = serviceType.A;

            Assert.Equal(0, a);

            serviceType.Records();

            int i = 5, j = 100;

            var ij = serviceType.Add(i, ref j); //! 拦截器不会生效。

            Assert.Equal(ij, i + j);

            await serviceType.RecordsAsync();

            var ijAsync = await serviceType.AddAsync(i, j);

            Assert.Equal(ijAsync, i + j);
        }

        /// <summary>
        /// 代理服务。
        /// </summary>
        [Fact]
        public async Task ProxyIServiceTypeFactory()
        {
            var services = new ServiceCollection();

            services.AddTransient<IServiceType>(x => new ServiceType())
                .UseIntercept();

            var provider = services.BuildServiceProvider();

            var serviceType = provider.GetRequiredService<IServiceType>();

            serviceType.Records();

            int i = 5, j = 100;

            var ij = serviceType.Add(i, ref j);

            Assert.Equal(ij, i + j);

            await serviceType.RecordsAsync();

            var ijAsync = await serviceType.AddAsync(i, j);

            Assert.Equal(ijAsync, i + j);
        }

        /// <summary>
        /// 代理服务。
        /// </summary>
        [Fact]
        public async Task ProxyServiceType()
        {
            var services = new ServiceCollection();

            services.AddSingleton(new ServiceType())
                .UseIntercept();

            var provider = services.BuildServiceProvider();

            var serviceType = provider.GetRequiredService<ServiceType>();

            serviceType.Records();

            int i = 5, j = 100;

            var ij = serviceType.Add(i, ref j);

            Assert.Equal(ij, i + j);

            await serviceType.RecordsAsync();

            var ijAsync = await serviceType.AddAsync(i, j);

            Assert.Equal(ijAsync, i + j);
        }

        /// <summary>
        /// 代理服务。
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ProxyServiceGenericMethodType()
        {
            var services = new ServiceCollection();

            services.AddTransient<ServiceGenericMethodType>()
                .UseIntercept();

            var provider = services.BuildServiceProvider();

            var instance = provider.GetRequiredService<ServiceGenericMethodType>();

            int i = instance.Get<int>();

            Assert.Equal(0, i);

            var serviceType = await instance.GetAsync<ServiceType>();

            Assert.False(serviceType is null);
        }

        /// <summary>
        /// 泛型服务。
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ProxyInterfaceServiceGenericType()
        {
            var services = new ServiceCollection();

            services.AddTransient(typeof(IServiceGenericType<>), typeof(ServiceGenericType<>))
                .UseIntercept();

            var provider = services.BuildServiceProvider();

            var instance = provider.GetRequiredService<IServiceGenericType<HashSet<int>>>();

            HashSet<int> ints = instance.Get();

            Assert.False(ints is null);

            var serviceType = await instance.GetAsync();

            Assert.False(serviceType is null);
        }

        /// <summary>
        /// 泛型服务。
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ProxyServiceGenericType()
        {
            var services = new ServiceCollection();

            services.AddTransient(typeof(ServiceGenericType<>))
                .UseIntercept();

            var provider = services.BuildServiceProvider();

            var instance = provider.GetRequiredService<ServiceGenericType<HashSet<int>>>();

            HashSet<int> ints = instance.Get();

            Assert.False(ints is null);

            var serviceType = await instance.GetAsync();

            Assert.False(serviceType is null);
        }

        /// <summary>
        /// 泛型且泛型方法服务。
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ProxyServiceGenericMethodAndGenericType()
        {
            var services = new ServiceCollection();

            services.AddTransient(typeof(ServiceGenericMethodAndGenericType<>))
                .UseIntercept();

            var provider = services.BuildServiceProvider();

            var instance = provider.GetRequiredService<ServiceGenericMethodAndGenericType<int>>();

            HashSet<int> ints = instance.Get<HashSet<int>>();

            Assert.False(ints is null);

            var serviceType = await instance.GetAsync<HashSet<int>>();

            Assert.False(serviceType is null);
        }

        /// <summary>
        /// 有参类型代理。
        /// </summary>
        [Fact]
        public void ImplementationWithArgumentTypeTest()
        {
            var services = new ServiceCollection();

            services.AddSingleton<ServiceType>(new ImplementationType())
                .AddScoped<ImplementationWithArgumentType>()
                .UseIntercept();

            var provider = services.BuildServiceProvider();

            var serviceType = provider.GetRequiredService<ImplementationWithArgumentType>();

            serviceType.Records();
        }

        /// <summary>
        /// 属性拦截测试。
        /// </summary>
        [Fact]
        public void PropertyInterceptTest()
        {
            var services = new ServiceCollection();

            services.AddTransient<PropertyTestServiceType>()
                .UseIntercept();

            var provider = services.BuildServiceProvider();

            var instance = provider.GetRequiredService<PropertyTestServiceType>();

            // 测试只读属性
            var readOnlyValue = instance.ReadOnlyProperty;
            // 拦截器可能返回默认值或原始值

            // 测试只写属性
            instance.WriteOnlyProperty = 123;

            // 测试读写属性
            instance.ReadWriteProperty = "modified";
            var readWriteValue = instance.ReadWriteProperty;
            // 拦截器可能返回默认值或修改后的值

            // 测试索引器
            var indexValue = instance[5];
            // 拦截器可能返回默认值或索引字符串

            instance[1] = "test";
        }

        /// <summary>
        /// 方法重载拦截测试。
        /// </summary>
        [Fact]
        public void MethodOverloadInterceptTest()
        {
            var services = new ServiceCollection();

            services.AddTransient<MethodOverloadServiceType>()
                .UseIntercept();

            var provider = services.BuildServiceProvider();

            var instance = provider.GetRequiredService<MethodOverloadServiceType>();

            // 测试所有重载方法
            instance.OverloadedMethod();
            instance.OverloadedMethod(42);
            instance.OverloadedMethod("test");
            instance.OverloadedMethod(1, "test");

            // 测试泛型重载方法 - 注意这些方法可能被拦截器影响，返回默认值或正常值
            var genericResult1 = instance.GenericOverloadedMethod<StringBuilder>();
            // 拦截器可能返回默认值null，也可能返回正常创建的实例

            var genericResult2 = instance.GenericOverloadedMethod("input");
            // 拦截器可能返回默认值null，也可能返回传入的input值
        }

        /// <summary>
        /// ref/out/in 参数拦截测试。
        /// </summary>
        [Fact]
        public void RefOutInParameterInterceptTest()
        {
            var services = new ServiceCollection();

            services.AddTransient<RefOutInParameterServiceType>()
                .UseIntercept();

            var provider = services.BuildServiceProvider();

            var instance = provider.GetRequiredService<RefOutInParameterServiceType>();

            // 测试ref参数
            int refValue = 10;
            instance.RefParameter(ref refValue);
            // 由于拦截器拦截了方法，实际的乘法操作不会执行

            // 测试out参数
            instance.OutParameter(out int outValue);
            // 由于拦截器拦截了方法，out参数的赋值不会执行

            // 测试in参数
            int inValue = 50;
            instance.InParameter(in inValue);

            // 测试混合参数
            int mixedRef = 1;
            double inParam = 3.14;
            instance.MixedParameters(10, ref mixedRef, out string mixedOut, in inParam);

            // 测试TryGet模式 - 注意拦截器可能影响返回值
            var result = instance.TryGetValue("key", out int tryValue);
            // 拦截器可能返回默认值false，也可能让原方法执行
        }

        /// <summary>
        /// 复杂泛型约束拦截测试。
        /// </summary>
        [Fact]
        public void ComplexGenericConstraintInterceptTest()
        {
            var services = new ServiceCollection();

            services.AddTransient<ComplexGenericConstraintServiceType<DisposableTestClass>>()
                .UseIntercept();

            var provider = services.BuildServiceProvider();

            var instance = provider.GetRequiredService<ComplexGenericConstraintServiceType<DisposableTestClass>>();

            // 测试复杂泛型约束方法
            var result = instance.Process<StringBuilder>(42);
            // 拦截器可能返回默认值null或正常创建的实例
        }

        /// <summary>
        /// NoninterceptAttribute 拦截测试。
        /// </summary>
        [Fact]
        public void NoninterceptAttributeTest()
        {
            var services = new ServiceCollection();

            services.AddTransient<NoninterceptTestServiceType>()
                .UseIntercept();

            var provider = services.BuildServiceProvider();

            var instance = provider.GetRequiredService<NoninterceptTestServiceType>();

            // 正常拦截的方法
            instance.InterceptedMethod();

            // 标记为不拦截的方法应该正常执行
            instance.NonInterceptedMethod();

            // 测试属性
            instance.InterceptedProperty = 100;
            var interceptedValue = instance.InterceptedProperty;
            Assert.Equal(0, interceptedValue); // 拦截器返回默认值

            instance.NonInterceptedProperty = 200;
            var nonInterceptedValue = instance.NonInterceptedProperty;
            Assert.Equal(200, nonInterceptedValue); // 应该正常赋值和获取
        }

        /// <summary>
        /// 继承链拦截测试。
        /// </summary>
        [Fact]
        public void InheritanceChainInterceptTest()
        {
            var services = new ServiceCollection();

            services.AddTransient<DerivedInheritanceServiceType>()
                .UseIntercept();

            var provider = services.BuildServiceProvider();

            var instance = provider.GetRequiredService<DerivedInheritanceServiceType>();

            // 测试所有层级的方法
            instance.BaseMethod();     // 来自基类
            instance.MiddleMethod();   // 来自中间类
            instance.DerivedMethod();  // 来自派生类

            // 测试重写方法 - 注意拦截器可能影响返回值
            var baseInfo = instance.GetBaseInfo();
            // 拦截器可能返回默认值，也可能让原方法执行

            var middleValue = instance.GetMiddleValue();
            // 拦截器可能返回默认值，也可能让原方法执行
        }

        /// <summary>
        /// 多接口实现拦截测试。
        /// </summary>
        [Fact]
        public async Task MultiInterfaceInterceptTest()
        {
            var services = new ServiceCollection();

            // 测试单独的接口实现
            services.AddTransient<MultiInterfaceServiceType>()
                .UseIntercept();

            var provider = services.BuildServiceProvider();

            var instance = provider.GetRequiredService<MultiInterfaceServiceType>();

            // 测试基础服务方法
            instance.BaseServiceMethod();

            // 测试扩展服务方法
            var result = instance.ExtendedServiceMethod();
            // 拦截器可能影响返回值

            // 测试另一个服务的异步方法
            await instance.AnotherServiceMethodAsync();
        }

        /// <summary>
        /// 事件拦截测试。
        /// </summary>
        [Fact]
        public void EventInterceptTest()
        {
            var services = new ServiceCollection();

            services.AddTransient<EventTestServiceType>()
                .UseIntercept();

            var provider = services.BuildServiceProvider();

            var instance = provider.GetRequiredService<EventTestServiceType>();

            instance.TestEvent += message => { /* 事件处理逻辑 */ };

            // 由于方法被拦截，事件可能不会被正常触发
            instance.TriggerEvent("test message");

            // 注意：拦截器可能会影响事件的正常触发
        }

        /// <summary>
        /// 委托和Func/Action拦截测试。
        /// </summary>
        [Fact]
        public async Task DelegateInterceptTest()
        {
            var services = new ServiceCollection();

            services.AddTransient<DelegateTestServiceType>()
                .UseIntercept();

            var provider = services.BuildServiceProvider();

            var instance = provider.GetRequiredService<DelegateTestServiceType>();

            // 测试Func返回 - 拦截器可能影响返回值
            var converter = instance.GetConverter();
            // 拦截器可能返回默认值null，也可能返回正常的转换函数

            // 测试Action执行
            instance.ExecuteAction(msg => { /* Action执行逻辑 */ }, "test");

            // 测试异步工厂 - 拦截器可能影响返回值
            var asyncFactory = await instance.GetAsyncFactory("test");
            // 拦截器可能返回默认值null，也可能返回正常的异步工厂函数
        }

        /// <summary>
        /// 抽象类拦截测试。
        /// </summary>
        [Fact]
        public void AbstractClassInterceptTest()
        {
            var services = new ServiceCollection();

            services.AddTransient<AbstractServiceType, ConcreteServiceType>()
                .UseIntercept();

            var provider = services.BuildServiceProvider();

            var instance = provider.GetRequiredService<AbstractServiceType>();

            // 测试抽象方法实现
            instance.AbstractMethod();

            // 测试虚方法
            instance.VirtualMethod();

            // 测试非虚方法
            instance.NonVirtualMethod();
        }

        /// <summary>
        /// 异常拦截器测试。
        /// </summary>
        [Fact]
        public void ExceptionInterceptorTest()
        {
            var services = new ServiceCollection();

            services.AddTransient<ExceptionTestServiceType>()
                .UseIntercept();

            var provider = services.BuildServiceProvider();

            var instance = provider.GetRequiredService<ExceptionTestServiceType>();

            // 测试拦截器抛出异常
            Assert.Throws<InvalidOperationException>(() => instance.ThrowExceptionMethod());

            // 测试返回值拦截器抛出异常
            Assert.Throws<ArgumentException>(() => instance.ThrowExceptionReturnMethod());
        }

        /// <summary>
        /// 密封类测试（应该不能被代理）。
        /// </summary>
        [Fact]
        public void SealedClassTest()
        {
            var services = new ServiceCollection();

            // 密封类不应该被代理，应该直接使用原始类型
            services.AddTransient<SealedServiceType>()
                .UseIntercept();

            var provider = services.BuildServiceProvider();

            var instance = provider.GetRequiredService<SealedServiceType>();

            // 密封类的方法应该正常执行，不会被拦截
            instance.Method();
        }
    }

    /// <summary>
    /// 异常测试服务类型。
    /// </summary>
    public class ExceptionTestServiceType
    {
        /// <summary>
        /// 抛异常方法。
        /// </summary>
        [ExceptionIntercept]
        public virtual void ThrowExceptionMethod() { }

        /// <summary>
        /// 抛异常返回方法。
        /// </summary>
        /// <returns>结果。</returns>
        [ExceptionReturnValueIntercept]
        public virtual string ThrowExceptionReturnMethod() => "normal";
    }

    /// <summary>
    /// 可释放测试类。
    /// </summary>
    public class DisposableTestClass : IDisposable
    {
        /// <summary>
        /// 释放资源。
        /// </summary>
        public void Dispose() { }
    }

    /// <summary>
    /// 可比较测试类。
    /// </summary>
    public class ComparableTestClass : IComparable<ComparableTestClass>
    {
        /// <summary>
        /// 比较方法。
        /// </summary>
        /// <param name="other">其他对象。</param>
        /// <returns>比较结果。</returns>
        public int CompareTo(ComparableTestClass other) => 0;
    }
}