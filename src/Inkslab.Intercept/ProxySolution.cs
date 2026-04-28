using Inkslab.Emitters;
using Inkslab.Intercept.Proxys;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Inkslab.Intercept
{
    using static Inkslab.Expression;

    /// <summary>
    /// 代理方案。
    /// </summary>
    public class ProxySolution
    {
        private readonly ModuleEmitter _moduleEmitter;

        /// <summary>
        /// 代理类型缓存：键为 (serviceType, implType)，值为 <see cref="Lazy{T}"/> 包裹的 <see cref="ProxyItem"/>。
        /// 使用 per-key <see cref="Lazy{T}"/> 替代全局互斥锁：
        /// 不同 (serviceType, implType) 的代理生成可并行进行，仅相同键的并发请求才会等待，
        /// 显著提升大量服务一次性注册时的吞吐。
        /// </summary>
        private readonly ConcurrentDictionary<Tuple<Type, Type>, Lazy<ProxyItem>> _proxyCachings
            = new ConcurrentDictionary<Tuple<Type, Type>, Lazy<ProxyItem>>();

        /// <summary>
        /// 已代理工厂的包装器。将此类实例作为委托的 Target，可在重复调用时通过
        /// <c>descriptor.ImplementationFactory?.Target is WrappedProxyFactory</c> 识别出
        /// 该工厂已由本方案处理过，从而避免对工厂委托进行二次包装。
        /// </summary>
        private sealed class WrappedProxyFactory
        {
            // 代理类型的构造器委托缓存，避免每次服务解析都走 Activator.CreateInstance 反射路径。
            private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, object, object>> _ctorCache
                = new ConcurrentDictionary<Type, Func<IServiceProvider, object, object>>();

            private readonly Func<IServiceProvider, object> _innerFactory;
            private readonly object _innerInstance;
            private readonly Func<IServiceProvider, object, object> _factory;

            public WrappedProxyFactory(Type proxyType, Func<IServiceProvider, object> innerFactory)
            {
                _innerFactory = innerFactory;
                _factory = _ctorCache.GetOrAdd(proxyType, BuildCtorInvoker);
            }

            public WrappedProxyFactory(Type proxyType, object innerInstance)
            {
                _innerInstance = innerInstance;
                _factory = _ctorCache.GetOrAdd(proxyType, BuildCtorInvoker);
            }

            public object Invoke(IServiceProvider serviceProvider)
            {
                var instance = _innerFactory != null
                    ? _innerFactory.Invoke(serviceProvider)
                    : _innerInstance;

                return _factory(serviceProvider, instance);
            }

            /// <summary>
            /// 为 (IServiceProvider, object) 双参构造器生成强类型委托。
            /// 生成失败时回退到 <see cref="Activator.CreateInstance(Type, object[])"/>。
            /// </summary>
            private static Func<IServiceProvider, object, object> BuildCtorInvoker(Type proxyType)
            {
                try
                {
                    var ctor = proxyType.GetConstructor(
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic,
                        binder: null,
                        types: new[] { typeof(IServiceProvider), typeof(object) },
                        modifiers: null);

                    if (ctor is null)
                    {
                        // 代理类中的 instance 字段为 serviceType，需查找匹配的构造函数。
                        var ctors = proxyType.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                        ctor = System.Array.Find(ctors, c =>
                        {
                            var ps = c.GetParameters();
                            return ps.Length == 2 && ps[0].ParameterType == typeof(IServiceProvider);
                        });
                    }

                    if (ctor is null)
                    {
                        return (sp, instance) => Activator.CreateInstance(proxyType, sp, instance);
                    }

                    var spParam = System.Linq.Expressions.Expression.Parameter(typeof(IServiceProvider), "sp");
                    var instanceParam = System.Linq.Expressions.Expression.Parameter(typeof(object), "instance");

                    var ctorParams = ctor.GetParameters();
                    var convertedInstance = ctorParams[1].ParameterType == typeof(object)
                        ? (System.Linq.Expressions.Expression)instanceParam
                        : System.Linq.Expressions.Expression.Convert(instanceParam, ctorParams[1].ParameterType);

                    var newExpr = System.Linq.Expressions.Expression.New(ctor, spParam, convertedInstance);
                    var body = System.Linq.Expressions.Expression.Convert(newExpr, typeof(object));

                    return System.Linq.Expressions.Expression
                        .Lambda<Func<IServiceProvider, object, object>>(body, spParam, instanceParam)
                        .Compile();
                }
                catch
                {
                    return (sp, instance) => Activator.CreateInstance(proxyType, sp, instance);
                }
            }
        }

        private class ProxyItem
        {
            public ProxyItem(Type destinationType, bool primitive)
            {
                DestinationType = destinationType;
                Primitive = primitive;
            }

            /// <summary>
            /// 目的地类型。
            /// </summary>
            public Type DestinationType { get; }

            /// <summary>
            /// 原始。
            /// </summary>
            public bool Primitive { get; }
        }

        private class MethodInfoEqualityComparer : IEqualityComparer<MethodInfo>
        {
            private static readonly Lazy<MethodInfoEqualityComparer> _lazy = new Lazy<MethodInfoEqualityComparer>(() => new MethodInfoEqualityComparer());

            private MethodInfoEqualityComparer() { }

            public bool Equals(MethodInfo x, MethodInfo y)
            {
                if (x is null)
                {
                    return y is null;
                }

                if (y is null)
                {
                    return false;
                }

                if (!string.Equals(x.Name, y.Name))
                {
                    return false;
                }

                if (x.IsGenericMethod ^ y.IsGenericMethod)
                {
                    return false;
                }

                if (x.IsGenericMethodDefinition ^ y.IsGenericMethodDefinition)
                {
                    return false;
                }

                var xParameterInfos = x.GetParameters();
                var yParameterInfos = y.GetParameters();

                if (xParameterInfos.Length != yParameterInfos.Length)
                {
                    return false;
                }

                for (var i = 0; i < xParameterInfos.Length; i++)
                {
                    if ((x.IsGenericMethod || x.DeclaringType.IsGenericType)
                        ? xParameterInfos[i].ParameterType.IsLike(yParameterInfos[i].ParameterType)
                        : xParameterInfos[i].ParameterType == yParameterInfos[i].ParameterType)
                    {
                        continue;
                    }

                    return false;
                }

                if (x.IsGenericMethod) //? 方法名称和参数类型相同的方法，不能有相同泛型参数个数的方法。
                {
                    var xArguments = x.GetGenericArguments();
                    var yArguments = y.GetGenericArguments();

                    return xArguments.Length == yArguments.Length;
                }

                return true;
            }

            public int GetHashCode(MethodInfo obj)
            {
                if (obj is null)
                {
                    return 0;
                }

                // 仅按 Name 哈希会让重载方法大量碰撞，进而退化为 O(n) 的 Equals 比较。
                // 加入参数个数和泛型标志可显著降低碰撞，且与 Equals 的判定一致。
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + obj.Name.GetHashCode();
                    hash = hash * 31 + obj.GetParameters().Length;
                    hash = hash * 31 + (obj.IsGenericMethod ? 1 : 0);
                    return hash;
                }
            }

            public static MethodInfoEqualityComparer Instance => _lazy.Value;
        }

        /// <summary>
        /// 解决方案。
        /// </summary>
        /// <param name="moduleEmitter">模块。</param>
        public ProxySolution(ModuleEmitter moduleEmitter)
        {
            _moduleEmitter = moduleEmitter;
        }

        /// <summary>
        /// 类型代理。
        /// </summary>
        /// <param name="descriptor">描述。</param>
        /// <returns>代理结果。</returns>
        public ServiceDescriptor Proxy(ServiceDescriptor descriptor)
        {
            //? 密封类不能被代理。
            if (descriptor.ServiceType.IsSealed || descriptor.ServiceType.IsValueType)
            {
                return descriptor;
            }

            // 若实现类型上已标记 ProxyGeneratedAttribute，说明该类型是本方案生成的代理类型，
            // 直接返回，避免重复包装导致 DI 循环依赖。
            if (descriptor.ImplementationType != null && descriptor.ImplementationType.IsDefined(typeof(ProxyGeneratedAttribute), false))
            {
                return descriptor;
            }

            // 若工厂委托的 Target 是 WrappedProxyFactory，说明该工厂已由本方案包装过，直接返回。
            if (descriptor.ImplementationFactory?.Target is WrappedProxyFactory)
            {
                return descriptor;
            }

            var tuple = Tuple.Create(descriptor.ServiceType, descriptor.ImplementationType);

            // GetOrAdd 配合 LazyThreadSafetyMode.ExecutionAndPublication：
            // 同一键的代理生成只发生一次；不同键之间不互相阻塞。
            var lazyItem = _proxyCachings.GetOrAdd(tuple, key => new Lazy<ProxyItem>(
                () =>
                {
                    var implementationType = key.Item2 is null
                        ? ProxyArgument(key.Item1)
                        : ProxyImplementation(key.Item1, key.Item2);

                    return new ProxyItem(implementationType, implementationType == (key.Item2 ?? key.Item1));
                },
                LazyThreadSafetyMode.ExecutionAndPublication));

            ProxyItem proxyItem = lazyItem.Value;

            if (proxyItem.Primitive)
            {
                return descriptor;
            }

            if (descriptor.ImplementationType is null)
            {
                var destinationType = proxyItem.DestinationType;
                var implementationInstance = descriptor.ImplementationInstance;
                var implementationFactory = descriptor.ImplementationFactory;

                var wrapper = implementationInstance is null
                    ? new WrappedProxyFactory(destinationType, implementationFactory)
                    : new WrappedProxyFactory(destinationType, implementationInstance);

                return new ServiceDescriptor(descriptor.ServiceType,
                    new Func<IServiceProvider, object>(wrapper.Invoke),
                    descriptor.Lifetime);
            }
            else
            {
                return new ServiceDescriptor(descriptor.ServiceType, proxyItem.DestinationType, descriptor.Lifetime);
            }
        }

        const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        /// <summary>
        /// 代理类型。
        /// </summary>
        /// <param name="serviceType">服务类型。</param>
        /// <returns>代理类型。</returns>
        protected virtual Type ProxyArgument(Type serviceType)
        {
            var methodInfos = serviceType.IsInterface
                ? serviceType.GetMethods(InstanceFlags)
                : System.Array.FindAll(serviceType.GetMethods(InstanceFlags), x => x.IsVirtual && x.DeclaringType != typeof(object));

            var proxyMethods = methodInfos.Select(x => ProxyMethod.Create(x, GetCustomAttributesData(x))).ToList();

            if (!proxyMethods.Exists(x => x.IsRequired()))
            {
                return serviceType;
            }

            string name = string.Concat(serviceType.Name, "Override");

            var classEmitter = serviceType.IsInterface
                ? _moduleEmitter.DefineType(name, TypeAttributes.Public | TypeAttributes.Class, null, serviceType.GetAllInterfaces())
                : _moduleEmitter.DefineType(name, TypeAttributes.Public | TypeAttributes.Class, serviceType);

            var constructorEmitter = classEmitter.DefineConstructor(MethodAttributes.Public);

            var servicesAst = classEmitter.DefineField("____services__", typeof(IServiceProvider), FieldAttributes.Private | FieldAttributes.InitOnly | FieldAttributes.NotSerialized);

            var instanceAst = classEmitter.DefineField("____instance__", serviceType, FieldAttributes.Private | FieldAttributes.InitOnly | FieldAttributes.NotSerialized);

            var servicesEmitter = constructorEmitter.DefineParameter(typeof(IServiceProvider), ParameterAttributes.None, "services");
            var parameterEmitter = constructorEmitter.DefineParameter(serviceType, ParameterAttributes.None, "instance");

            constructorEmitter.Append(Assign(servicesAst, servicesEmitter));
            constructorEmitter.Append(Assign(instanceAst, parameterEmitter));

            var propertyInfos = serviceType.GetProperties(InstanceFlags);

            return ProxyAlways(classEmitter, servicesAst, instanceAst, propertyInfos, proxyMethods);
        }

        /// <summary>
        /// 代理类型。
        /// </summary>
        /// <param name="serviceType">服务类型。</param>
        /// <param name="implementationType">实现类型。</param>
        /// <returns>代理类型。</returns>
        protected virtual Type ProxyImplementation(Type serviceType, Type implementationType)
        {
            if (serviceType.IsInterface)
            {
                return ProxyServiceIsInterface(serviceType, implementationType);
            }

            var baseConstructor = serviceType.GetConstructor(InstanceFlags, null, Type.EmptyTypes, null);

            if (baseConstructor != null)
            {
                return ProxyServiceIsParameterlessConstructor(serviceType, implementationType, baseConstructor);
            }

            var methodInfos = System.Array.FindAll(serviceType.GetMethods(InstanceFlags), x => x.IsVirtual && x.DeclaringType != typeof(object));
            var implementatioMethodInfos = System.Array.FindAll(implementationType.GetMethods(InstanceFlags), x => x.IsVirtual && x.DeclaringType != typeof(object));

            var proxyMethods = methodInfos.Join(implementatioMethodInfos, x => x, y => y, (x, y) =>
            {
                return ProxyMethod.Create(y, GetCustomAttributesData(y));
            }, MethodInfoEqualityComparer.Instance).ToList();

            if (!proxyMethods.Exists(x => x.IsRequired()))
            {
                return implementationType;
            }

            if (implementationType.IsSealed)
            {
                throw new NotSupportedException($"服务“{serviceType.Name}”不含无参构造函数，并且服务实现“{implementationType.Name}”是密封类！");
            }

            string name = string.Concat(serviceType.Name, "Override");

            var classEmitter = serviceType.IsInterface
                ? _moduleEmitter.DefineType(name, TypeAttributes.Public | TypeAttributes.Class, null, serviceType.GetAllInterfaces())
                : _moduleEmitter.DefineType(name, TypeAttributes.Public | TypeAttributes.Class, serviceType);

            var servicesAst = classEmitter.DefineField("____services__", typeof(IServiceProvider), FieldAttributes.Private | FieldAttributes.InitOnly | FieldAttributes.NotSerialized);

            var propertyInfos = serviceType.GetProperties(InstanceFlags);

            ReplicateImplementationConstructors(
                classEmitter,
                implementationType,
                servicesAst,
                (ctor, ctorInfo, args) => ctor.InvokeBaseConstructor(ctorInfo, args));

            return ProxyAlways(classEmitter, servicesAst, This(classEmitter), propertyInfos, proxyMethods);
        }

        #region 代理。
        /// <summary>
        /// 代理服务是接口。
        /// </summary>
        private Type ProxyServiceIsInterface(Type serviceType, Type implementationType)
        {
            var methodInfos = serviceType.GetMethods(InstanceFlags);
            var implementatioMethodInfos = System.Array.FindAll(implementationType.GetMethods(InstanceFlags), x => x.IsVirtual && x.DeclaringType != typeof(object));

            var proxyMethods = methodInfos.Join(implementatioMethodInfos, x => x, y => y, (x, y) =>
            {
                return ProxyMethod.Create(x, GetCustomAttributesData(y));
            }, MethodInfoEqualityComparer.Instance).ToList();

            if (!proxyMethods.Exists(x => x.IsRequired()))
            {
                return implementationType;
            }

            string name = string.Concat(serviceType.Name, "Override");

            var classEmitter = _moduleEmitter.DefineType(name, TypeAttributes.Public | TypeAttributes.Class, null, serviceType.GetAllInterfaces());

            var servicesAst = classEmitter.DefineField("____services__", typeof(IServiceProvider), FieldAttributes.Private | FieldAttributes.InitOnly | FieldAttributes.NotSerialized);

            var instanceAst = classEmitter.DefineField("____instance__", serviceType, FieldAttributes.Private | FieldAttributes.InitOnly | FieldAttributes.NotSerialized);

            ReplicateImplementationConstructors(
                classEmitter,
                implementationType,
                servicesAst,
                (ctor, ctorInfo, args) => ctor.Append(Assign(instanceAst, Convert(New(ctorInfo, args), serviceType))));

            var propertyInfos = serviceType.GetProperties(InstanceFlags);

            return ProxyAlways(classEmitter, servicesAst, instanceAst, propertyInfos, proxyMethods);
        }

        /// <summary>
        /// 代理服务有无参构造函数。
        /// </summary>
        private Type ProxyServiceIsParameterlessConstructor(Type serviceType, Type implementationType, ConstructorInfo baseConstructor)
        {
            var methodInfos = System.Array.FindAll(serviceType.GetMethods(InstanceFlags), x => x.IsVirtual && x.DeclaringType != typeof(object));
            var implementatioMethodInfos = System.Array.FindAll(implementationType.GetMethods(InstanceFlags), x => x.IsVirtual && x.DeclaringType != typeof(object));

            var proxyMethods = methodInfos.Join(implementatioMethodInfos, x => x, y => y, (x, y) =>
            {
                return ProxyMethod.Create(x, GetCustomAttributesData(y));
            }, MethodInfoEqualityComparer.Instance).ToList();

            if (!proxyMethods.Exists(x => x.IsRequired()))
            {
                return implementationType;
            }

            string name = string.Concat(serviceType.Name, "Override");

            var classEmitter = _moduleEmitter.DefineType(name, TypeAttributes.Public | TypeAttributes.Class, serviceType);

            var servicesAst = classEmitter.DefineField("____services__", typeof(IServiceProvider), FieldAttributes.Private | FieldAttributes.InitOnly | FieldAttributes.NotSerialized);

            var instanceAst = classEmitter.DefineField("____instance__", serviceType, FieldAttributes.Private | FieldAttributes.InitOnly | FieldAttributes.NotSerialized);

            var propertyInfos = serviceType.GetProperties(InstanceFlags);

            ReplicateImplementationConstructors(
                classEmitter,
                implementationType,
                servicesAst,
                (ctor, ctorInfo, args) =>
                {
                    ctor.Append(Assign(instanceAst, Convert(New(ctorInfo, args), serviceType)));
                    ctor.InvokeBaseConstructor(baseConstructor);
                });

            return serviceType.IsAbstract
                ? ProxyAlways(classEmitter, servicesAst, instanceAst, propertyInfos, proxyMethods)
                : Proxy(classEmitter, servicesAst, instanceAst, propertyInfos, proxyMethods);
        }

        /// <summary>
        /// 复制实现类型的所有构造函数到代理类，并赋值 <paramref name="servicesAst"/>。
        /// <paramref name="emitBody"/> 负责发出不同代理场景下的个性化逻辑（如赋值 instance 字段、调用 base 构造函数等）。
        /// </summary>
        private static void ReplicateImplementationConstructors(
            ClassEmitter classEmitter,
            Type implementationType,
            FieldEmitter servicesAst,
            Action<ConstructorEmitter, ConstructorInfo, Expression[]> emitBody)
        {
            foreach (var constructorInfo in implementationType.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic))
            {
                var constructorEmitter = classEmitter.DefineConstructor(constructorInfo.Attributes);

                var servicesEmitter = constructorEmitter.DefineParameter(typeof(IServiceProvider), ParameterAttributes.None, "____services__");

                var parameterInfos = constructorInfo.GetParameters();

                var parameterEmiters = new Expression[parameterInfos.Length];

                for (int i = 0; i < parameterInfos.Length; i++)
                {
                    parameterEmiters[i] = constructorEmitter.DefineParameter(parameterInfos[i]);
                }

                constructorEmitter.Append(Assign(servicesAst, servicesEmitter));

                emitBody(constructorEmitter, constructorInfo, parameterEmiters);
            }
        }

        /// <summary>
        /// 仅代理必须代理的方法。
        /// </summary>
        private static Type Proxy(ClassEmitter classEmitter, FieldEmitter servicesAst, Expression instanceAst, PropertyInfo[] propertyInfos, List<ProxyMethod> proxyMethods)
        {
            var propertyMethods = new HashSet<MethodInfo>();

            foreach (var propertyInfo in propertyInfos)
            {
                var proxyPropertyMethods = new List<ProxyMethod>(2);

                if (propertyInfo.CanRead)
                {
                    var getter = propertyInfo.GetMethod ?? propertyInfo.GetGetMethod(true);

                    propertyMethods.Add(getter);

                    proxyPropertyMethods.Add(ProxyMethod.Create(getter, GetCustomAttributesData(getter)));
                }

                if (propertyInfo.CanWrite)
                {
                    var setter = propertyInfo.SetMethod ?? propertyInfo.GetSetMethod(true);

                    propertyMethods.Add(setter);

                    proxyPropertyMethods.Add(ProxyMethod.Create(setter, GetCustomAttributesData(setter)));
                }

                if (proxyPropertyMethods.Exists(x => x.IsRequired()))
                {
                    PropertyEmitter propertyEmitter = classEmitter.DefineProperty(propertyInfo.Name, propertyInfo.Attributes, propertyInfo.PropertyType, System.Array.ConvertAll(propertyInfo.GetIndexParameters(), x => x.ParameterType));

                    if (propertyInfo.CanRead)
                    {
                        propertyEmitter.SetGetMethod(proxyPropertyMethods[0].OverrideMethod(classEmitter, servicesAst, instanceAst));
                    }

                    if (propertyInfo.CanWrite)
                    {
                        propertyEmitter.SetSetMethod(proxyPropertyMethods[propertyInfo.CanRead ? 1 : 0].OverrideMethod(classEmitter, servicesAst, instanceAst));
                    }
                }
            }

            foreach (var proxyMethod in proxyMethods)
            {
                if (propertyMethods.Contains(proxyMethod.Method))
                {
                    continue;
                }

                if (proxyMethod.IsRequired())
                {
                    proxyMethod.OverrideMethod(classEmitter, servicesAst, instanceAst);
                }
            }

            classEmitter.DefineCustomAttribute<ProxyGeneratedAttribute>();
            return classEmitter.CreateType();
        }

        /// <summary>
        /// 代理所有方法。
        /// </summary>
        private static Type ProxyAlways(ClassEmitter classEmitter, FieldEmitter servicesAst, Expression instanceAst, PropertyInfo[] propertyInfos, List<ProxyMethod> proxyMethods)
        {
            var propertyMethods = new HashSet<MethodInfo>();

            foreach (var propertyInfo in propertyInfos)
            {
                PropertyEmitter propertyEmitter = classEmitter.DefineProperty(propertyInfo.Name, propertyInfo.Attributes, propertyInfo.PropertyType, System.Array.ConvertAll(propertyInfo.GetIndexParameters(), x => x.ParameterType));

                if (propertyInfo.CanRead)
                {
                    var getter = propertyInfo.GetMethod ?? propertyInfo.GetGetMethod(true);

                    propertyMethods.Add(getter);

                    var proxyMethod = ProxyMethod.Create(getter, GetCustomAttributesData(getter));

                    propertyEmitter.SetGetMethod(proxyMethod.OverrideMethod(classEmitter, servicesAst, instanceAst));
                }

                if (propertyInfo.CanWrite)
                {
                    var setter = propertyInfo.SetMethod ?? propertyInfo.GetSetMethod(true);

                    propertyMethods.Add(setter);

                    var proxyMethod = ProxyMethod.Create(setter, GetCustomAttributesData(setter));

                    propertyEmitter.SetSetMethod(proxyMethod.OverrideMethod(classEmitter, servicesAst, instanceAst));
                }
            }

            foreach (var proxyMethod in proxyMethods)
            {
                if (propertyMethods.Contains(proxyMethod.Method))
                {
                    continue;
                }

                proxyMethod.OverrideMethod(classEmitter, servicesAst, instanceAst);
            }

            classEmitter.DefineCustomAttribute<ProxyGeneratedAttribute>();
            return classEmitter.CreateType();
        }

        private static List<CustomAttributeData> GetCustomAttributesData(MethodInfo methodInfo)
        {
            var declaringType = methodInfo.DeclaringType;

            var interfaceTypes = declaringType.GetInterfaces();

            var attributeDatas = new List<CustomAttributeData>(methodInfo.GetCustomAttributesData());

            while ((declaringType = declaringType.BaseType) != null && declaringType != typeof(object))
            {
                attributeDatas.AddRange(declaringType.GetMethods(InstanceFlags)
                    .Where(x => MethodInfoEqualityComparer.Instance.Equals(x, methodInfo))
                    .SelectMany(x => x.GetCustomAttributesData()));
            }

            foreach (var interfaceType in interfaceTypes)
            {
                attributeDatas.AddRange(interfaceType.GetMethods(InstanceFlags)
                    .Where(x => MethodInfoEqualityComparer.Instance.Equals(x, methodInfo))
                    .SelectMany(x => x.GetCustomAttributesData()));
            }

            return attributeDatas;
        }
        #endregion
    }

    /// <summary>
    /// 标记由 <see cref="ProxySolution"/> 动态生成的代理类型。
    /// 写入到生成类的元数据中，随类型本身常驻运行时而无额外内存常驻，
    /// 通过 <see cref="MemberInfo.IsDefined"/> 即可判断某类型是否已被代理。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class ProxyGeneratedAttribute : Attribute { }
}
