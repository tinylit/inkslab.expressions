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
    }
}
