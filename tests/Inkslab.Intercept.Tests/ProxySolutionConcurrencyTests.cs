using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Inkslab;
using Inkslab.Intercept;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

#pragma warning disable CS1591 // 测试类型不需要 XML 注释

namespace Inkslab.Intercept.Tests
{
    /// <summary>
    /// 验证 <see cref="ProxySolution"/> 在并发场景下：
    /// 1) 不会因为锁粒度优化（per-key Lazy）破坏正确性；
    /// 2) 同一服务类型多次代理仅生成一次代理类型（缓存命中）。
    /// </summary>
    public class ProxySolutionConcurrencyTests
    {
        public class ParallelInterceptAttribute : InterceptAttribute
        {
            public override void Run(InterceptContext context, Intercept intercept) => intercept.Run(context);
        }

        public interface IService1 { [ParallelIntercept] void Do(); }
        public interface IService2 { [ParallelIntercept] void Do(); }
        public interface IService3 { [ParallelIntercept] void Do(); }
        public interface IService4 { [ParallelIntercept] void Do(); }

        public class Service1 : IService1 { public void Do() { } }
        public class Service2 : IService2 { public void Do() { } }
        public class Service3 : IService3 { public void Do() { } }
        public class Service4 : IService4 { public void Do() { } }

        [Fact]
        public async Task Proxy_ConcurrentDifferentTypes_AllSucceed()
        {
            // 触发解决方案的并发代理生成路径，验证不同 (serviceType, implType)
            // 之间不再共享锁，可以并行完成。
            var moduleEmitter = new ModuleEmitter("Inkslab.Override.Concurrency");
            var solution = new ProxySolution(moduleEmitter);

            var services = new[]
            {
                new ServiceDescriptor(typeof(IService1), typeof(Service1), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IService2), typeof(Service2), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IService3), typeof(Service3), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IService4), typeof(Service4), ServiceLifetime.Transient),
            };

            var results = new ConcurrentBag<ServiceDescriptor>();

            var tasks = services.Select(d => Task.Run(() => results.Add(solution.Proxy(d)))).ToArray();
            await Task.WhenAll(tasks);

            Assert.Equal(services.Length, results.Count);
            // 每个描述符返回的代理实现类型必须不同（对应各自的服务）。
            Assert.Equal(services.Length, results.Select(r => r.ImplementationType).Distinct().Count());
        }

        [Fact]
        public async Task Proxy_ConcurrentSameType_GeneratesSinglePoxyType()
        {
            // 同一 (serviceType, implType) 在并发请求下，per-key Lazy
            // 必须保证只生成一次代理类型——所有请求拿到同一 ImplementationType。
            var moduleEmitter = new ModuleEmitter("Inkslab.Override.SameType");
            var solution = new ProxySolution(moduleEmitter);

            const int taskCount = 16;
            var bag = new ConcurrentBag<Type>();

            var tasks = Enumerable.Range(0, taskCount).Select(_ => Task.Run(() =>
            {
                var d = new ServiceDescriptor(typeof(IService1), typeof(Service1), ServiceLifetime.Transient);
                var proxied = solution.Proxy(d);
                bag.Add(proxied.ImplementationType);
            })).ToArray();

            await Task.WhenAll(tasks);

            Assert.Equal(taskCount, bag.Count);
            Assert.Single(bag.Distinct());
        }
    }
}
