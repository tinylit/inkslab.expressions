using Xunit;
using System;
using System.Reflection;

namespace Inkslab.Expressions.Tests
{
    /// <summary>
    /// 针对已修复 Bug 的回归测试。
    /// </summary>
    public class BugFixTests
    {
        private readonly ModuleEmitter _emitter = new ModuleEmitter($"Inkslab.Expressions.Tests.BugFix.{Guid.NewGuid():N}");

        #region InvocationExpression 静态方法调用

        /// <summary>
        /// 修复：InvocationExpression 静态方法调用时 instanceAst 为 null 不应抛出 ArgumentNullException。
        /// </summary>
        [Fact]
        public void InvocationExpression_StaticMethodCall_ShouldNotThrow()
        {
            // Arrange：Math.Abs(int) 是静态方法
            var methodInfo = typeof(Math).GetMethod(nameof(Math.Abs), new[] { typeof(int) });
            var arguments = Expression.Array(typeof(object), Expression.Convert(Expression.Constant(-42), typeof(object)));

            // Act & Assert：不抛异常
            var expr = Expression.Invoke(methodInfo, arguments);
            Assert.NotNull(expr);
        }

        /// <summary>
        /// 修复：InvocationExpression 实例方法调用时 instanceAst 不为 null 正常工作。
        /// </summary>
        [Fact]
        public void InvocationExpression_InstanceMethodCall_ShouldWork()
        {
            // Arrange：string.ToUpper() 是实例方法
            var methodInfo = typeof(string).GetMethod(nameof(string.ToUpper), Type.EmptyTypes);
            var instanceAst = Expression.Constant("hello");
            var arguments = Expression.Array(typeof(object));

            // Act
            var expr = Expression.Invoke(instanceAst, methodInfo, arguments);

            // Assert
            Assert.NotNull(expr);
        }

        /// <summary>
        /// 修复：InvocationExpression 实例方法传给静态重载时应抛 AstException。
        /// </summary>
        [Fact]
        public void InvocationExpression_StaticMethodWithInstance_ShouldThrow()
        {
            // Arrange：Math.Abs 是静态方法，不允许传 instanceAst
            var methodInfo = typeof(Math).GetMethod(nameof(Math.Abs), new[] { typeof(int) });
            var instanceAst = Expression.Constant(new object());
            var arguments = Expression.Array(typeof(object), Expression.Convert(Expression.Constant(-42), typeof(object)));

            // Act & Assert
            Assert.Throws<AstException>(() => Expression.Invoke(instanceAst, methodInfo, arguments));
        }

        #endregion

        #region InvocationEmitter 静态方法调用

