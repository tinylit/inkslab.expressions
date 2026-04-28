using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Inkslab.Emitters;
using Xunit;

namespace Inkslab.Expressions.Tests
{
    /// <summary>
    /// 针对命名重构提交中 IDE 代码风格修复（IDE0078/IDE0074/IDE0052 等）的回归测试。
    /// 确保 pattern matching、compound assignment、unused member removal 等重构未引入行为变化。
    /// </summary>
    public class CodeStyleRefactorTests
    {
        private readonly ModuleEmitter _mod = new ModuleEmitter($"CSR_{Guid.NewGuid():N}");

        private Type BuildStatic(string name, Type ret, Action<MethodEmitter> body)
        {
            var te = _mod.DefineType($"{name}_{Guid.NewGuid():N}", TypeAttributes.Public);
            te.DefineDefaultConstructor();
            var me = te.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, ret);
            body(me);
            return te.CreateType();
        }

        private object Invoke(Type t, params object[] args)
            => t.GetMethod("Run").Invoke(null, args);

        #region BlockExpression.IsClosed — pattern matching (IDE0078)

        /// <summary>
        /// IsClosed 在空的 void BlockExpression 上返回 true（IsVoid = true）。
        /// </summary>
        [Fact]
        public void IsClosed_EmptyVoidBlock_ReturnsTrue()
        {
            var block = Expression.Block();
            Assert.True(block.IsEmpty);
            Assert.True(block.IsClosed);
        }

        /// <summary>
        /// IsClosed：最后一条语句为 ReturnExpression 时返回 true。
        /// </summary>
        [Fact]
        public void IsClosed_LastIsReturn_ReturnsTrue()
        {
            var block = Expression.Block();
            block.Append(Expression.Return());
            Assert.True(block.IsClosed);
        }

        /// <summary>
        /// IsClosed：最后一条语句为 ThrowExpression 时返回 true。
        /// </summary>
        [Fact]
        public void IsClosed_LastIsThrow_ReturnsTrue()
        {
            var block = Expression.Block();
            block.Append(Expression.Throw(typeof(InvalidOperationException)));
            Assert.True(block.IsClosed);
        }

        /// <summary>
        /// IsClosed：最后一条语句为 GotoExpression 时返回 true。
        /// </summary>
        [Fact]
        public void IsClosed_LastIsGoto_ReturnsTrue()
        {
            var label = Expression.Label();
            var block = Expression.Block();
            block.Append(Expression.Goto(label));
            Assert.True(block.IsClosed);
        }

        /// <summary>
        /// IsClosed：最后一条语句不是终结表达式时返回 false。
        /// </summary>
        [Fact]
        public void IsClosed_LastIsAssign_ReturnsFalse()
        {
            var block = Expression.Block();
            var v = Expression.Variable(typeof(int));
            block.Append(Expression.Assign(v, Expression.Constant(42)));
            Assert.False(block.IsClosed);
        }

        #endregion

        #region BlockExpression.Append — 去重逻辑 (IDE0078)

        /// <summary>
        /// Append：在已有 Return 的 Block 后再追加 Return，直接忽略返回 this。
        /// </summary>
        [Fact]
        public void Append_DuplicateReturnAfterReturn_IsIgnored()
        {
            var block = Expression.Block();
            block.Append(Expression.Return());
            var result = block.Append(Expression.Return());
            Assert.Same(block, result);
        }

        /// <summary>
        /// Append：在已有 Throw 的 Block 后再追加 Throw，直接忽略返回 this。
        /// </summary>
        [Fact]
        public void Append_DuplicateThrowAfterThrow_IsIgnored()
        {
            var block = Expression.Block();
            block.Append(Expression.Throw(typeof(InvalidOperationException)));
            var result = block.Append(Expression.Throw(typeof(ArgumentException)));
            Assert.Same(block, result);
        }

        /// <summary>
        /// Append：在已有 Goto 的 Block 后再追加终结表达式，直接忽略返回 this。
        /// </summary>
        [Fact]
        public void Append_TerminalAfterGoto_IsIgnored()
        {
            var label = Expression.Label();
            var block = Expression.Block();
            block.Append(Expression.Goto(label));
            var result = block.Append(Expression.Return());
            Assert.Same(block, result);
        }

        #endregion

        #region EmitUtils.EmitInt — 边界值 pattern matching (IDE0078)

        /// <summary>
        /// EmitInt：值为 -128（sbyte 最小值），应走 Ldc_I4_S 分支。
        /// </summary>
        [Fact]
        public void EmitInt_NegativeSByte_Min()
        {
            var t = BuildStatic("EINSM", typeof(int), me =>
            {
                me.Append(Expression.Constant(-128));
            });
            Assert.Equal(-128, Invoke(t));
        }

        /// <summary>
        /// EmitInt：值为 127（sbyte 最大值），应走 Ldc_I4_S 分支。
        /// </summary>
        [Fact]
        public void EmitInt_PositiveSByte_Max()
        {
            var t = BuildStatic("EIPSM", typeof(int), me =>
            {
                me.Append(Expression.Constant(127));
            });
            Assert.Equal(127, Invoke(t));
        }

