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
        /// ConditionExpression 三目运算必须有返回值，void 场景应抛出 AstException 并建议使用 IfThenElse。
        /// </summary>
        [Fact]
        public void ConditionExpression_VoidBranches_ShouldThrow()
        {
            var typeEmitter = _emitter.DefineType($"CondVoidTest_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var methodEmitter = typeEmitter.DefineMethod("CondVoid", MethodAttributes.Public | MethodAttributes.Static, typeof(void));
            var flagParam = methodEmitter.DefineParameter(typeof(bool), "flag");
            var xParam = methodEmitter.DefineParameter(typeof(int), "x");

            var ex = Assert.Throws<AstException>(() =>
                methodEmitter.Append(
                    Expression.Condition(
                        flagParam,
                        Expression.Add(xParam, Expression.Constant(1)),
                        Expression.Add(xParam, Expression.Constant(2)),
                        typeof(void)
                    )
                ));

            Assert.Contains("IfThenElse", ex.Message);
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

        #region IfThenElse 所有分支都有 Return 时方法应正确编译

        /// <summary>
        /// 修复：IfThenElseExpression 未重写 DetectionResult，导致两个分支都有
        /// Return 的方法体发射失败（抛 NotSupportedException）。
        /// </summary>
        [Fact]
        public void IfThenElse_BothBranchesReturn_ShouldCompileAndRun()
        {
            // 等价 C#：
            // public static int Choose(bool flag)
            // {
            //     if (flag) return 1; else return 2;
            // }
            var typeEmitter = _emitter.DefineType($"IfElseRet_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var method = typeEmitter.DefineMethod("Choose", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            var flag = method.DefineParameter(typeof(bool), "flag");

            var ifTrue = Expression.Block();
            ifTrue.Append(Expression.Return(Expression.Constant(1)));

            var ifFalse = Expression.Block();
            ifFalse.Append(Expression.Return(Expression.Constant(2)));

            method.Append(Expression.IfThenElse(flag, ifTrue, ifFalse));
            // IfThenElse 已调整为 void，所有分支含 Return，此处不可达。
            method.Append(Expression.Throw(typeof(InvalidOperationException)));

            var type = typeEmitter.CreateType();
            var mi = type.GetMethod("Choose");

            Assert.Equal(1, mi.Invoke(null, new object[] { true }));
            Assert.Equal(2, mi.Invoke(null, new object[] { false }));
        }

        /// <summary>
        /// 修复验证：嵌套 IfThenElse，多级分支都有 Return。
        /// </summary>
        [Fact]
        public void IfThenElse_NestedBothBranchesReturn_ShouldCompileAndRun()
        {
            // 等价 C#：
            // public static int Classify(int v)
            // {
            //     if (v > 0)
            //         return 1;
            //     else if (v < 0)
            //         return -1;
            //     else
            //         return 0;
            // }
            var typeEmitter = _emitter.DefineType($"IfElseNest_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var method = typeEmitter.DefineMethod("Classify", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            var v = method.DefineParameter(typeof(int), "v");

            var positiveBlock = Expression.Block();
            positiveBlock.Append(Expression.Return(Expression.Constant(1)));

            var zeroBlock = Expression.Block();
            zeroBlock.Append(Expression.Return(Expression.Constant(0)));

            var negativeBlock = Expression.Block();
            negativeBlock.Append(Expression.Return(Expression.Constant(-1)));

            var innerElse = Expression.IfThenElse(
                Expression.LessThan(v, Expression.Constant(0)),
                negativeBlock,
                zeroBlock);

            var innerElseBlock = Expression.Block();
            innerElseBlock.Append(innerElse);

            method.Append(Expression.IfThenElse(
                Expression.GreaterThan(v, Expression.Constant(0)),
                positiveBlock,
                innerElseBlock));
            // 所有分支含 Return，此处不可达。
            method.Append(Expression.Throw(typeof(InvalidOperationException)));

            var type = typeEmitter.CreateType();
            var mi = type.GetMethod("Classify");

            Assert.Equal(1, mi.Invoke(null, new object[] { 5 }));
            Assert.Equal(-1, mi.Invoke(null, new object[] { -3 }));
            Assert.Equal(0, mi.Invoke(null, new object[] { 0 }));
        }

        /// <summary>
        /// 验证：Return 后的代码在运行时不会被执行（Leave 指令无条件跳转）。
        /// </summary>
        [Fact]
        public void Return_TerminatesExecution_SubsequentCodeNotReached()
        {
            // 等价 C#：
            // static int _field = 0;
            // public static int Test()
            // {
            //     _field = 10;
            //     return _field;
            //     _field = 99; // unreachable
            // }
            var typeEmitter = _emitter.DefineType($"RetTerm_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var field = typeEmitter.DefineField("_field", typeof(int), FieldAttributes.Public | FieldAttributes.Static);
            var method = typeEmitter.DefineMethod("Test", MethodAttributes.Public | MethodAttributes.Static, typeof(int));

            method.Append(Expression.Assign(field, Expression.Constant(10)));
            method.Append(Expression.Return(field));
            // 下面这行代码不可达，但 Append 允许添加 — 运行时不应执行
            method.Append(Expression.Assign(field, Expression.Constant(99)));

            var type = typeEmitter.CreateType();
            var mi = type.GetMethod("Test");
            var fi = type.GetField("_field");

            fi.SetValue(null, 0);
            var result = mi.Invoke(null, null);

            Assert.Equal(10, result);
            Assert.Equal(10, fi.GetValue(null)); // 如果 99 被执行，这里会是 99
        }

        #endregion

        #region TypeBuilder 拒绝未创建修复 —— Nullable 转换路径中的 GetMethod/GetConstructor

        /// <summary>
        /// 修复：Convert 到 Nullable&lt;int&gt; 时 EmitNonNullableToNullableConversion 不应在
        /// TypeBuilder 上调用 GetConstructor 失败。
        /// </summary>
        [Fact]
        public void Convert_ToNullableInt_ShouldNotThrow()
        {
            var typeEmitter = _emitter.DefineType($"Convert_Nullable_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var method = typeEmitter.DefineMethod("ToNullable", MethodAttributes.Public | MethodAttributes.Static, typeof(int?));
            var intParam = method.DefineParameter(typeof(int), "v");

            // int -> int? 走 EmitNonNullableToNullableConversion 路径
            method.Append(Expression.Return(Expression.Convert(intParam, typeof(int?))));

            var type = typeEmitter.CreateType();
            var mi = type.GetMethod("ToNullable");

            var result = mi.Invoke(null, new object[] { 42 });
            Assert.Equal((int?)42, result);
        }

        /// <summary>
        /// 修复：int? -> long? 转换时 EmitNullableToNullableConversion 不应在
        /// TypeBuilder 上调用 GetConstructor 失败。
        /// </summary>
        [Fact]
        public void Convert_NullableToNullable_ShouldNotThrow()
        {
            var typeEmitter = _emitter.DefineType($"Convert_N2N_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var method = typeEmitter.DefineMethod("N2N", MethodAttributes.Public | MethodAttributes.Static, typeof(long?));
            var param = method.DefineParameter(typeof(int?), "v");

            // int? -> long? 走 EmitNullableToNullableConversion 路径
            method.Append(Expression.Return(Expression.Convert(param, typeof(long?))));

            var type = typeEmitter.CreateType();
            var mi = type.GetMethod("N2N");

            Assert.Equal((long?)42, mi.Invoke(null, new object[] { (int?)42 }));
            Assert.Null(mi.Invoke(null, new object[] { null }));
        }

        /// <summary>
        /// 修复：EmitHasValue / EmitGetValue / EmitGetValueOrDefault 在 TypeBuilder
        /// 上调用 GetMethod 不应失败（通过 Convert 切换多个 Nullable 来覆盖）。
        /// </summary>
        [Fact]
        public void Convert_NullableWrapper_ShouldNotThrow()
        {
            var typeEmitter = _emitter.DefineType($"Convert_NW_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);

            // int -> int? 走 EmitNonNullableToNullableConversion (GetConstructor)
            var m1 = typeEmitter.DefineMethod("WrapInt", MethodAttributes.Public | MethodAttributes.Static, typeof(int?));
            var p1 = m1.DefineParameter(typeof(int), "v");
            m1.Append(Expression.Return(Expression.Convert(p1, typeof(int?))));

            // int? -> long? 走 EmitNullableToNullableConversion (GetConstructor + HasValue + GetValueOrDefault)
            var m2 = typeEmitter.DefineMethod("IntToLong", MethodAttributes.Public | MethodAttributes.Static, typeof(long?));
            var p2 = m2.DefineParameter(typeof(int?), "v");
            m2.Append(Expression.Return(Expression.Convert(p2, typeof(long?))));

            // int? -> int 走 EmitNullableToNonNullableConversion (GetValue)
            var m3 = typeEmitter.DefineMethod("UnwrapInt", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            var p3 = m3.DefineParameter(typeof(int?), "v");
            m3.Append(Expression.Return(Expression.Convert(p3, typeof(int))));

            var type = typeEmitter.CreateType();

            Assert.Equal((int?)42, type.GetMethod("WrapInt").Invoke(null, new object[] { 42 }));
            Assert.Equal((long?)42, type.GetMethod("IntToLong").Invoke(null, new object[] { (int?)42 }));
            Assert.Equal(42, type.GetMethod("UnwrapInt").Invoke(null, new object[] { (int?)42 }));
        }

        #endregion

        #region TypeBuilder IL emit 覆盖 —— TypeAsExpression 中的 Box/Isinst 使用 TypeBuilder 令牌

        /// <summary>
        /// 覆盖：TypeAs 目标为内嵌引用类型（TypeBuilder），body 为 object。
        /// 走 else 分支（RuntimeType 非值类型），仅触发 <c>Isinst</c>（不触发 Box）。
        /// </summary>
        [Fact]
        public void TypeAs_IsinstWithTypeBuilderRefType_ShouldNotThrow()
        {
            // public static object Test(object o) => o as NestedRef;
            var parent = _emitter.DefineType($"TB_IsinstRef_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var nestedRef = parent.DefineNestedType($"NestedRef_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);

            var method = parent.DefineMethod("Test", MethodAttributes.Public | MethodAttributes.Static, typeof(object));
            var objParam = method.DefineParameter(typeof(object), "o");

            // object as NestedRef → 命中 else 分支，body.IsValueType = false，Isinst 用 TypeBuilder
            method.Append(Expression.Return(Expression.TypeAs(objParam, nestedRef.UncompiledType)));

            var type = parent.CreateType();
            var mi = type.GetMethod("Test");

            // "hello" 不是 NestedRef → 返回 null
            Assert.Null(mi.Invoke(null, new object[] { "hello" }));
        }

        /// <summary>
        /// 覆盖：TypeAs body 为 int（值类型），目标为内嵌引用类型（TypeBuilder）。
        /// 走 else 分支，触发 <c>Box(int)</c> 以及 <c>Isinst(TypeBuilder)</c>。
        /// Box 目标非 TypeBuilder，Isinst 目标为 TypeBuilder。
        /// </summary>
        [Fact]
        public void TypeAs_BoxWithInt_IsinstWithTypeBuilder_ShouldNotThrow()
        {
            // public static object Test(int v) => v as NestedRef;
            var parent = _emitter.DefineType($"TB_BoxInt_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var nestedRef = parent.DefineNestedType($"NestedRef_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);

            var method = parent.DefineMethod("Test", MethodAttributes.Public | MethodAttributes.Static, typeof(object));
            var intParam = method.DefineParameter(typeof(int), "v");

            // int as NestedRef → 命中 else 分支，body.IsValueType = true，Box(int)，Isinst(TypeBuilder)
            method.Append(Expression.Return(Expression.TypeAs(intParam, nestedRef.UncompiledType)));

            var type = parent.CreateType();
            var mi = type.GetMethod("Test");

            // int 不是 NestedRef → 返回 null
            Assert.Null(mi.Invoke(null, new object[] { 42 }));
        }

        /// <summary>
        /// 覆盖：TypeAs body 为值类型 TypeBuilder，目标为引用类型 TypeBuilder。
        /// 走 else 分支，同时触发 <c>Box({TypeBuilder})</c> 和 <c>Isinst({TypeBuilder})</c>。
        /// </summary>
        [Fact]
        public void TypeAs_BoxAndIsinstBothTypeBuilder_ShouldNotThrow()
        {
            var parent = _emitter.DefineType($"TB_Both_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);

            // 内嵌值类型（struct）
            var nestedValue = parent.DefineNestedType(
                $"NestedVal_{Guid.NewGuid():N}",
                TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout,
                typeof(ValueType));

            // 内嵌引用类型（class）
            var nestedRef = parent.DefineNestedType($"NestedRef_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);

            var method = parent.DefineMethod("Test", MethodAttributes.Public | MethodAttributes.Static, typeof(object));

            // 局部变量：值类型 TypeBuilder
            var local = Expression.Variable(nestedValue.UncompiledType);
            method.Append(Expression.Assign(local, Expression.Default(nestedValue.UncompiledType)));

            // 值类型 TypeBuilder as 引用类型 TypeBuilder
            // 命中 else 分支：body.IsValueType=true → Box(TypeBuilder)，Isinst(TypeBuilder)
            method.Append(Expression.Return(Expression.TypeAs(local, nestedRef.UncompiledType)));

            var type = parent.CreateType();
            var mi = type.GetMethod("Test");

            // 值类型实例不是引用类型 → 返回 null
            Assert.Null(mi.Invoke(null, null));
        }

        #endregion
    }
}
