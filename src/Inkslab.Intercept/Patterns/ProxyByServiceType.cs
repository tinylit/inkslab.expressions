using Inkslab.Emitters;
using Inkslab.Expressions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Inkslab.Intercept.Patterns
{
    using static Inkslab.Expression;

    abstract class ProxyByServiceType : IProxyByPattern
    {
        private enum InterceptWay
        {
            Void,
            ReturnValue,
            Async,
            ReturnValueAsync
        }

        private static readonly Type interceptAttributeType = typeof(InterceptAttribute);
        private static readonly Type interceptAsyncAttributeType = typeof(InterceptAsyncAttribute);
        private static readonly Type returnValueInterceptAttributeType = typeof(ReturnValueInterceptAttribute);
        private static readonly Type returnValueInterceptAsyncAttributeType = typeof(ReturnValueInterceptAsyncAttribute);

        private static readonly Type noninterceptAttributeType = typeof(NoninterceptAttribute);

        private static readonly Type interceptAttributeArrayType = typeof(InterceptAttribute[]);
        private static readonly Type interceptAsyncAttributeArrayType = typeof(InterceptAsyncAttribute[]);
        private static readonly Type returnValueInterceptAttributeArrayType = typeof(ReturnValueInterceptAttribute[]);
        private static readonly Type returnValueInterceptAsyncAttributeArrayType = typeof(ReturnValueInterceptAsyncAttribute[]);

        private static readonly Type interceptContextType = typeof(InterceptContext);
        private static readonly Type implementInvocationType = typeof(ImplementInvocation);
        private static readonly Type invocationType = typeof(IInvocation);

        private static readonly MethodInfo invocationInvokeFn = invocationType.GetMethod(nameof(IInvocation.Invoke));

        private static readonly ConstructorInfo interceptContextTypeCtor = interceptContextType.GetConstructor(new Type[] { typeof(IServiceProvider), typeof(MethodInfo), typeof(object[]) });
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
            var interceptAsyncAttributeArrayArg = new Type[] { invocationType, interceptAsyncAttributeArrayType };
            var returnValueInterceptAttributeArrayArg = new Type[] { invocationType, returnValueInterceptAttributeArrayType };
            var returnValueInterceptAsyncAttributeArrayArg = new Type[] { invocationType, returnValueInterceptAsyncAttributeArrayType };

            var middlewareInterceptType = typeof(MiddlewareIntercept);
            var middlewareInterceptAsyncType = typeof(MiddlewareInterceptAsync);

            middlewareInterceptCtor = middlewareInterceptType.GetConstructor(interceptAttributeArrayArg);
            middlewareInterceptRunFn = middlewareInterceptType.GetMethod("Run", interceptContextTypeArg);


            middlewareInterceptAsyncCtor = middlewareInterceptAsyncType.GetConstructor(interceptAsyncAttributeArrayArg);
            middlewareInterceptAsyncRunFn = middlewareInterceptAsyncType.GetMethod("RunAsync", interceptContextTypeArg);

            middlewareInterceptGenericType = typeof(MiddlewareIntercept<>);
            middlewareInterceptGenericAsyncType = typeof(MiddlewareInterceptAsync<>);

            middlewareInterceptGenericCtor = middlewareInterceptGenericType.GetConstructor(returnValueInterceptAttributeArrayArg);
            middlewareInterceptGenericRunFn = middlewareInterceptGenericType.GetMethod("Run", interceptContextTypeArg);

            middlewareInterceptAsyncGenericCtor = middlewareInterceptGenericAsyncType.GetConstructor(returnValueInterceptAsyncAttributeArrayArg);
            middlewareInterceptAsyncGenericRunFn = middlewareInterceptGenericAsyncType.GetMethod("RunAsync", interceptContextTypeArg);
        }

        public abstract ServiceDescriptor Ref();

        private static InterceptWay GetInterceptWay(Type type)
        {
            if (type == typeof(void))
            {
                return InterceptWay.Void;
            }

            if (type.IsValueType && type.IsGenericType)
            {
                var typeDefinition = type.GetGenericTypeDefinition();

                if (typeDefinition == typeof(ValueTask<>))
                {
                    return InterceptWay.ReturnValueAsync;
                }

                return InterceptWay.ReturnValue;
            }

            do
            {
                if (type.IsGenericType)
                {
                    if (type.GetGenericTypeDefinition() == typeof(Task<>))
                    {
                        return InterceptWay.ReturnValueAsync;
                    }
                }

                if (type == typeof(Task))
                {
                    return InterceptWay.Async;
                }

                type = type.BaseType;

            } while (type != null);

            return InterceptWay.ReturnValue;
        }

        private static Type GetInterceptAttributeType(InterceptWay interceptWay)
        {
            switch (interceptWay)
            {
                case InterceptWay.Void:
                    return interceptAttributeType;
                case InterceptWay.ReturnValue:
                    return returnValueInterceptAttributeType;
                case InterceptWay.Async:
                    return interceptAsyncAttributeType;
                case InterceptWay.ReturnValueAsync:
                    return returnValueInterceptAsyncAttributeType;
                default:
                    throw new NotImplementedException();
            }
        }

        private static Type GetInterceptAttributeArrayType(InterceptWay interceptWay)
        {
            switch (interceptWay)
            {
                case InterceptWay.Void:
                    return interceptAttributeArrayType;
                case InterceptWay.ReturnValue:
                    return returnValueInterceptAttributeArrayType;
                case InterceptWay.Async:
                    return interceptAsyncAttributeArrayType;
                case InterceptWay.ReturnValueAsync:
                    return returnValueInterceptAsyncAttributeArrayType;
                default:
                    throw new NotImplementedException();
            }
        }

        protected static bool Intercept(MethodInfo methodInfo)
        {
            if (methodInfo is null)
            {
                throw new ArgumentNullException(nameof(methodInfo));
            }

            if (methodInfo.IsDefined(noninterceptAttributeType, true))
            {
                return false;
            }

            Type interceptAttributeType = GetInterceptAttributeType(GetInterceptWay(methodInfo.ReturnType));

            return methodInfo.IsDefined(interceptAttributeType, true);
        }

        private static bool IsVirtual(MethodInfo methodInfo) => methodInfo.IsVirtual || methodInfo.DeclaringType.IsAbstract || methodInfo.DeclaringType.IsInterface;

        public static bool Intercept(ServiceDescriptor descriptor) => Intercept(descriptor.ServiceType);

        private static bool Intercept(Type serviceType)
        {
            if (serviceType is null)
            {
                return false;
            }

            if (serviceType.IsDefined(noninterceptAttributeType, true))
            {
                return false;
            }

            var hashSet = new HashSet<MethodInfo>();

            // 排除属性标记了忽略拦截器的方法。
            foreach (var propertyInfo in serviceType.GetProperties())
            {
                if (propertyInfo.IsDefined(noninterceptAttributeType, true))
                {
                    hashSet.Add(propertyInfo.GetMethod ?? propertyInfo.GetGetMethod(true));
                    hashSet.Add(propertyInfo.SetMethod ?? propertyInfo.GetSetMethod(true));
                }
            }

            foreach (var methodInfo in serviceType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty | BindingFlags.GetProperty))
            {
                if (hashSet.Contains(methodInfo))
                {
                    continue;
                }

                if (Intercept(methodInfo))
                {
                    return true;
                }
            }

            return false;
        }

        public static List<CustomAttributeData> GetCustomAttributeDatas(MethodInfo methodInfo, Type serviceType)
        {
            if (methodInfo is null)
            {
                throw new ArgumentNullException(nameof(methodInfo));
            }

            if (serviceType is null)
            {
                throw new ArgumentNullException(nameof(serviceType));
            }

            BindingFlags bindingFlags = BindingFlags.GetProperty | BindingFlags.SetProperty;

            if (methodInfo.IsStatic)
            {
                bindingFlags |= BindingFlags.Static;
            }
            else
            {
                bindingFlags |= BindingFlags.Instance;
            }

            if (methodInfo.IsPublic)
            {
                bindingFlags |= BindingFlags.Public;
            }
            else
            {
                bindingFlags |= BindingFlags.NonPublic;
            }

            var types = System.Array.ConvertAll(methodInfo.GetParameters(), x => x.ParameterType);

            var attributeType = GetInterceptAttributeType(GetInterceptWay(methodInfo.ReturnType));

            var attributeDatas = new List<CustomAttributeData>(methodInfo.GetCustomAttributesData());

            do
            {
                var baseDefinition = methodInfo.GetBaseDefinition();

                if (baseDefinition == methodInfo)
                {
                    break;
                }

                methodInfo = baseDefinition;

                foreach (var attributeData in methodInfo.GetCustomAttributesData())
                {
                    if (attributeType.IsAssignableFrom(attributeData.AttributeType))
                    {
                        attributeDatas.Add(attributeData);
                    }
                }

            } while (true);

            if (methodInfo.IsPublic && serviceType.IsClass)
            {
                var interfacTypes = serviceType.GetInterfaces();

                List<Type> independentTypes = new List<Type>(interfacTypes);

                System.Array.ForEach(interfacTypes, type =>
                {
                    System.Array.ForEach(type.GetInterfaces(), baseType => independentTypes.Remove(baseType));
                });

                foreach (var independentType in independentTypes)
                {
                    var independentMethod = independentType.GetMethod(methodInfo.Name, bindingFlags, null, types, null);

                    if (independentMethod is null)
                    {
                        continue;
                    }

                    foreach (var attributeData in independentMethod.GetCustomAttributesData())
                    {
                        if (attributeType.IsAssignableFrom(attributeData.AttributeType))
                        {
                            attributeDatas.Add(attributeData);
                        }
                    }
                }
            }

            return attributeDatas;
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

        public static Type OverrideType(ClassEmitter classEmitter, FieldEmitter servicesAst, Type serviceType) => OverrideType(classEmitter, This(classEmitter), servicesAst, serviceType);

        public static Type OverrideType(ClassEmitter classEmitter, Expression instanceAst, FieldEmitter servicesAst, Type serviceType)
        {
            var propertyMethods = new HashSet<MethodInfo>();

            foreach (var propertyInfo in serviceType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (propertyInfo.DeclaringType == typeof(object))
                {
                    continue;
                }

                bool noninterceptFlag = propertyInfo.IsDefined(noninterceptAttributeType, true);

                PropertyEmitter propertyEmitter = null;

                if (propertyInfo.CanRead)
                {
                    var getter = propertyInfo.GetMethod ?? propertyInfo.GetGetMethod(true);

                    propertyMethods.Add(getter);

                    var attributeDatas = noninterceptFlag
                        ? new List<CustomAttributeData>(0)
                        : GetCustomAttributeDatas(getter, serviceType);

                    if (serviceType.IsAbstract || attributeDatas.Count > 0)
                    {
                        propertyEmitter ??= classEmitter.DefineProperty(propertyInfo.Name, propertyInfo.Attributes, propertyInfo.PropertyType, GetIndexParameterTypes(propertyInfo));

                        propertyEmitter.SetGetMethod(DefineMethodOverride(classEmitter, servicesAst, instanceAst, getter, attributeDatas));
                    }
                }

                if (propertyInfo.CanWrite)
                {
                    var setter = propertyInfo.SetMethod ?? propertyInfo.GetSetMethod(true);

                    propertyMethods.Add(setter);

                    var attributeDatas = noninterceptFlag
                        ? new List<CustomAttributeData>(0)
                        : GetCustomAttributeDatas(setter, serviceType);

                    if (serviceType.IsAbstract || attributeDatas.Count > 0)
                    {
                        propertyEmitter ??= classEmitter.DefineProperty(propertyInfo.Name, propertyInfo.Attributes, propertyInfo.PropertyType, GetIndexParameterTypes(propertyInfo));

                        propertyEmitter.SetSetMethod(DefineMethodOverride(classEmitter, servicesAst, instanceAst, setter, attributeDatas));
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

                var attributeDatas = GetCustomAttributeDatas(methodInfo, serviceType);

                if (serviceType.IsAbstract || attributeDatas.Count > 0)
                {
                    DefineMethodOverride(classEmitter, servicesAst, instanceAst, methodInfo, attributeDatas);
                }
            }

            propertyMethods.Clear();

            return classEmitter.CreateType();
        }

        private static List<Expression> MethodOverrideInterceptAttributes(MethodEmitter overrideEmitter, IEnumerable<CustomAttributeData> attributeDatas, Type interceptAttribute)
        {
            var interceptAttributes = new List<Expression>();

            foreach (var attributeData in attributeDatas)
            {
                if (interceptAttribute.IsAssignableFrom(attributeData.AttributeType))
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

        private static MethodEmitter DefineMethodOverride(ClassEmitter classEmitter, FieldEmitter servicesAst, Expression instanceAst, MethodInfo methodInfo, IEnumerable<CustomAttributeData> attributeDatas)
        {
            var overrideEmitter = classEmitter.DefineMethodOverride(ref methodInfo);

            var parameterEmitters = overrideEmitter.GetParameters();

            InterceptWay interceptWay = GetInterceptWay(methodInfo.ReturnType);

            Type interceptAttribute = GetInterceptAttributeType(interceptWay);

            Type interceptAttributeArray = GetInterceptAttributeArrayType(interceptWay);

            var interceptAttributes = MethodOverrideInterceptAttributes(overrideEmitter, attributeDatas, interceptAttribute);

            if (interceptAttributes.Count == 0)
            {
                if (interceptWay == InterceptWay.Void)
                {
                    overrideEmitter.Append(Call(instanceAst, methodInfo, parameterEmitters));
                }
                else
                {
                    overrideEmitter.Append(Return(Call(instanceAst, methodInfo, parameterEmitters)));
                }

                return overrideEmitter;
            }

            //? 方法拦截属性。
            var interceptAttrEmitter = classEmitter.DefineField($"____intercept_attr_{methodInfo.Name}_{methodInfo.MetadataToken}", interceptAttributeArray, FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly);

            classEmitter.TypeInitializer.Append(Assign(interceptAttrEmitter, Array(interceptAttribute, interceptAttributes.ToArray())));

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

            switch (interceptWay)
            {
                case InterceptWay.Void:

                    blockAst.Append(Call(interceptVar, middlewareInterceptRunFn, contextVar));

                    break;
                case InterceptWay.ReturnValue:
                    var interceptRunFn = TypeCompiler.GetMethod(middlewareInterceptGenericType.MakeGenericType(returnType), middlewareInterceptGenericRunFn);

                    blockAst.Append(Return(Call(interceptVar, interceptRunFn, contextVar)));

                    break;
                case InterceptWay.Async:
                    {
                        Expression bodyAst = Call(interceptVar, middlewareInterceptAsyncRunFn, contextVar);

                        if (returnType.IsValueType)
                        {
                            var valueTaskCtor = returnType.GetConstructor(new Type[] { bodyAst.RuntimeType });

                            bodyAst = New(valueTaskCtor, bodyAst);
                        }

                        blockAst.Append(Return(bodyAst));
                        break;
                    }
                case InterceptWay.ReturnValueAsync:
                    {
                        var interceptRunAsyncFn = TypeCompiler.GetMethod(middlewareInterceptGenericAsyncType.MakeGenericType(returnType.GetGenericArguments()), middlewareInterceptAsyncGenericRunFn);

                        Expression bodyAst = Call(interceptVar, interceptRunAsyncFn, contextVar);

                        if (returnType.IsValueType)
                        {
                            var valueTaskCtor = returnType.GetConstructor(new Type[] { bodyAst.RuntimeType });

                            bodyAst = New(valueTaskCtor, bodyAst);
                        }

                        blockAst.Append(Return(bodyAst));

                        break;
                    }
                default:
                    throw new NotImplementedException();
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
            var typeEmitter = classEmitter.DefineNestedType($"{classEmitter.Name}_{methodInfo.Name}_{methodInfo.MetadataToken}", TypeAttributes.Public, typeof(object), new Type[] { invocationType });

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
