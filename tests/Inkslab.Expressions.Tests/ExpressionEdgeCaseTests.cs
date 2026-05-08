using Inkslab.Emitters;
using System;
using System.Reflection;
using Xunit;

namespace Inkslab.Expressions.Tests
{
    /// <summary>
    /// 针对 SwitchExpression / TryExpression / ParameterEmitter / PropertyExpression
    /// 等表达式的边界与异常路径补充测试。
    /// </summary>
    public class ExpressionEdgeCaseTests
    {
        private readonly ModuleEmitter _mod = new ModuleEmitter($"EE_{Guid.NewGuid():N}");

        private Type BuildStatic(string name, Type ret, Action<MethodEmitter> body)
        {
            var te = _mod.DefineType($"{name}_{Guid.NewGuid():N}", TypeAttributes.Public);
            te.DefineDefaultConstructor();
            var me = te.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, ret);
            body(me);
            return te.CreateType();
        }

        private object Invoke(Type t, params object[] args) => t.GetMethod("Run").Invoke(null, args);

        // ===== SwitchExpression 边界 =====

        [Fact]
        public void Switch_EmptyCases_NoDefault_Throws()
        {
            // 没有 case，没有 default 时 Load 阶段抛 AstException
            Assert.Throws<AstException>(() =>
            {
                BuildStatic("SwEm", typeof(int), m =>
                {
                    var s = Expression.Switch(Expression.Constant(1));
                    m.Append(s);
                });
            });
        }

        [Fact]
        public void Switch_OnlyDefault_Works()
        {
            var t = BuildStatic("SwOD", typeof(int), m =>
            {
                var s = Expression.Switch(Expression.Constant(1), Expression.Constant(99));
                m.Append(s);
            });
            Assert.Equal(99, Invoke(t));
        }

