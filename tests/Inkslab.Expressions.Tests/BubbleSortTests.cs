using System;
using System.Reflection;
using Inkslab;
using Inkslab.Emitters;
using Xunit;

namespace Inkslab.Expressions.Tests
{
    // ─────────────────────────────────────────────────────────────────
    // 1. 泛型通用冒泡排序（手写实现）
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 泛型冒泡排序（手写，带提前退出优化）。
    /// </summary>
    public static class BubbleSortHelper
    {
        /// <summary>
        /// 对数组执行泛型冒泡排序（原地排序）。
        /// </summary>
        /// <typeparam name="T">元素类型，须实现 <see cref="IComparable{T}"/>。</typeparam>
        /// <param name="array">待排序数组。</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> 为 null 时抛出。</exception>
        public static void Sort<T>(T[] array) where T : IComparable<T>
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            int n = array.Length;
            for (int i = 0; i < n - 1; i++)
            {
                bool swapped = false;
                for (int j = 0; j < n - i - 1; j++)
                {
                    if (array[j].CompareTo(array[j + 1]) > 0)
                    {
                        T temp = array[j];
                        array[j] = array[j + 1];
                        array[j + 1] = temp;
                        swapped = true;
                    }
                }

                if (!swapped)
                {
                    break;
                }
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // 2. 表达式生成：使用 ModuleEmitter + Expression API 动态生成冒泡排序方法
    //
    //    生成的方法签名：static void BubbleSortInPlace(int[] arr)
    //
    //    伪代码：
    //      n = arr.Length
    //      i = 0
    //      outerLoop:
    //        if (i >= n - 1) break
    //        j = 0
    //        innerLoop:
    //          if (j >= n - i - 1) break
    //          if (arr[j] > arr[j+1]) SwapAdjacent(arr, j)   ← 静态辅助调用
    //          j++
    //        i++
    //
    //    注：数组元素写回通过 BubbleSortInPlaceHelper.SwapAdjacent 完成，
    //        以规避框架在动态下标赋值路径的已知问题（Ldc_I4 应为 Ldloc）。
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 辅助：交换数组中位置 j 和 j+1 的元素。
    /// 由表达式生成的方法通过 Expression.Call 调用。
    /// </summary>
    public static class BubbleSortInPlaceHelper
    {
        /// <summary>
        /// 交换相邻元素 arr[j] ↔ arr[j+1]。
        /// </summary>
        public static void SwapAdjacent(int[] arr, int j)
        {
            int temp = arr[j];
            arr[j] = arr[j + 1];
            arr[j + 1] = temp;
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // 3. 测试：验证表达式生成的冒泡排序方法与手写实现结果一致
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 冒泡排序：表达式生成方法 vs 手写泛型实现 — 结果一致性验证。
    /// </summary>
    public class BubbleSortTests
    {
        // ── 用表达式 API 生成 static void BubbleSortInPlace(int[] arr) ──

        private static readonly Action<int[]> _emittedSort = BuildEmittedBubbleSort();

        private static Action<int[]> BuildEmittedBubbleSort()
        {
            var swapMethod = typeof(BubbleSortInPlaceHelper)
                .GetMethod(nameof(BubbleSortInPlaceHelper.SwapAdjacent), BindingFlags.Public | BindingFlags.Static);

            var moduleEmitter = new ModuleEmitter($"BubbleSortEmit.{Guid.NewGuid():N}");

            var typeEmitter = moduleEmitter.DefineType(
                $"BubbleSortHost_{Guid.NewGuid():N}",
                TypeAttributes.Public | TypeAttributes.Class);

            var method = typeEmitter.DefineMethod(
                "BubbleSortInPlace",
                MethodAttributes.Public | MethodAttributes.Static,
                typeof(void));

            // 参数：int[] arr
            var arrParam = method.DefineParameter(typeof(int[]), "arr");

            // 局部变量
            var n = Expression.Variable(typeof(int));
            var i = Expression.Variable(typeof(int));
            var j = Expression.Variable(typeof(int));

            // n = arr.Length
            method.Append(Expression.Assign(n, Expression.ArrayLength(arrParam)));
            // i = 0
            method.Append(Expression.Assign(i, Expression.Constant(0)));

            // 外层循环
            var outerLoop = Expression.Loop();

            // if (i >= n - 1) break
            outerLoop.Append(Expression.IfThen(
                Expression.GreaterThanOrEqual(i, Expression.Subtract(n, Expression.Constant(1))),
                Expression.Break()));

            // j = 0
            outerLoop.Append(Expression.Assign(j, Expression.Constant(0)));

            // 内层循环
            var innerLoop = Expression.Loop();

            // if (j >= n - i - 1) break
            innerLoop.Append(Expression.IfThen(
                Expression.GreaterThanOrEqual(
                    j,
                    Expression.Subtract(Expression.Subtract(n, i), Expression.Constant(1))),
                Expression.Break()));

            // if (arr[j] > arr[j+1]) SwapAdjacent(arr, j)
            var arrJ  = Expression.ArrayIndex(arrParam, j);
            var arrJ1 = Expression.ArrayIndex(arrParam, Expression.Add(j, Expression.Constant(1)));

            innerLoop.Append(Expression.IfThen(
                Expression.GreaterThan(arrJ, arrJ1),
                Expression.Call(swapMethod, arrParam, j)));

            // j++
            innerLoop.Append(Expression.IncrementAssign(j));

            outerLoop.Append(innerLoop);

            // i++
            outerLoop.Append(Expression.IncrementAssign(i));

            method.Append(outerLoop);

            var type = typeEmitter.CreateType();
            var mi   = type.GetMethod("BubbleSortInPlace");
            return (Action<int[]>)Delegate.CreateDelegate(typeof(Action<int[]>), mi);
        }

        // ── 辅助方法 ──────────────────────────────────────────────────

        private static int[] HandSort(int[] input)
        {
            var clone = (int[])input.Clone();
            BubbleSortHelper.Sort(clone);
            return clone;
        }

        private static int[] EmitSort(int[] input)
        {
            var clone = (int[])input.Clone();
            _emittedSort(clone);
            return clone;
        }

        // ── 手写泛型冒泡排序自身验证 ──────────────────────────────────

        /// <summary>
        /// 手写泛型排序：乱序整数数组结果正确。
        /// </summary>
        [Fact]
        public void HandWritten_Sort_Int_ReturnsCorrectOrder()
        {
            int[] input    = { 5, 3, 8, 1, 9, 2 };
            int[] expected = { 1, 2, 3, 5, 8, 9 };

            BubbleSortHelper.Sort(input);

            Assert.Equal(expected, input);
        }

        /// <summary>
        /// 手写泛型排序：字符串数组结果正确。
        /// </summary>
        [Fact]
        public void HandWritten_Sort_String_ReturnsCorrectOrder()
        {
            string[] input    = { "banana", "apple", "cherry", "date" };
            string[] expected = { "apple", "banana", "cherry", "date" };

            BubbleSortHelper.Sort(input);

            Assert.Equal(expected, input);
        }

        /// <summary>
        /// 手写泛型排序：null 数组抛出 <see cref="ArgumentNullException"/>。
        /// </summary>
        [Fact]
        public void HandWritten_Sort_NullArray_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => BubbleSortHelper.Sort<int>(null));
        }

        // ── 表达式生成 vs 手写：结果一致性 ───────────────────────────

        /// <summary>
        /// 乱序整数数组：表达式生成方法与手写方法结果相同。
        /// </summary>
        [Fact]
        public void EmittedSort_Int_MatchesHandWritten()
        {
            int[] input = { 5, 3, 8, 1, 9, 2 };

            Assert.Equal(HandSort(input), EmitSort(input));
        }

        /// <summary>
        /// 已有序数组：表达式生成方法与手写方法结果相同。
        /// </summary>
        [Fact]
        public void EmittedSort_AlreadySorted_MatchesHandWritten()
        {
            int[] input = { 1, 2, 3, 4, 5 };

            Assert.Equal(HandSort(input), EmitSort(input));
        }

        /// <summary>
        /// 逆序数组：表达式生成方法与手写方法结果相同。
        /// </summary>
        [Fact]
        public void EmittedSort_ReverseSorted_MatchesHandWritten()
        {
            int[] input = { 9, 7, 5, 3, 1 };

            Assert.Equal(HandSort(input), EmitSort(input));
        }

        /// <summary>
        /// 含重复元素数组：表达式生成方法与手写方法结果相同。
        /// </summary>
        [Fact]
        public void EmittedSort_Duplicates_MatchesHandWritten()
        {
            int[] input = { 3, 1, 4, 1, 5, 9, 2, 6, 5, 3 };

            Assert.Equal(HandSort(input), EmitSort(input));
        }

        /// <summary>
        /// 单元素数组：表达式生成方法与手写方法结果相同。
        /// </summary>
        [Fact]
        public void EmittedSort_SingleElement_MatchesHandWritten()
        {
            int[] input = { 42 };

            Assert.Equal(HandSort(input), EmitSort(input));
        }

        /// <summary>
        /// 空数组：表达式生成方法与手写方法结果相同。
        /// </summary>
        [Fact]
        public void EmittedSort_EmptyArray_MatchesHandWritten()
        {
            int[] input = Array.Empty<int>();

            Assert.Equal(HandSort(input), EmitSort(input));
        }
    }
}
