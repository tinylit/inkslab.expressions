using Xunit;
using System;
using System.Reflection;
using Inkslab.Emitters;

namespace Inkslab.Expressions.Tests
{
    /// <summary>
    /// UnaryExpression 对 Nullable&lt;T&gt; 的提升语义（lifted）测试。
    /// </summary>
    public class NullableUnaryTests
    {
        private readonly ModuleEmitter _mod = new ModuleEmitter($"NU_{Guid.NewGuid():N}");

        private Type BuildStatic(string name, Type ret, Type paramType, Action<MethodEmitter, ParameterEmitter> body)
        {
            var te = _mod.DefineType($"{name}_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var me = te.DefineMethod(name, MethodAttributes.Public | MethodAttributes.Static, ret);
            var p = me.DefineParameter(paramType, "p");
            body(me, p);
            return te.CreateType();
        }

        private object Invoke(Type t, string name, params object[] args) => t.GetMethod(name).Invoke(null, args);

        // ===== Negate (-x) =====

        [Fact]
        public void Negate_NullableInt_HasValue()
        {
            var t = BuildStatic("NIH", typeof(int?), typeof(int?), (m, p) => m.Append(Expression.Negate(p)));
            Assert.Equal(-5, (int?)Invoke(t, "NIH", (int?)5));
        }

        [Fact]
        public void Negate_NullableInt_Null()
        {
            var t = BuildStatic("NIN", typeof(int?), typeof(int?), (m, p) => m.Append(Expression.Negate(p)));
            Assert.Null((int?)Invoke(t, "NIN", new object[] { null }));
        }

        [Fact]
        public void Negate_NullableUInt_Throws()
        {
            Assert.ThrowsAny<Exception>(() =>
            {
                BuildStatic("NUI", typeof(uint?), typeof(uint?), (m, p) => m.Append(Expression.Negate(p)));
            });
        }

        // ===== Not (~x / !x) =====

        [Fact]
        public void Not_NullableBool_True()
        {
            var t = BuildStatic("NBT", typeof(bool?), typeof(bool?), (m, p) => m.Append(Expression.Not(p)));
            Assert.Equal(false, (bool?)Invoke(t, "NBT", (bool?)true));
        }

        [Fact]
        public void Not_NullableBool_False()
        {
            var t = BuildStatic("NBF", typeof(bool?), typeof(bool?), (m, p) => m.Append(Expression.Not(p)));
            Assert.Equal(true, (bool?)Invoke(t, "NBF", (bool?)false));
        }

        [Fact]
        public void Not_NullableBool_Null()
        {
            var t = BuildStatic("NBN", typeof(bool?), typeof(bool?), (m, p) => m.Append(Expression.Not(p)));
            Assert.Null((bool?)Invoke(t, "NBN", new object[] { null }));
        }

        [Fact]
        public void Not_NullableInt_BitwiseNot()
        {
            var t = BuildStatic("NIB", typeof(int?), typeof(int?), (m, p) => m.Append(Expression.Not(p)));
            Assert.Equal(~5, (int?)Invoke(t, "NIB", (int?)5));
        }

        [Fact]
        public void Not_NullableDouble_Throws()
        {
            Assert.ThrowsAny<Exception>(() =>
            {
                BuildStatic("NDB", typeof(double?), typeof(double?), (m, p) => m.Append(Expression.Not(p)));
            });
        }

        // ===== Increment / Decrement =====

        [Fact]
        public void Increment_NullableInt_HasValue()
        {
            var t = BuildStatic("INI", typeof(int?), typeof(int?), (m, p) => m.Append(Expression.Increment(p)));
            Assert.Equal(6, (int?)Invoke(t, "INI", (int?)5));
        }

        [Fact]
        public void Increment_NullableInt_Null()
        {
            var t = BuildStatic("INN", typeof(int?), typeof(int?), (m, p) => m.Append(Expression.Increment(p)));
            Assert.Null((int?)Invoke(t, "INN", new object[] { null }));
        }

        [Fact]
        public void Decrement_NullableLong_HasValue()
        {
            var t = BuildStatic("DLH", typeof(long?), typeof(long?), (m, p) => m.Append(Expression.Decrement(p)));
            Assert.Equal(9L, (long?)Invoke(t, "DLH", (long?)10L));
        }

        [Fact]
        public void IncrementAssign_NullableInt_HasValue()
        {
            var t = BuildStatic("IAH", typeof(int?), typeof(int?), (m, p) =>
            {
                var v = Expression.Variable(typeof(int?));
                m.Append(Expression.Assign(v, p));
                m.Append(Expression.IncrementAssign(v));
                m.Append(v);
            });
            Assert.Equal(11, (int?)Invoke(t, "IAH", (int?)10));
        }

        [Fact]
        public void IncrementAssign_NullableInt_NullStaysNull()
        {
            var t = BuildStatic("IAN", typeof(int?), typeof(int?), (m, p) =>
            {
                var v = Expression.Variable(typeof(int?));
                m.Append(Expression.Assign(v, p));
                m.Append(Expression.IncrementAssign(v));
                m.Append(v);
            });
            Assert.Null((int?)Invoke(t, "IAN", new object[] { null }));
        }

        [Fact]
        public void IncrementAssign_NullableBool_Throws()
        {
            Assert.ThrowsAny<Exception>(() =>
            {
                BuildStatic("IAB", typeof(bool?), typeof(bool?), (m, p) =>
                {
                    var v = Expression.Variable(typeof(bool?));
                    m.Append(Expression.Assign(v, p));
                    m.Append(Expression.IncrementAssign(v));
                    m.Append(v);
                });
            });
        }

        // ===== IsFalse —— C# 原生不支持 bool? 的 false 运算符，应抛错 =====

        [Fact]
        public void IsFalse_NullableBool_Throws()
        {
            Assert.ThrowsAny<Exception>(() =>
            {
                BuildStatic("IFNB", typeof(bool?), typeof(bool?), (m, p) => m.Append(Expression.IsFalse(p)));
            });
        }
    }
}