        [Fact]
        public void Switch_NullSwitchValue_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Expression.Switch(null));
        }

        [Fact]
        public void Switch_NullDefault_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Expression.Switch(Expression.Constant(1), null));
        }

        [Fact]
        public void Switch_NullConstantCase_Throws()
        {
            var s = Expression.Switch(Expression.Constant(1));
            Assert.Throws<ArgumentNullException>(() => s.Case((ConstantExpression)null));
        }

        [Fact]
        public void Switch_NullVariableCase_Throws()
        {
            var s = Expression.Switch(Expression.Constant((object)null, typeof(object)));
            Assert.Throws<ArgumentNullException>(() => s.Case((VariableExpression)null));
        }

        [Fact]
        public void Switch_VariableCase_OnArithmetic_Throws()
        {
            // Arithmetic kind 不允许使用 Case(VariableExpression)
            var s = Expression.Switch(Expression.Constant(1));
            Assert.Throws<AstException>(() => s.Case(Expression.Variable(typeof(int))));
        }

        [Fact]
        public void Switch_VariableCase_OnEquality_Throws()
        {
            // string 是 Equality kind，不允许 Case(VariableExpression)
            var s = Expression.Switch(Expression.Constant("x"));
            Assert.Throws<AstException>(() => s.Case(Expression.Variable(typeof(string))));
        }

        [Fact]
        public void Switch_ConstantCase_OnRuntimeType_Throws()
        {
            // object 是 RuntimeType kind，不允许 Case(ConstantExpression)
            var s = Expression.Switch(Expression.Constant((object)1, typeof(object)));
            Assert.Throws<AstException>(() => s.Case(Expression.Constant(1)));
        }

        [Fact]
        public void Switch_RuntimeType_VariableCase_Works()
        {
            // object 上对 string 类型分支的 case，使用 VariableExpression
            var t = BuildStatic("SwRT", typeof(string), m =>
            {
                var v = Expression.Variable(typeof(string));
                var result = Expression.Variable(typeof(string));
                m.Append(Expression.Assign(result, Expression.Constant("none")));

                var s = Expression.Switch(Expression.Constant((object)"hello", typeof(object)), Expression.Assign(result, Expression.Constant("default")));
                s.Case(v).Append(Expression.Assign(result, v));
                m.Append(s);
                m.Append(result);
            });
            Assert.Equal("hello", Invoke(t));
        }

        [Fact]
        public void Switch_RuntimeType_VariableCase_NoMatch_NoChange()
        {
            // object 上对 string 分支的 case，传入非 string 时不进入任何 case
            // 框架行为：多个 case 时 default 表达式不参与执行；外部 result 保持初始值
            var t = BuildStatic("SwRTNM", typeof(string), m =>
            {
                var v = Expression.Variable(typeof(string));
                var result = Expression.Variable(typeof(string));
                m.Append(Expression.Assign(result, Expression.Constant("none")));

                var s = Expression.Switch(Expression.Constant((object)42, typeof(object)), Expression.Assign(result, Expression.Constant("default")));
                s.Case(v).Append(Expression.Assign(result, v));
                m.Append(s);
                m.Append(result);
            });
            Assert.Equal("none", Invoke(t));
        }

        [Fact]
        public void Switch_StringCase_Match()
        {
            var t = BuildStatic("SwStr2", typeof(int), m =>
            {
                var result = Expression.Variable(typeof(int));
                m.Append(Expression.Assign(result, Expression.Constant(0)));

                var s = Expression.Switch(Expression.Constant("b"));
                s.Case(Expression.Constant("a")).Append(Expression.Assign(result, Expression.Constant(1)));
                s.Case(Expression.Constant("b")).Append(Expression.Assign(result, Expression.Constant(2)));
                m.Append(s);
                m.Append(result);
            });
            Assert.Equal(2, Invoke(t));
        }

        [Fact]
        public void Switch_NoComparison_Throws()
        {
            // CustomType 没有 op_Equality 且没有 IEquatable<T>
            Assert.Throws<InvalidOperationException>(() =>
            {
                var s = Expression.Switch(Expression.Constant(new NoEqualityType()));
                s.Case(Expression.Constant(new NoEqualityType()));
            });
        }

        public class NoEqualityType
        {
            public int X;
        }

        // ===== TryExpression 边界 =====

        [Fact]
        public void Try_EmptyBlock_Throws()
        {
            // 既没有 catch 也没有 finally，Load 阶段抛 AstException
            Assert.Throws<AstException>(() =>
            {
                BuildStatic("TrEm", typeof(int), m =>
                {
                    var tr = Expression.Try();
                    tr.Append(Expression.Constant(1));
                    m.Append(tr);
                });
            });
        }

        [Fact]
        public void Try_CatchTypeNotException_Throws()
        {
            var tr = Expression.Try();
            // string 不是 Exception 子类
            Assert.Throws<AstException>(() => tr.Catch(typeof(string)));
        }

        [Fact]
        public void Try_CatchVariableTypeNotException_Throws()
        {
            var tr = Expression.Try();
            var v = Expression.Variable(typeof(string));
            Assert.Throws<AstException>(() => tr.Catch(v));
        }

        [Fact]
        public void Try_NullExceptionType_Throws()
        {
            var tr = Expression.Try();
            Assert.Throws<ArgumentNullException>(() => tr.Catch((Type)null));
        }

        [Fact]
        public void Try_NullVariable_Throws()
        {
            var tr = Expression.Try();
            Assert.Throws<ArgumentNullException>(() => tr.Catch((VariableExpression)null));
        }

        [Fact]
        public void Try_NullFinally_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Expression.Try(null));
        }

        [Fact]
        public void Try_FinallyOnly_Works()
        {
            // finally only 是合法的 (catch 数量 0，但 finally 不为 null)
            var t = BuildStatic("TrFO", typeof(int), m =>
            {
                var v = Expression.Variable(typeof(int));
                var tr = Expression.Try(Expression.Assign(v, Expression.Constant(7)));
                tr.Append(Expression.Assign(v, Expression.Constant(3)));
                m.Append(tr);
                m.Append(v);
            });
            // finally 后 v 为 7
            Assert.Equal(7, Invoke(t));
        }

        [Fact]
        public void Try_CatchVariable_AssignsException()
        {
            // 抛异常被 catch 并将异常对象赋给变量
            // 注意：当前框架只允许 Catch(Variable) 的类型为 Exception 本身或其基类
            var t = BuildStatic("TrCV", typeof(string), m =>
            {
                var ev = Expression.Variable(typeof(Exception));
                var msg = Expression.Variable(typeof(string));
                m.Append(Expression.Assign(msg, Expression.Constant("none")));

                var tr = Expression.Try();
                tr.Append(Expression.Throw(typeof(InvalidOperationException), "hi"));
                tr.Catch(ev)
                    .Append(Expression.Assign(msg, Expression.Property(ev, typeof(Exception).GetProperty(nameof(Exception.Message)))));
                m.Append(tr);
                m.Append(msg);
            });
            Assert.Equal("hi", Invoke(t));
        }

        [Fact]
        public void Try_CatchVariable_NonExceptionBase_Throws()
        {
            // 框架要求 Catch(Variable) 的类型为 Exception 或其基类（object 等），
            // 这里用 InvalidOperationException（子类）应抛 AstException。
            var tr = Expression.Try();
            var ev = Expression.Variable(typeof(InvalidOperationException));
            Assert.Throws<AstException>(() => tr.Catch(ev));
        }

        [Fact]
        public void Try_CatchAll_NoVariable_Works()
        {
            // catch 任意异常
            var t = BuildStatic("TrCA", typeof(int), m =>
            {
                var v = Expression.Variable(typeof(int));
                m.Append(Expression.Assign(v, Expression.Constant(0)));

                var tr = Expression.Try();
                tr.Append(Expression.Throw(typeof(InvalidOperationException), "x"));
                tr.Catch().Append(Expression.Assign(v, Expression.Constant(99)));
                m.Append(tr);
                m.Append(v);
            });
            Assert.Equal(99, Invoke(t));
        }

        [Fact]
        public void Try_TryCatchFinally_AllExecuted()
        {
            // try 抛异常，catch 处理，finally 也执行
            var t = BuildStatic("TrCF", typeof(int), m =>
            {
                var v = Expression.Variable(typeof(int));
                m.Append(Expression.Assign(v, Expression.Constant(0)));

                var tr = Expression.Try(Expression.Assign(v, Expression.Add(v, Expression.Constant(10))));
                tr.Append(Expression.Throw(typeof(InvalidOperationException), "x"));
                tr.Catch().Append(Expression.Assign(v, Expression.Add(v, Expression.Constant(1))));
                m.Append(tr);
                m.Append(v);
            });
            Assert.Equal(11, Invoke(t));
        }

        // ===== PropertyExpression =====

        [Fact]
        public void Property_Static_Read()
        {
            // string.Empty 不是属性，但 DateTime.Now 是静态属性
            var prop = typeof(DateTime).GetProperty(nameof(DateTime.Now));
            var t = BuildStatic("PSR", typeof(DateTime), m =>
            {
                m.Append(Expression.Property(prop));
            });
            var d1 = (DateTime)Invoke(t);
            Assert.True(d1 > DateTime.MinValue);
        }

        [Fact]
        public void Property_Instance_Read()
        {
            var prop = typeof(string).GetProperty(nameof(string.Length));
            var t = BuildStatic("PIR", typeof(int), m =>
            {
                m.Append(Expression.Property(Expression.Constant("hello"), prop));
            });
            Assert.Equal(5, Invoke(t));
        }

        [Fact]
        public void Property_Static_PassInstance_Throws()
        {
            // 给静态属性传入 instance 会抛 InvalidOperationException
            var prop = typeof(DateTime).GetProperty(nameof(DateTime.Now));
            Assert.Throws<InvalidOperationException>(() =>
                Expression.Property(Expression.Constant("x"), prop));
        }

        [Fact]
        public void Property_Instance_NullInstance_Throws()
        {
            var prop = typeof(string).GetProperty(nameof(string.Length));
            Assert.Throws<InvalidOperationException>(() =>
                Expression.Property((Expression)null, prop));
        }

        // ===== Field =====

        [Fact]
        public void Field_Static_Read()
        {
            // string.Empty
            var f = typeof(string).GetField(nameof(string.Empty));
            var t = BuildStatic("FSR", typeof(string), m => m.Append(Expression.Field(f)));
            Assert.Equal(string.Empty, Invoke(t));
        }

        // ===== Variable =====

        [Fact]
        public void Variable_Type()
        {
            var v = Expression.Variable(typeof(int));
            Assert.Equal(typeof(int), v.RuntimeType);
        }

        // ===== Return runtime =====

        [Fact]
        public void Return_Empty_FromVoid_Works()
        {
            var t = BuildStatic("RetEm", typeof(void), m => m.Append(Expression.Return()));
            Assert.Null(Invoke(t));
        }

        [Fact]
        public void Return_WithBody_Works()
        {
            var t = BuildStatic("RetB", typeof(int), m => m.Append(Expression.Return(Expression.Constant(42))));
            Assert.Equal(42, Invoke(t));
        }

        // ===== Loop / Break / Continue =====

        [Fact]
        public void Loop_Break_Works()
        {
            var t = BuildStatic("LpB", typeof(int), m =>
            {
                var v = Expression.Variable(typeof(int));
                m.Append(Expression.Assign(v, Expression.Constant(0)));
                var loop = Expression.Loop();
                loop.Append(Expression.IfThen(
                    Expression.GreaterThanOrEqual(v, Expression.Constant(5)),
                    Expression.Break()));
                loop.Append(Expression.IncrementAssign(v));
                m.Append(loop);
                m.Append(v);
            });
            Assert.Equal(5, Invoke(t));
        }

        [Fact]
        public void Loop_Continue_Works()
        {
            // 累加 0..9 的偶数
            var t = BuildStatic("LpC", typeof(int), m =>
            {
                var i = Expression.Variable(typeof(int));
                var sum = Expression.Variable(typeof(int));
                m.Append(Expression.Assign(i, Expression.Constant(0)));
                m.Append(Expression.Assign(sum, Expression.Constant(0)));

                var loop = Expression.Loop();
                loop.Append(Expression.IfThen(
                    Expression.GreaterThanOrEqual(i, Expression.Constant(10)),
                    Expression.Break()));
                loop.Append(Expression.IfThenElse(
                    Expression.Equal(Expression.Modulo(i, Expression.Constant(2)), Expression.Constant(0)),
                    Expression.Block().Append(Expression.AddAssign(sum, i)).Append(Expression.IncrementAssign(i)),
                    Expression.Block().Append(Expression.IncrementAssign(i)).Append(Expression.Continue())));

                m.Append(loop);
                m.Append(sum);
            });
            Assert.Equal(0 + 2 + 4 + 6 + 8, Invoke(t));
        }

        // ===== Goto / Label =====

        [Fact]
        public void Goto_Label_Works()
        {
            var t = BuildStatic("GtL", typeof(int), m =>
            {
                var v = Expression.Variable(typeof(int));
                m.Append(Expression.Assign(v, Expression.Constant(1)));

                var label = Expression.Label();
                m.Append(Expression.Goto(label));
                m.Append(Expression.Assign(v, Expression.Constant(2))); // 应跳过
                m.Append(Expression.Label(label));
                m.Append(v);
            });
            Assert.Equal(1, Invoke(t));
        }

        // ===== Coalesce 运行时 =====

        [Fact]
        public void Coalesce_String_Null()
        {
            var t = BuildStatic("CoaS", typeof(string), m =>
            {
                m.Append(Expression.Coalesce(Expression.Constant(null, typeof(string)), Expression.Constant("fb")));
            });
            Assert.Equal("fb", Invoke(t));
        }

        [Fact]
        public void Coalesce_String_HasValue()
        {
            var t = BuildStatic("CoaS2", typeof(string), m =>
            {
                m.Append(Expression.Coalesce(Expression.Constant("hi"), Expression.Constant("fb")));
            });
            Assert.Equal("hi", Invoke(t));
        }

        [Fact]
        public void Coalesce_Object_Null()
        {
            var t = BuildStatic("CoaO", typeof(object), m =>
            {
                m.Append(Expression.Coalesce(
                    Expression.Constant(null, typeof(object)),
                    Expression.Convert(Expression.Constant(42), typeof(object))));
            });
            Assert.Equal(42, Invoke(t));
        }
    }
}
