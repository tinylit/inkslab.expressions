using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Inkslab.Expressions.Tests
{
    /// <summary>
    /// <see cref="NamingScope"/> 的功能与并发安全测试。
    /// </summary>
    public class NamingScopeTests
    {
        [Fact]
        public void GetUniqueName_FirstCall_ReturnsOriginalName()
        {
            var scope = new NamingScope();

            Assert.Equal("Foo", scope.GetUniqueName("Foo"));
        }

        [Fact]
        public void GetUniqueName_RepeatedCalls_AppendsIncrementingSuffix()
        {
            var scope = new NamingScope();

            Assert.Equal("Foo", scope.GetUniqueName("Foo"));
            Assert.Equal("Foo_1", scope.GetUniqueName("Foo"));
            Assert.Equal("Foo_2", scope.GetUniqueName("Foo"));
            Assert.Equal("Foo_3", scope.GetUniqueName("Foo"));
        }

        [Fact]
        public void GetUniqueName_DifferentNames_AreIndependent()
        {
            var scope = new NamingScope();

            Assert.Equal("A", scope.GetUniqueName("A"));
            Assert.Equal("B", scope.GetUniqueName("B"));
            Assert.Equal("A_1", scope.GetUniqueName("A"));
            Assert.Equal("B_1", scope.GetUniqueName("B"));
        }

        [Fact]
        public void BeginScope_ReturnsIsolatedScope()
        {
            var root = new NamingScope();
            root.GetUniqueName("Foo");
            root.GetUniqueName("Foo");

            var child = root.BeginScope();

            // 子作用域是全新独立的，不应继承父作用域的计数。
            Assert.Equal("Foo", child.GetUniqueName("Foo"));

            // 父作用域计数继续推进，不受子作用域影响。
            Assert.Equal("Foo_2", root.GetUniqueName("Foo"));
        }

        /// <summary>
        /// 并发调用同一作用域时，必须保证：
        /// 1) 不会抛出 InvalidOperationException 等集合损坏异常；
        /// 2) 返回的所有名称两两不重复（唯一性）。
        /// 这是修复前 <c>Dictionary&lt;string,int&gt;</c> 实现做不到的核心保证。
        /// </summary>
        [Fact]
        public async Task GetUniqueName_ConcurrentInvocations_ProduceUniqueNames()
        {
            const int taskCount = 32;
            const int iterationsPerTask = 500;

            var scope = new NamingScope();
            var bag = new ConcurrentBag<string>();

            var tasks = Enumerable.Range(0, taskCount).Select(_ => Task.Run(() =>
            {
                for (int i = 0; i < iterationsPerTask; i++)
                {
                    bag.Add(scope.GetUniqueName("Shared"));
                }
            })).ToArray();

            await Task.WhenAll(tasks);

            int total = taskCount * iterationsPerTask;
            Assert.Equal(total, bag.Count);
            // 唯一性是关键：任何丢失唯一性都会让动态生成的字段/方法名冲突。
            Assert.Equal(total, bag.Distinct().Count());
        }
    }
}
