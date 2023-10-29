using Inkslab.Emitters;
using Inkslab.Expressions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Inkslab.Intercept.Patterns
{
    using static Inkslab.Expression;

    abstract class ProxyByServiceType : IProxyByPattern
    {
        private static readonly Type interceptAttributeType = typeof(InterceptAttribute);
        private static readonly Type interceptAsyncAttributeType = typeof(InterceptAsyncAttribute);

        private static readonly Type noninterceptAttributeType = typeof(NoninterceptAttribute);

        private static readonly Type interceptAttributeArrayType = typeof(InterceptAttribute[]);
        private static readonly Type interceptAsyncAttributeArrayType = typeof(InterceptAsyncAttribute[]);

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

            var middlewareInterceptType = typeof(MiddlewareIntercept);
            var middlewareInterceptAsyncType = typeof(MiddlewareInterceptAsync);

            middlewareInterceptCtor = middlewareInterceptType.GetConstructor(interceptAttributeArrayArg);
            middlewareInterceptRunFn = middlewareInterceptType.GetMethod("Run", interceptContextTypeArg);


            middlewareInterceptAsyncCtor = middlewareInterceptAsyncType.GetConstructor(interceptAsyncAttributeArrayArg);
            middlewareInterceptAsyncRunFn = middlewareInterceptAsyncType.GetMethod("RunAsync", interceptContextTypeArg);

            middlewareInterceptGenericType = typeof(MiddlewareIntercept<>);
            middlewareInterceptGenericAsyncType = typeof(MiddlewareInterceptAsync<>);

            middlewareInterceptGenericCtor = middlewareInterceptGenericType.GetConstructor(interceptAttributeArrayArg);
            middlewareInterceptGenericRunFn = middlewareInterceptGenericType.GetMethod("Run", interceptContextTypeArg);

            middlewareInterceptAsyncGenericCtor = middlewareInterceptGenericAsyncType.GetConstructor(interceptAsyncAttributeArrayArg);
            middlewareInterceptAsyncGenericRunFn = middlewareInterceptGenericAsyncType.GetMethod("RunAsync", interceptContextTypeArg);
        }

        public abstract ServiceDescriptor Ref();

        protected static bool Intercept(MethodInfo methodInfo)
        {
            if (methodInfo is null)
            {
                throw new ArgumentNullException(nameof(methodInfo));
            }

            if (methodInfo.DeclaringType.IsAbstract || methodInfo.DeclaringType.IsInterface)
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

            return IsAsync(methodInfo) ? methodInfo.IsDefined(interceptAsyncAttributeType, true) : methodInfo.IsDefined(interceptAttributeType, true);
        }

        private static bool IsVirtual(MethodInfo methodInfo) => methodInfo.IsVirtual || methodInfo.DeclaringType.IsAbstract || methodInfo.DeclaringType.IsInterface;

        protected static bool Intercept(PropertyInfo propertyInfo)
        {
            if (propertyInfo is null)
            {
                throw new ArgumentNullException(nameof(propertyInfo));
            }

            if (propertyInfo.DeclaringType.IsInterface)
            {
                return true;
            }

            if (propertyInfo.IsDefined(noninterceptAttributeType, true))
            {
                return false;
            }

            if (IsAsync(propertyInfo.PropertyType) ? propertyInfo.IsDefined(interceptAsyncAttributeType, true) : propertyInfo.IsDefined(interceptAttributeType, true))
            {
                if (propertyInfo.CanRead)
                {
                    if (IsVirtual(propertyInfo.GetMethod ?? propertyInfo.GetGetMethod(true)))
                    {
                        return true;
                    }
                }

                if (propertyInfo.CanWrite)
                {
                    if (IsVirtual(propertyInfo.SetMethod ?? propertyInfo.GetSetMethod(true)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool Intercept(ServiceDescriptor descriptor) => Intercept(descriptor.ImplementationType ?? descriptor.ImplementationInstance?.GetType() ?? descriptor.ServiceType);

        public static bool IsAsync(MethodInfo methodInfo) => IsAsync(methodInfo.ReturnType);

        public static bool IsAsync(Type type)
        {
            if (type.IsValueType)
            {
                return type.IsGenericType ? type.GetGenericTypeDefinition() == typeof(ValueTask<>) : type == typeof(ValueTask);
            }

            return typeof(Task).IsAssignableFrom(type);
        }

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

            if (serviceType.IsInterface && serviceType.IsDefined(interceptAttributeType, true))
            {
                foreach (var methodInfo in serviceType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
                {
                    if (IsAsync(methodInfo))
                    {
                        continue;
                    }

                    if (methodInfo.IsDefined(noninterceptAttributeType, true))
                    {
                        continue;
                    }

                    return true;
                }

                foreach (var propertyInfo in serviceType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
                {
                    if (IsAsync(propertyInfo.PropertyType))
                    {
                        continue;
                    }

                    if (propertyInfo.IsDefined(noninterceptAttributeType, true))
                    {
                        continue;
                    }

                    return true;
                }
            }

            if (serviceType.IsInterface && serviceType.IsDefined(interceptAsyncAttributeType, true))
            {
                foreach (var methodInfo in serviceType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
                {
                    if (IsAsync(methodInfo))
                    {
                        if (methodInfo.IsDefined(noninterceptAttributeType, true))
                        {
                            continue;
                        }

                        return true;
                    }
                }
            }

            foreach (var methodInfo in serviceType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty | BindingFlags.GetProperty))
            {
                if (Intercept(methodInfo))
                {
                    return true;
                }
            }

            return false;
        }

        public static List<CustomAttributeData> GetCustomAttributeDatas(MethodInfo methodInfo, Type implementationType, bool skipDeclaringTypeAttribute = false)
        {
            if (methodInfo is null)
            {
                throw new ArgumentNullException(nameof(methodInfo));
            }

            if (implementationType is null)
            {
                throw new ArgumentNullException(nameof(implementationType));
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

            var isAsync = IsAsync(methodInfo);

            var attributeType = isAsync
                ? interceptAsyncAttributeType
                : interceptAttributeType;

            var hashSet = new HashSet<Type>();

            var attributeDatas = new List<CustomAttributeData>(methodInfo.GetCustomAttributesData());

            if (methodInfo.IsPublic)
            {
                Done(methodInfo.ReflectedType, true);
            }

            var methodInfos = new List<MethodInfo>();

            var referenceInfo = implementationType.GetMethod(methodInfo.Name, bindingFlags, null, types, null) ?? throw new MissingMethodException(implementationType.Name, methodInfo.Name);

            while (referenceInfo != methodInfo)
            {
                methodInfos.Insert(0, referenceInfo);

                var baseDefinition = referenceInfo.GetBaseDefinition();

                if (baseDefinition == referenceInfo)
                {
                    break;
                }

                referenceInfo = baseDefinition;
            }

            methodInfos.ForEach(methodInfo =>
            {
                foreach (var attributeData in methodInfo.GetCustomAttributesData())
                {
                    if (attributeType.IsAssignableFrom(attributeData.AttributeType))
                    {
                        attributeDatas.Add(attributeData);
                    }
                }

                if (methodInfo.IsPublic)
                {
                    Done(methodInfo.ReflectedType, true);
                }
            });

            void Done(Type type, bool skipMethodAttribute)
            {
                if (hashSet.Add(type))
                {
                    if (skipMethodAttribute)
                    {
                        goto label_core;
                    }

                    var method = type.GetMethod(methodInfo.Name, bindingFlags, null, types, null);

                    if (method is null)
                    {
                        goto label_declaring;
                    }

                    foreach (var attributeData in method.GetCustomAttributesData())
                    {
                        if (attributeType.IsAssignableFrom(attributeData.AttributeType))
                        {
                            attributeDatas.Add(attributeData);
                        }
                    }

label_core:
                    if (skipDeclaringTypeAttribute)
                    {
                        goto label_declaring;
                    }

                    foreach (var attributeData in type.GetCustomAttributesData())
                    {
                        if (attributeType.IsAssignableFrom(attributeData.AttributeType))
                        {
                            attributeDatas.Add(attributeData);
                        }
                    }
label_declaring:

                    foreach (var interfaceType in type.GetInterfaces())
                    {
                        Done(interfaceType, false);
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

        public static Type OverrideType(ClassEmitter classEmitter, FieldEmitter servicesAst, Type serviceType, Type implementationType) => OverrideType(classEmitter, This(classEmitter), servicesAst, serviceType, implementationType);

        public static Type OverrideType(ClassEmitter classEmitter, Expression instanceAst, FieldEmitter servicesAst, Type serviceType, Type implementationType)
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

                    var attributeDatas = GetCustomAttributeDatas(getter, implementationType, true);

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

                    var attributeDatas = GetCustomAttributeDatas(setter, implementationType, true);

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

                var attributeDatas = GetCustomAttributeDatas(methodInfo, implementationType);

                if (serviceType.IsAbstract || attributeDatas.Count > 0)
                {
                    DefineMethodOverride(classEmitter, servicesAst, instanceAst, methodInfo, attributeDatas);
                }
            }

            propertyMethods.Clear();

            return classEmitter.CreateType();
        }

        private static List<Expression> MethodOverrideInterceptAttributes(MethodEmitter overrideEmitter, IEnumerable<CustomAttributeData> attributeDatas, bool isAsync)
        {
            var interceptAttributes = new List<Expression>();

            Type attributeType = isAsync
                ? interceptAsyncAttributeType
                : interceptAttributeType; ;

            foreach (var attributeData in attributeDatas)
            {
                if (attributeType.IsAssignableFrom(attributeData.AttributeType))
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

            bool isAsync = IsAsync(methodInfo);

            var interceptAttributes = MethodOverrideInterceptAttributes(overrideEmitter, attributeDatas, isAsync);

            if (interceptAttributes.Count == 0)
            {
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

            //? 方法拦截属性。
            var interceptAttrEmitter = classEmitter.DefineField($"____intercept_attr_{methodInfo.Name}_{methodInfo.MetadataToken}", isAsync ? interceptAsyncAttributeArrayType : interceptAttributeArrayType, FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly);

            classEmitter.TypeInitializer.Append(Assign(interceptAttrEmitter, Array(isAsync ? interceptAsyncAttributeType : interceptAttributeType, interceptAttributes.ToArray())));

            //? 方法主体。
            Expression[] arguments;

            Expression methodAst;

            var variableAst = Variable(typeof(object[]));

            overrideEmitter.Append(Assign(variableAst, Array(parameterEmitters)));

            if (overrideEmitter.IsGenericMethod)
            {
                methodAst = Constant(methodInfo);

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

            if (isAsync)
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
