using Inkslab.Emitters;
using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace Inkslab.Expressions.Tests
{
    /// <summary>
    /// 针对 Expression 静态工厂方法的参数校验、边界与异常路径补充测试，
    /// 主要覆盖 Expression.Factory 中 ArgumentNullException / AstException / NotSupportedException 分支。
    /// </summary>
    public class FactoryValidationTests
    {
        private readonly ModuleEmitter _mod = new ModuleEmitter($"FV_{Guid.NewGuid():N}");

        private Type BuildStatic(string name, Type ret, Action<MethodEmitter> body)
        {
            var te = _mod.DefineType($"{name}_{Guid.NewGuid():N}", TypeAttributes.Public);
            te.DefineDefaultConstructor();
            var me = te.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, ret);
            body(me);
            return te.CreateType();
        }

        private object Invoke(Type t, params object[] args) => t.GetMethod("Run").Invoke(null, args);

        // ===== Convert =====

        [Fact]
        public void Convert_NullBody_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Expression.Convert(null, typeof(int)));
        }

        [Fact]
        public void Convert_VoidBody_Throws()
        {
            // ReturnExpression() 是 void 类型
            var ret = Expression.Return();
            Assert.Throws<AstException>(() => Expression.Convert(ret, typeof(int)));
        }

        [Fact]
        public void Convert_TargetVoid_Throws()
        {
            Assert.Throws<AstException>(() => Expression.Convert(Expression.Constant(1), typeof(void)));
        }

        [Fact]
        public void Convert_NullTypeEmitter_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Expression.Convert(Expression.Constant(1), (AbstractTypeEmitter)null));
        }

        [Fact]
        public void Convert_TypeEmitter_Works()
        {
            var te = _mod.DefineType($"CTE_{Guid.NewGuid():N}", TypeAttributes.Public);
            te.DefineDefaultConstructor();
            te.CreateType();
            var expr = Expression.Convert(Expression.Constant(null, typeof(object)), te);
            Assert.NotNull(expr);
        }

        // ===== TypeIs / TypeAs =====

        [Fact]
        public void TypeIs_NullBody_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Expression.TypeIs(null, typeof(int)));
        }

        [Fact]
        public void TypeIs_NullType_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Expression.TypeIs(Expression.Constant(1), (Type)null));
        }

        [Fact]
        public void TypeIs_VoidBody_Throws()
        {
            var ret = Expression.Return();
            Assert.Throws<AstException>(() => Expression.TypeIs(ret, typeof(int)));
        }

        [Fact]
        public void TypeIs_NullTypeEmitter_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Expression.TypeIs(Expression.Constant(1), (AbstractTypeEmitter)null));
        }

        [Fact]
        public void TypeIs_TypeEmitter_Works()
        {
            var te = _mod.DefineType($"TIE_{Guid.NewGuid():N}", TypeAttributes.Public);
            te.DefineDefaultConstructor();
            te.CreateType();
            var expr = Expression.TypeIs(Expression.Constant(null, typeof(object)), te);
            Assert.NotNull(expr);
        }

        [Fact]
        public void TypeAs_NullBody_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Expression.TypeAs(null, typeof(string)));
        }

        [Fact]
        public void TypeAs_VoidBody_Throws()
        {
            var ret = Expression.Return();
            Assert.Throws<AstException>(() => Expression.TypeAs(ret, typeof(string)));
        }

        [Fact]
        public void TypeAs_NullTypeEmitter_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Expression.TypeAs(Expression.Constant(1), (AbstractTypeEmitter)null));
        }

        [Fact]
        public void TypeAs_TypeEmitter_Works()
        {
            var te = _mod.DefineType($"TAE_{Guid.NewGuid():N}", TypeAttributes.Public);
            te.DefineDefaultConstructor();
            te.CreateType();
            var expr = Expression.TypeAs(Expression.Constant(null, typeof(object)), te);
            Assert.NotNull(expr);
        }

        // ===== New =====

        [Fact]
        public void New_NullCtor_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Expression.New((ConstructorInfo)null));
        }

        [Fact]
        public void New_NullCtorWithArgs_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Expression.New((ConstructorInfo)null, Expression.Constant(1)));
        }

        [Fact]
        public void New_WrongParamCount_Throws()
        {
            // Exception(string) 期望 1 个参数，传 0 个
            var ctor = typeof(Exception).GetConstructor(new[] { typeof(string) });
            Assert.Throws<AstException>(() => Expression.New(ctor));
        }

        [Fact]
        public void New_WrongParamType_Throws()
        {
            // Exception(string) — 传 int
            var ctor = typeof(Exception).GetConstructor(new[] { typeof(string) });
            Assert.Throws<AstException>(() => Expression.New(ctor, Expression.Constant(123)));
        }

        // ===== Bind =====

        [Fact]
        public void Bind_NullMember_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Expression.Bind((MemberInfo)null, Expression.Constant(1)));
        }

        [Fact]
        public void Bind_NullExpression_Throws()
        {
            var prop = typeof(SimplePoco).GetProperty(nameof(SimplePoco.Name));
            Assert.Throws<ArgumentNullException>(() => Expression.Bind(prop, null));
        }

        [Fact]
        public void Bind_ReadOnlyProperty_Throws()
        {
            var prop = typeof(SimplePoco).GetProperty(nameof(SimplePoco.ReadOnlyValue));
            Assert.Throws<NotSupportedException>(() => Expression.Bind(prop, Expression.Constant(1)));
        }

        [Fact]
        public void Bind_TypeMismatch_Throws()
        {
            var prop = typeof(SimplePoco).GetProperty(nameof(SimplePoco.Name));
            // Name 是 string，传 int
            Assert.Throws<NotSupportedException>(() => Expression.Bind(prop, Expression.Constant(123)));
        }

        [Fact]
        public void Bind_MethodMember_Throws()
        {
            // ToString 不是字段或可写属性
            var method = typeof(object).GetMethod(nameof(object.ToString));
            Assert.Throws<NotSupportedException>(() => Expression.Bind(method, Expression.Constant("x")));
        }

        [Fact]
        public void Bind_FieldMember_Works()
        {
            var field = typeof(SimplePoco).GetField(nameof(SimplePoco.IntField));
            var binding = Expression.Bind(field, Expression.Constant(42));
            Assert.NotNull(binding);
        }

        // ===== MemberInit =====

        [Fact]
        public void MemberInit_NullNewExpression_Throws()
        {
            var prop = typeof(SimplePoco).GetProperty(nameof(SimplePoco.Name));
            var binding = Expression.Bind(prop, Expression.Constant("a"));
            Assert.Throws<ArgumentNullException>(() => Expression.MemberInit(null, binding));
        }

        [Fact]
        public void MemberInit_NullBinding_InList_Throws()
        {
            var newExpr = Expression.New(typeof(SimplePoco));
            // 传入包含 null 的绑定
            Assert.Throws<ArgumentNullException>(() => Expression.MemberInit(newExpr, new MemberAssignment[] { null }));
        }

        [Fact]
        public void MemberInit_MemberFromOtherType_Throws()
        {
            var newExpr = Expression.New(typeof(SimplePoco));
            // 取另一类型的属性来构造绑定
            var foreignProp = typeof(OtherPoco).GetProperty(nameof(OtherPoco.Other));
            var binding = Expression.Bind(foreignProp, Expression.Constant("a"));
            Assert.Throws<MissingMemberException>(() => Expression.MemberInit(newExpr, binding));
        }

        [Fact]
        public void MemberInit_NullBindings_NoOp()
        {
            var newExpr = Expression.New(typeof(SimplePoco));
            // bindings 为 null 时，会被替换为空集合
            var expr = Expression.MemberInit(newExpr, (IEnumerable<MemberAssignment>)null);
            Assert.NotNull(expr);
        }

        // ===== Coalesce =====

        [Fact]
        public void Coalesce_NullLeft_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Expression.Coalesce(null, Expression.Constant("x")));
        }

        [Fact]
        public void Coalesce_NullRight_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Expression.Coalesce(Expression.Constant("x"), null));
        }

        // ===== Array =====

        [Fact]
        public void Array_NullArguments_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Expression.Array((Expression[])null));
        }

        [Fact]
        public void Array_NullElementType_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Expression.Array((Type)null, Expression.Constant(1)));
        }

        [Fact]
        public void Array_NullArgsWithType_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Expression.Array(typeof(int), (Expression[])null));
        }

        [Fact]
        public void Array_TypeMismatch_Throws()
        {
            // string 不能赋值给 int[]
            Assert.Throws<AstException>(() => Expression.Array(typeof(int), Expression.Constant("hi")));
        }

        [Fact]
        public void Array_EmptyTypedArray_Works()
        {
            var arr = Expression.Array(typeof(int));
            Assert.NotNull(arr);
        }

        // ===== ArrayIndex / ArrayLength =====

        [Fact]
        public void ArrayIndex_NullArray_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Expression.ArrayIndex(null, 0));
        }

        [Fact]
        public void ArrayIndex_NotArray_Throws()
        {
            Assert.Throws<ArgumentException>(() => Expression.ArrayIndex(Expression.Constant(1), 0));
        }

        [Fact]
        public void ArrayIndex_NegativeIndex_Throws()
        {
            var arr = Expression.Array(Expression.Constant(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Expression.ArrayIndex(arr, -1));
        }

        [Fact]
        public void ArrayIndex_NullIndexExpression_Throws()
        {
            var arr = Expression.Array(Expression.Constant(1));
            Assert.Throws<ArgumentNullException>(() => Expression.ArrayIndex(arr, (Expression)null));
        }

        [Fact]
        public void ArrayLength_NullArray_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Expression.ArrayLength(null));
        }

        [Fact]
        public void ArrayLength_NotArray_Throws()
        {
            Assert.Throws<ArgumentException>(() => Expression.ArrayLength(Expression.Constant(1)));
        }

        // ===== IfThen / IfThenElse / Condition =====

        [Fact]
        public void IfThen_NullTest_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Expression.IfThen(null, Expression.Constant(1)));
        }

        [Fact]
        public void IfThen_NullIfTrue_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Expression.IfThen(Expression.Constant(true), null));
        }

        [Fact]
        public void IfThen_TestNotBool_Throws()
        {
            Assert.Throws<ArgumentException>(() => Expression.IfThen(Expression.Constant(1), Expression.Constant(2)));
        }

        [Fact]
        public void IfThenElse_NullTest_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Expression.IfThenElse(null, Expression.Constant(1), Expression.Constant(2)));
        }

        [Fact]
        public void IfThenElse_NullIfTrue_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Expression.IfThenElse(Expression.Constant(true), null, Expression.Constant(2)));
        }

        [Fact]
        public void IfThenElse_NullIfFalse_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Expression.IfThenElse(Expression.Constant(true), Expression.Constant(1), null));
        }

        [Fact]
        public void IfThenElse_TestNotBool_Throws()
        {
            Assert.Throws<ArgumentException>(() => Expression.IfThenElse(Expression.Constant(1), Expression.Constant(2), Expression.Constant(3)));
        }

        [Fact]
        public void Condition_NullTest_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Expression.Condition(null, Expression.Constant(1), Expression.Constant(2)));
        }

        [Fact]
        public void Condition_NullIfTrue_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Expression.Condition(Expression.Constant(true), null, Expression.Constant(2)));
        }

        [Fact]
        public void Condition_NullIfFalse_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Expression.Condition(Expression.Constant(true), Expression.Constant(1), null));
        }

        [Fact]
        public void Condition_TestNotBool_Throws()
        {
            Assert.Throws<ArgumentException>(() => Expression.Condition(Expression.Constant(1), Expression.Constant(2), Expression.Constant(3)));
        }

        [Fact]
        public void Condition_TypeMismatchTrueBranch_Throws()
        {
            // 显式给 returnType=string，但 ifTrue 是 int 且不能转 string
            Assert.Throws<ArgumentException>(() =>
                Expression.Condition(Expression.Constant(true), Expression.Constant(1), Expression.Constant("s"), typeof(string)));
        }

        [Fact]
        public void Condition_VoidReturnType_Throws()
        {
            Assert.Throws<AstException>(() =>
                Expression.Condition(Expression.Constant(true), Expression.Constant(1), Expression.Constant(2), typeof(void)));
        }

        // ===== Throw =====

        [Fact]
        public void Throw_NullExpression_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Expression.Throw((Expression)null));
        }

        [Fact]
        public void Throw_NonExceptionExpression_Throws()
        {
            // Constant(int) 不是 Exception 子类
            Assert.Throws<AstException>(() => Expression.Throw(Expression.Constant(1)));
        }

        [Fact]
        public void Throw_TypeOnly_RuntimeThrows()
        {
            var t = BuildStatic("ThT", typeof(void), m => m.Append(Expression.Throw(typeof(InvalidOperationException))));
            var ex = Assert.Throws<TargetInvocationException>(() => Invoke(t));
            Assert.IsType<InvalidOperationException>(ex.InnerException);
        }

        [Fact]
        public void Throw_TypeWithMessage_RuntimeThrowsWithMessage()
        {
            var t = BuildStatic("ThM", typeof(void), m => m.Append(Expression.Throw(typeof(InvalidOperationException), "boom")));
            var ex = Assert.Throws<TargetInvocationException>(() => Invoke(t));
            var inner = Assert.IsType<InvalidOperationException>(ex.InnerException);
            Assert.Equal("boom", inner.Message);
        }

        [Fact]
        public void Throw_NewExpression_RuntimeThrows()
        {
            var ctor = typeof(InvalidOperationException).GetConstructor(new[] { typeof(string) });
            var t = BuildStatic("ThN", typeof(void), m =>
                m.Append(Expression.Throw(Expression.New(ctor, Expression.Constant("kapow")))));
            var ex = Assert.Throws<TargetInvocationException>(() => Invoke(t));
            var inner = Assert.IsType<InvalidOperationException>(ex.InnerException);
            Assert.Equal("kapow", inner.Message);
        }

        // ===== Label / Goto =====

        [Fact]
        public void Label_NullLabel_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Expression.Label((Label)null));
        }

        [Fact]
        public void Goto_NullLabel_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Expression.Goto(null));
        }

        // ===== Return =====

        [Fact]
        public void Return_NullBody_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Expression.Return((Expression)null));
        }

        [Fact]
        public void Return_VoidBody_Throws()
        {
            // Inner return is itself a void body
            var r = Expression.Return();
            Assert.Throws<AstException>(() => Expression.Return(r));
        }

        // ===== ForEach =====

        [Fact]
        public void ForEach_NullItem_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Expression.ForEach(null, Expression.Constant(new int[0])));
        }

        [Fact]
        public void ForEach_NullSource_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Expression.ForEach(Expression.Variable(typeof(int)), null));
        }

        // ===== 测试用类型 =====
        public class SimplePoco
        {
            public string Name { get; set; }
            public int IntField;
            public string ReadOnlyValue { get; } = "ro";
        }

        public class OtherPoco
        {
            public string Other { get; set; }
        }
    }
}