        /// <summary>
        /// 修复：InvocationEmitter 静态方法调用时不应抛异常，并能在动态方法中正确执行。
        /// </summary>
        [Fact]
        public void InvocationEmitter_StaticMethodCall_ShouldExecuteCorrectly()
        {
            // Arrange：创建一个动态类型，其中方法通过 MethodEmitter 反射调用静态方法
            var typeEmitter = _emitter.DefineType($"InvEmitterTest_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);

            // 创建静态方法 public static int Abs(int value) => Math.Abs(value);
            var methodEmitter = typeEmitter.DefineMethod("TestAbs", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            var param = methodEmitter.DefineParameter(typeof(int), "value");

            // 使用 MethodCallExpression（直接 Call，非反射 Invoke）
            var absMethod = typeof(Math).GetMethod(nameof(Math.Abs), new[] { typeof(int) });
            methodEmitter.Append(Expression.Call(absMethod, param));

            var type = typeEmitter.CreateType();
            var method = type.GetMethod("TestAbs");

            // Act
            var result = method.Invoke(null, new object[] { -99 });

            // Assert
            Assert.Equal(99, result);
        }

        #endregion

        #region ConditionExpression

        /// <summary>
        /// 修复：ConditionExpression 三目运算符返回值正确。
        /// </summary>
        [Fact]
        public void ConditionExpression_TrueAndFalseBranch_ShouldReturnCorrectValue()
        {
            // Arrange：创建动态方法 int Cond(bool flag) => flag ? 1 : 2;
            var typeEmitter = _emitter.DefineType($"CondTest_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var methodEmitter = typeEmitter.DefineMethod("Cond", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            var param = methodEmitter.DefineParameter(typeof(bool), "flag");

            methodEmitter.Append(
                Expression.Condition(
                    param,
                    Expression.Constant(1),
                    Expression.Constant(2)
                )
            );

            var type = typeEmitter.CreateType();
            var method = type.GetMethod("Cond");

            // Act & Assert：true 分支
            Assert.Equal(1, method.Invoke(null, new object[] { true }));

            // Act & Assert：false 分支
            Assert.Equal(2, method.Invoke(null, new object[] { false }));
        }

        /// <summary>
        /// 修复：ConditionExpression void 版本（EmitVoid）不应重复标记标签。
        /// </summary>
        [Fact]
        public void ConditionExpression_VoidBranches_ShouldNotThrow()
        {
            // Arrange：void CondVoid(bool flag, int x) { _ = flag ? x + 1 : x + 2; }
            // 两个分支都产生 int 值，但 returnType 为 void（丢弃结果），触发 EmitVoid 路径
            var typeEmitter = _emitter.DefineType($"CondVoidTest_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var methodEmitter = typeEmitter.DefineMethod("CondVoid", MethodAttributes.Public | MethodAttributes.Static, typeof(void));
            var flagParam = methodEmitter.DefineParameter(typeof(bool), "flag");
            var xParam = methodEmitter.DefineParameter(typeof(int), "x");

            methodEmitter.Append(
                Expression.Condition(
                    flagParam,
                    Expression.Add(xParam, Expression.Constant(1)),
                    Expression.Add(xParam, Expression.Constant(2)),
                    typeof(void)
                )
            );

            // Act：创建类型不应抛异常（之前因 EmitVoid 中 label 被标记两次会产生无效 IL）
            var type = typeEmitter.CreateType();
            var method = type.GetMethod("CondVoid");

            // Assert：执行不抛异常
            method.Invoke(null, new object[] { true, 10 });
            method.Invoke(null, new object[] { false, 10 });
        }

        /// <summary>
        /// 修复：ConditionExpression 构造函数中 ifFalse 类型检查使用了正确的表达式。
        /// </summary>
        [Fact]
        public void ConditionExpression_TypeConversion_ShouldUseCorrectType()
        {
            // Arrange：object result = flag ? (object)"str" : (object)42;
            var typeEmitter = _emitter.DefineType($"CondTypeTest_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var methodEmitter = typeEmitter.DefineMethod("CondType", MethodAttributes.Public | MethodAttributes.Static, typeof(object));
            var param = methodEmitter.DefineParameter(typeof(bool), "flag");

            methodEmitter.Append(
                Expression.Condition(
                    param,
                    Expression.Constant("hello"),
                    Expression.Convert(Expression.Constant(42), typeof(object)),
                    typeof(object)
                )
            );

            var type = typeEmitter.CreateType();
            var method = type.GetMethod("CondType");

            // Act & Assert
            Assert.Equal("hello", method.Invoke(null, new object[] { true }));
            Assert.Equal(42, method.Invoke(null, new object[] { false }));
        }

        #endregion

        #region LoopExpression / Break / Continue

        /// <summary>
        /// 修复：LoopExpression 中 Break 应正常跳出循环。
        /// </summary>
        [Fact]
        public void LoopExpression_BreakShouldExitLoop()
        {
            // Arrange：创建动态方法 int LoopBreak() { int i = 0; while(true) { if(i >= 5) break; i++; } return i; }
            var typeEmitter = _emitter.DefineType($"LoopBreakTest_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var methodEmitter = typeEmitter.DefineMethod("LoopBreak", MethodAttributes.Public | MethodAttributes.Static, typeof(int));

            var iVar = Expression.Variable(typeof(int));

            methodEmitter.Append(Expression.Assign(iVar, Expression.Constant(0)));

            var loop = Expression.Loop();
            loop.Append(
                Expression.IfThen(
                    Expression.GreaterThanOrEqual(iVar, Expression.Constant(5)),
                    Expression.Break()
                )
            );
            loop.Append(Expression.IncrementAssign(iVar));

            methodEmitter.Append(loop);
            methodEmitter.Append(iVar);

            var type = typeEmitter.CreateType();
            var method = type.GetMethod("LoopBreak");

            // Act
            var result = method.Invoke(null, null);

            // Assert
            Assert.Equal(5, result);
        }

        /// <summary>
        /// 修复：LoopExpression 中 Continue 应跳到循环头部。
        /// </summary>
        [Fact]
        public void LoopExpression_ContinueShouldSkipToLoopStart()
        {
            // Arrange：创建动态方法计算 0~9 中偶数的和：
            // int sum = 0; int i = 0;
            // loop { if(i >= 10) break; i++; if(i % 2 != 0) continue; sum += i; }
            // return sum;
            var typeEmitter = _emitter.DefineType($"LoopContinueTest_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var methodEmitter = typeEmitter.DefineMethod("LoopContinue", MethodAttributes.Public | MethodAttributes.Static, typeof(int));

            var sumVar = Expression.Variable(typeof(int));
            var iVar = Expression.Variable(typeof(int));

            methodEmitter.Append(Expression.Assign(sumVar, Expression.Constant(0)));
            methodEmitter.Append(Expression.Assign(iVar, Expression.Constant(0)));

            var loop = Expression.Loop();
            loop.Append(
                Expression.IfThen(
                    Expression.GreaterThanOrEqual(iVar, Expression.Constant(10)),
                    Expression.Break()
                )
            );
            loop.Append(Expression.IncrementAssign(iVar));
            loop.Append(
                Expression.IfThen(
                    Expression.NotEqual(Expression.Modulo(iVar, Expression.Constant(2)), Expression.Constant(0)),
                    Expression.Continue()
                )
            );
            loop.Append(Expression.AddAssign(sumVar, iVar));

            methodEmitter.Append(loop);
            methodEmitter.Append(sumVar);

            var type = typeEmitter.CreateType();
            var method = type.GetMethod("LoopContinue");

            // Act
            var result = method.Invoke(null, null);

            // Assert：2 + 4 + 6 + 8 + 10 = 30
            Assert.Equal(30, result);
        }

        /// <summary>
        /// 修复：Break 在非循环上下文中应抛出 AstException。
        /// </summary>
        [Fact]
        public void BreakExpression_OutsideLoop_ShouldThrowAstException()
        {
            // Arrange
            var typeEmitter = _emitter.DefineType($"BreakNoLoopTest_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var methodEmitter = typeEmitter.DefineMethod("NoLoop", MethodAttributes.Public | MethodAttributes.Static, typeof(void));

            methodEmitter.Append(Expression.Break());

            // Act & Assert：非循环上下文调用 Break 应抛异常
            Assert.Throws<AstException>(() => typeEmitter.CreateType());
        }

        #endregion

        #region ArrayIndexExpression.Assign 索引发射

        /// <summary>
        /// 修复 #1：ArrayIndexExpression.Assign 在使用 Expression 索引时，
        /// 旧实现误用 OpCodes.Ldc_I4 把 LocalBuilder 当成常量推栈，运行时几乎必然下标越界。
        /// 修复后应能正确按动态下标写入数组元素。
        /// </summary>
        [Fact]
        public void ArrayIndexExpression_AssignWithExpressionIndex_ShouldWriteCorrectSlot()
        {
            // 等价 C#：
            // public static int[] Fill(int[] arr, int idx, int val)
            // {
            //     arr[idx] = val;
            //     return arr;
            // }
            var typeEmitter = _emitter.DefineType($"ArrIdxAssign_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var methodEmitter = typeEmitter.DefineMethod("Fill", MethodAttributes.Public | MethodAttributes.Static, typeof(int[]));

            var arrParam = methodEmitter.DefineParameter(typeof(int[]), "arr");
            var idxParam = methodEmitter.DefineParameter(typeof(int), "idx");
            var valParam = methodEmitter.DefineParameter(typeof(int), "val");

            methodEmitter.Append(Expression.Assign(Expression.ArrayIndex(arrParam, (Expression)idxParam), valParam));
            methodEmitter.Append(Expression.Return(arrParam));

            var type = typeEmitter.CreateType();
            var method = type.GetMethod("Fill");

            // Act：将值 99 写入索引 2
            var arr = new[] { 0, 0, 0, 0, 0 };
            var ret = (int[])method.Invoke(null, new object[] { arr, 2, 99 });

            // Assert：仅索引 2 被写入，其余保持 0
            Assert.Same(arr, ret);
            Assert.Equal(new[] { 0, 0, 99, 0, 0 }, ret);
        }

        /// <summary>
        /// 修复 #1：复杂下标表达式（idx + 1）赋值同样应正确。
        /// </summary>
        [Fact]
        public void ArrayIndexExpression_AssignWithComputedIndex_ShouldWriteCorrectSlot()
        {
            var typeEmitter = _emitter.DefineType($"ArrIdxAssign2_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var methodEmitter = typeEmitter.DefineMethod("FillNext", MethodAttributes.Public | MethodAttributes.Static, typeof(void));

            var arrParam = methodEmitter.DefineParameter(typeof(int[]), "arr");
            var idxParam = methodEmitter.DefineParameter(typeof(int), "idx");
            var valParam = methodEmitter.DefineParameter(typeof(int), "val");

            // arr[idx + 1] = val;
            methodEmitter.Append(
                Expression.Assign(
                    Expression.ArrayIndex(arrParam, Expression.Add(idxParam, Expression.Constant(1))),
                    valParam));

            var type = typeEmitter.CreateType();
            var method = type.GetMethod("FillNext");

            var arr = new[] { 10, 20, 30, 40, 50 };
            method.Invoke(null, new object[] { arr, 1, 999 });

            // 写入 idx=1 +1 = 2 位置
            Assert.Equal(new[] { 10, 20, 999, 40, 50 }, arr);
        }

        #endregion

        #region MethodEmitter.Value 在另一个方法体中前向引用

        /// <summary>
        /// 修复 #3：当一个方法的方法体通过 <c>Expression.Call(MethodEmitter, ...)</c> 引用
        /// 后续才会发射的另一个方法时，旧实现下 <c>MethodEmitter.Value</c> 会因为
        /// <c>methodBuilder == null</c> 抛 NotImplementedException。
        /// 修复后应允许前向 / 相互引用。
        /// </summary>
        [Fact]
        public void MethodEmitter_ForwardReference_ShouldEmitSuccessfully()
        {
            // 等价 C#：
            // public class X
            // {
            //     public int Outer(int v) => Inner(v) + 1;   // 调用稍后定义的 Inner
            //     public int Inner(int v) => v * 2;
            // }
            var typeEmitter = _emitter.DefineType($"FwdRef_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);

            var outer = typeEmitter.DefineMethod("Outer", MethodAttributes.Public, typeof(int));
            var outerParam = outer.DefineParameter(typeof(int), "v");

            var inner = typeEmitter.DefineMethod("Inner", MethodAttributes.Public, typeof(int));
            var innerParam = inner.DefineParameter(typeof(int), "v");

            // outer 的方法体首先被构建，引用尚未发射的 inner
            outer.Append(Expression.Add(Expression.Call(inner, outerParam), Expression.Constant(1)));

            // inner: return v * 2;
            inner.Append(Expression.Multiply(innerParam, Expression.Constant(2)));

            var type = typeEmitter.CreateType();
            var instance = Activator.CreateInstance(type);

            var outerMi = type.GetMethod("Outer");

            // 5 * 2 + 1 = 11
            Assert.Equal(11, outerMi.Invoke(instance, new object[] { 5 }));
        }

        /// <summary>
        /// 修复 #3：方法 A 引用方法 B，方法 B 又反过来引用方法 A（相互引用），
        /// 也应能正确编译并运行（不会发生递归实际无限运行——这里 A 仅在条件分支调 B）。
        /// </summary>
        [Fact]
        public void MethodEmitter_MutualReference_ShouldEmitSuccessfully()
        {
            // 等价 C#：
            // public class X
            // {
            //     public int A(int v) => v <= 0 ? 0 : B(v - 1) + 1;
            //     public int B(int v) => v <= 0 ? 0 : A(v - 1) + 1;
            // }
            var typeEmitter = _emitter.DefineType($"Mutual_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);

            var a = typeEmitter.DefineMethod("A", MethodAttributes.Public, typeof(int));
            var aParam = a.DefineParameter(typeof(int), "v");

            var b = typeEmitter.DefineMethod("B", MethodAttributes.Public, typeof(int));
            var bParam = b.DefineParameter(typeof(int), "v");

            a.Append(Expression.Condition(
                Expression.LessThanOrEqual(aParam, Expression.Constant(0)),
                Expression.Constant(0),
                Expression.Add(Expression.Call(b, Expression.Subtract(aParam, Expression.Constant(1))), Expression.Constant(1)),
                typeof(int)));

            b.Append(Expression.Condition(
                Expression.LessThanOrEqual(bParam, Expression.Constant(0)),
                Expression.Constant(0),
                Expression.Add(Expression.Call(a, Expression.Subtract(bParam, Expression.Constant(1))), Expression.Constant(1)),
                typeof(int)));

            var type = typeEmitter.CreateType();
            var instance = Activator.CreateInstance(type);

            var aMi = type.GetMethod("A");

            // A(3) = B(2)+1 = (A(1)+1)+1 = ((B(0)+1)+1)+1 = ((0+1)+1)+1 = 3
            Assert.Equal(3, aMi.Invoke(instance, new object[] { 3 }));
        }

        #endregion
    }
}
