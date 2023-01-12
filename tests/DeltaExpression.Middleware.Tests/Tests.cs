using Delta.Middleware;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Xunit;

namespace DeltaExpression.Middleware.Tests
{
    /// <summary>
    /// 添加。
    /// </summary>
    public class ServiceTypeInterceptAttribute : InterceptAttribute
    {
        /// <inheritdoc/>
        public override void Run(InterceptContext context, Intercept intercept)
        {
            intercept.Run(context);
        }

        /// <inheritdoc/>
        public override T Run<T>(InterceptContext context, Intercept<T> intercept)
        {
            return intercept.Run(context);
        }

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
        /// 记录。
        /// </summary>
        void Records();

        /// <summary>
        /// 加法。
        /// </summary>
        int Add(int i, ref int j);

        /// <summary>
        /// 异步记录。
        /// </summary>
        Task RecordsAsync();

        /// <summary>
        /// 异步加法。
        /// </summary>
        Task<int> AddAsync(int i, int j);
    }

    /// <summary>
    /// 服务类型。
    /// </summary>
    public class ServiceType : IServiceType
    {
        /// <summary>
        /// 记录。
        /// </summary>
        [ServiceTypeIntercept]
        public virtual void Records()
        {
        }

        /// <summary>
        /// 加法。
        /// </summary>
        [ServiceTypeIntercept]
        public virtual int Add(int i, ref int j) => i + j;

        /// <summary>
        /// 异步记录。
        /// </summary>
        [ServiceTypeIntercept]
        public virtual Task RecordsAsync() => Task.CompletedTask;

        /// <summary>
        /// 异步加法。
        /// </summary>
        [ServiceTypeIntercept]
        public virtual Task<int> AddAsync(int i, int j) => Task.FromResult(i + j);
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
    /// 泛型方法测试。
    /// </summary>
    public class ServiceGenericMethodType
    {
        /// <summary>
        /// 测试。
        /// </summary>
        /// <typeparam name="T">泛型。</typeparam>
        /// <returns>实例。</returns>
        [ServiceTypeIntercept]
        public virtual T Get<T>() where T : new() => new T();

        /// <summary>
        /// 测试。
        /// </summary>
        /// <typeparam name="T">泛型。</typeparam>
        /// <returns>实例。</returns>
        [ServiceTypeIntercept]
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
                .UseMiddleware();

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

            services.AddTransient<ServiceType>()
                .UseMiddleware();

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
                .UseMiddleware();

            var provider = services.BuildServiceProvider();

            var instance = provider.GetRequiredService<ServiceGenericMethodType>();

            int i = instance.Get<int>();

            Assert.True(i == 0);

            var serviceType = await instance.GetAsync<ServiceType>();

            Assert.False(serviceType is null);
        }
    }
}