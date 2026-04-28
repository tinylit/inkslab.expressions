using Inkslab.Emitters;
using System;
using System.Reflection;
using Xunit;

namespace Inkslab.Expressions.Tests
{
    /// <summary>
    /// Coverage boost: MethodEmitter, ParameterExpression ByRef, TryExpression catch+finally,
    /// InvocationEmitter, NewInstanceEmitter, ConvertExpression numeric paths, DynamicMethod.
    /// </summary>
    public class CoverageBoostTests13
    {
        private readonly ModuleEmitter _mod = new ModuleEmitter($"CB13_{Guid.NewGuid():N}");

        private Type BuildStatic(string name, Type ret, Action<MethodEmitter> body)
        {
            var cls = _mod.DefineType($"C13_{name}", TypeAttributes.Public);
            var me = cls.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, ret);
            body(me);
            return cls.CreateType();
        }

        private Type BuildStaticP(string name, Type ret, Type p1, Action<MethodEmitter, ParameterExpression> body)
        {
            var cls = _mod.DefineType($"C13P_{name}", TypeAttributes.Public);
            var me = cls.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, ret);
            var param = me.DefineParameter(p1, ParameterAttributes.None, "a");
            body(me, param);
            return cls.CreateType();
        }

        private object Invoke(Type t, params object[] args) => t.GetMethod("Run").Invoke(null, args);

        // ===== TryExpression: catch + finally combined =====

        [Fact]
        public void Try_CatchAndFinally()
        {
            var t = BuildStatic("TCF", typeof(int), me =>
            {
                var result = Expression.Variable(typeof(int));
                me.Append(Expression.Assign(result, Expression.Constant(0)));

                var tryExpr = Expression.Try(Expression.Assign(result, Expression.Add(result, Expression.Constant(100))));
                tryExpr.Append(Expression.Throw(typeof(InvalidOperationException)));
                tryExpr.Catch().Append(Expression.Assign(result, Expression.Constant(10)));

                me.Append(tryExpr);
                me.Append(result);
            });
            Assert.Equal(110, Invoke(t));
        }

        [Fact]
        public void Try_FinallyNoException()
        {
            var t = BuildStatic("TFN", typeof(int), me =>
            {
                var result = Expression.Variable(typeof(int));
                me.Append(Expression.Assign(result, Expression.Constant(0)));

                var tryExpr = Expression.Try(Expression.Assign(result, Expression.Add(result, Expression.Constant(1))));
                tryExpr.Append(Expression.Assign(result, Expression.Constant(42)));

                me.Append(tryExpr);
                me.Append(result);
            });
            Assert.Equal(43, Invoke(t));
        }

        [Fact]
        public void Try_CatchWithVariableAndFinally()
        {
            var t = BuildStatic("TCVF", typeof(int), me =>
            {
                var result = Expression.Variable(typeof(int));
                me.Append(Expression.Assign(result, Expression.Constant(0)));

                var finallyExpr = Expression.Assign(result, Expression.Add(result, Expression.Constant(100)));
                var tryExpr = Expression.Try(finallyExpr);
                tryExpr.Append(Expression.Throw(typeof(InvalidOperationException)));

                var exVar = Expression.Variable(typeof(Exception));
                tryExpr.Catch(exVar).Append(Expression.Assign(result, Expression.Constant(10)));

                me.Append(tryExpr);
                me.Append(result);
            });
            Assert.Equal(110, Invoke(t));
        }

        // ===== ConvertExpression: various numeric conversions =====

        [Fact]
        public void Convert_IntToShort()
        {
            var t = BuildStaticP("I2S", typeof(short), typeof(int), (me, a) =>
            {
                me.Append(Expression.Convert(a, typeof(short)));
            });
            Assert.Equal((short)42, Invoke(t, 42));
        }

        [Fact]
        public void Convert_IntToByte()
        {
            var t = BuildStaticP("I2B", typeof(byte), typeof(int), (me, a) =>
            {
                me.Append(Expression.Convert(a, typeof(byte)));
            });
            Assert.Equal((byte)42, Invoke(t, 42));
        }

        [Fact]
        public void Convert_IntToSByte()
        {
            var t = BuildStaticP("I2SB", typeof(sbyte), typeof(int), (me, a) =>
            {
                me.Append(Expression.Convert(a, typeof(sbyte)));
            });
            Assert.Equal((sbyte)42, Invoke(t, 42));
        }

        [Fact]
        public void Convert_IntToLong()
        {
            var t = BuildStaticP("I2L", typeof(long), typeof(int), (me, a) =>
            {
                me.Append(Expression.Convert(a, typeof(long)));
            });
            Assert.Equal(42L, Invoke(t, 42));
        }

        [Fact]
        public void Convert_IntToULong()
        {
            var t = BuildStaticP("I2UL", typeof(ulong), typeof(int), (me, a) =>
            {
                me.Append(Expression.Convert(a, typeof(ulong)));
            });
            Assert.Equal(42UL, Invoke(t, 42));
        }

        [Fact]
        public void Convert_IntToUInt()
        {
            var t = BuildStaticP("I2UI", typeof(uint), typeof(int), (me, a) =>
            {
                me.Append(Expression.Convert(a, typeof(uint)));
            });
            Assert.Equal(42u, Invoke(t, 42));
        }

        [Fact]
        public void Convert_IntToUShort()
        {
            var t = BuildStaticP("I2US", typeof(ushort), typeof(int), (me, a) =>
            {
                me.Append(Expression.Convert(a, typeof(ushort)));
            });
            Assert.Equal((ushort)42, Invoke(t, 42));
        }

        [Fact]
        public void Convert_IntToFloat()
        {
            var t = BuildStaticP("I2F", typeof(float), typeof(int), (me, a) =>
            {
                me.Append(Expression.Convert(a, typeof(float)));
            });
            Assert.Equal(42.0f, Invoke(t, 42));
        }

        [Fact]
        public void Convert_IntToDouble()
        {
            var t = BuildStaticP("I2D", typeof(double), typeof(int), (me, a) =>
            {
                me.Append(Expression.Convert(a, typeof(double)));
            });
            Assert.Equal(42.0, Invoke(t, 42));
        }

        [Fact]
        public void Convert_UIntToFloat()
        {
            var t = BuildStaticP("UI2F", typeof(float), typeof(uint), (me, a) =>
            {
                me.Append(Expression.Convert(a, typeof(float)));
            });
            Assert.Equal(42.0f, Invoke(t, 42u));
        }

        [Fact]
        public void Convert_UIntToDouble()
        {
            var t = BuildStaticP("UI2D", typeof(double), typeof(uint), (me, a) =>
            {
                me.Append(Expression.Convert(a, typeof(double)));
            });
            Assert.Equal(42.0, Invoke(t, 42u));
        }

        [Fact]
        public void Convert_LongToFloat()
        {
            var t = BuildStaticP("L2F", typeof(float), typeof(long), (me, a) =>
            {
                me.Append(Expression.Convert(a, typeof(float)));
            });
            Assert.Equal(42.0f, Invoke(t, 42L));
        }

        [Fact]
        public void Convert_DoubleToFloat()
        {
            var t = BuildStaticP("D2F", typeof(float), typeof(double), (me, a) =>
            {
                me.Append(Expression.Convert(a, typeof(float)));
            });
            Assert.Equal(3.14f, (float)Invoke(t, 3.14));
        }

        [Fact]
        public void Convert_FloatToDouble()
        {
            var t = BuildStaticP("F2D", typeof(double), typeof(float), (me, a) =>
            {
                me.Append(Expression.Convert(a, typeof(double)));
            });
            var result = (double)Invoke(t, 3.14f);
            Assert.True(Math.Abs(result - 3.14) < 0.01);
        }

        [Fact]
        public void Convert_ULongToDouble()
        {
            var t = BuildStaticP("UL2D", typeof(double), typeof(ulong), (me, a) =>
            {
                me.Append(Expression.Convert(a, typeof(double)));
            });
            Assert.Equal(42.0, Invoke(t, 42UL));
        }

        [Fact]
        public void Convert_CharToInt()
        {
            var t = BuildStaticP("C2I", typeof(int), typeof(char), (me, a) =>
            {
                me.Append(Expression.Convert(a, typeof(int)));
            });
            Assert.Equal(65, Invoke(t, 'A'));
        }

        // ===== MethodEmitter: multiple method in class =====

        [Fact]
        public void MethodEmitter_Properties()
        {
            var cls = _mod.DefineType("ME_Props", TypeAttributes.Public);
            var me = cls.DefineMethod("Foo", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            Assert.Equal("Foo", me.Name);
            Assert.Equal(typeof(int), me.ReturnType);
            Assert.True(me.IsStatic);
            Assert.False(me.IsGenericMethod);

            me.Append(Expression.Constant(42));
            var t = cls.CreateType();
            Assert.Equal(42, t.GetMethod("Foo").Invoke(null, null));
        }

        [Fact]
        public void MethodEmitter_SetCustomAttribute()
        {
            var cls = _mod.DefineType("ME_CA", TypeAttributes.Public);
            var me = cls.DefineMethod("Bar", MethodAttributes.Public | MethodAttributes.Static, typeof(void));
            me.SetCustomAttribute<ObsoleteAttribute>();
            me.Append(Expression.Default(typeof(void)));
            var t = cls.CreateType();
            Assert.NotNull(t.GetMethod("Bar").GetCustomAttribute<ObsoleteAttribute>());
        }

        [Fact]
        public void MethodEmitter_DefineParameter_Simple()
        {
            var cls = _mod.DefineType("ME_DP", TypeAttributes.Public);
            var me = cls.DefineMethod("Add", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            var p1 = me.DefineParameter(typeof(int), "x");
            var p2 = me.DefineParameter(typeof(int), "y");
            me.Append(Expression.Add(p1, p2));
            var t = cls.CreateType();
            Assert.Equal(7, t.GetMethod("Add").Invoke(null, new object[] { 3, 4 }));
        }

        // ===== InvocationEmitter: call MethodEmitter with array args =====

        [Fact]
        public void InvocationEmitter_StaticReturnOnly()
        {
            var cls = _mod.DefineType("IE1", TypeAttributes.Public);
            var helper = cls.DefineMethod("Helper", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            helper.Append(Expression.Constant(42));

            var run = cls.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, typeof(object));
            var args = Expression.Array(typeof(object));
            run.Append(Expression.Invoke(helper, args));
            var t = cls.CreateType();
            Assert.Equal(42, t.GetMethod("Run").Invoke(null, null));
        }

        [Fact]
        public void InvocationEmitter_StaticReturnCall()
        {
            var cls = _mod.DefineType("IE2", TypeAttributes.Public);
            var helper = cls.DefineMethod("GetNum", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            helper.Append(Expression.Constant(99));

            var run = cls.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, typeof(object));
            var args = Expression.Array(typeof(object));
            run.Append(Expression.Invoke(helper, args));
            var t = cls.CreateType();
            Assert.Equal(99, t.GetMethod("Run").Invoke(null, null));
        }

        // ===== NewInstanceEmitter =====

        [Fact]
        public void NewInstanceEmitter_DefaultConstructor()
        {
            var cls = _mod.DefineType("NIE1", TypeAttributes.Public);
            var ctor = cls.DefineConstructor(MethodAttributes.Public);
            ctor.InvokeBaseConstructor();

            var run = cls.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, typeof(object));
            run.Append(Expression.Convert(Expression.New(ctor), typeof(object)));
            var t = cls.CreateType();
            var result = t.GetMethod("Run").Invoke(null, null);
            Assert.NotNull(result);
        }

        [Fact]
        public void NewInstanceEmitter_WithParams()
        {
            var cls = _mod.DefineType("NIE2", TypeAttributes.Public);
            var field = cls.DefineField("_val", typeof(int));

            var ctor = cls.DefineConstructor(MethodAttributes.Public);
            var cp = ctor.DefineParameter(typeof(int), ParameterAttributes.None, "val");
            ctor.InvokeBaseConstructor();
            ctor.Append(Expression.Assign(field, cp));

            var getVal = cls.DefineMethod("GetVal", MethodAttributes.Public, typeof(int));
            getVal.Append(field);

            var run = cls.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            var newExpr = Expression.New(ctor, Expression.Constant(42));
            var v = Expression.Variable(typeof(object));
            run.Append(Expression.Assign(v, newExpr));
            run.Append(Expression.Call(v, typeof(object).GetMethod("GetHashCode")));

            var t = cls.CreateType();
            // Just verify it doesn't throw
            Assert.NotNull(t.GetMethod("Run").Invoke(null, null));
        }

        // ===== ConditionExpression: ternary =====

        [Fact]
        public void Condition_Ternary()
        {
            var t = BuildStaticP("Tern", typeof(int), typeof(int), (me, a) =>
            {
                me.Append(Expression.Condition(
                    Expression.GreaterThan(a, Expression.Constant(0)),
                    Expression.Constant(1),
                    Expression.Constant(-1)
                ));
            });
            Assert.Equal(1, Invoke(t, 5));
            Assert.Equal(-1, Invoke(t, -5));
        }

        // ===== ThrowExpression =====

        [Fact]
        public void Throw_NewException()
        {
            var t = BuildStaticP("Thr", typeof(int), typeof(bool), (me, a) =>
            {
                me.Append(Expression.IfThen(a, Expression.Throw(typeof(InvalidOperationException))));
                me.Append(Expression.Constant(0));
            });
            Assert.Equal(0, Invoke(t, false));
            Assert.Throws<TargetInvocationException>(() => Invoke(t, true));
        }

        // ===== CoalesceExpression =====

        [Fact]
        public void Coalesce_WithValue()
        {
            var t = BuildStaticP("Coal", typeof(string), typeof(string), (me, a) =>
            {
                me.Append(Expression.Coalesce(a, Expression.Constant("default")));
            });
            Assert.Equal("hello", Invoke(t, "hello"));
            Assert.Equal("default", Invoke(t, (string)null));
        }

        // ===== TypeAs / TypeIs =====

        [Fact]
        public void TypeAs_Success()
        {
            var t = BuildStaticP("TAs", typeof(string), typeof(object), (me, a) =>
            {
                me.Append(Expression.TypeAs(a, typeof(string)));
            });
            Assert.Equal("hello", Invoke(t, (object)"hello"));
            Assert.Null(Invoke(t, (object)42));
        }

        [Fact]
        public void TypeIs_True()
        {
            var t = BuildStaticP("TIs", typeof(bool), typeof(object), (me, a) =>
            {
                me.Append(Expression.TypeIs(a, typeof(string)));
            });
            Assert.Equal(true, Invoke(t, (object)"hello"));
            Assert.Equal(false, Invoke(t, (object)42));
        }

        // ===== ArrayExpression: various element types =====

        [Fact]
        public void Array_StringElements()
        {
            var t = BuildStatic("ArrS", typeof(string[]), me =>
            {
                me.Append(Expression.Array(typeof(string), Expression.Constant("a"), Expression.Constant("b")));
            });
            Assert.Equal(new[] { "a", "b" }, (string[])Invoke(t));
        }

        [Fact]
        public void Array_ObjectWithMixedElements()
        {
            var t = BuildStatic("ArrO", typeof(object[]), me =>
            {
                me.Append(Expression.Array(typeof(object),
                    Expression.Convert(Expression.Constant(1), typeof(object)),
                    Expression.Constant("hello")));
            });
            var result = (object[])Invoke(t);
            Assert.Equal(2, result.Length);
            Assert.Equal(1, result[0]);
            Assert.Equal("hello", result[1]);
        }

        // ===== ArrayIndex and ArrayLength =====

        [Fact]
        public void ArrayIndex_ReadElement()
        {
            var t = BuildStaticP("ArrIdx", typeof(int), typeof(int[]), (me, a) =>
            {
                me.Append(Expression.ArrayIndex(a, Expression.Constant(1)));
            });
            Assert.Equal(20, Invoke(t, new int[] { 10, 20, 30 }));
        }

        [Fact]
        public void ArrayLength_Get()
        {
            var t = BuildStaticP("ArrLen", typeof(int), typeof(int[]), (me, a) =>
            {
                me.Append(Expression.ArrayLength(a));
            });
            Assert.Equal(3, Invoke(t, new int[] { 1, 2, 3 }));
        }

        // ===== Loop + Break =====

        [Fact]
        public void Loop_SumTo10()
        {
            var t = BuildStatic("Loop10", typeof(int), me =>
            {
                var sum = Expression.Variable(typeof(int));
                var i = Expression.Variable(typeof(int));
                me.Append(Expression.Assign(sum, Expression.Constant(0)));
                me.Append(Expression.Assign(i, Expression.Constant(0)));

                var loop = Expression.Loop();
                loop.Append(Expression.IfThen(
                    Expression.GreaterThanOrEqual(i, Expression.Constant(10)),
                    Expression.Break()
                ));
                loop.Append(Expression.Assign(sum, Expression.Add(sum, i)));
                loop.Append(Expression.Assign(i, Expression.Add(i, Expression.Constant(1))));
                me.Append(loop);
                me.Append(sum);
            });
            Assert.Equal(45, Invoke(t));
        }

        // ===== IfThenElse =====

        [Fact]
        public void IfThenElse_WithVariable()
        {
            var t = BuildStaticP("ITE", typeof(int), typeof(int), (me, a) =>
            {
                me.Append(Expression.Condition(
                    Expression.GreaterThan(a, Expression.Constant(0)),
                    Expression.Constant(1),
                    Expression.Constant(-1)
                ));
            });
            Assert.Equal(1, Invoke(t, 5));
            Assert.Equal(-1, Invoke(t, -5));
        }

        // ===== Expression: Assign checked errors =====

        [Fact]
        public void Assign_TypeMismatch_Throws()
        {
            var mod = new ModuleEmitter($"AT_{Guid.NewGuid():N}");
            var cls = mod.DefineType("ATC", TypeAttributes.Public);
            var me = cls.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, typeof(void));
            var v = Expression.Variable(typeof(int));
            Assert.Throws<AstException>(() => me.Append(Expression.Assign(v, Expression.Constant("str"))));
        }

        // ===== ConstantExpression: null =====

        [Fact]
        public void Constant_Null()
        {
            var t = BuildStatic("CN", typeof(object), me =>
            {
                me.Append(Expression.Constant(null, typeof(object)));
            });
            Assert.Null(Invoke(t));
        }

        [Fact]
        public void Constant_NullString()
        {
            var t = BuildStatic("CNS", typeof(string), me =>
            {
                me.Append(Expression.Constant(null, typeof(string)));
            });
            Assert.Null(Invoke(t));
        }

        // ===== Numeric conversions: checked paths =====

        [Fact]
        public void Convert_IntToCharChecked()
        {
            var t = BuildStaticP("I2CC", typeof(char), typeof(int), (me, a) =>
            {
                me.Append(Expression.Convert(a, typeof(char)));
            });
            Assert.Equal('A', Invoke(t, 65));
        }

        [Fact]
        public void Convert_LongToInt()
        {
            var t = BuildStaticP("L2I", typeof(int), typeof(long), (me, a) =>
            {
                me.Append(Expression.Convert(a, typeof(int)));
            });
            Assert.Equal(42, Invoke(t, 42L));
        }

        [Fact]
        public void Convert_DoubleToInt()
        {
            var t = BuildStaticP("D2I", typeof(int), typeof(double), (me, a) =>
            {
                me.Append(Expression.Convert(a, typeof(int)));
            });
            Assert.Equal(42, Invoke(t, 42.7));
        }

        [Fact]
        public void Convert_FloatToInt()
        {
            var t = BuildStaticP("F2I", typeof(int), typeof(float), (me, a) =>
            {
                me.Append(Expression.Convert(a, typeof(int)));
            });
            Assert.Equal(42, Invoke(t, 42.7f));
        }

        [Fact]
        public void Convert_ByteToShort()
        {
            var t = BuildStaticP("B2S", typeof(short), typeof(byte), (me, a) =>
            {
                me.Append(Expression.Convert(a, typeof(short)));
            });
            Assert.Equal((short)42, Invoke(t, (byte)42));
        }

        [Fact]
        public void Convert_UShortToInt()
        {
            var t = BuildStaticP("US2I", typeof(int), typeof(ushort), (me, a) =>
            {
                me.Append(Expression.Convert(a, typeof(int)));
            });
            Assert.Equal(42, Invoke(t, (ushort)42));
        }

        [Fact]
        public void Convert_SByteToLong()
        {
            var t = BuildStaticP("SB2L", typeof(long), typeof(sbyte), (me, a) =>
            {
                me.Append(Expression.Convert(a, typeof(long)));
            });
            Assert.Equal(42L, Invoke(t, (sbyte)42));
        }
    }
}
