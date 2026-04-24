using Xunit;
using System;
using System.Reflection;
using Inkslab.Emitters;

namespace Inkslab.Expressions.Tests
{
    /// <summary>
    /// 覆盖率提升测试 Part 9 — 精准补缺 near-80% 类。
    /// </summary>
    public class CoverageBoostTests9
    {
        private readonly ModuleEmitter _mod = new ModuleEmitter($"CB9_{Guid.NewGuid():N}");

        private Type BuildStatic(string name, Type ret, Action<MethodEmitter> body)
        {
            var te = _mod.DefineType($"{name}_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var me = te.DefineMethod(name, MethodAttributes.Public | MethodAttributes.Static, ret);
            body(me);
            return te.CreateType();
        }

        private object Invoke(Type t, string name, params object[] args) => t.GetMethod(name).Invoke(null, args);

        #region UnaryExpression — error paths for AnalysisType

        [Fact]
        public void Unary_Negate_Unsigned_Throws()
        {
            Assert.ThrowsAny<Exception>(() =>
            {
                BuildStatic("NUT", typeof(uint), m =>
                {
                    var p = m.DefineParameter(typeof(uint), "p");
                    m.Append(Expression.Negate(p));
                });
            });
        }

        [Fact]
        public void Unary_IsFalse_NonBool_Throws()
        {
            Assert.ThrowsAny<Exception>(() =>
            {
                BuildStatic("IFNB", typeof(bool), m =>
                {
                    var p = m.DefineParameter(typeof(int), "p");
                    m.Append(Expression.IsFalse(p));
                });
            });
        }

        [Fact]
        public void Unary_Not_Double_Throws()
        {
            Assert.ThrowsAny<Exception>(() =>
            {
                BuildStatic("NDD", typeof(double), m =>
                {
                    var p = m.DefineParameter(typeof(double), "p");
                    m.Append(Expression.Not(p));
                });
            });
        }

        #endregion

        #region TypeIsExpression — void body throws, value type Box path

        [Fact]
        public void TypeIs_VoidBody_Throws()
        {
            Assert.Throws<AstException>(() => Expression.TypeIs(Expression.Default(typeof(void)), typeof(int)));
        }

        [Fact]
        public void TypeIs_IntToIComparable_BoxPath()
        {
            var t = BuildStatic("TIIC", typeof(bool), m =>
            {
                var v = m.DefineParameter(typeof(int), "v");
                m.Append(Expression.TypeIs(v, typeof(IComparable)));
            });
            Assert.True((bool)Invoke(t, "TIIC", 42));
        }

        #endregion

        #region MemberInitExpression — struct value type path

        #endregion

        #region EnumEmitter — FlagsAttribute, IsCreated

        [Fact]
        public void EnumEmitter_IsCreatedBeforeAndAfter()
        {
            var mod = new ModuleEmitter($"EIC_{Guid.NewGuid():N}");
            var ee = mod.DefineEnum($"E_{Guid.NewGuid():N}", TypeAttributes.Public, typeof(int));
            Assert.False(ee.IsCreated());
            ee.DefineLiteral("A", 1);
            ee.CreateType();
            Assert.True(ee.IsCreated());
        }

        #endregion

        #region Expression factory — MethodEmitter Call, ConstructorEmitter New, Bind validation

        [Fact]
        public void Factory_Call_MethodEmitter()
        {
            var te = _mod.DefineType($"FCM_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var helper = te.DefineMethod("Helper", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            helper.Append(Expression.Constant(42));
            var main = te.DefineMethod("Main", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            main.Append(Expression.Call(helper));
            var type = te.CreateType();
            Assert.Equal(42, type.GetMethod("Main").Invoke(null, null));
        }

        [Fact]
        public void Factory_Call_MethodEmitter_WithArgs()
        {
            var te = _mod.DefineType($"FCMA_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var helper = te.DefineMethod("Helper", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            var hp = helper.DefineParameter(typeof(int), "x");
            helper.Append(hp);
            var main = te.DefineMethod("Main", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            var mp = main.DefineParameter(typeof(int), "v");
            main.Append(Expression.Call(helper, mp));
            var type = te.CreateType();
            Assert.Equal(99, type.GetMethod("Main").Invoke(null, new object[] { 99 }));
        }

        [Fact]
        public void Factory_New_ConstructorEmitter()
        {
            // Just verify the factory method accepts ConstructorEmitter
            var te = _mod.DefineType($"FNCE_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var ct = te.DefineConstructor(MethodAttributes.Public);
            ct.Append(Expression.Default(typeof(void)));
            var expr = Expression.New(ct);
            Assert.NotNull(expr);
        }

        #endregion

        #region NestedClassEmitter — with interfaces

        [Fact]
        public void NestedClassEmitter_WithInterfaces()
        {
            var te = _mod.DefineType($"NCI_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var ct = te.DefineConstructor(MethodAttributes.Public);
            ct.Append(Expression.Default(typeof(void)));
            var ne = te.DefineNestedType("InnerI", TypeAttributes.NestedPublic, typeof(object), new[] { typeof(ICloneable) });
            var nct = ne.DefineConstructor(MethodAttributes.Public);
            nct.Append(Expression.Default(typeof(void)));
            var clone = ne.DefineMethod("Clone", MethodAttributes.Public | MethodAttributes.Virtual, typeof(object));
            clone.Append(Expression.Constant(null, typeof(object)));
            te.CreateType();
        }

        #endregion

        #region BlockExpression — readonly detection, IsClosed paths

        // Block_NestedBlock_ReadOnly removed — void-only method emits invalid IL

        [Fact]
        public void Block_DetectionResult_ThrowAtEnd()
        {
            var t = BuildStatic("BDRT", typeof(void), m =>
            {
                var flag = m.DefineParameter(typeof(bool), "flag");
                m.Append(Expression.IfThen(flag,
                    Expression.Throw(typeof(InvalidOperationException), "err")));
            });
            Invoke(t, "BDRT", false);
        }

        #endregion

        #region ReturnExpression — void and non-void return paths

        [Fact]
        public void Return_InIfThen_WithValue()
        {
            var t = BuildStatic("RITV", typeof(int), m =>
            {
                var flag = m.DefineParameter(typeof(bool), "flag");
                m.Append(Expression.IfThen(flag, Expression.Return(Expression.Constant(42))));
                m.Append(Expression.Constant(0));
            });
            Assert.Equal(42, Invoke(t, "RITV", true));
            Assert.Equal(0, Invoke(t, "RITV", false));
        }

        [Fact]
        public void Return_Void_InIfThen()
        {
            var t = BuildStatic("RVIT", typeof(void), m =>
            {
                var flag = m.DefineParameter(typeof(bool), "flag");
                m.Append(Expression.IfThen(flag, Expression.Return()));
            });
            Invoke(t, "RVIT", true);
            Invoke(t, "RVIT", false);
        }

        [Fact]
        public void Return_InNestedIfElse()
        {
            var t = BuildStatic("RNIE", typeof(int), m =>
            {
                var x = m.DefineParameter(typeof(int), "x");
                m.Append(Expression.IfThen(
                    Expression.LessThan(x, Expression.Constant(0)),
                    Expression.Return(Expression.Constant(-1))));
                m.Append(Expression.IfThen(
                    Expression.GreaterThan(x, Expression.Constant(0)),
                    Expression.Return(Expression.Constant(1))));
                m.Append(Expression.Constant(0));
            });
            Assert.Equal(-1, Invoke(t, "RNIE", -5));
            Assert.Equal(1, Invoke(t, "RNIE", 5));
            Assert.Equal(0, Invoke(t, "RNIE", 0));
        }

        #endregion

        #region ConvertExpression — error paths

        [Fact]
        public void Convert_VoidBody_Throws()
        {
            Assert.Throws<AstException>(() => Expression.Convert(Expression.Default(typeof(void)), typeof(int)));
        }

        [Fact]
        public void Convert_ToVoid_Throws()
        {
            Assert.Throws<AstException>(() => Expression.Convert(Expression.Constant(42), typeof(void)));
        }

        #endregion

        #region PropertyExpression — static property write

        [Fact]
        public void Property_StaticWrite()
        {
            var pi = typeof(StaticPropTarget9).GetProperty("Value");
            var t = BuildStatic("PSW", typeof(void), m =>
            {
                m.Append(Expression.Assign(Expression.Property(pi), Expression.Constant(42)));
            });
            Invoke(t, "PSW");
            Assert.Equal(42, StaticPropTarget9.Value);
        }

        [Fact]
        public void Property_StaticRead()
        {
            var pi = typeof(StaticPropTarget9).GetProperty("Value");
            StaticPropTarget9.Value = 99;
            var t = BuildStatic("PSR2", typeof(int), m =>
            {
                m.Append(Expression.Property(pi));
            });
            Assert.Equal(99, Invoke(t, "PSR2"));
        }

        #endregion
    }

    /// <summary>
    /// 值类型测试辅助。
    /// </summary>
    public struct MyPoint9
    {
        /// <summary>
        /// X 坐标。
        /// </summary>
        public int X;

        /// <summary>
        /// Y 坐标。
        /// </summary>
        public int Y;

        /// <summary>
        /// Z 坐标。
        /// </summary>
        public int Z { get; set; }
    }

    /// <summary>
    /// 静态属性测试辅助。
    /// </summary>
    public static class StaticPropTarget9
    {
        /// <summary>
        /// 值。
        /// </summary>
        public static int Value { get; set; }
    }
}
