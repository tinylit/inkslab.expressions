using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Inkslab.Intercept.Tests
{
    // ═══════════════════════════════════════════════════════════════════════
    // 公共拦截器（本文件复用）
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 直传型返回值拦截器（透明代理，不改变方法行为）。
    /// </summary>
    public class PassThroughInterceptAttribute : ReturnValueInterceptAttribute
    {
        /// <inheritdoc/>
        public override T Run<T>(InterceptContext context, Intercept<T> intercept)
            => intercept.Run(context);
    }

    /// <summary>
    /// 直传型 void 拦截器（透明代理，不改变方法行为）。
    /// </summary>
    public class PassThroughVoidInterceptAttribute : InterceptAttribute
    {
        /// <inheritdoc/>
        public override void Run(InterceptContext context, Intercept intercept)
            => intercept.Run(context);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // 场景 1：非泛型类 + 泛型方法（new() 约束）
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 非泛型类，仅含 <c>new()</c> 约束泛型方法（与现有 ServiceGenericMethodType 相同模式）。
    /// </summary>
    public class NewConstraintGenericMethodClass
    {
        /// <summary>创建并返回 T 实例（new() 约束）。</summary>
        [PassThroughIntercept]
        public virtual T Create<T>() where T : new() => new T();

        /// <summary>异步创建并返回 T 实例（new() 约束）。</summary>
        [PassThroughIntercept]
        public virtual Task<T> CreateAsync<T>() where T : new() => Task.FromResult(new T());
    }

    // ═══════════════════════════════════════════════════════════════════════
    // 场景 2：泛型接口 + 非泛型方法
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 单类型参数泛型接口（类型参数有 class, new() 约束）。
    /// </summary>
    public interface IEntityStore<T> where T : class, new()
    {
        /// <summary>根据 ID 查找实体。</summary>
        [PassThroughIntercept]
        T FindById(int id);

        /// <summary>获取全部实体。</summary>
        [PassThroughIntercept]
        IReadOnlyList<T> GetAll();

        /// <summary>异步保存实体。</summary>
        [PassThroughIntercept]
        Task<bool> SaveAsync(T entity);
    }

    /// <summary>Order 实体（有无参构造函数，可用于泛型约束）。</summary>
    public class OrderEntity
    {
        /// <summary>ID。</summary>
        public int Id { get; set; }

        /// <summary>名称。</summary>
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>固定的 Order 仓储（关闭泛型注册验证用）。</summary>
    public class OrderEntityStore : IEntityStore<OrderEntity>
    {
        /// <inheritdoc/>
        public virtual OrderEntity FindById(int id) => new OrderEntity { Id = id };

        /// <inheritdoc/>
        public virtual IReadOnlyList<OrderEntity> GetAll()
            => new List<OrderEntity> { new OrderEntity { Id = 1 }, new OrderEntity { Id = 2 } };

        /// <inheritdoc/>
        public virtual Task<bool> SaveAsync(OrderEntity entity) => Task.FromResult(true);
    }

    /// <summary>双类型参数泛型接口（键值对映射器）。</summary>
    public interface IObjectMapper<TSource, TTarget>
    {
        /// <summary>将 TSource 映射为 TTarget。</summary>
        [PassThroughIntercept]
        TTarget Map(TSource source);

        /// <summary>批量映射。</summary>
        [PassThroughIntercept]
        IReadOnlyList<TTarget> MapMany(IEnumerable<TSource> sources);
    }

    /// <summary>string → int 长度映射器（关闭双泛型注册验证用）。</summary>
    public class StringToIntLengthMapper : IObjectMapper<string, int>
    {
        /// <inheritdoc/>
        public virtual int Map(string source) => source?.Length ?? 0;

        /// <inheritdoc/>
        public virtual IReadOnlyList<int> MapMany(IEnumerable<string> sources)
        {
            var result = new List<int>();
            foreach (var s in sources)
            {
                result.Add(Map(s));
            }

            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // 场景 3：泛型接口 + 泛型方法（单个方法级类型参数）
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 泛型接口：方法级带一个额外泛型参数，可与接口类型参数配合。
    /// </summary>
    public interface IProjector<T>
    {
        /// <summary>将 T 投影为 TResult。</summary>
        [PassThroughIntercept]
        TResult Project<TResult>(T input, Func<T, TResult> selector);

        /// <summary>批量投影。</summary>
        [PassThroughIntercept]
        IReadOnlyList<TResult> ProjectMany<TResult>(
            IEnumerable<T> inputs, Func<T, TResult> selector);

        /// <summary>异步投影（返回 Task&lt;TResult&gt;）。</summary>
        [PassThroughIntercept]
        Task<TResult> ProjectAsync<TResult>(T input, Func<T, Task<TResult>> asyncSelector);
    }

    /// <summary>IProjector&lt;string&gt; 的实现（关闭泛型注册验证用）。</summary>
    public class StringProjector : IProjector<string>
    {
        /// <inheritdoc/>
        public virtual TResult Project<TResult>(string input, Func<string, TResult> selector)
            => selector(input);

        /// <inheritdoc/>
        public virtual IReadOnlyList<TResult> ProjectMany<TResult>(
            IEnumerable<string> inputs, Func<string, TResult> selector)
        {
            var result = new List<TResult>();
            foreach (var s in inputs)
            {
                result.Add(selector(s));
            }

            return result;
        }

        /// <inheritdoc/>
        public virtual Task<TResult> ProjectAsync<TResult>(
            string input, Func<string, Task<TResult>> asyncSelector)
            => asyncSelector(input);
    }

    /// <summary>IProjector&lt;int&gt; 的实现（同接口、不同类型参数，验证多个关闭泛型共存）。</summary>
    public class IntProjector : IProjector<int>
    {
        /// <inheritdoc/>
        public virtual TResult Project<TResult>(int input, Func<int, TResult> selector)
            => selector(input);

        /// <inheritdoc/>
        public virtual IReadOnlyList<TResult> ProjectMany<TResult>(
            IEnumerable<int> inputs, Func<int, TResult> selector)
        {
            var result = new List<TResult>();
            foreach (var n in inputs)
            {
                result.Add(selector(n));
            }

            return result;
        }

        /// <inheritdoc/>
        public virtual Task<TResult> ProjectAsync<TResult>(
            int input, Func<int, Task<TResult>> asyncSelector)
            => asyncSelector(input);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // 场景 4：泛型接口 + 带约束的泛型方法（TResult : class, new()）
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>泛型接口，方法级 TResult 带 class, new() 约束。</summary>
    public interface IConstrainedProjector<TBase> where TBase : class, new()
    {
        /// <summary>创建 TBase 并转换为 TResult（TResult : class, new()）。</summary>
        [PassThroughIntercept]
        TResult CreateAndProject<TResult>(Func<TBase, TResult> selector)
            where TResult : class, new();

        /// <summary>异步批量创建并转换（TResult : class, new()）。</summary>
        [PassThroughIntercept]
        Task<IReadOnlyList<TResult>> BatchProjectAsync<TResult>(
            int count, Func<TBase, TResult> selector)
            where TResult : class, new();
    }

    /// <summary>IConstrainedProjector&lt;OrderEntity&gt; 实现。</summary>
    public class OrderEntityProjector : IConstrainedProjector<OrderEntity>
    {
        /// <inheritdoc/>
        public virtual TResult CreateAndProject<TResult>(Func<OrderEntity, TResult> selector)
            where TResult : class, new()
            => selector(new OrderEntity { Id = 1, Name = "test" });

        /// <inheritdoc/>
        public virtual Task<IReadOnlyList<TResult>> BatchProjectAsync<TResult>(
            int count, Func<OrderEntity, TResult> selector)
            where TResult : class, new()
        {
            var list = new List<TResult>(count);
            for (int i = 0; i < count; i++)
            {
                list.Add(selector(new OrderEntity { Id = i, Name = $"item{i}" }));
            }

            return Task.FromResult<IReadOnlyList<TResult>>(list);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // 场景 5：泛型类实现多个泛型接口（各自关闭注册）
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>简单的 CRUD 泛型接口（与 IEntityStore 互补，用于多接口实现类）。</summary>
    public interface ICrudStore<T> where T : class, new()
    {
        /// <summary>添加实体。</summary>
        [PassThroughVoidIntercept]
        void Add(T item);

        /// <summary>根据 ID 获取实体。</summary>
        [PassThroughIntercept]
        T Get(int id);
    }

    /// <summary>统计泛型接口（用于多接口实现类）。</summary>
    public interface ICountStore<T> where T : class, new()
    {
        /// <summary>获取实体总数。</summary>
        [PassThroughIntercept]
        int Count();
    }

    /// <summary>同时实现 ICrudStore、ICountStore 和 IProjector 的泛型类。</summary>
    public class MultiInterfaceGenericServiceStore<T> : ICrudStore<T>, ICountStore<T>, IProjector<T>
        where T : class, new()
    {
        private readonly List<T> _store = new List<T>();

        /// <inheritdoc/>
        public virtual void Add(T item) => _store.Add(item);

        /// <inheritdoc/>
        public virtual T Get(int id) => _store.Count > id ? _store[id] : new T();

        /// <inheritdoc/>
        public virtual int Count() => _store.Count;

        /// <inheritdoc/>
        public virtual TResult Project<TResult>(T input, Func<T, TResult> selector)
            => selector(input);

        /// <inheritdoc/>
        public virtual IReadOnlyList<TResult> ProjectMany<TResult>(
            IEnumerable<T> inputs, Func<T, TResult> selector)
        {
            var result = new List<TResult>();
            foreach (var item in inputs)
            {
                result.Add(selector(item));
            }

            return result;
        }

        /// <inheritdoc/>
        public virtual Task<TResult> ProjectAsync<TResult>(
            T input, Func<T, Task<TResult>> asyncSelector)
            => asyncSelector(input);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // 场景 6：关闭泛型接口 + 异步泛型方法（Task<TResult>）
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>异步处理管道泛型接口（仅 Task 版本，已排除有问题的 ValueTask）。</summary>
    public interface IAsyncProcessor<TInput>
    {
        /// <summary>异步执行单步转换。</summary>
        [PassThroughIntercept]
        Task<TOutput> ProcessAsync<TOutput>(TInput input, Func<TInput, Task<TOutput>> step);

        /// <summary>异步执行两步管道（中间结果类型固定为 string）。</summary>
        [PassThroughIntercept]
        Task<TFinal> PipeStringAsync<TFinal>(
            TInput input,
            Func<TInput, string> step1,
            Func<string, Task<TFinal>> step2);
    }

    /// <summary>IAsyncProcessor&lt;string&gt; 实现。</summary>
    public class StringAsyncProcessor : IAsyncProcessor<string>
    {
        /// <inheritdoc/>
        public virtual Task<TOutput> ProcessAsync<TOutput>(
            string input, Func<string, Task<TOutput>> step)
            => step(input);

        /// <inheritdoc/>
        public virtual async Task<TFinal> PipeStringAsync<TFinal>(
            string input,
            Func<string, string> step1,
            Func<string, Task<TFinal>> step2)
        {
            var middle = step1(input);
            return await step2(middle);
        }
    }

    /// <summary>IAsyncProcessor&lt;int&gt; 实现（同接口不同参数类型）。</summary>
    public class IntAsyncProcessor : IAsyncProcessor<int>
    {
        /// <inheritdoc/>
        public virtual Task<TOutput> ProcessAsync<TOutput>(
            int input, Func<int, Task<TOutput>> step)
            => step(input);

        /// <inheritdoc/>
        public virtual async Task<TFinal> PipeStringAsync<TFinal>(
            int input,
            Func<int, string> step1,
            Func<string, Task<TFinal>> step2)
        {
            var middle = step1(input);
            return await step2(middle);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // 测试类
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 泛型方法、泛型接口、泛型接口的泛型方法 — 代理实现类单元测试。
    /// </summary>
    public class GenericProxyTests
    {
        // ── 辅助构建 ──────────────────────────────────────────────────────

        private static IServiceProvider BuildClosed<TService, TImpl>()
            where TService : class
            where TImpl : class, TService
        {
            var services = new ServiceCollection();
            services.AddTransient<TService, TImpl>();
            services.UseIntercept();
            return services.BuildServiceProvider();
        }

        private static IServiceProvider BuildDirectClass<TImpl>()
            where TImpl : class
        {
            var services = new ServiceCollection();
            services.AddTransient<TImpl>();
            services.UseIntercept();
            return services.BuildServiceProvider();
        }

        // ═══════════════════════════════════════════════════════════════════
        // 场景 1：非泛型类 + 泛型方法（new() 约束）
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>非泛型类 + new() 约束泛型方法：代理后可创建 StringBuilder 实例。</summary>
        [Fact]
        public void NonGenericClass_NewConstraintGenericMethod_Create_ProxyWorks()
        {
            var provider = BuildDirectClass<NewConstraintGenericMethodClass>();
            var instance = provider.GetRequiredService<NewConstraintGenericMethodClass>();

            var sb = instance.Create<StringBuilder>();
            Assert.NotNull(sb);

            var order = instance.Create<OrderEntity>();
            Assert.NotNull(order);
        }

        /// <summary>非泛型类 + new() 约束异步泛型方法：代理后可异步创建 OrderEntity 实例。</summary>
        [Fact]
        public async Task NonGenericClass_NewConstraintGenericMethod_CreateAsync_ProxyWorks()
        {
            var provider = BuildDirectClass<NewConstraintGenericMethodClass>();
            var instance = provider.GetRequiredService<NewConstraintGenericMethodClass>();

            var order = await instance.CreateAsync<OrderEntity>();
            Assert.NotNull(order);

            var sb = await instance.CreateAsync<StringBuilder>();
            Assert.NotNull(sb);
        }

        // ═══════════════════════════════════════════════════════════════════
        // 场景 2：关闭泛型接口 + 非泛型方法
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// 关闭泛型接口（IEntityStore&lt;OrderEntity&gt;）FindById：代理后返回正确实体。
        /// </summary>
        [Fact]
        public void ClosedGenericInterface_NonGenericMethod_FindById_ProxyWorks()
        {
            var provider = BuildClosed<IEntityStore<OrderEntity>, OrderEntityStore>();
            var store = provider.GetRequiredService<IEntityStore<OrderEntity>>();

            var entity = store.FindById(7);
            Assert.NotNull(entity);
            Assert.Equal(7, entity.Id);
        }

        /// <summary>
        /// 关闭泛型接口（IEntityStore&lt;OrderEntity&gt;）GetAll：代理后返回非空列表。
        /// </summary>
        [Fact]
        public void ClosedGenericInterface_NonGenericMethod_GetAll_ProxyWorks()
        {
            var provider = BuildClosed<IEntityStore<OrderEntity>, OrderEntityStore>();
            var store = provider.GetRequiredService<IEntityStore<OrderEntity>>();

            var all = store.GetAll();
            Assert.NotEmpty(all);
        }

        /// <summary>
        /// 关闭泛型接口（IEntityStore&lt;OrderEntity&gt;）SaveAsync：代理后返回 true。
        /// </summary>
        [Fact]
        public async Task ClosedGenericInterface_NonGenericMethod_SaveAsync_ProxyWorks()
        {
            var provider = BuildClosed<IEntityStore<OrderEntity>, OrderEntityStore>();
            var store = provider.GetRequiredService<IEntityStore<OrderEntity>>();

            var result = await store.SaveAsync(new OrderEntity { Id = 99 });
            Assert.True(result);
        }

        /// <summary>
        /// 双类型参数接口（IObjectMapper&lt;string, int&gt;）Map：代理后映射结果正确。
        /// </summary>
        [Fact]
        public void TwoTypeParamInterface_NonGenericMethod_Map_ProxyWorks()
        {
            var provider = BuildClosed<IObjectMapper<string, int>, StringToIntLengthMapper>();
            var mapper = provider.GetRequiredService<IObjectMapper<string, int>>();

            Assert.Equal(5, mapper.Map("hello"));
            Assert.Equal(0, mapper.Map(null));
        }

        /// <summary>
        /// 双类型参数接口（IObjectMapper&lt;string, int&gt;）MapMany：代理后批量映射正确。
        /// </summary>
        [Fact]
        public void TwoTypeParamInterface_NonGenericMethod_MapMany_ProxyWorks()
        {
            var provider = BuildClosed<IObjectMapper<string, int>, StringToIntLengthMapper>();
            var mapper = provider.GetRequiredService<IObjectMapper<string, int>>();

            var result = mapper.MapMany(new[] { "a", "bb", "ccc" });
            Assert.Equal(new[] { 1, 2, 3 }, result);
        }

        // ═══════════════════════════════════════════════════════════════════
        // 场景 3：关闭泛型接口 + 泛型方法（单个方法级类型参数）
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// 关闭泛型接口（IProjector&lt;string&gt;）泛型方法 Project&lt;int&gt;：代理后投影结果正确。
        /// </summary>
        [Fact]
        public void ClosedGenericInterface_GenericMethod_Project_ProxyWorks()
        {
            var provider = BuildClosed<IProjector<string>, StringProjector>();
            var projector = provider.GetRequiredService<IProjector<string>>();

            var result = projector.Project("hello", s => s.Length);
            Assert.Equal(5, result);
        }

        /// <summary>
        /// 关闭泛型接口（IProjector&lt;string&gt;）泛型方法 ProjectMany：代理后批量投影正确。
        /// </summary>
        [Fact]
        public void ClosedGenericInterface_GenericMethod_ProjectMany_ProxyWorks()
        {
            var provider = BuildClosed<IProjector<string>, StringProjector>();
            var projector = provider.GetRequiredService<IProjector<string>>();

            var result = projector.ProjectMany(new[] { "a", "bb", "ccc" }, s => s.Length);
            Assert.Equal(new[] { 1, 2, 3 }, result);
        }

        /// <summary>
        /// 关闭泛型接口（IProjector&lt;string&gt;）异步泛型方法 ProjectAsync：代理后结果正确。
        /// </summary>
        [Fact]
        public async Task ClosedGenericInterface_GenericMethod_ProjectAsync_ProxyWorks()
        {
            var provider = BuildClosed<IProjector<string>, StringProjector>();
            var projector = provider.GetRequiredService<IProjector<string>>();

            var result = await projector.ProjectAsync("hello", s => Task.FromResult(s.Length));
            Assert.Equal(5, result);
        }

        /// <summary>
        /// 同一接口不同关闭类型（IProjector&lt;int&gt;）可独立代理，互不影响。
        /// </summary>
        [Fact]
        public void ClosedGenericInterface_DifferentClosedType_BothProxyWork()
        {
            var providerStr = BuildClosed<IProjector<string>, StringProjector>();
            var providerInt = BuildClosed<IProjector<int>, IntProjector>();

            var strProj = providerStr.GetRequiredService<IProjector<string>>();
            var intProj = providerInt.GetRequiredService<IProjector<int>>();

            Assert.Equal("HELLO", strProj.Project("hello", s => s.ToUpper()));
            Assert.Equal("42",    intProj.Project(42, n => n.ToString()));
        }

        // ═══════════════════════════════════════════════════════════════════
        // 场景 4：关闭泛型接口 + 带约束的泛型方法（TResult : class, new()）
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// 约束泛型接口（IConstrainedProjector&lt;OrderEntity&gt;）CreateAndProject&lt;StringBuilder&gt;：
        /// 代理后转换结果正确。
        /// </summary>
        [Fact]
        public void ConstrainedGenericInterface_GenericMethod_CreateAndProject_ProxyWorks()
        {
            var provider = BuildClosed<IConstrainedProjector<OrderEntity>, OrderEntityProjector>();
            var projector = provider.GetRequiredService<IConstrainedProjector<OrderEntity>>();

            var result = projector.CreateAndProject(e => new StringBuilder(e.Name));
            Assert.NotNull(result);
            Assert.Equal("test", result.ToString());
        }

        /// <summary>
        /// 约束泛型接口 BatchProjectAsync&lt;OrderEntity&gt;：代理后批量创建数量正确。
        /// </summary>
        [Fact]
        public async Task ConstrainedGenericInterface_GenericMethod_BatchProjectAsync_ProxyWorks()
        {
            var provider = BuildClosed<IConstrainedProjector<OrderEntity>, OrderEntityProjector>();
            var projector = provider.GetRequiredService<IConstrainedProjector<OrderEntity>>();

            var results = await projector.BatchProjectAsync(
                4, e => new OrderEntity { Id = e.Id, Name = e.Name });

            Assert.Equal(4, results.Count);
        }

        // ═══════════════════════════════════════════════════════════════════
        // 场景 5：泛型类实现多个泛型接口（各自关闭注册）
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// 多接口泛型类（以 ICrudStore&lt;OrderEntity&gt; 注入）void Add + Get 代理正确。
        /// </summary>
        [Fact]
        public void MultiInterfaceGenericClass_AsCrudStore_ProxyWorks()
        {
            var provider = BuildClosed<ICrudStore<OrderEntity>, MultiInterfaceGenericServiceStore<OrderEntity>>();
            var store = provider.GetRequiredService<ICrudStore<OrderEntity>>();

            store.Add(new OrderEntity { Id = 1 });
            var entity = store.Get(0);
            Assert.NotNull(entity);
        }

        /// <summary>
        /// 多接口泛型类（以 ICountStore&lt;OrderEntity&gt; 注入）Count 代理正确。
        /// </summary>
        [Fact]
        public void MultiInterfaceGenericClass_AsCountStore_ProxyWorks()
        {
            var provider = BuildClosed<ICountStore<OrderEntity>, MultiInterfaceGenericServiceStore<OrderEntity>>();
            var store = provider.GetRequiredService<ICountStore<OrderEntity>>();

            Assert.Equal(0, store.Count());
        }

        /// <summary>
        /// 多接口泛型类（以 IProjector&lt;OrderEntity&gt; 注入）泛型方法 Project 代理正确。
        /// </summary>
        [Fact]
        public void MultiInterfaceGenericClass_AsProjector_GenericMethod_ProxyWorks()
        {
            var provider = BuildClosed<IProjector<OrderEntity>, MultiInterfaceGenericServiceStore<OrderEntity>>();
            var projector = provider.GetRequiredService<IProjector<OrderEntity>>();

            var entity = new OrderEntity { Id = 42, Name = "test" };
            var name = projector.Project(entity, e => e.Name);
            Assert.Equal("test", name);
        }

        /// <summary>
        /// 多接口泛型类（以 IProjector&lt;OrderEntity&gt; 注入）ProjectMany 批量泛型方法代理正确。
        /// </summary>
        [Fact]
        public void MultiInterfaceGenericClass_AsProjector_ProjectMany_ProxyWorks()
        {
            var provider = BuildClosed<IProjector<OrderEntity>, MultiInterfaceGenericServiceStore<OrderEntity>>();
            var projector = provider.GetRequiredService<IProjector<OrderEntity>>();

            var entities = new[]
            {
                new OrderEntity { Id = 1, Name = "A" },
                new OrderEntity { Id = 2, Name = "B" },
            };
            var names = projector.ProjectMany(entities, e => e.Name);
            Assert.Equal(new[] { "A", "B" }, names);
        }

        /// <summary>
        /// 多接口泛型类（以 IProjector&lt;OrderEntity&gt; 注入）异步泛型方法代理正确。
        /// </summary>
        [Fact]
        public async Task MultiInterfaceGenericClass_AsProjector_ProjectAsync_ProxyWorks()
        {
            var provider = BuildClosed<IProjector<OrderEntity>, MultiInterfaceGenericServiceStore<OrderEntity>>();
            var projector = provider.GetRequiredService<IProjector<OrderEntity>>();

            var entity = new OrderEntity { Id = 10, Name = "async" };
            var id = await projector.ProjectAsync(entity, e => Task.FromResult(e.Id));
            Assert.Equal(10, id);
        }

        // ═══════════════════════════════════════════════════════════════════
        // 场景 6：关闭泛型接口 + 异步泛型方法（Task<TResult>）
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// 关闭泛型异步接口（IAsyncProcessor&lt;string&gt;）ProcessAsync&lt;int&gt;：代理后结果正确。
        /// </summary>
        [Fact]
        public async Task AsyncGenericInterface_GenericMethod_ProcessAsync_ProxyWorks()
        {
            var provider = BuildClosed<IAsyncProcessor<string>, StringAsyncProcessor>();
            var processor = provider.GetRequiredService<IAsyncProcessor<string>>();

            var result = await processor.ProcessAsync("hello", s => Task.FromResult(s.Length));
            Assert.Equal(5, result);
        }

        /// <summary>
        /// 关闭泛型异步接口（IAsyncProcessor&lt;string&gt;）PipeStringAsync&lt;bool&gt;：两步管道代理正确。
        /// </summary>
        [Fact]
        public async Task AsyncGenericInterface_GenericMethod_PipeAsync_ProxyWorks()
        {
            var provider = BuildClosed<IAsyncProcessor<string>, StringAsyncProcessor>();
            var processor = provider.GetRequiredService<IAsyncProcessor<string>>();

            var result = await processor.PipeStringAsync(
                "hello",
                s => s.ToUpper(),
                s => Task.FromResult(s.StartsWith("H")));

            Assert.True(result);
        }

        /// <summary>
        /// 同一异步接口不同关闭类型（IAsyncProcessor&lt;int&gt;）代理独立工作。
        /// </summary>
        [Fact]
        public async Task AsyncGenericInterface_DifferentClosedType_ProxyWorks()
        {
            var provider = BuildClosed<IAsyncProcessor<int>, IntAsyncProcessor>();
            var processor = provider.GetRequiredService<IAsyncProcessor<int>>();

            var result = await processor.ProcessAsync(42, n => Task.FromResult(n * 2));
            Assert.Equal(84, result);
        }
    }
}