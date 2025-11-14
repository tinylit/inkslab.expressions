using Inkslab.Emitters;
using Inkslab.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Inkslab.Intercept.Proxys
{
    using static Inkslab.Expression;

    public abstract partial class ProxyMethod
    {
        private class ProxyReturnValueAsyncMethod : ProxyMethod
        {
            private static readonly Type _interceptAttribute = typeof(ReturnValueInterceptAsyncAttribute);
            private static readonly Type _interceptsAttribute = typeof(ReturnValueInterceptAsyncAttribute[]);

            private static readonly Type _middlewareInterceptType;

            private static readonly MethodInfo _middlewareInterceptFn;
            private static readonly ConstructorInfo _middlewareInterceptCtor;

            static ProxyReturnValueAsyncMethod()
            {
                _middlewareInterceptType = typeof(MiddlewareInterceptAsync<>);

                _middlewareInterceptCtor = _middlewareInterceptType.GetConstructor(new Type[] { _invocationType, _interceptsAttribute });
                _middlewareInterceptFn = _middlewareInterceptType.GetMethod("RunAsync", new Type[] { _interceptContextType });
            }

            public ProxyReturnValueAsyncMethod(MethodInfo method, IList<CustomAttributeData> attributeDatas) : base(method, attributeDatas)
            {

            }

            public override bool IsRequired() => AttributeDatas.Any(x => x.AttributeType.IsSubclassOf(_interceptAttribute));

            protected override MethodEmitter OverrideMethod(ClassEmitter classEmitter, FieldEmitter servicesAst, Expression instanceAst, MethodInfo methodInfo)
            {
                var overrideEmitter = classEmitter.DefineMethodOverride(ref methodInfo);

                var parameterEmitters = overrideEmitter.GetParameters();

                var interceptAttributes = MethodOverrideInterceptAttributes(overrideEmitter, AttributeDatas, _interceptAttribute);

                //? 方法拦截属性。
                var interceptAttrEmitter = classEmitter.DefineField($"____intercept_attr_{methodInfo.Name}_{methodInfo.MetadataToken}", _interceptsAttribute, FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly);

                classEmitter.TypeInitializer.Append(Assign(interceptAttrEmitter, Array(_interceptAttribute, interceptAttributes.ToArray())));

                //? 方法主体。
                Expression[] arguments;

                Expression methodAst;

                var variableAst = Variable(typeof(object[]));

                overrideEmitter.Append(Assign(variableAst, Array(parameterEmitters)));

                if (methodInfo.IsGenericMethod)
                {
                    methodAst = Variable(typeof(MethodInfo));

                    overrideEmitter.Append(Assign(methodAst, Constant(methodInfo)));

                    arguments = new Expression[] { servicesAst, methodAst, variableAst };
                }
                else
                {
                    methodAst = classEmitter.DefineField($"____token__{methodInfo.Name}_{methodInfo.MetadataToken}", typeof(MethodInfo), FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly);
                    classEmitter.TypeInitializer.Append(Assign(methodAst, Constant(methodInfo)));

                    arguments = new Expression[] { servicesAst, methodAst, variableAst };
                }

                Expression contextAst = New(_interceptContextTypeCtor, arguments);

                Expression interceptAst = MakeIntercept(classEmitter, instanceAst, methodAst, parameterEmitters, interceptAttrEmitter, methodInfo);

                var contextVar = Variable(_interceptContextType);
                var interceptVar = Variable(interceptAst.RuntimeType);

                BlockExpression blockAst = Block();

                blockAst.Append(Assign(interceptVar, interceptAst))
                    .Append(Assign(contextVar, contextAst));

                var middlewareInterceptFn = TypeCompiler.GetMethod(_middlewareInterceptType.MakeGenericType(methodInfo.ReturnType.GetGenericArguments()), _middlewareInterceptFn);

                Expression bodyAst = Call(interceptVar, middlewareInterceptFn, contextVar);

                if (methodInfo.ReturnType.IsValueType)
                {
                    var valueTaskCtor = methodInfo.ReturnType.GetConstructor(new Type[] { bodyAst.RuntimeType });

                    overrideEmitter.Append(Return(New(valueTaskCtor, bodyAst)));
                }
                else
                {
                    blockAst.Append(Return(bodyAst));

                    overrideEmitter.Append(blockAst);
                }

                return overrideEmitter;
            }

            private static Expression MakeIntercept(ClassEmitter classEmitter, Expression instanceAst, Expression methodAst, ParameterEmitter[] parameterEmitters, Expression interceptAttrEmitter, MethodInfo methodInfo)
            {
                Expression invocationAst = methodInfo.DeclaringType.IsInterface
                        ? New(_implementInvocationCtor, instanceAst, methodAst)
                        : MakeInvocation(classEmitter, instanceAst, methodInfo, parameterEmitters);

                var interceptCtor = TypeCompiler.GetConstructor(_middlewareInterceptType.MakeGenericType(methodInfo.ReturnType.GetGenericArguments()), _middlewareInterceptCtor);

                return New(interceptCtor, invocationAst, interceptAttrEmitter);
            }
        }
    }
}
