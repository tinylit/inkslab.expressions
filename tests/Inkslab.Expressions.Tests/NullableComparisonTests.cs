using System;
using System.Reflection;
using Inkslab.Emitters;
using Xunit;

namespace Inkslab.Expressions.Tests
{
    /// <summary>
    /// 测试可空类型的比较操作：T? == T?, T? == T, T == T?
    /// </summary>
    public class NullableComparisonTests
    {
        private readonly ModuleEmitter _mod = new ModuleEmitter($"NullCmp_{Guid.NewGuid():N}");

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

        // ========== 两个操作数都是可空类型：T? == T? ==========

        [Fact]
        public void Equal_BothNullable_BothHaveValue_Equal()
        {
            var type = BuildStatic("EqNN_VV_T", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(int?), "a");
                var b = m.DefineParameter(typeof(int?), "b");
                m.Append(Expression.Equal(a, b));
            });
            Assert.True((bool)Invoke(type, 42, 42));
        }

        [Fact]
        public void Equal_BothNullable_BothHaveValue_NotEqual()
        {
            var type = BuildStatic("EqNN_VV_F", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(int?), "a");
                var b = m.DefineParameter(typeof(int?), "b");
                m.Append(Expression.Equal(a, b));
            });
            Assert.False((bool)Invoke(type, 42, 99));
        }

        [Fact]
        public void Equal_BothNullable_BothNull()
        {
            var type = BuildStatic("EqNN_NN", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(int?), "a");
                var b = m.DefineParameter(typeof(int?), "b");
                m.Append(Expression.Equal(a, b));
            });
            Assert.True((bool)Invoke(type, null, null));
        }

        [Fact]
        public void Equal_BothNullable_LeftNull_RightHasValue()
        {
            var type = BuildStatic("EqNN_NV", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(int?), "a");
                var b = m.DefineParameter(typeof(int?), "b");
                m.Append(Expression.Equal(a, b));
            });
            Assert.False((bool)Invoke(type, null, 42));
        }

        [Fact]
        public void Equal_BothNullable_LeftHasValue_RightNull()
        {
            var type = BuildStatic("EqNN_VN", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(int?), "a");
                var b = m.DefineParameter(typeof(int?), "b");
                m.Append(Expression.Equal(a, b));
            });
            Assert.False((bool)Invoke(type, 42, null));
        }

        [Fact]
        public void NotEqual_BothNullable_BothHaveValue_Equal()
        {
            var type = BuildStatic("NEqNN_VV_F", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(int?), "a");
                var b = m.DefineParameter(typeof(int?), "b");
                m.Append(Expression.NotEqual(a, b));
            });
            Assert.False((bool)Invoke(type, 42, 42));
        }

        [Fact]
        public void NotEqual_BothNullable_BothHaveValue_NotEqual()
        {
            var type = BuildStatic("NEqNN_VV_T", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(int?), "a");
                var b = m.DefineParameter(typeof(int?), "b");
                m.Append(Expression.NotEqual(a, b));
            });
            Assert.True((bool)Invoke(type, 42, 99));
        }

        [Fact]
        public void NotEqual_BothNullable_BothNull()
        {
            var type = BuildStatic("NEqNN_NN", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(int?), "a");
                var b = m.DefineParameter(typeof(int?), "b");
                m.Append(Expression.NotEqual(a, b));
            });
            Assert.False((bool)Invoke(type, null, null));
        }

        [Fact]
        public void NotEqual_BothNullable_OneNull()
        {
            var type = BuildStatic("NEqNN_ON", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(int?), "a");
                var b = m.DefineParameter(typeof(int?), "b");
                m.Append(Expression.NotEqual(a, b));
            });
            Assert.True((bool)Invoke(type, 42, null));
            Assert.True((bool)Invoke(type, null, 99));
        }

        // ========== 左操作数可空，右操作数不可空：T? == T ==========

        [Fact]
        public void Equal_LeftNullable_RightNonNullable_Equal()
        {
            var type = BuildStatic("EqLN_T", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(int?), "a");
                var b = m.DefineParameter(typeof(int), "b");
                m.Append(Expression.Equal(a, b));
            });
            Assert.True((bool)Invoke(type, 42, 42));
        }

        [Fact]
        public void Equal_LeftNullable_RightNonNullable_NotEqual()
        {
            var type = BuildStatic("EqLN_F", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(int?), "a");
                var b = m.DefineParameter(typeof(int), "b");
                m.Append(Expression.Equal(a, b));
            });
            Assert.False((bool)Invoke(type, 42, 99));
        }

        [Fact]
        public void Equal_LeftNullable_RightNonNullable_LeftNull()
        {
            var type = BuildStatic("EqLN_LN", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(int?), "a");
                var b = m.DefineParameter(typeof(int), "b");
                m.Append(Expression.Equal(a, b));
            });
            Assert.False((bool)Invoke(type, null, 42));
        }

        [Fact]
        public void NotEqual_LeftNullable_RightNonNullable_Equal()
        {
            var type = BuildStatic("NEqLN_F", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(int?), "a");
                var b = m.DefineParameter(typeof(int), "b");
                m.Append(Expression.NotEqual(a, b));
            });
            Assert.False((bool)Invoke(type, 42, 42));
        }

        [Fact]
        public void NotEqual_LeftNullable_RightNonNullable_NotEqual()
        {
            var type = BuildStatic("NEqLN_T", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(int?), "a");
                var b = m.DefineParameter(typeof(int), "b");
                m.Append(Expression.NotEqual(a, b));
            });
            Assert.True((bool)Invoke(type, 42, 99));
        }

        [Fact]
        public void NotEqual_LeftNullable_RightNonNullable_LeftNull()
        {
            var type = BuildStatic("NEqLN_LN", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(int?), "a");
                var b = m.DefineParameter(typeof(int), "b");
                m.Append(Expression.NotEqual(a, b));
            });
            Assert.True((bool)Invoke(type, null, 42));
        }

        // ========== 左操作数不可空，右操作数可空：T == T? ==========

        [Fact]
        public void Equal_LeftNonNullable_RightNullable_Equal()
        {
            var type = BuildStatic("EqRN_T", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(int), "a");
                var b = m.DefineParameter(typeof(int?), "b");
                m.Append(Expression.Equal(a, b));
            });
            Assert.True((bool)Invoke(type, 42, 42));
        }

        [Fact]
        public void Equal_LeftNonNullable_RightNullable_NotEqual()
        {
            var type = BuildStatic("EqRN_F", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(int), "a");
                var b = m.DefineParameter(typeof(int?), "b");
                m.Append(Expression.Equal(a, b));
            });
            Assert.False((bool)Invoke(type, 42, 99));
        }

        [Fact]
        public void Equal_LeftNonNullable_RightNullable_RightNull()
        {
            var type = BuildStatic("EqRN_RN", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(int), "a");
                var b = m.DefineParameter(typeof(int?), "b");
                m.Append(Expression.Equal(a, b));
            });
            Assert.False((bool)Invoke(type, 42, null));
        }

        [Fact]
        public void NotEqual_LeftNonNullable_RightNullable_Equal()
        {
            var type = BuildStatic("NEqRN_F", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(int), "a");
                var b = m.DefineParameter(typeof(int?), "b");
                m.Append(Expression.NotEqual(a, b));
            });
            Assert.False((bool)Invoke(type, 42, 42));
        }

        [Fact]
        public void NotEqual_LeftNonNullable_RightNullable_NotEqual()
        {
            var type = BuildStatic("NEqRN_T", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(int), "a");
                var b = m.DefineParameter(typeof(int?), "b");
                m.Append(Expression.NotEqual(a, b));
            });
            Assert.True((bool)Invoke(type, 42, 99));
        }

        [Fact]
        public void NotEqual_LeftNonNullable_RightNullable_RightNull()
        {
            var type = BuildStatic("NEqRN_RN", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(int), "a");
                var b = m.DefineParameter(typeof(int?), "b");
                m.Append(Expression.NotEqual(a, b));
            });
            Assert.True((bool)Invoke(type, 42, null));
        }

        // ========== 其他可空类型测试 ==========

        [Fact]
        public void Equal_BothNullable_Double()
        {
            var type = BuildStatic("EqNN_Dbl", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(double?), "a");
                var b = m.DefineParameter(typeof(double?), "b");
                m.Append(Expression.Equal(a, b));
            });
            Assert.True((bool)Invoke(type, 3.14, 3.14));
            Assert.False((bool)Invoke(type, 3.14, 2.71));
            Assert.True((bool)Invoke(type, null, null));
            Assert.False((bool)Invoke(type, 3.14, null));
        }

        [Fact]
        public void Equal_MixedNullable_Long()
        {
            var type = BuildStatic("EqMix_Lng", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(long?), "a");
                var b = m.DefineParameter(typeof(long), "b");
                m.Append(Expression.Equal(a, b));
            });
            Assert.True((bool)Invoke(type, 100L, 100L));
            Assert.False((bool)Invoke(type, null, 100L));
        }

        [Fact]
        public void NotEqual_MixedNullable_Byte()
        {
            var type = BuildStatic("NEqMix_Byt", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(byte), "a");
                var b = m.DefineParameter(typeof(byte?), "b");
                m.Append(Expression.NotEqual(a, b));
            });
            Assert.False((bool)Invoke(type, (byte)10, (byte)10));
            Assert.True((bool)Invoke(type, (byte)10, null));
        }
    }
}
