using Xunit;
using System;
using System.Reflection;

namespace Inkslab.Expressions.Tests
{
    /// <summary>
    /// 针对 cross-scope-memberref-framework-issue.md 中记录问题的修复验证测试。
    /// </summary>
    public class FrameworkIssueFixTests
    {
        private readonly ModuleEmitter _emitter = new ModuleEmitter($"Inkslab.Expressions.Tests.Fix.{Guid.NewGuid():N}");

        #region 辅助接口定义

        /// <summary>
        /// 用于测试接口方法通过 object 实例调用的接口。
        /// </summary>
        public interface IBindable
        {
            string BindMemberRefs(int slot, int level);
        }

        /// <summary>
        /// 用于测试接口方法通过 Convert + object 实例调用的接口。
        /// </summary>
        public interface IValueProvider
        {
            int GetValue();
        }

        #endregion

        #region Issue #6: 接口 MethodInfo + object 实例被类型检查拒绝

        /// <summary>
        /// Issue #6: 当 instance 类型为 object、MethodInfo.DeclaringType 为接口时，
        /// GetReturnType 的 IsAssignableFrom 检查失败（接口不可赋值给 object）。
        /// 修复后应允许 object 实例调用接口方法。
        /// </summary>
        [Fact]
        public void Call_InterfaceMethod_WithObjectInstance_ShouldNotThrow()
        {
            // Arrange：定义实现 IBindable 的动态类型
            var implEmitter = _emitter.DefineType($"Impl6A_{Guid.NewGuid():N}",
                TypeAttributes.Public | TypeAttributes.Class, typeof(object), new[] { typeof(IBindable) });

            var methodInfo = typeof(IBindable).GetMethod(nameof(IBindable.BindMemberRefs));

            var overrideEmitter = implEmitter.DefineMethodOverride(ref methodInfo);
            // DefineMethodOverride 已自动注册参数，直接获取
            var oParams = overrideEmitter.GetParameters();
            // 返回固定字符串以简化验证
            overrideEmitter.Append(Expression.Constant("bound"));

            var implType = implEmitter.CreateType();

            // Act: 通过 object 类型实例调用接口方法（修复前此处抛 AstException）
            var testEmitter = _emitter.DefineType($"Test6A_{Guid.NewGuid():N}",
                TypeAttributes.Public | TypeAttributes.Class);

            var callMethod = testEmitter.DefineMethod("CallBind",
                MethodAttributes.Public | MethodAttributes.Virtual, typeof(string));
            var objParam = callMethod.DefineParameter(typeof(object), "obj");
            var sParam = callMethod.DefineParameter(typeof(int), "slot");
            var lParam = callMethod.DefineParameter(typeof(int), "level");

            // 直接以 object 实例调用接口方法（Issue #6 的修复点）
            callMethod.Append(Expression.Call(objParam, methodInfo, sParam, lParam));

            var testType = testEmitter.CreateType();
            var testInst = Activator.CreateInstance(testType);
            var implInst = Activator.CreateInstance(implType);

            var result = testType.GetMethod("CallBind").Invoke(testInst,
                new object[] { implInst, 1, 2 });

            Assert.Equal("bound", result);
        }

        /// <summary>
        /// Issue #6 补充：通过 Expression.Convert 将 object 转为接口类型后调用。
        /// 验证 Convert + Call 组合生成的 IL 正确。
        /// </summary>
        [Fact]
        public void Call_InterfaceMethod_AfterConvert_ShouldReturnCorrectValue()
        {
            var implEmitter = _emitter.DefineType($"Impl6B_{Guid.NewGuid():N}",
                TypeAttributes.Public | TypeAttributes.Class, typeof(object), new[] { typeof(IValueProvider) });

            var methodInfo = typeof(IValueProvider).GetMethod(nameof(IValueProvider.GetValue));
            var overrideEmitter = implEmitter.DefineMethodOverride(ref methodInfo);
            overrideEmitter.Append(Expression.Constant(42));

            var implType = implEmitter.CreateType();

            var testEmitter = _emitter.DefineType($"Test6B_{Guid.NewGuid():N}",
                TypeAttributes.Public | TypeAttributes.Class);

            var callMethod = testEmitter.DefineMethod("GetViaInterface",
                MethodAttributes.Public | MethodAttributes.Virtual, typeof(int));
            var objParam = callMethod.DefineParameter(typeof(object), "obj");

            // Convert(object → IValueProvider) 然后 Call
            var asBinder = Expression.Convert(objParam, typeof(IValueProvider));
            callMethod.Append(Expression.Call(asBinder, methodInfo));

            var testType = testEmitter.CreateType();
            var testInst = Activator.CreateInstance(testType);
            var implInst = Activator.CreateInstance(implType);

            var result = testType.GetMethod("GetViaInterface").Invoke(testInst,
                new object[] { implInst });

            Assert.Equal(42, result);
        }

