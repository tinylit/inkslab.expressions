using System;
using System.Reflection;
using Inkslab.Emitters;
using Xunit;

namespace Inkslab.Expressions.Tests
{
    /// <summary>
    /// 测试虚方法调用是否正确使用 Callvirt 指令
    /// </summary>
    public class VirtualMethodCallTests
    {
        private readonly ModuleEmitter _mod = new ModuleEmitter($"VMC_{Guid.NewGuid():N}");

        private Type BuildStatic(string name, Type ret, Action<MethodEmitter> body)
        {
            var te = _mod.DefineType($"{name}_{Guid.NewGuid():N}", TypeAttributes.Public);
            te.DefineDefaultConstructor();
            var me = te.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, ret);
            body(me);
            return te.CreateType();
        }

        private object Invoke(Type t, params object[] args)
            => t.GetMethod("Run")!.Invoke(null, args);

        /// <summary>
        /// 基类，重写了 Equals
        /// </summary>
        public class BaseClass
        {
            public int Value { get; set; }

            public override bool Equals(object obj)
            {
                return obj is BaseClass other && Value == other.Value;
            }

            public override int GetHashCode() => Value.GetHashCode();
        }

        /// <summary>
        /// 派生类，进一步重写 Equals
        /// </summary>
        public class DerivedClass : BaseClass
        {
            public string Name { get; set; }

            public override bool Equals(object obj)
            {
                return obj is DerivedClass other && Value == other.Value && Name == other.Name;
            }

            public override int GetHashCode() => (Value, Name).GetHashCode();
        }

        [Fact]
        public void Equal_VirtualDispatch_UsesOverriddenEquals()
        {
            // 测试虚方法调度：当使用基类类型但实际是派生类实例时，应调用派生类的 Equals
            var type = BuildStatic("EqVirt", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(BaseClass), "a");
                var b = m.DefineParameter(typeof(BaseClass), "b");
                m.Append(Expression.Equal(a, b));
            });

            var derived1 = new DerivedClass { Value = 1, Name = "Test" };
            var derived2 = new DerivedClass { Value = 1, Name = "Test" };
            var derived3 = new DerivedClass { Value = 1, Name = "Different" };

            // 应该使用 DerivedClass.Equals，因此需要 Name 也相同
            Assert.True((bool)Invoke(type, derived1, derived2));
            Assert.False((bool)Invoke(type, derived1, derived3));
        }

        [Fact]
        public void NotEqual_VirtualDispatch_UsesOverriddenEquals()
        {
            var type = BuildStatic("NEqVirt", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(BaseClass), "a");
                var b = m.DefineParameter(typeof(BaseClass), "b");
                m.Append(Expression.NotEqual(a, b));
            });

            var derived1 = new DerivedClass { Value = 1, Name = "Test" };
            var derived2 = new DerivedClass { Value = 1, Name = "Test" };
            var derived3 = new DerivedClass { Value = 1, Name = "Different" };

            Assert.False((bool)Invoke(type, derived1, derived2));
            Assert.True((bool)Invoke(type, derived1, derived3));
        }

        [Fact]
        public void Equal_BaseClassInstances_UsesBaseEquals()
        {
            var type = BuildStatic("EqBase", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(BaseClass), "a");
                var b = m.DefineParameter(typeof(BaseClass), "b");
                m.Append(Expression.Equal(a, b));
            });

            var base1 = new BaseClass { Value = 42 };
            var base2 = new BaseClass { Value = 42 };
            var base3 = new BaseClass { Value = 99 };

            Assert.True((bool)Invoke(type, base1, base2));
            Assert.False((bool)Invoke(type, base1, base3));
        }
    }
}
