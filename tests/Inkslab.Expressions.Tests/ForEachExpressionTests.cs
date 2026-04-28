using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace Inkslab.Expressions.Tests
{
    /// <summary>
    /// <see cref="ForEachExpression"/> 单元测试（合并自原 ForExpressionTests / EachExpressionTests）。
    /// </summary>
    public class ForEachExpressionTests
    {
        private readonly ModuleEmitter _emitter = new ModuleEmitter($"Inkslab.Expressions.Tests.ForEach.{Guid.NewGuid():N}");

        // ---------- for 索引循环路径 ----------

        [Fact]
        public void Array_SumByItem()
        {
            var typeEmitter = _emitter.DefineType($"FA_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var method = typeEmitter.DefineMethod("Sum", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            var arr = method.DefineParameter(typeof(int[]), "arr");

            var s = Expression.Variable(typeof(int));
            var x = Expression.Variable(typeof(int));

            method.Append(Expression.Assign(s, Expression.Constant(0)));

            var fe = Expression.ForEach(x, arr);
            fe.Append(Expression.AddAssign(s, x));

            method.Append(fe);
            method.Append(s);

            var mi = typeEmitter.CreateType().GetMethod("Sum");
            Assert.Equal(15, mi.Invoke(null, new object[] { new[] { 1, 2, 3, 4, 5 } }));
            Assert.Equal(0, mi.Invoke(null, new object[] { Array.Empty<int>() }));
        }

        [Fact]
        public void GenericList_PrefersIndexerLoop()
        {
            // List<int> 优先走 for 索引路径（具备 Count + this[int] 且类型匹配）。
            var typeEmitter = _emitter.DefineType($"FL_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var method = typeEmitter.DefineMethod("Sum", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            var listParam = method.DefineParameter(typeof(List<int>), "list");

            var s = Expression.Variable(typeof(int));
            var x = Expression.Variable(typeof(int));

            method.Append(Expression.Assign(s, Expression.Constant(0)));

            var fe = Expression.ForEach(x, listParam);
            fe.Append(Expression.AddAssign(s, x));

            method.Append(fe);
            method.Append(s);

            var mi = typeEmitter.CreateType().GetMethod("Sum");
            Assert.Equal(10, mi.Invoke(null, new object[] { new List<int> { 1, 2, 3, 4 } }));
            Assert.Equal(0, mi.Invoke(null, new object[] { new List<int>() }));
        }

        [Fact]
        public void GenericIList_SumByItem()
        {
            var typeEmitter = _emitter.DefineType($"FIL_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var method = typeEmitter.DefineMethod("Sum", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            var listParam = method.DefineParameter(typeof(IList<int>), "list");

            var s = Expression.Variable(typeof(int));
            var x = Expression.Variable(typeof(int));

            method.Append(Expression.Assign(s, Expression.Constant(0)));

            var fe = Expression.ForEach(x, listParam);
            fe.Append(Expression.AddAssign(s, x));

            method.Append(fe);
            method.Append(s);

            var mi = typeEmitter.CreateType().GetMethod("Sum");
            Assert.Equal(15, mi.Invoke(null, new object[] { new List<int> { 1, 2, 3, 4, 5 } }));
        }

        [Fact]
        public void Array_Break_StopsIteration()
        {
            var typeEmitter = _emitter.DefineType($"FB_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var method = typeEmitter.DefineMethod("FirstNonZero", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            var arr = method.DefineParameter(typeof(int[]), "arr");

            var r = Expression.Variable(typeof(int));
            var x = Expression.Variable(typeof(int));

            method.Append(Expression.Assign(r, Expression.Constant(-1)));

            var fe = Expression.ForEach(x, arr);
            var thenBlock = Expression.Block();
            thenBlock.Append(Expression.Assign(r, x));
            thenBlock.Append(Expression.Break());
            fe.Append(Expression.IfThen(Expression.NotEqual(x, Expression.Constant(0)), thenBlock));

            method.Append(fe);
            method.Append(r);

            var mi = typeEmitter.CreateType().GetMethod("FirstNonZero");
            Assert.Equal(7, mi.Invoke(null, new object[] { new[] { 0, 0, 7, 9 } }));
            Assert.Equal(-1, mi.Invoke(null, new object[] { new[] { 0, 0, 0 } }));
        }

        [Fact]
        public void Array_Continue_SkipsIteration()
        {
            var typeEmitter = _emitter.DefineType($"FC_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var method = typeEmitter.DefineMethod("SumOdd", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            var arr = method.DefineParameter(typeof(int[]), "arr");

            var s = Expression.Variable(typeof(int));
            var x = Expression.Variable(typeof(int));

            method.Append(Expression.Assign(s, Expression.Constant(0)));

            var fe = Expression.ForEach(x, arr);
            fe.Append(Expression.IfThen(
                Expression.Equal(Expression.Modulo(x, Expression.Constant(2)), Expression.Constant(0)),
                Expression.Continue()));
            fe.Append(Expression.AddAssign(s, x));

            method.Append(fe);
            method.Append(s);

            var mi = typeEmitter.CreateType().GetMethod("SumOdd");
            Assert.Equal(9, mi.Invoke(null, new object[] { new[] { 1, 2, 3, 4, 5 } }));
        }

        // ---------- foreach 枚举器路径 ----------

        [Fact]
        public void GenericEnumerable_DisposesEnumeratorOnException()
        {
            // TrackedEnumerable 仅实现 IEnumerable<int> 与 IEnumerable，没有 Count/Length，
            // 因此走 foreach 枚举器路径并应在 finally 中释放枚举器。
            var typeEmitter = _emitter.DefineType($"FD_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var method = typeEmitter.DefineMethod("ThrowOnEvery", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            var src = method.DefineParameter(typeof(TrackedEnumerable), "src");

            var x = Expression.Variable(typeof(int));

            var fe = Expression.ForEach(x, src);
            fe.Append(Expression.Throw(typeof(InvalidOperationException), "boom"));

            method.Append(fe);
            method.Append(Expression.Constant(0));

            var mi = typeEmitter.CreateType().GetMethod("ThrowOnEvery");

            var src1 = new TrackedEnumerable(new[] { 1, 2, 3 });
            var ex = Assert.Throws<TargetInvocationException>(() => mi.Invoke(null, new object[] { src1 }));
            Assert.IsType<InvalidOperationException>(ex.InnerException);
            Assert.True(src1.LastEnumeratorDisposed, "enumerator should be disposed in finally");
        }

        [Fact]
        public void NonGenericEnumerable_ItemObject_Counts()
        {
            var typeEmitter = _emitter.DefineType($"FN_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var method = typeEmitter.DefineMethod("Count", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            var listParam = method.DefineParameter(typeof(IEnumerable), "list");

            var c = Expression.Variable(typeof(int));
            var x = Expression.Variable(typeof(object));

            method.Append(Expression.Assign(c, Expression.Constant(0)));

            var fe = Expression.ForEach(x, listParam);
            fe.Append(Expression.IncrementAssign(c));

            method.Append(fe);
            method.Append(c);

            var mi = typeEmitter.CreateType().GetMethod("Count");
            Assert.Equal(3, mi.Invoke(null, new object[] { new ArrayList { 1, "a", null } }));
        }

        [Fact]
        public void NonGenericEnumerable_ItemValueType_UnboxesCurrent()
        {
            // 源仅静态类型为非泛型 IEnumerable，但运行时全为 int —— item:int 应通过 unbox.any 安全取值。
            var typeEmitter = _emitter.DefineType($"FNU_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var method = typeEmitter.DefineMethod("Sum", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            var listParam = method.DefineParameter(typeof(IEnumerable), "list");

            var s = Expression.Variable(typeof(int));
            var x = Expression.Variable(typeof(int));

            method.Append(Expression.Assign(s, Expression.Constant(0)));

            var fe = Expression.ForEach(x, listParam);
            fe.Append(Expression.AddAssign(s, x));

            method.Append(fe);
            method.Append(s);

            var mi = typeEmitter.CreateType().GetMethod("Sum");
            Assert.Equal(6, mi.Invoke(null, new object[] { new ArrayList { 1, 2, 3 } }));
        }

        [Fact]
        public void NonGenericEnumerable_ItemReferenceType_CastsCurrent()
        {
            // 验证 castclass：每轮迭代将 x（强转后的 string）赋给 last，结束时 last 应为最后一个元素。
            var typeEmitter = _emitter.DefineType($"FNC_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var method = typeEmitter.DefineMethod("Last", MethodAttributes.Public | MethodAttributes.Static, typeof(string));
            var listParam = method.DefineParameter(typeof(IEnumerable), "list");

            var last = Expression.Variable(typeof(string));
            var x = Expression.Variable(typeof(string));

            method.Append(Expression.Assign(last, Expression.Constant(null, typeof(string))));

            var fe = Expression.ForEach(x, listParam);
            fe.Append(Expression.Assign(last, x));

            method.Append(fe);
            method.Append(last);

            var mi = typeEmitter.CreateType().GetMethod("Last");
            Assert.Equal("c", mi.Invoke(null, new object[] { new ArrayList { "a", "b", "c" } }));

            // 元素类型不匹配时，castclass 会在运行时抛 InvalidCastException。
            var ex = Assert.Throws<TargetInvocationException>(() => mi.Invoke(null, new object[] { new ArrayList { "a", 1, "c" } }));
            Assert.IsType<InvalidCastException>(ex.InnerException);
        }

        [Fact]
        public void GenericEnumerable_PrefersExactMatch()
        {
            // TrackedEnumerable : IEnumerable<int>，item:int —— 走泛型枚举器分支，不需要 unbox。
            var typeEmitter = _emitter.DefineType($"FGE_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var method = typeEmitter.DefineMethod("Sum", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            var src = method.DefineParameter(typeof(TrackedEnumerable), "src");

            var s = Expression.Variable(typeof(int));
            var x = Expression.Variable(typeof(int));

            method.Append(Expression.Assign(s, Expression.Constant(0)));

            var fe = Expression.ForEach(x, src);
            fe.Append(Expression.AddAssign(s, x));

            method.Append(fe);
            method.Append(s);

            var mi = typeEmitter.CreateType().GetMethod("Sum");
            Assert.Equal(6, mi.Invoke(null, new object[] { new TrackedEnumerable(new[] { 1, 2, 3 }) }));
        }

        // ---------- 异常路径 ----------

        [Fact]
        public void ItemTypeIncompatibleWithGenericEnumerable_Throws()
        {
            // List<int> 暴露 IEnumerable<int>，循环变量为 long —— 编译期已可断定无法转化，应抛 AstException。
            var listParam = Expression.Variable(typeof(List<int>));
            var x = Expression.Variable(typeof(long));

            Assert.Throws<AstException>(() => Expression.ForEach(x, listParam));
        }

        [Fact]
        public void ArrayElementTypeMismatch_Throws()
        {
            // int[] 同时实现 IEnumerable<int>，循环变量 string 与之不一致，且不可强转。
            var arr = Expression.Variable(typeof(int[]));
            var x = Expression.Variable(typeof(string));

            Assert.Throws<AstException>(() => Expression.ForEach(x, arr));
        }

        [Fact]
        public void UnsupportedSource_Throws()
        {
            var src = Expression.Variable(typeof(int));
            var item = Expression.Variable(typeof(int));

            Assert.Throws<AstException>(() => Expression.ForEach(item, src));
        }

        [Fact]
        public void MultiDimArray_Throws()
        {
            var src = Expression.Variable(typeof(int[,]));
            var item = Expression.Variable(typeof(int));

            Assert.Throws<AstException>(() => Expression.ForEach(item, src));
        }

        [Fact]
        public void EmptyBody_Throws()
        {
            var typeEmitter = _emitter.DefineType($"FE_Empty_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var method = typeEmitter.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, typeof(void));
            var arr = method.DefineParameter(typeof(int[]), "arr");

            var x = Expression.Variable(typeof(int));
            method.Append(Expression.ForEach(x, arr));

            Assert.Throws<AstException>(() => typeEmitter.CreateType());
        }

        // ---------- 辅助类型 ----------

        public class TrackedEnumerable : IEnumerable<int>
        {
            private readonly int[] _data;

            public TrackedEnumerable(int[] data)
            {
                _data = data;
            }

            public bool LastEnumeratorDisposed { get; private set; }

            public IEnumerator<int> GetEnumerator()
            {
                var e = new TrackedEnumerator(_data, () => LastEnumeratorDisposed = true);
                LastEnumeratorDisposed = false;
                return e;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class TrackedEnumerator : IEnumerator<int>
            {
                private readonly int[] _data;
                private readonly Action _onDispose;
                private int _index = -1;

                public TrackedEnumerator(int[] data, Action onDispose)
                {
                    _data = data;
                    _onDispose = onDispose;
                }

                public int Current => _data[_index];

                object IEnumerator.Current => Current;

                public bool MoveNext() => ++_index < _data.Length;

                public void Reset() => _index = -1;

                public void Dispose() => _onDispose();
            }
        }
    }
}
