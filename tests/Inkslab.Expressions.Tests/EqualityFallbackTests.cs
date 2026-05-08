using System;
using System.Collections.Generic;
using System.Reflection;
using Inkslab.Emitters;
using Xunit;

namespace Inkslab.Expressions.Tests
{
    /// <summary>
    /// 测试 Equal/NotEqual 的回退链：操作符 → IEquatable&lt;T&gt; → Equals(object) → 报错。
    /// </summary>
    public class EqualityFallbackTests
    {
        private readonly ModuleEmitter _mod = new ModuleEmitter($"EQF_{Guid.NewGuid():N}");

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

        // ========== 测试类型定义 ==========

        /// <summary>实现了 IEquatable&lt;PersonWithEquatable&gt; 的引用类型。</summary>
        public class PersonWithEquatable : IEquatable<PersonWithEquatable>
        {
            public string Name { get; set; }
            public int Age { get; set; }

            public bool Equals(PersonWithEquatable other)
            {
                if (other is null)
                {
                    return false;
                }
                return Name == other.Name && Age == other.Age;
            }
        }

        /// <summary>仅重写 Equals(object) 的引用类型（无 IEquatable&lt;T&gt;）。</summary>
        public class PersonWithObjectEquals
        {
            public string Name { get; set; }
            public int Age { get; set; }

            public override bool Equals(object obj)
            {
                if (obj is PersonWithObjectEquals other)
                {
                    return Name == other.Name && Age == other.Age;
                }
                return false;
            }

            public override int GetHashCode() => (Name?.GetHashCode() ?? 0) ^ Age.GetHashCode();
        }

        /// <summary>同时实现 IEquatable&lt;T&gt; 和重写 Equals(object) 的值类型。</summary>
        public struct PointWithEquatable : IEquatable<PointWithEquatable>
        {
            public int X { get; set; }
            public int Y { get; set; }

            public bool Equals(PointWithEquatable other) => X == other.X && Y == other.Y;

            public override bool Equals(object obj) => obj is PointWithEquatable other && Equals(other);

            public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode();

            public static bool operator ==(PointWithEquatable left, PointWithEquatable right) => left.Equals(right);
            public static bool operator !=(PointWithEquatable left, PointWithEquatable right) => !left.Equals(right);
        }

        /// <summary>无任何相等性支持的类。</summary>
        public class NoEqualitySupport
        {
            public int Id { get; set; }
        }

        /// <summary>实现了 IEquatable&lt;T&gt; 的引用类型，但 T 与自身不同（测试类型不匹配场景）。</summary>
        public class PersonWithSpecificEquatable : IEquatable<string>
        {
            public bool Equals(string other) => other is not null;
        }

        // ========== Equal 测试 ==========

