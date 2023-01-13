using Delta.Emitters;
using Delta.Expressions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace Delta.AOP.Patterns
{
    using static Delta.Expression;

    abstract class ProxyByServiceType : IProxyByPattern
    {
        private static readonly Type interceptAttributeType = typeof(InterceptAttribute);
        private static readonly Type noninterceptAttributeType = typeof(NoninterceptAttribute);
        private static readonly Type interceptAttributeArrayType = typeof(InterceptAttribute[]);
        private static readonly Type interceptContextType = typeof(InterceptContext);
        private static readonly Type implementInvocationType = typeof(ImplementInvocation);
        private static readonly Type invocationType = typeof(IInvocation);

        private static readonly MethodInfo invocationInvokeFn = invocationType.GetMethod(nameof(IInvocation.Invoke));

        private static readonly ConstructorInfo interceptContextTypeCtor = interceptContextType.GetConstructor(new Type[] { typeof(MethodInfo), typeof(object[]) });
        private static readonly ConstructorInfo implementInvocationCtor = implementInvocationType.GetConstructor(new Type[] { typeof(object), typeof(MethodInfo) });

        private static readonly Type middlewareInterceptGenericType;
        private static readonly Type middlewareInterceptGenericAsyncType;

        private static readonly ConstructorInfo middlewareInterceptCtor;
        private static readonly ConstructorInfo middlewareInterceptAsyncCtor;

        private static readonly MethodInfo middlewareInterceptRunFn;
        private static readonly MethodInfo middlewareInterceptAsyncRunFn;

        private static readonly ConstructorInfo middlewareInterceptGenericCtor;
        private static readonly ConstructorInfo middlewareInterceptAsyncGenericCtor;

        private static readonly MethodInfo middlewareInterceptGenericRunFn;
        private static readonly MethodInfo middlewareInterceptAsyncGenericRunFn;

        static ProxyByServiceType()
        {
            var interceptContextTypeArg = new Type[] { interceptContextType };
            var interceptAttributeArrayArg = new Type[] { invocationType, interceptAttributeArrayType };

            var middlewareInterceptType = typeof(MiddlewareIntercept);
            var middlewareInterceptAsyncType = typeof(MiddlewareInterceptAsync);

            middlewareInterceptCtor = middlewareInterceptType.GetConstructor(interceptAttributeArrayArg);
            middlewareInterceptRunFn = middlewareInterceptType.GetMethod("Run", interceptContextTypeArg);


            middlewareInterceptAsyncCtor = middlewareInterceptAsyncType.GetConstructor(interceptAttributeArrayArg);
            middlewareInterceptAsyncRunFn = middlewareInterceptAsyncType.GetMethod("RunAsync", interceptContextTypeArg);

            middlewareInterceptGenericType = typeof(MiddlewareIntercept<>);
            middlewareInterceptGenericAsyncType = typeof(MiddlewareInterceptAsync<>);

            middlewareInterceptGenericCtor = middlewareInterceptGenericType.GetConstructor(interceptAttributeArrayArg);
            middlewareInterceptGenericRunFn = middlewareInterceptGenericType.GetMethod("Run", interceptContextTypeArg);

            middlewareInterceptAsyncGenericCtor = middlewareInterceptGenericAsyncType.GetConstructor(interceptAttributeArrayArg);
            middlewareInterceptAsyncGenericRunFn = middlewareInterceptGenericAsyncType.GetMethod("RunAsync", interceptContextTypeArg);
        }

        public abstract ServiceDescriptor Ref();

        protected static bool Intercept(MethodInfo methodInfo)
        {
            if (methodInfo is null)
            {
                throw new ArgumentNullException(nameof(methodInfo));
            }

            if (methodInfo.DeclaringType.IsInterface)
            {
                return true;
            }

            if (!methodInfo.IsVirtual)
            {
                return false;
            }

            if (methodInfo.IsDefined(noninterceptAttributeType, true))
            {
                return false;
            }

            return methodInfo.IsDefined(interceptAttributeType, true);
        }

        public static bool Intercept(ServiceDescriptor descriptor)
        {
            return Intercept(descriptor.ServiceType) || Intercept(descriptor.ImplementationType);
        }

        private static bool Intercept(Type serviceType)
        {
            if (serviceType is null)
            {
                return false;
            }

            if (serviceType.IsDefined(interceptAttributeType, true))
            {
                return true;
            }

            foreach (var methodInfo in serviceType.GetMethods())
            {
                if (methodInfo.IsDefined(interceptAttributeType, true))
                {
                    return true;
                }
            }

            return false;
        }

        public static IEnumerable<CustomAttributeData> GetCustomAttributeDatas(MethodInfo methodInfo, Type serviceType, Type implementationType)
        {
            if (methodInfo is null)
            {
                throw new ArgumentNullException(nameof(methodInfo));
            }

            if (implementationType is null)
            {
                throw new ArgumentNullException(nameof(implementationType));
            }

            return GetCustomAttributeDatasByMulti(methodInfo, serviceType, implementationType);
        }

        private static MethodInfo GetMethod(MethodInfo referenceInfo, Type implementationType)
        {
            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.DeclaredOnly;

            if (referenceInfo.IsPublic)
            {
                bindingFlags |= BindingFlags.Public;
            }
            else
            {
                bindingFlags |= BindingFlags.NonPublic;
            }

            var parameterInfos = referenceInfo.GetParameters();

            foreach (var methodInfo in implementationType.GetMethods(bindingFlags))
            {
                if (methodInfo.Name != referenceInfo.Name)
                {
                    continue;
                }

                if (methodInfo.IsGenericMethod ^ referenceInfo.IsGenericMethod)
                {
                    continue;
                }

                var parameters = methodInfo.GetParameters();

                if (parameters.Length != parameterInfos.Length)
                {
                    continue;
                }

                for (int i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].ParameterType == parameterInfos[i].ParameterType)
                    {
                        continue;
                    }

                    goto label_continue;
                }

                return methodInfo;
label_continue:
                continue;
            }

            throw new MissingMethodException(implementationType.Name, referenceInfo.Name);
        }

        private static IEnumerable<CustomAttributeData> GetCustomAttributeDatasByMulti(MethodInfo referenceInfo, Type serviceType, Type implementationType)
        {
            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.DeclaredOnly;

            if (referenceInfo.IsPublic)
            {
                bindingFlags |= BindingFlags.Public;
            }
            else
            {
                bindingFlags |= BindingFlags.NonPublic;
            }

            var interfaceTypes = new HashSet<Type>();

            var parameterInfos = referenceInfo.GetParameters();

            do
            {
                if (implementationType.IsDefined(noninterceptAttributeType, false))
                {
                    yield break;
                }

                foreach (var customAttributeData in GetCustomAttributeDatasByMulti(referenceInfo, serviceType, implementationType, bindingFlags, parameterInfos))
                {
                    yield return customAttributeData;
                }

                foreach (var interfaceType in implementationType.GetInterfaces())
                {
                    if (!interfaceTypes.Add(interfaceType))
                    {
                        continue;
                    }

                    if (interfaceType.IsDefined(noninterceptAttributeType, false))
                    {
                        System.Array.ForEach(interfaceType.GetInterfaces(), x => interfaceTypes.Add(x));

                        continue;
                    }

                    foreach (var customAttributeData in GetCustomAttributeDatasByMulti(referenceInfo, serviceType, interfaceType, bindingFlags, parameterInfos))
                    {
                        yield return customAttributeData;
                    }
                }

                implementationType = implementationType.BaseType;

                if (implementationType is null || implementationType == serviceType)
                {
                    yield break;
                }

            } while (implementationType != typeof(object));
        }

        private static IEnumerable<CustomAttributeData> GetCustomAttributeDatasByMulti(MethodInfo referenceInfo, Type serviceType, Type implementationType, BindingFlags bindingFlags, ParameterInfo[] parameterInfos)
        {
            foreach (var methodInfo in implementationType.GetMethods(bindingFlags))
            {
                if (methodInfo.Name != referenceInfo.Name)
                {
                    continue;
                }

                if (methodInfo.IsGenericMethod ^ referenceInfo.IsGenericMethod)
                {
                    continue;
                }

                var parameters = methodInfo.GetParameters();

                if (parameters.Length != parameterInfos.Length)
                {
                    continue;
                }

                for (int i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].ParameterType == parameterInfos[i].ParameterType)
                    {
                        continue;
                    }

                    goto label_continue;
                }

                foreach (var customAttributeData in GetCustomAttributeDatas(methodInfo, serviceType))
                {
                    yield return customAttributeData;
                }