        /// <summary>
        /// Issue #6 边界：值类型实例调用接口方法仍应拒绝（值类型需要装箱，不适用放宽规则）。
        /// </summary>
        [Fact]
        public void Call_InterfaceMethod_WithValueTypeInstance_ShouldStillThrow()
        {
            var methodInfo = typeof(IValueProvider).GetMethod(nameof(IValueProvider.GetValue));

            Assert.Throws<AstException>(() =>
            {
                Expression.Call(Expression.Constant(0), methodInfo);
            });
        }

        #endregion

        #region Issue #2: MethodBuilder.GetParameters() 应延迟校验

        /// <summary>
        /// Issue #2: 在 TypeBuilder.CreateType() 之前，MethodBuilder.GetParameters() 会抛
        /// NotSupportedException。修复后 MethodCallExpression 检测到 MethodBuilder 时跳过参数校验。
        /// 验证：同类型内的方法互引用（IL 发射阶段）不抛异常。
        /// </summary>
        [Fact]
        public void Call_MethodBuilder_BeforeCreateType_ShouldNotThrow()
        {
            var typeEmitter = _emitter.DefineType($"MBTest_{Guid.NewGuid():N}",
                TypeAttributes.Public | TypeAttributes.Class);

            // 先 DefineMethod 拿到 MethodBuilder
            var methodToCall = typeEmitter.DefineMethod("GetNumber",
                MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            methodToCall.Append(Expression.Constant(100));

            // 在 DefineMethod 之后、CreateType 之前，MethodBuilder 的 GetParameters() 不可用
            // 但 MethodCallExpression 应能工作
            var callerMethod = typeEmitter.DefineMethod("CallGetNumber",
                MethodAttributes.Public | MethodAttributes.Static, typeof(int));

            // 通过 MethodEmitter 调用（同类型内，自动注入 this）
            callerMethod.Append(Expression.Call(methodToCall));

            var type = typeEmitter.CreateType();
            var result = type.GetMethod("CallGetNumber").Invoke(null, null);

            Assert.Equal(100, result);
        }

        #endregion

        #region Issue #4: InstanceMethodCallEmitter — 跨类型显式实例调用

        /// <summary>
        /// Issue #4: 新增 Expression.Call(Expression instance, MethodEmitter, params Expression[]) 重载。
        /// 验证：跨类型调用（在一个动态类型的实例方法中，调用另一个动态类型的实例方法）。
        /// </summary>
        [Fact]
        public void Call_ExplicitInstance_CrossTypeMethod_ShouldReturnCorrectValue()
        {
            // 类型 A：被调用的 "helper" 类型
            var typeA = _emitter.DefineType($"Helper_{Guid.NewGuid():N}",
                TypeAttributes.Public | TypeAttributes.Class);
            var fieldA = typeA.DefineField("_multiplier", typeof(int), FieldAttributes.Public);
            var ctorA = typeA.DefineConstructor(MethodAttributes.Public);
            var ctorParam = ctorA.DefineParameter(typeof(int), ParameterAttributes.None, "m");
            ctorA.Append(Expression.Assign(fieldA, ctorParam));

            var multiplyMethod = typeA.DefineMethod("Multiply",
                MethodAttributes.Public | MethodAttributes.Virtual, typeof(int));
            var mulParam = multiplyMethod.DefineParameter(typeof(int), "value");
            multiplyMethod.Append(Expression.Multiply(mulParam, fieldA));

            var helperType = typeA.CreateType();

            // 类型 B：调用者
            var typeB = _emitter.DefineType($"Caller_{Guid.NewGuid():N}",
                TypeAttributes.Public | TypeAttributes.Class);

            var callerMethod = typeB.DefineMethod("CallHelperMultiply",
                MethodAttributes.Public | MethodAttributes.Virtual, typeof(int));
            var helperParam = callerMethod.DefineParameter(typeof(object), "helper");
            var valueParam = callerMethod.DefineParameter(typeof(int), "value");

            // 跨类型调用：使用 helperParam 作为实例
            callerMethod.Append(Expression.Call(helperParam, multiplyMethod, valueParam));

            var callerType = typeB.CreateType();

            var helperInst = Activator.CreateInstance(helperType, 3);
            var callerInst = Activator.CreateInstance(callerType);

            var result = callerType.GetMethod("CallHelperMultiply")
                .Invoke(callerInst, new object[] { helperInst, 7 });

            Assert.Equal(21, result);
        }

        /// <summary>
        /// Issue #4 边界：静态 MethodEmitter 不应接受显式实例。
        /// </summary>
        [Fact]
        public void Call_StaticMethodEmitter_WithExplicitInstance_ShouldThrow()
        {
            var typeEmitter = _emitter.DefineType($"Static4B_{Guid.NewGuid():N}",
                TypeAttributes.Public | TypeAttributes.Class);
            var staticMethod = typeEmitter.DefineMethod("StaticHelper",
                MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            staticMethod.Append(Expression.Constant(0));

            Assert.Throws<AstException>(() =>
            {
                Expression.Call(Expression.Constant(new object()), staticMethod);
            });
        }

        /// <summary>
        /// Issue #4 边界：null 实例应抛 ArgumentNullException。
        /// </summary>
        [Fact]
        public void Call_ExplicitInstance_NullInstance_ShouldThrowArgumentNull()
        {
            var typeEmitter = _emitter.DefineType($"Null4C_{Guid.NewGuid():N}",
                TypeAttributes.Public | TypeAttributes.Class);
            var instanceMethod = typeEmitter.DefineMethod("SomeMethod",
                MethodAttributes.Public | MethodAttributes.Virtual, typeof(int));
            instanceMethod.Append(Expression.Constant(0));

            Assert.Throws<ArgumentNullException>(() =>
            {
                Expression.Call((Expression)null, instanceMethod);
            });
        }

        /// <summary>
        /// Issue #4：验证 InstanceMethodCallEmitter 通过 TypeAs 后的实例调用方法。
        /// </summary>
        [Fact]
        public void Call_ExplicitInstance_AfterTypeAs_ShouldWork()
        {
            // 构建实现 IValueProvider 的类型
            var implEmitter = _emitter.DefineType($"TypeAs4D_{Guid.NewGuid():N}",
                TypeAttributes.Public | TypeAttributes.Class, typeof(object), new[] { typeof(IValueProvider) });

            var methodInfo = typeof(IValueProvider).GetMethod(nameof(IValueProvider.GetValue));
            var overrideEmitter = implEmitter.DefineMethodOverride(ref methodInfo);
            overrideEmitter.Append(Expression.Constant(77));

            var implType = implEmitter.CreateType();

            // 构建调用类型
            var callerEmitter = _emitter.DefineType($"CallerTypeAs4D_{Guid.NewGuid():N}",
                TypeAttributes.Public | TypeAttributes.Class);

            var callerMethod = callerEmitter.DefineMethod("GetViaAs",
                MethodAttributes.Public | MethodAttributes.Virtual, typeof(int));
            var objParam = callerMethod.DefineParameter(typeof(object), "obj");

            // TypeAs + Call 组合
            var asExpr = Expression.TypeAs(objParam, typeof(IValueProvider));
            callerMethod.Append(Expression.Call(asExpr, methodInfo));

            var callerType = callerEmitter.CreateType();
            var implInst = Activator.CreateInstance(implType);
            var callerInst = Activator.CreateInstance(callerType);

            var result = callerType.GetMethod("GetViaAs")
                .Invoke(callerInst, new object[] { implInst });

            Assert.Equal(77, result);
        }

        #endregion

        #region Issue #5: IfThenExpression.DetectionResult

        /// <summary>
        /// Issue #5: IfThen(cond, Return(value)) + Return(null) 校验误报。
        /// 验证带有尾随 Return 的方法能正常创建。
        /// </summary>
        [Fact]
        public void IfThen_WithReturn_PlusTrailingReturn_ShouldNotThrow()
        {
            var typeEmitter = _emitter.DefineType($"IfThen5A_{Guid.NewGuid():N}",
                TypeAttributes.Public | TypeAttributes.Class);

            var method = typeEmitter.DefineMethod("GetBySlot",
                MethodAttributes.Public | MethodAttributes.Virtual, typeof(object));
            var slotParam = method.DefineParameter(typeof(int), "slot");

            // IfThen(cond, Return(value)) — 类似显式接口方法场景
            method.Append(Expression.IfThen(
                Expression.Equal(slotParam, Expression.Constant(0)),
                Expression.Return(Expression.Constant("zero"))));

            // 尾随 Return 作为兜底
            method.Append(Expression.Return(Expression.Constant(null, typeof(object))));

            // 不应抛 "并发所有代码路径都有返回值！"
            var type = typeEmitter.CreateType();
            var inst = Activator.CreateInstance(type);

            var result = type.GetMethod("GetBySlot").Invoke(inst, new object[] { 0 });
            Assert.Equal("zero", result);

            var resultNull = type.GetMethod("GetBySlot").Invoke(inst, new object[] { 99 });
            Assert.Null(resultNull);
        }

        /// <summary>
        /// Issue #5: IfThen(cond, Return(value)) 无尾随 Return 时应正常抛异常。
        /// （因为仅覆盖一条分支，else 穿透后无返回值）
        /// </summary>
        [Fact]
        public void IfThen_WithReturn_WithoutTrailingReturn_ShouldThrow()
        {
            var typeEmitter = _emitter.DefineType($"IfThen5B_{Guid.NewGuid():N}",
                TypeAttributes.Public | TypeAttributes.Class);

            var method = typeEmitter.DefineMethod("Bad",
                MethodAttributes.Public | MethodAttributes.Virtual, typeof(object));
            var slotParam = method.DefineParameter(typeof(int), "slot");

            // 仅 IfThen(cond, Return(value))，无尾随 Return
            method.Append(Expression.IfThen(
                Expression.Equal(slotParam, Expression.Constant(0)),
                Expression.Return(Expression.Constant("zero"))));

            Assert.Throws<NotSupportedException>(() => typeEmitter.CreateType());
        }

        /// <summary>
        /// Issue #5: IfThenExpression.DetectionResult 返回 false（因为 else 穿透）。
        /// 通过反射调用 protected internal 方法验证。
        /// </summary>
        [Fact]
        public void IfThenExpression_DetectionResult_ReturnsFalse()
        {
            var ifThen = Expression.IfThen(
                Expression.Equal(Expression.Constant(1), Expression.Constant(1)),
                Expression.Return(Expression.Constant(42)));

            // IfThen 仅覆盖 true 分支，else 穿透 → 不保证所有路径有返回值
            var result = InvokeDetectionResult(ifThen, typeof(int));
            Assert.False(result);
        }

        /// <summary>
        /// Issue #5: IfThenExpression.DetectionResult 对 void 返回 false。
        /// </summary>
        [Fact]
        public void IfThenExpression_DetectionResult_ForVoid_ReturnsFalse()
        {
            var ifThen = Expression.IfThen(
                Expression.Equal(Expression.Constant(1), Expression.Constant(1)),
                Expression.Return());

            var result = InvokeDetectionResult(ifThen, typeof(void));
            Assert.False(result);
        }

        /// <summary>
        /// 通过反射调用 Expression.DetectionResult（protected internal）。
        /// </summary>
        private static bool InvokeDetectionResult(Expression expression, Type returnType)
        {
            var method = typeof(Expression).GetMethod("DetectionResult",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return (bool)method.Invoke(expression, new object[] { returnType });
        }

        /// <summary>
        /// Issue #5 边界：多次 IfThen 各自带 Return + 尾随 Return 应正常工作。
        /// </summary>
        [Fact]
        public void IfThen_Multiple_WithReturn_PlusTrailingReturn_ShouldWork()
        {
            var typeEmitter = _emitter.DefineType($"IfThen5C_{Guid.NewGuid():N}",
                TypeAttributes.Public | TypeAttributes.Class);

            var method = typeEmitter.DefineMethod("MultiSlot",
                MethodAttributes.Public | MethodAttributes.Virtual, typeof(object));
            var slotParam = method.DefineParameter(typeof(int), "slot");

            method.Append(Expression.IfThen(
                Expression.Equal(slotParam, Expression.Constant(0)),
                Expression.Return(Expression.Constant("a"))));

            method.Append(Expression.IfThen(
                Expression.Equal(slotParam, Expression.Constant(1)),
                Expression.Return(Expression.Constant("b"))));

            method.Append(Expression.Return(Expression.Constant(null, typeof(object))));

            var type = typeEmitter.CreateType();
            var inst = Activator.CreateInstance(type);

            Assert.Equal("a", type.GetMethod("MultiSlot").Invoke(inst, new object[] { 0 }));
            Assert.Equal("b", type.GetMethod("MultiSlot").Invoke(inst, new object[] { 1 }));
            Assert.Null(type.GetMethod("MultiSlot").Invoke(inst, new object[] { 99 }));
        }

        #endregion

        #region 综合测试：多个修复项的组合场景

        /// <summary>
        /// 组合测试：object 实例 → Convert 为接口 → 调用接口方法 + IfThen/Return。
        /// 覆盖 Issue #1, #5, #6。
        /// </summary>
        [Fact]
        public void Combined_InterfaceCall_WithIfThenReturn_ShouldWork()
        {
            // 实现 IValueProvider 的类型
            var implEmitter = _emitter.DefineType($"Comb6A_{Guid.NewGuid():N}",
                TypeAttributes.Public | TypeAttributes.Class, typeof(object), new[] { typeof(IValueProvider) });

            var methodInfo = typeof(IValueProvider).GetMethod(nameof(IValueProvider.GetValue));
            var overrideEmitter = implEmitter.DefineMethodOverride(ref methodInfo);
            overrideEmitter.Append(Expression.Constant(123));

            var implType = implEmitter.CreateType();

            // 调用类型：object → Convert → Call，并用 IfThen 选择分支
            var callerEmitter = _emitter.DefineType($"CombCaller6A_{Guid.NewGuid():N}",
                TypeAttributes.Public | TypeAttributes.Class);

            var callerMethod = callerEmitter.DefineMethod("Combined",
                MethodAttributes.Public | MethodAttributes.Virtual, typeof(object));
            var objParam = callerMethod.DefineParameter(typeof(object), "obj");

            var asBinder = Expression.Convert(objParam, typeof(IValueProvider));
            var callResult = Expression.Call(asBinder, methodInfo);

            // IfThen: 仅包装返回值
            callerMethod.Append(Expression.IfThen(
                Expression.Equal(callResult, Expression.Constant(123)),
                Expression.Return(Expression.Constant("matched"))));

            callerMethod.Append(Expression.Return(Expression.Constant(null, typeof(object))));

            var callerType = callerEmitter.CreateType();
            var implInst = Activator.CreateInstance(implType);
            var callerInst = Activator.CreateInstance(callerType);

            var result = callerType.GetMethod("Combined")
                .Invoke(callerInst, new object[] { implInst });

            Assert.Equal("matched", result);
        }

        #endregion
    }
}