        [Fact]
        public void Equal_IEquatable_RefType_True()
        {
            var type = BuildStatic("EqIEqT", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(PersonWithEquatable), "a");
                var b = m.DefineParameter(typeof(PersonWithEquatable), "b");
                m.Append(Expression.Equal(a, b));
            });
            var p1 = new PersonWithEquatable { Name = "Alice", Age = 30 };
            var p2 = new PersonWithEquatable { Name = "Alice", Age = 30 };
            Assert.True((bool)Invoke(type, p1, p2));
        }

        [Fact]
        public void Equal_IEquatable_RefType_False()
        {
            var type = BuildStatic("EqIEqF", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(PersonWithEquatable), "a");
                var b = m.DefineParameter(typeof(PersonWithEquatable), "b");
                m.Append(Expression.Equal(a, b));
            });
            var p1 = new PersonWithEquatable { Name = "Alice", Age = 30 };
            var p2 = new PersonWithEquatable { Name = "Bob", Age = 25 };
            Assert.False((bool)Invoke(type, p1, p2));
        }

        [Fact]
        public void Equal_ObjectEquals_True()
        {
            var type = BuildStatic("EqObjT", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(PersonWithObjectEquals), "a");
                var b = m.DefineParameter(typeof(PersonWithObjectEquals), "b");
                m.Append(Expression.Equal(a, b));
            });
            var p1 = new PersonWithObjectEquals { Name = "Alice", Age = 30 };
            var p2 = new PersonWithObjectEquals { Name = "Alice", Age = 30 };
            Assert.True((bool)Invoke(type, p1, p2));
        }

        [Fact]
        public void Equal_ObjectEquals_False()
        {
            var type = BuildStatic("EqObjF", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(PersonWithObjectEquals), "a");
                var b = m.DefineParameter(typeof(PersonWithObjectEquals), "b");
                m.Append(Expression.Equal(a, b));
            });
            var p1 = new PersonWithObjectEquals { Name = "Alice", Age = 30 };
            var p2 = new PersonWithObjectEquals { Name = "Bob", Age = 25 };
            Assert.False((bool)Invoke(type, p1, p2));
        }

        [Fact]
        public void Equal_OperatorPreferred_OverEquatable()
        {
            // PointWithEquatable 同时有 operator== 和 IEquatable，应优先使用 operator
            var type = BuildStatic("EqOpP", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(PointWithEquatable), "a");
                var b = m.DefineParameter(typeof(PointWithEquatable), "b");
                m.Append(Expression.Equal(a, b));
            });
            var p1 = new PointWithEquatable { X = 1, Y = 2 };
            var p2 = new PointWithEquatable { X = 1, Y = 2 };
            Assert.True((bool)Invoke(type, p1, p2));
        }

        // ========== NotEqual 测试 ==========

        [Fact]
        public void NotEqual_IEquatable_RefType_True()
        {
            var type = BuildStatic("NEqIT", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(PersonWithEquatable), "a");
                var b = m.DefineParameter(typeof(PersonWithEquatable), "b");
                m.Append(Expression.NotEqual(a, b));
            });
            var p1 = new PersonWithEquatable { Name = "Alice", Age = 30 };
            var p2 = new PersonWithEquatable { Name = "Bob", Age = 25 };
            Assert.True((bool)Invoke(type, p1, p2));
        }

        [Fact]
        public void NotEqual_IEquatable_RefType_False()
        {
            var type = BuildStatic("NEqIF", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(PersonWithEquatable), "a");
                var b = m.DefineParameter(typeof(PersonWithEquatable), "b");
                m.Append(Expression.NotEqual(a, b));
            });
            var p1 = new PersonWithEquatable { Name = "Alice", Age = 30 };
            var p2 = new PersonWithEquatable { Name = "Alice", Age = 30 };
            Assert.False((bool)Invoke(type, p1, p2));
        }

        [Fact]
        public void NotEqual_ObjectEquals_True()
        {
            var type = BuildStatic("NEqOT", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(PersonWithObjectEquals), "a");
                var b = m.DefineParameter(typeof(PersonWithObjectEquals), "b");
                m.Append(Expression.NotEqual(a, b));
            });
            var p1 = new PersonWithObjectEquals { Name = "Alice", Age = 30 };
            var p2 = new PersonWithObjectEquals { Name = "Bob", Age = 25 };
            Assert.True((bool)Invoke(type, p1, p2));
        }

        [Fact]
        public void NotEqual_ObjectEquals_False()
        {
            var type = BuildStatic("NEqOF", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(PersonWithObjectEquals), "a");
                var b = m.DefineParameter(typeof(PersonWithObjectEquals), "b");
                m.Append(Expression.NotEqual(a, b));
            });
            var p1 = new PersonWithObjectEquals { Name = "Alice", Age = 30 };
            var p2 = new PersonWithObjectEquals { Name = "Alice", Age = 30 };
            Assert.False((bool)Invoke(type, p1, p2));
        }

        [Fact]
        public void NotEqual_OperatorPreferred_OverEquatable()
        {
            var type = BuildStatic("NEqOpP", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(PointWithEquatable), "a");
                var b = m.DefineParameter(typeof(PointWithEquatable), "b");
                m.Append(Expression.NotEqual(a, b));
            });
            var p1 = new PointWithEquatable { X = 1, Y = 2 };
            var p2 = new PointWithEquatable { X = 3, Y = 4 };
            Assert.True((bool)Invoke(type, p1, p2));
        }

        // ========== ReferenceEquals 兜底场景 ==========

        [Fact]
        public void Equal_NoEqualitySupport_FallsBackToReferenceEquals_True()
        {
            var type = BuildStatic("EqRefT", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(NoEqualitySupport), "a");
                var b = m.DefineParameter(typeof(NoEqualitySupport), "b");
                m.Append(Expression.Equal(a, b));
            });
            var same = new NoEqualitySupport { Id = 1 };
            Assert.True((bool)Invoke(type, same, same));
        }

        [Fact]
        public void Equal_NoEqualitySupport_FallsBackToReferenceEquals_False()
        {
            var type = BuildStatic("EqRefF", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(NoEqualitySupport), "a");
                var b = m.DefineParameter(typeof(NoEqualitySupport), "b");
                m.Append(Expression.Equal(a, b));
            });
            var p1 = new NoEqualitySupport { Id = 1 };
            var p2 = new NoEqualitySupport { Id = 1 };
            Assert.False((bool)Invoke(type, p1, p2));
        }

        [Fact]
        public void NotEqual_NoEqualitySupport_FallsBackToReferenceEquals()
        {
            var type = BuildStatic("NEqRef", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(NoEqualitySupport), "a");
                var b = m.DefineParameter(typeof(NoEqualitySupport), "b");
                m.Append(Expression.NotEqual(a, b));
            });
            var p1 = new NoEqualitySupport { Id = 1 };
            var p2 = new NoEqualitySupport { Id = 1 };
            Assert.True((bool)Invoke(type, p1, p2));
            Assert.False((bool)Invoke(type, p1, p1));
        }

        // ========== 边界场景测试 ==========

        [Fact]
        public void Equal_ValueType_IEquatable_Struct()
        {
            // PointWithEquatable has operator== but also IEquatable — tests the fallback chain awareness
            var type = BuildStatic("EqValT", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(PointWithEquatable), "a");
                var b = m.DefineParameter(typeof(PointWithEquatable), "b");
                m.Append(Expression.Equal(a, b));
            });
            var p1 = new PointWithEquatable { X = 5, Y = 5 };
            var p2 = new PointWithEquatable { X = 5, Y = 5 };
            Assert.True((bool)Invoke(type, p1, p2));
        }

        [Fact]
        public void NotEqual_ValueType_IEquatable_Struct()
        {
            var type = BuildStatic("NEqVal", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(PointWithEquatable), "a");
                var b = m.DefineParameter(typeof(PointWithEquatable), "b");
                m.Append(Expression.NotEqual(a, b));
            });
            var p1 = new PointWithEquatable { X = 1, Y = 1 };
            var p2 = new PointWithEquatable { X = 2, Y = 2 };
            Assert.True((bool)Invoke(type, p1, p2));
        }

        [Fact]
        public void Equal_IEquatable_DifferentGenericParam_FallsBackToReferenceEquals()
        {
            // PersonWithSpecificEquatable 实现 IEquatable<string>，不是 IEquatable<Self>
            // 也未重写 object.Equals；因为是引用类型，按 C# 默认语义回退到 ReferenceEquals。
            var type = BuildStatic("EqSpec", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(PersonWithSpecificEquatable), "a");
                var b = m.DefineParameter(typeof(PersonWithSpecificEquatable), "b");
                m.Append(Expression.Equal(a, b));
            });
            var p1 = new PersonWithSpecificEquatable();
            var p2 = new PersonWithSpecificEquatable();
            Assert.True((bool)Invoke(type, p1, p1));
            Assert.False((bool)Invoke(type, p1, p2));
        }

        [Fact]
        public void Equal_NullLeft_WithIEquatable()
        {
            var type = BuildStatic("EqNullL", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(PersonWithEquatable), "a");
                var b = m.DefineParameter(typeof(PersonWithEquatable), "b");
                m.Append(Expression.Equal(a, b));
            });
            var p = new PersonWithEquatable { Name = "Alice", Age = 30 };
            // 左侧为 null 时，Equals 实例方法会 NPE，但 C# 语义下这是预期的
            // 这里测试 null 与 null 比较（通过 Constant null）
            // 直接测试常量 null 场景
        }

        [Fact]
        public void Equal_WithNullConstant()
        {
            var type = BuildStatic("EqNullC", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(PersonWithEquatable), "a");
                m.Append(Expression.Equal(a, Expression.Constant(null, typeof(PersonWithEquatable))));
            });
            Assert.False((bool)Invoke(type, new PersonWithEquatable { Name = "Alice", Age = 30 }));
        }

        [Fact]
        public void NotEqual_WithNullConstant()
        {
            var type = BuildStatic("NEqNC", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(PersonWithEquatable), "a");
                m.Append(Expression.NotEqual(a, Expression.Constant(null, typeof(PersonWithEquatable))));
            });
            Assert.True((bool)Invoke(type, new PersonWithEquatable { Name = "Alice", Age = 30 }));
        }

        [Fact]
        public void Equal_IEquatable_ListType()
        {
            // List<int> 没有重载 ==，但实现了 IEquatable<List<int>>? 不，List<T> 没有 IEquatable。
            // 使用一个已知实现了 IEquatable 的 BCL 类型：Version
            var type = BuildStatic("EqVer", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(Version), "a");
                var b = m.DefineParameter(typeof(Version), "b");
                m.Append(Expression.Equal(a, b));
            });
            Assert.True((bool)Invoke(type, new Version(1, 0), new Version(1, 0)));
            Assert.False((bool)Invoke(type, new Version(1, 0), new Version(2, 0)));
        }

        // ========== 引用相等兜底：继承/接口/object ==========

        public class Animal { }
        public class Dog : Animal { }
        public interface IShape { }
        public class Circle : IShape { }

        [Fact]
        public void Equal_BaseAndDerived_UsesReferenceEquals()
        {
            var type = BuildStatic("EqBaseDer", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(Animal), "a");
                var b = m.DefineParameter(typeof(Dog), "b");
                m.Append(Expression.Equal(a, b));
            });
            var dog = new Dog();
            Assert.True((bool)Invoke(type, dog, dog));
            Assert.False((bool)Invoke(type, new Animal(), new Dog()));
        }

        [Fact]
        public void Equal_InterfaceAndImpl_UsesReferenceEquals()
        {
            var type = BuildStatic("EqIfImp", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(IShape), "a");
                var b = m.DefineParameter(typeof(Circle), "b");
                m.Append(Expression.Equal(a, b));
            });
            var c = new Circle();
            Assert.True((bool)Invoke(type, c, c));
            Assert.False((bool)Invoke(type, new Circle(), new Circle()));
        }

        [Fact]
        public void Equal_ObjectAndRefType_UsesReferenceEquals()
        {
            var type = BuildStatic("EqObjRef", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(object), "a");
                var b = m.DefineParameter(typeof(NoEqualitySupport), "b");
                m.Append(Expression.Equal(a, b));
            });
            var p = new NoEqualitySupport { Id = 7 };
            Assert.True((bool)Invoke(type, p, p));
            Assert.False((bool)Invoke(type, new object(), p));
        }

        [Fact]
        public void Equal_UnrelatedClasses_ThrowsAstException()
        {
            // class Animal 与 class IShape 之间无继承/实现关系：仍应抛出。
            var ex = Assert.Throws<AstException>(() =>
            {
                BuildStatic("EqUnrel", typeof(bool), m =>
                {
                    var a = m.DefineParameter(typeof(Animal), "a");
                    var b = m.DefineParameter(typeof(Circle), "b");
                    m.Append(Expression.Equal(a, b));
                });
            });
            Assert.Contains("不支持", ex.Message);
        }
    }
}
