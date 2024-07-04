using Inkslab.Emitters;
using Inkslab.Expressions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Inkslab.Intercept.Proxys
{
    using static Inkslab.Expression;

    /// <summary>
    /// 代理方法。
    /// </summary>
    [DebuggerDisplay("{Method.Name}")]
    public abstract partial class ProxyMethod
    {
        private static readonly Type _invocationType = typeof(IInvocation);
        private static readonly Type _interceptContextType = typeof(InterceptContext);
        private static readonly Type _implementInvocationType = typeof(ImplementInvocation);
        private static readonly Type _noninterceptAttributeType = typeof(NoninterceptAttribute);

        private static readonly MethodInfo _invocationInvoke;
        private static readonly ConstructorInfo _implementInvocationCtor;
        private static readonly ConstructorInfo _interceptContextTypeCtor;

        static ProxyMethod()
        {
            _invocationInvoke = _invocationType.GetMethod(nameof(IInvocation.Invoke));
            _implementInvocationCtor = _implementInvocationType.GetConstructor(new Type[] { typeof(object), typeof(MethodInfo) });
            _interceptContextTypeCtor = _interceptContextType.GetConstructor(new Type[] { typeof(IServiceProvider), typeof(MethodInfo), typeof(object[]) });
        }

        /// <summary>
        /// 代理方法。
        /// </summary>
        private ProxyMethod(MethodInfo method, IList<CustomAttributeData> attributeDatas)
        {
            Method = method;

            AttributeDatas = attributeDatas;
        }

        /// <summary>
        /// 方法。
        /// </summary>
        public MethodInfo Method { get; }

        /// <summary>
        /// 代理属性。
        /// </summary>
        public IList<CustomAttributeData> AttributeDatas { get; }

        /// <summary>
        /// 代理。
        /// </summary>
        /// <returns>是否必须代理。</returns>
        public abstract bool IsRequired();

        /// <summary>
        /// 重写方法。
        /// </summary>
        /// <param name="classEmitter">类。</param>
        /// <param name="servicesAst">服务。</param>
        /// <param name="instanceAst">实列。</param>
        /// <returns>重新的方法。</returns>
        public MethodEmitter OverrideMethod(ClassEmitter classEmitter, FieldEmitter servicesAst, Expression instanceAst)
        {
            return IsRequired()
                ? OverrideMethod(classEmitter, servicesAst, instanceAst, Method)
                : OverrideNoInterceptionRequiredMethod(classEmitter, instanceAst, Method);
        }

        /// <summary>
        /// 重写方法（无需拦截）。
        /// </summary>
        /// <param name="classEmitter">类。</param>
        /// <param name="instanceAst">实列。</param>
        /// <param name="methodInfo">方法。</param>
        /// <returns>重新的方法。</returns>
        private static MethodEmitter OverrideNoInterceptionRequiredMethod(ClassEmitter classEmitter, Expression instanceAst, MethodInfo methodInfo)
        {
            var overrideEmitter = classEmitter.DefineMethodOverride(ref methodInfo);

            var parameterEmitters = overrideEmitter.GetParameters();

            if (methodInfo.ReturnType == typeof(void))
            {
                overrideEmitter.Append(Call(instanceAst, methodInfo, parameterEmitters));
            }
            else
            {
                overrideEmitter.Append(Return(Call(instanceAst, methodInfo, parameterEmitters)));
            }

            return overrideEmitter;
        }

        private static List<Expression> MethodOverrideInterceptAttributes(MethodEmitter overrideEmitter, IEnumerable<CustomAttributeData> attributeDatas, Type interceptAttribute)
        {
            var interceptAttributes = new List<Expression>();

            foreach (var attributeData in attributeDatas)
            {
                if (attributeData.AttributeType.IsSubclassOf(interceptAttribute))
                {
                    var attrArguments = new Expression[attributeData.ConstructorArguments.Count];

                    for (int i = 0, len = attributeData.ConstructorArguments.Count; i < len; i++)
                    {
                        var typedArgument = attributeData.ConstructorArguments[i];

                        attrArguments[i] = Constant(typedArgument.Value, typedArgument.ArgumentType);
                    }

                    var instance = New(attributeData.Constructor, attrArguments);

                    if (attributeData.NamedArguments.Count > 0)
                    {
                        var bindings = new List<MemberAssignment>(attributeData.NamedArguments.Count);

                        foreach (var arg in attributeData.NamedArguments)
                        {
                            bindings.Add(Bind(arg.MemberInfo, Constant(arg.TypedValue.Value, arg.TypedValue.ArgumentType)));
                        }

                        interceptAttributes.Add(MemberInit(instance, bindings));
                    }
                    else
                    {
                        interceptAttributes.Add(instance);
                    }
                }
                else
                {
                    overrideEmitter.SetCustomAttribute(attributeData);
                }
            }

            return interceptAttributes;
        }

        private static Expression MakeInvocation(ClassEmitter classEmitter, Expression instanceAst, MethodInfo methodInfo, ParameterEmitter[] parameterEmitters)
        {
            if (methodInfo.IsGenericMethod)
            {
                return MakeInvocationByGeneric(classEmitter, instanceAst, methodInfo, parameterEmitters);
            }

            var typeEmitter = classEmitter.DefineNestedType($"{classEmitter.Name}_{methodInfo.Name}", TypeAttributes.Public, typeof(object), new Type[] { _invocationType });

            MethodEmitter methodEmitter = typeEmitter.DefineMethod(methodInfo.Name, MethodAttributes.Public | MethodAttributes.Virtual, methodInfo.ReturnType);

            var invocationAst = typeEmitter.DefineField("invocation", methodInfo.DeclaringType, FieldAttributes.Private | FieldAttributes.InitOnly | FieldAttributes.NotSerialized);

            var constructorEmitter = typeEmitter.DefineConstructor(MethodAttributes.Public);

            constructorEmitter.Append(Assign(invocationAst, constructorEmitter.DefineParameter(methodInfo.DeclaringType, ParameterAttributes.None, "invocation")));

            var parameters = System.Array.ConvertAll(parameterEmitters, x =>
            {
                return methodEmitter.DefineParameter(x.ParameterType, x.Attributes, x.ParameterName);
            });

            if (methodInfo.ReturnType == typeof(void))
            {
                methodEmitter.Append(DeclaringCall(invocationAst, methodInfo, parameters));
            }
            else
            {
                methodEmitter.Append(Return(DeclaringCall(invocationAst, methodInfo, parameters)));
            }

            var invocationInvoke = _invocationInvoke;

            var invokeEmitter = typeEmitter.DefineMethodOverride(ref invocationInvoke);

            invokeEmitter.Append(Return(Invoke(This(typeEmitter), methodEmitter, invokeEmitter.GetParameters().Single())));

            return New(constructorEmitter, instanceAst);
        }

        private static Expression MakeInvocationByGeneric(ClassEmitter classEmitter, Expression instanceAst, MethodInfo methodInfo, ParameterEmitter[] parameterEmitters)
        {
            var typeEmitter = classEmitter.DefineNestedType($"{classEmitter.Name}_{methodInfo.Name}_{methodInfo.MetadataToken}", TypeAttributes.Public, typeof(object), new Type[] { _invocationType });

            var genericArguments = methodInfo.GetGenericArguments();

            var typeArguments = typeEmitter.DefineGenericParameters(genericArguments);

            MethodEmitter methodEmitter = typeEmitter.DefineMethod(methodInfo.Name, MethodAttributes.Public | MethodAttributes.Virtual, TypeCompiler.GetReturnType(methodInfo, typeArguments, typeEmitter.GetGenericArguments()));

            var invocationAst = typeEmitter.DefineField("invocation", methodInfo.DeclaringType, FieldAttributes.Private | FieldAttributes.InitOnly | FieldAttributes.NotSerialized);

            var constructorEmitter = typeEmitter.DefineConstructor(MethodAttributes.Public);

            constructorEmitter.Append(Assign(invocationAst, constructorEmitter.DefineParameter(methodInfo.DeclaringType, ParameterAttributes.None, "invocation")));

            var parameters = System.Array.ConvertAll(parameterEmitters, x =>
            {
                return methodEmitter.DefineParameter(x.ParameterType, x.Attributes, x.ParameterName);
            });

            if (methodInfo.ReturnType == typeof(void))
            {
                methodEmitter.Append(DeclaringCall(invocationAst, methodInfo.MakeGenericMethod(typeArguments), parameters));
            }
            else
            {
                methodEmitter.Append(Return(DeclaringCall(invocationAst, methodInfo.MakeGenericMethod(typeArguments), parameters)));
            }

            var invocationInvoke = _invocationInvoke;

            var invokeEmitter = typeEmitter.DefineMethodOverride(ref invocationInvoke);

            invokeEmitter.Append(Return(Invoke(This(typeEmitter), methodEmitter, invokeEmitter.GetParameters().Single())));

            return New(constructorEmitter.MakeGenericConstructor(genericArguments), instanceAst);
        }

        /// <summary>
        /// 重写方法。
        /// </summary>
        /// <param name="classEmitter">类。</param>
        /// <param name="servicesAst">服务。</param>
        /// <param name="instanceAst">实列。</param>
        /// <param name="methodInfo">方法。</param>
        /// <returns>重新的方法。</returns>
        protected abstract MethodEmitter OverrideMethod(ClassEmitter classEmitter, FieldEmitter servicesAst, Expression instanceAst, MethodInfo methodInfo);

        /// <summary>
        /// 创建代理。
        /// </summary>
        /// <param name="methodInfo">方法。</param>
        /// <param name="attributeDatas">属性。</param>
        /// <returns>代理实列。</returns>
        /// <exception cref="ArgumentNullException">参数为空。</exception>
        public static ProxyMethod Create(MethodInfo methodInfo, IList<CustomAttributeData> attributeDatas)
        {
            if (methodInfo is null)
            {
                throw new ArgumentNullException(nameof(methodInfo));
            }

            if (attributeDatas is null)
            {
                throw new ArgumentNullException(nameof(attributeDatas));
            }

            if (methodInfo.IsDefined(_noninterceptAttributeType, true))
            {
                return new ProxyNoninterceptMethod(methodInfo, attributeDatas);
            }

            var type = methodInfo.ReturnType;

            if (type == typeof(void))
            {
                return new ProxyVoidMethod(methodInfo, attributeDatas);
            }

            if (type.IsValueType
                ? (type.IsGenericType ? type.GetGenericTypeDefinition() == typeof(ValueTask<>) : type == typeof(ValueTask))
                : (type.IsGenericType ? type.GetGenericTypeDefinition() == typeof(Task<>) : type == typeof(Task)))
            {
                return type.IsGenericType
                    ? new ProxyReturnValueAsyncMethod(methodInfo, attributeDatas)
                    : new ProxyAsyncMethod(methodInfo, attributeDatas);
            }

            return new ProxyReturnValueMethod(methodInfo, attributeDatas);
        }
    }
}