label_continue:
                continue;
            }

        }

        private static IEnumerable<CustomAttributeData> GetCustomAttributeDatas(MethodInfo methodInfo, Type serviceType)
        {
            if (methodInfo.IsDefined(noninterceptAttributeType, false))
            {
                yield break;
            }

            bool serviceFlag = methodInfo.DeclaringType == serviceType;

            foreach (var customAttributeData in methodInfo.GetCustomAttributesData())
            {
                if (serviceFlag || interceptAttributeType.IsAssignableFrom(customAttributeData.AttributeType))
                {
                    yield return customAttributeData;
                }
            }

            foreach (var customAttributeData in methodInfo.DeclaringType.GetCustomAttributesData())
            {
                if (serviceFlag || interceptAttributeType.IsAssignableFrom(customAttributeData.AttributeType))
                {
                    yield return customAttributeData;
                }
            }
        }

        private static Type[] GetIndexParameterTypes(PropertyInfo propertyInfo)
        {
            var parameterInfos = propertyInfo.GetIndexParameters();

            var parameterTypes = new Type[parameterInfos.Length];

            for (int i = 0; i < parameterInfos.Length; i++)
            {
                parameterTypes[i] = parameterInfos[i].ParameterType;
            }

            return parameterTypes;
        }

        public static Type OverrideType(ClassEmitter classEmitter, Type serviceType, Type implementationType) => OverrideType(This(classEmitter), classEmitter, serviceType, implementationType);

        public static Type OverrideType(Expression instanceAst, ClassEmitter classEmitter, Type serviceType, Type implementationType)
        {
            var propertyMethods = new HashSet<MethodInfo>();

            foreach (var propertyInfo in serviceType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (propertyInfo.DeclaringType == typeof(object))
                {
                    continue;
                }

                PropertyEmitter propertyEmitter = null;

                if (propertyInfo.CanRead)
                {
                    var getter = propertyInfo.GetMethod ?? propertyInfo.GetGetMethod(true);

                    propertyMethods.Add(getter);

                    if (serviceType.IsAbstract || Intercept(getter))
                    {
                        propertyEmitter ??= classEmitter.DefineProperty(propertyInfo.Name, propertyInfo.Attributes, propertyInfo.PropertyType, GetIndexParameterTypes(propertyInfo));

                        propertyEmitter.SetGetMethod(DefineMethodOverride(classEmitter, instanceAst, getter, GetCustomAttributeDatas(getter, serviceType, implementationType)));
                    }
                }

                if (propertyInfo.CanWrite)
                {
                    var setter = propertyInfo.SetMethod ?? propertyInfo.GetSetMethod(true);

                    propertyMethods.Add(setter);

                    if (serviceType.IsAbstract || Intercept(setter))
                    {
                        propertyEmitter ??= classEmitter.DefineProperty(propertyInfo.Name, propertyInfo.Attributes, propertyInfo.PropertyType, GetIndexParameterTypes(propertyInfo));

                        propertyEmitter.SetSetMethod(DefineMethodOverride(classEmitter, instanceAst, setter, GetCustomAttributeDatas(setter, serviceType, implementationType)));
                    }
                }
            }

            foreach (var methodInfo in serviceType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (propertyMethods.Contains(methodInfo))
                {
                    continue;
                }

                if (methodInfo.DeclaringType == typeof(object))
                {
                    continue;
                }

                if (serviceType.IsAbstract || Intercept(methodInfo))
                {
                    DefineMethodOverride(classEmitter, instanceAst, methodInfo, GetCustomAttributeDatas(methodInfo, serviceType, implementationType));
                }
            }

            propertyMethods.Clear();

            return classEmitter.CreateType();
        }

        private static List<Expression> MethodOverrideInterceptAttributes(MethodEmitter overrideEmitter, IEnumerable<CustomAttributeData> attributeDatas)
        {
            var interceptAttributes = new List<Expression>();

            foreach (var attributeData in attributeDatas)
            {
                if (interceptAttributeType.IsAssignableFrom(attributeData.AttributeType))
                {
                    var attrArguments = new Expression[attributeData.ConstructorArguments.Count];

                    for (int i = 0, len = attributeData.ConstructorArguments.Count; i < len; i++)
                    {
                        var typedArgument = attributeData.ConstructorArguments[i];

                        attrArguments[i] = Constant(typedArgument.Value, typedArgument.ArgumentType);
                    }

                    interceptAttributes.Add(New(attributeData.Constructor, attrArguments));
                }
                else
                {
                    overrideEmitter.SetCustomAttribute(attributeData);
                }
            }

            return interceptAttributes;
        }

        private static MethodEmitter DefineMethodOverride(ClassEmitter classEmitter, Expression instanceAst, MethodInfo methodInfo, IEnumerable<CustomAttributeData> attributeDatas)
        {
            var overrideEmitter = classEmitter.DefineMethodOverride(ref methodInfo);

            var parameterEmitters = overrideEmitter.GetParameters();

            var interceptAttributes = MethodOverrideInterceptAttributes(overrideEmitter, attributeDatas);

            //? 方法拦截属性。
            var interceptAttrEmitter = classEmitter.DefineField($"____intercept_attr_{methodInfo.Name}", interceptAttributeArrayType, FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly);

            classEmitter.TypeInitializer.Append(Assign(interceptAttrEmitter, Array(interceptAttributeType, interceptAttributes.ToArray())));

            //? 方法主体。
            Expression[] arguments;

            Expression methodAst;

            var variableAst = Variable(typeof(object[]));

            overrideEmitter.Append(Assign(variableAst, Array(parameterEmitters)));

            if (overrideEmitter.IsGenericMethod)
            {
                methodAst = Constant(methodInfo);

                arguments = new Expression[] { methodAst, variableAst };
            }
            else
            {
                methodAst = classEmitter.DefineField($"____token__{methodInfo.Name}", typeof(MethodInfo), FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly);

                classEmitter.TypeInitializer.Append(Assign(methodAst, Constant(methodInfo)));

                arguments = new Expression[] { methodAst, variableAst };
            }

            BlockExpression blockAst;

            if (parameterEmitters.Any(x => x.RuntimeType.IsByRef))
            {
                var finallyAst = Block();

                for (int i = 0; i < parameterEmitters.Length; i++)
                {
                    var paramterEmitter = parameterEmitters[i];

                    if (!paramterEmitter.IsByRef)
                    {
                        continue;
                    }

                    finallyAst.Append(Assign(paramterEmitter, Convert(ArrayIndex(variableAst, i), paramterEmitter.ParameterType)));
                }

                blockAst = Try(finallyAst);
            }
            else
            {
                blockAst = Block();
            }

            Expression contextAst = New(interceptContextTypeCtor, arguments);

            Expression interceptAst = MakeIntercept(classEmitter, instanceAst, methodAst, parameterEmitters, interceptAttrEmitter, methodInfo);

            var contextVar = Variable(interceptContextType);
            var interceptVar = Variable(interceptAst.RuntimeType);

            blockAst.Append(Assign(interceptVar, interceptAst))
                .Append(Assign(contextVar, contextAst));

            var returnType = methodInfo.ReturnType;

            if (returnType.IsValueType
                ? (returnType.IsGenericType ? returnType.GetGenericTypeDefinition() == typeof(ValueTask<>) : returnType == typeof(ValueTask))
                : (returnType.IsGenericType ? returnType.GetGenericTypeDefinition() == typeof(Task<>) : returnType == typeof(Task)))
            {
                Expression bodyAst;

                if (returnType.IsGenericType)
                {
                    var interceptRunFn = TypeCompiler.GetMethod(middlewareInterceptGenericAsyncType.MakeGenericType(returnType.GetGenericArguments()), middlewareInterceptAsyncGenericRunFn);

                    bodyAst = Call(interceptVar, interceptRunFn, contextVar);
                }
                else
                {
                    bodyAst = Call(interceptVar, middlewareInterceptAsyncRunFn, contextVar);
                }

                if (returnType.IsValueType)
                {
                    var valueTaskCtor = returnType.GetConstructor(new Type[] { bodyAst.RuntimeType });

                    bodyAst = New(valueTaskCtor, bodyAst);
                }

                blockAst.Append(Return(bodyAst));
            }
            else if (returnType == typeof(void))
            {
                blockAst.Append(Call(interceptVar, middlewareInterceptRunFn, contextVar));
            }
            else
            {
                var interceptRunFn = TypeCompiler.GetMethod(middlewareInterceptGenericType.MakeGenericType(returnType), middlewareInterceptGenericRunFn);

                blockAst.Append(Return(Call(interceptVar, interceptRunFn, contextVar)));
            }

            overrideEmitter.Append(blockAst);

            return overrideEmitter;
        }

        private static Expression MakeInvocation(ClassEmitter classEmitter, Expression instanceAst, MethodInfo methodInfo, ParameterEmitter[] parameterEmitters)
        {
            if (methodInfo.IsGenericMethod)
            {
                return MakeInvocationByGeneric(classEmitter, instanceAst, methodInfo, parameterEmitters);
            }

            var typeEmitter = classEmitter.DefineNestedType($"{classEmitter.Name}_{methodInfo.Name}", TypeAttributes.Public, typeof(object), new Type[] { invocationType });

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

            var invocationInvoke = invocationInvokeFn;

            var invokeEmitter = typeEmitter.DefineMethodOverride(ref invocationInvoke);

            invokeEmitter.Append(Return(Invoke(This(typeEmitter), methodEmitter, invokeEmitter.GetParameters().Single())));

            return New(constructorEmitter, instanceAst);
        }

        private static Expression MakeInvocationByGeneric(ClassEmitter classEmitter, Expression instanceAst, MethodInfo methodInfo, ParameterEmitter[] parameterEmitters)
        {
            var typeEmitter = classEmitter.DefineNestedType($"{classEmitter.Name}_{methodInfo.Name}", TypeAttributes.Public, typeof(object), new Type[] { invocationType });

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

            var invocationInvoke = invocationInvokeFn;

            var invokeEmitter = typeEmitter.DefineMethodOverride(ref invocationInvoke);

            invokeEmitter.Append(Return(Invoke(This(typeEmitter), methodEmitter, invokeEmitter.GetParameters().Single())));

            return New(constructorEmitter.MakeGenericConstructor(genericArguments), instanceAst);
        }

        private static Expression MakeIntercept(ClassEmitter classEmitter, Expression instanceAst, Expression methodAst, ParameterEmitter[] parameterEmitters, Expression interceptAttrEmitter, MethodInfo methodInfo)
        {
            Expression invocationAst = methodInfo.DeclaringType.IsInterface
                    ? New(implementInvocationCtor, instanceAst, methodAst)
                    : MakeInvocation(classEmitter, instanceAst, methodInfo, parameterEmitters);

            var returnType = methodInfo.ReturnType;

            if (returnType.IsValueType
                ? (returnType.IsGenericType ? returnType.GetGenericTypeDefinition() == typeof(ValueTask<>) : returnType == typeof(ValueTask))
                : (returnType.IsGenericType ? returnType.GetGenericTypeDefinition() == typeof(Task<>) : returnType == typeof(Task)))
            {
                if (returnType.IsGenericType)
                {
                    var interceptCtor = TypeCompiler.GetConstructor(middlewareInterceptGenericAsyncType.MakeGenericType(returnType.GetGenericArguments()), middlewareInterceptAsyncGenericCtor);

                    return New(interceptCtor, invocationAst, interceptAttrEmitter);
                }
                else
                {
                    return New(middlewareInterceptAsyncCtor, invocationAst, interceptAttrEmitter);
                }
            }
            else if (returnType == typeof(void))
            {
                return New(middlewareInterceptCtor, invocationAst, interceptAttrEmitter);
            }
            else
            {
                var interceptCtor = TypeCompiler.GetConstructor(middlewareInterceptGenericType.MakeGenericType(returnType), middlewareInterceptGenericCtor);

                return New(interceptCtor, invocationAst, interceptAttrEmitter);
            }
        }
    }
}
