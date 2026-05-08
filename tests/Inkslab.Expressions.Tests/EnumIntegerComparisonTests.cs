using System;
using System.Reflection;
using Inkslab.Emitters;
using Xunit;

namespace Inkslab.Expressions.Tests
{
    /// <summary>
    /// 测试 enum 与其底层整数类型之间的比较：
    /// MyEnum == int / int == MyEnum / MyEnum &lt; int 等。
    /// </summary>
    public class EnumIntegerComparisonTests
    {
        private readonly ModuleEmitter _mod = new ModuleEmitter($"EnumCmp_{Guid.NewGuid():N}");

        public enum Color { Red = 0, Green = 1, Blue = 2 }
        public enum Big : long { A = 0, B = 100 }
        public enum Tiny : byte { X = 1, Y = 2 }

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

        // ========== Equal/NotEqual ==========

        [Fact]
        public void Equal_EnumLeft_IntRight()
        {
            var type = BuildStatic("EqEnumInt", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(Color), "a");
                var b = m.DefineParameter(typeof(int), "b");
                m.Append(Expression.Equal(a, b));
            });
            Assert.True((bool)Invoke(type, Color.Green, 1));
            Assert.False((bool)Invoke(type, Color.Green, 2));
        }

        [Fact]
        public void Equal_IntLeft_EnumRight()
        {
            var type = BuildStatic("EqIntEnum", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(int), "a");
                var b = m.DefineParameter(typeof(Color), "b");
                m.Append(Expression.Equal(a, b));
            });
            Assert.True((bool)Invoke(type, 2, Color.Blue));
            Assert.False((bool)Invoke(type, 2, Color.Red));
        }

        [Fact]
        public void NotEqual_EnumLeft_IntRight()
        {
            var type = BuildStatic("NEqEnumInt", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(Color), "a");
                var b = m.DefineParameter(typeof(int), "b");
                m.Append(Expression.NotEqual(a, b));
            });
            Assert.False((bool)Invoke(type, Color.Red, 0));
            Assert.True((bool)Invoke(type, Color.Red, 1));
        }

        [Fact]
        public void Equal_LongEnum_AndLong()
        {
            var type = BuildStatic("EqLongEnum", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(Big), "a");
                var b = m.DefineParameter(typeof(long), "b");
                m.Append(Expression.Equal(a, b));
            });
            Assert.True((bool)Invoke(type, Big.B, 100L));
            Assert.False((bool)Invoke(type, Big.A, 100L));
        }

        [Fact]
        public void Equal_ByteEnum_AndByte()
        {
            var type = BuildStatic("EqByteEnum", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(Tiny), "a");
                var b = m.DefineParameter(typeof(byte), "b");
                m.Append(Expression.Equal(a, b));
            });
            Assert.True((bool)Invoke(type, Tiny.Y, (byte)2));
            Assert.False((bool)Invoke(type, Tiny.X, (byte)2));
        }

        // ========== 关系比较 ==========

        [Fact]
        public void LessThan_EnumLeft_IntRight()
        {
            var type = BuildStatic("LtEnumInt", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(Color), "a");
                var b = m.DefineParameter(typeof(int), "b");
                m.Append(Expression.LessThan(a, b));
            });
            Assert.True((bool)Invoke(type, Color.Red, 1));
            Assert.False((bool)Invoke(type, Color.Blue, 1));
        }

        [Fact]
        public void GreaterThanOrEqual_IntLeft_EnumRight()
        {
            var type = BuildStatic("GeIntEnum", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(int), "a");
                var b = m.DefineParameter(typeof(Color), "b");
                m.Append(Expression.GreaterThanOrEqual(a, b));
            });
            Assert.True((bool)Invoke(type, 2, Color.Green));
            Assert.True((bool)Invoke(type, 1, Color.Green));
            Assert.False((bool)Invoke(type, 0, Color.Green));
        }
    }
}
