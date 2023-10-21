using Inkslab.Intercept;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace DeltaExpression.AOP.Tests
{
    /// <summary>
    /// ��ӡ�
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
    /// ʵ�ֽӿڡ�
    /// </summary>
    public interface IServiceType
    {
        /// <summary>
        /// ��¼��
        /// </summary>
        [ServiceTypeIntercept]
        void Records();

        /// <summary>
        /// �ӷ���
        /// </summary>
        int Add(int i, ref int j);

        /// <summary>
        /// �첽��¼��
        /// </summary>
        Task RecordsAsync();

        /// <summary>
        /// �첽�ӷ���
        /// </summary>
        ValueTask<int> AddAsync(int i, int j);
    }

    /// <summary>
    /// �������͡�
    /// </summary>
    public class ServiceType : IServiceType
    {
        /// <summary>
        /// ��¼��
        /// </summary>
        [ServiceTypeIntercept]
        public virtual void Records()
        {
        }

        /// <summary>
        /// �ӷ���
        /// </summary>
        [ServiceTypeIntercept]
        public virtual int Add(int i, ref int j) => i + j;

        /// <summary>
        /// �첽��¼��
        /// </summary>
        [ServiceTypeIntercept]
        public virtual Task RecordsAsync() => Task.CompletedTask;

        /// <summary>
        /// �첽�ӷ���
        /// </summary>
        [ServiceTypeIntercept]
        public virtual ValueTask<int> AddAsync(int i, int j) => new ValueTask<int>(Task.FromResult(i + j));
    }

    /// <summary>
    /// ʵ�����͡�
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
    /// ���ͷ������ԡ�
    /// </summary>
    public class ServiceGenericMethodType
    {
        /// <summary>
        /// ���ԡ�
        /// </summary>
        /// <typeparam name="T">���͡�</typeparam>
        /// <returns>ʵ����</returns>
        [ServiceTypeIntercept]
        public virtual T Get<T>() where T : new() => new T();

        /// <summary>
        /// ���ԡ�
        /// </summary>
        /// <typeparam name="T">���͡�</typeparam>
        /// <returns>ʵ����</returns>
        [ServiceTypeIntercept]
        public virtual Task<T> GetAsync<T>() where T : new() => Task.FromResult(new T());
    }

    /// <summary>
    /// ʵ�����͡�
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
        /// ���ԡ�
        /// </summary>
        public override T Get<T>()
        {
            var invocation = new ServiceGenericMethodTypeOverride_Get<T>(this);

            return invocation.Get();
        }
    }

    /// <summary>
    /// ���͡�
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ServiceGenericMethodAndGenericType<T> where T : struct
    {
        /// <summary>
        /// ���ԡ�
        /// </summary>
        /// <returns>ʵ����</returns>
        [ServiceTypeIntercept]
        public virtual TResult Get<TResult>() where TResult : IEnumerable<T>, new() => new TResult();

        /// <summary>
        /// ���ԡ�
        /// </summary>
        /// <returns>ʵ����</returns>
        [ServiceTypeIntercept]
        public virtual Task<TResult> GetAsync<TResult>() where TResult : IEnumerable<T>, new() => Task.FromResult(new TResult());
    }

    /// <summary>
    /// ���͡�
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ServiceGenericType<T> where T : class, new()
    {
        /// <summary>
        /// ���ԡ�
        /// </summary>
        /// <returns>ʵ����</returns>
        [ServiceTypeIntercept]
        public virtual T Get() => new T();

        /// <summary>
        /// ���ԡ�
        /// </summary>
        /// <returns>ʵ����</returns>
        [ServiceTypeIntercept]
        public virtual Task<T> GetAsync() => Task.FromResult(new T());
    }

    /// <summary>
    /// ���ԡ�
    /// </summary>
    public class Tests
    {
        /// <summary>
        /// �������
        /// </summary>
        [Fact]
        public async Task ProxyIServiceType()
        {
            var services = new ServiceCollection();

            services.AddTransient<IServiceType, ServiceType>()
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
        /// �������
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
        /// �������
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
        /// �������
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

            Assert.True(i == 0);

            var serviceType = await instance.GetAsync<ServiceType>();

            Assert.False(serviceType is null);
        }

        /// <summary>
        /// ���ͷ���
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
        /// �����ҷ��ͷ�������
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
    }
}