        /// <summary>
        /// EmitInt：值为 -129（超出 sbyte 范围），应走 Ldc_I4 分支。
        /// </summary>
        [Fact]
        public void EmitInt_JustBelowSByte()
        {
            var t = BuildStatic("EIJBS", typeof(int), me =>
            {
                me.Append(Expression.Constant(-129));
            });
            Assert.Equal(-129, Invoke(t));
        }

        /// <summary>
        /// EmitInt：值为 128（超出 sbyte 范围），应走 Ldc_I4 分支。
        /// </summary>
        [Fact]
        public void EmitInt_JustAboveSByte()
        {
            var t = BuildStatic("EIJAS", typeof(int), me =>
            {
                me.Append(Expression.Constant(128));
            });
            Assert.Equal(128, Invoke(t));
        }

        /// <summary>
        /// EmitInt：负数 -100（在 sbyte 范围内），应走 Ldc_I4_S 分支。
        /// </summary>
        [Fact]
        public void EmitInt_NegativeSByte_InRange()
        {
            var t = BuildStatic("EINSR", typeof(int), me =>
            {
                me.Append(Expression.Constant(-100));
            });
            Assert.Equal(-100, Invoke(t));
        }

        #endregion

        #region EmitUtils.EmitDecimal — long 范围分支 (IDE0078)

        /// <summary>
        /// EmitDecimal：值在 long 范围（超出 int 范围），应走 new decimal(long) 分支。
        /// </summary>
        [Fact]
        public void EmitDecimal_LongRange_Positive()
        {
            var t = BuildStatic("EDcLP", typeof(decimal), me =>
            {
                me.Append(Expression.Constant(3000000000m));
            });
            Assert.Equal(3000000000m, Invoke(t));
        }

        /// <summary>
        /// EmitDecimal：负 long 范围值。
        /// </summary>
        [Fact]
        public void EmitDecimal_LongRange_Negative()
        {
            var t = BuildStatic("EDcLN", typeof(decimal), me =>
            {
                me.Append(Expression.Constant(-3000000000m));
            });
            Assert.Equal(-3000000000m, Invoke(t));
        }

        /// <summary>
        /// EmitDecimal：int 边界值 int.MaxValue。
        /// </summary>
        [Fact]
        public void EmitDecimal_IntMax()
        {
            var t = BuildStatic("EDcIM", typeof(decimal), me =>
            {
                me.Append(Expression.Constant((decimal)int.MaxValue));
            });
            Assert.Equal((decimal)int.MaxValue, Invoke(t));
        }

        /// <summary>
        /// EmitDecimal：int 边界值 int.MinValue。
        /// </summary>
        [Fact]
        public void EmitDecimal_IntMin()
        {
            var t = BuildStatic("EDcImn", typeof(decimal), me =>
            {
                me.Append(Expression.Constant((decimal)int.MinValue));
            });
            Assert.Equal((decimal)int.MinValue, Invoke(t));
        }

        /// <summary>
        /// EmitDecimal：long.MaxValue（long 范围边界）。
        /// </summary>
        [Fact]
        public void EmitDecimal_LongMax()
        {
            var t = BuildStatic("EDcLMx", typeof(decimal), me =>
            {
                me.Append(Expression.Constant((decimal)long.MaxValue));
            });
            Assert.Equal((decimal)long.MaxValue, Invoke(t));
        }

        #endregion

        #region TypeIsExpression — KnownTrue 分支 (IDE0078)

        /// <summary>
        /// TypeIs：int is IComparable — 非 Nullable 值类型实现接口，应触发 KnownTrue 路径。
        /// </summary>
        [Fact]
        public void TypeIs_ValueType_IsInterface_KnownTrue()
        {
            var t = BuildStatic("TISVT", typeof(bool), me =>
            {
                var p = me.DefineParameter(typeof(int), "v");
                me.Append(Expression.TypeIs(p, typeof(IComparable)));
            });
            // int 始终实现 IComparable，编译期即可确定为 true
            Assert.Equal(true, Invoke(t, 42));
        }

        /// <summary>
        /// TypeIs：int is ValueType — 非 Nullable 值类型对 ValueType 基类，应触发 KnownTrue。
        /// </summary>
        [Fact]
        public void TypeIs_Int_IsValueType_KnownTrue()
        {
            var t = BuildStatic("TISIVT", typeof(bool), me =>
            {
                var p = me.DefineParameter(typeof(int), "v");
                me.Append(Expression.TypeIs(p, typeof(ValueType)));
            });
            Assert.Equal(true, Invoke(t, 0));
        }

        /// <summary>
        /// TypeIs：int is object — 值类型对 object，应触发 KnownTrue。
        /// </summary>
        [Fact]
        public void TypeIs_Int_IsObject_KnownTrue()
        {
            var t = BuildStatic("TISIO", typeof(bool), me =>
            {
                var p = me.DefineParameter(typeof(int), "v");
                me.Append(Expression.TypeIs(p, typeof(object)));
            });
            Assert.Equal(true, Invoke(t, 0));
        }

