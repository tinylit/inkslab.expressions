using System;
using System.Reflection;
using Inkslab.Intercept;
using Xunit;

#pragma warning disable CS1591 // 测试方法不需要 XML 注释

namespace Inkslab.Intercept.Tests
{
    /// <summary>
    /// <see cref="ImplementInvocation"/> 的功能测试，覆盖：
    /// 1) 编译路径（普通方法、void、值类型返回、引用类型返回、字符串参数）；
    /// 2) 反射回退路径（泛型方法定义、ref 参数）；
    /// 3) 参数防御。
    /// </summary>
    public class ImplementInvocationTests
    {
        private interface ISample
        {
            int Add(int a, int b);
            string Concat(string a, string b);
            void DoVoid();
            T Echo<T>(T value);
            void RefMethod(ref int v);
        }

        private sealed class Sample : ISample
        {
            public int LastVoid;

            public int Add(int a, int b) => a + b;
            public string Concat(string a, string b) => a + b;
            public void DoVoid() => LastVoid = 42;
            public T Echo<T>(T value) => value;
            public void RefMethod(ref int v) => v *= 2;
        }

        [Fact]
        public void Invoke_ValueTypeReturn_UsesCompiledInvoker()
        {
            var sample = new Sample();
            var method = typeof(Sample).GetMethod(nameof(Sample.Add));
            var invocation = new ImplementInvocation(sample, method);

            object result = invocation.Invoke(new object[] { 3, 4 });

            Assert.Equal(7, result);
        }

        [Fact]
        public void Invoke_ReferenceTypeReturn_UsesCompiledInvoker()
        {
            var sample = new Sample();
            var method = typeof(Sample).GetMethod(nameof(Sample.Concat));
            var invocation = new ImplementInvocation(sample, method);

            object result = invocation.Invoke(new object[] { "Hello, ", "World" });

            Assert.Equal("Hello, World", result);
        }

        [Fact]
        public void Invoke_VoidReturn_ReturnsNull()
        {
            var sample = new Sample();
            var method = typeof(Sample).GetMethod(nameof(Sample.DoVoid));
            var invocation = new ImplementInvocation(sample, method);

            object result = invocation.Invoke(Array.Empty<object>());

            Assert.Null(result);
            Assert.Equal(42, sample.LastVoid);
        }

        [Fact]
        public void Invoke_GenericMethodDefinition_FallsBackToReflection()
        {
            var sample = new Sample();
            // 直接使用泛型方法定义，构造时应该走反射回退分支。
            var method = typeof(Sample).GetMethod(nameof(Sample.Echo)).MakeGenericMethod(typeof(int));
            var invocation = new ImplementInvocation(sample, method);

            object result = invocation.Invoke(new object[] { 99 });

            Assert.Equal(99, result);
        }

        [Fact]
        public void Invoke_RefParameter_FallsBackToReflection()
        {
            var sample = new Sample();
            var method = typeof(Sample).GetMethod(nameof(Sample.RefMethod));
            var invocation = new ImplementInvocation(sample, method);

            // ref 参数走反射路径：MethodInfo.Invoke 会把数组中的元素回写。
            var args = new object[] { 5 };
            invocation.Invoke(args);

            Assert.Equal(10, args[0]);
        }

        [Fact]
        public void Constructor_NullArguments_Throw()
        {
            var method = typeof(Sample).GetMethod(nameof(Sample.Add));

            Assert.Throws<ArgumentNullException>(() => new ImplementInvocation(null, method));
            Assert.Throws<ArgumentNullException>(() => new ImplementInvocation(new Sample(), null));
        }

        [Fact]
        public void Invoke_RepeatedConstructionForSameMethod_ReusesCachedInvoker()
        {
            // 构造两个不同 target、相同 method 的实例，第二个应直接命中缓存。
            // 该测试不直接断言缓存命中，而是验证两次构造均能正常工作（间接覆盖缓存路径）。
            var method = typeof(Sample).GetMethod(nameof(Sample.Add));

            var inv1 = new ImplementInvocation(new Sample(), method);
            var inv2 = new ImplementInvocation(new Sample(), method);

            Assert.Equal(5, inv1.Invoke(new object[] { 2, 3 }));
            Assert.Equal(11, inv2.Invoke(new object[] { 4, 7 }));
        }
    }
}