        #endregion

        #region ForEachExpression — 接口回退路径 (IDE0074 ??=)

        /// <summary>
        /// 自定义集合类型：不直接暴露 Count/this[int]，但实现 IList&lt;int&gt;。
        /// 验证 ??= 回退到接口查找 Count + this[int] 的分支。
        /// </summary>
        public class HiddenIndexerCollection : IList<int>
        {
            private readonly List<int> _inner;

            /// <summary>
            /// 构造函数。
            /// </summary>
            public HiddenIndexerCollection(IEnumerable<int> items) => _inner = new List<int>(items);

            // 显式实现接口，类型自身不暴露 Count 和 this[int]
            int ICollection<int>.Count => _inner.Count;
            int IList<int>.this[int index] { get => _inner[index]; set => _inner[index] = value; }
            bool ICollection<int>.IsReadOnly => true;
            void ICollection<int>.Add(int item) => throw new NotSupportedException();
            void ICollection<int>.Clear() => throw new NotSupportedException();
            bool ICollection<int>.Contains(int item) => _inner.Contains(item);
            void ICollection<int>.CopyTo(int[] array, int arrayIndex) => _inner.CopyTo(array, arrayIndex);
            IEnumerator<int> IEnumerable<int>.GetEnumerator() => _inner.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => _inner.GetEnumerator();
            int IList<int>.IndexOf(int item) => _inner.IndexOf(item);
            void IList<int>.Insert(int index, int item) => throw new NotSupportedException();
            bool ICollection<int>.Remove(int item) => throw new NotSupportedException();
            void IList<int>.RemoveAt(int index) => throw new NotSupportedException();
        }

        /// <summary>
        /// ForEach：遍历显式实现 IList&lt;int&gt; 的集合，
        /// 触发 TryResolveIndexer 中 ??= 接口回退分支。
        /// </summary>
        [Fact]
        public void ForEach_InterfaceFallback_IndexerPath()
        {
            var typeEmitter = _mod.DefineType($"FEIF_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var method = typeEmitter.DefineMethod("Sum", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            var param = method.DefineParameter(typeof(HiddenIndexerCollection), "col");

            var s = Expression.Variable(typeof(int));
            var x = Expression.Variable(typeof(int));

            method.Append(Expression.Assign(s, Expression.Constant(0)));

            var fe = Expression.ForEach(x, param);
            fe.Append(Expression.AddAssign(s, x));

            method.Append(fe);
            method.Append(s);

            var mi = typeEmitter.CreateType().GetMethod("Sum");
            var col = new HiddenIndexerCollection(new[] { 10, 20, 30 });
            Assert.Equal(60, mi.Invoke(null, new object[] { col }));
        }

        #endregion

        #region ConstantExpression — pattern matching 类型推断 (IDE0078)

        /// <summary>
        /// ConstantExpression：MethodInfo 值自动推断为 typeof(MethodInfo)。
        /// </summary>
        [Fact]
        public void ConstantExpression_MethodInfo_InfersType()
        {
            var mi = typeof(string).GetMethod(nameof(string.ToUpper), Type.EmptyTypes);
            var expr = Expression.Constant(mi);
            Assert.Equal(typeof(MethodInfo), expr.RuntimeType);
        }

        /// <summary>
        /// ConstantExpression：Type 值自动推断为 typeof(Type)。
        /// </summary>
        [Fact]
        public void ConstantExpression_Type_InfersType()
        {
            var expr = Expression.Constant(typeof(int));
            Assert.Equal(typeof(Type), expr.RuntimeType);
        }

        /// <summary>
        /// ConstantExpression：普通值自动推断为其实际类型。
        /// </summary>
        [Fact]
        public void ConstantExpression_OrdinaryValue_InfersActualType()
        {
            var expr = Expression.Constant(42);
            Assert.Equal(typeof(int), expr.RuntimeType);
        }

        /// <summary>
        /// ConstantExpression：null 值指定类型。
        /// </summary>
        [Fact]
        public void ConstantExpression_NullWithRefType_Succeeds()
        {
            var expr = Expression.Constant(null, typeof(string));
            Assert.Equal(typeof(string), expr.RuntimeType);
            Assert.True(expr.IsNull);
        }

        /// <summary>
        /// ConstantExpression：null 值对非 Nullable 值类型应抛异常。
        /// </summary>
        [Fact]
        public void ConstantExpression_NullWithValueType_Throws()
        {
            Assert.Throws<NotSupportedException>(() => Expression.Constant(null, typeof(int)));
        }

        /// <summary>
        /// ConstantExpression：值与指定类型不兼容应抛异常。
        /// </summary>
        [Fact]
        public void ConstantExpression_IncompatibleType_Throws()
        {
            Assert.Throws<NotSupportedException>(() => Expression.Constant("hello", typeof(int)));
        }

        #endregion
    }
}
