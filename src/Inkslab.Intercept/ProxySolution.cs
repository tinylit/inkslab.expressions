using Inkslab.Emitters;
using Inkslab.Intercept.Proxys;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Inkslab.Intercept
{
    using static Inkslab.Expression;

    /// <summary>
    /// 代理方案。
    /// </summary>
    public class ProxySolution
    {
        private readonly ModuleEmitter _moduleEmitter;
        private readonly Dictionary<Tuple<Type, Type>, ProxyItem> proxyCachings = new Dictionary<Tuple<Type, Type>, ProxyItem>();

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

            public int GetHashCode(MethodInfo obj) => obj is null ? 0 : obj.Name.GetHashCode();

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

            var tuple = Tuple.Create(descriptor.ServiceType, descriptor.ImplementationType ?? descriptor.ServiceType);

            if (proxyCachings.TryGetValue(tuple, out ProxyItem proxyItem))
            {
                if (proxyItem.Primitive)
                {
                    return descriptor;
                }

                goto label_ready;
            }

            var implementationType = descriptor.ImplementationType is null
                ? ProxyArgument(descriptor.ServiceType)
                : ProxyImplementation(descriptor.ServiceType, descriptor.ImplementationType);

            proxyCachings.Add(tuple, proxyItem = new ProxyItem(implementationType, implementationType == tuple.Item2));

label_ready:

            if (descriptor.ImplementationType is null)
            {
                var destinationType = proxyItem.DestinationType;
                var implementationInstance = descriptor.ImplementationInstance;
                var implementationFactory = descriptor.ImplementationFactory;

                return implementationInstance is null
                    ? new ServiceDescriptor(descriptor.ServiceType, serviceProvider =>
                    {
                        var instance = implementationFactory.Invoke(serviceProvider);

                        return Activator.CreateInstance(destinationType, serviceProvider, instance);

                    }, descriptor.Lifetime)
                    : new ServiceDescriptor(descriptor.ServiceType, serviceProvider =>
                    {
                        return Activator.CreateInstance(destinationType, serviceProvider, implementationInstance);
                    }, descriptor.Lifetime);
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

                constructorEmitter.InvokeBaseConstructor(constructorInfo);
            }

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
                constructorEmitter.Append(Assign(instanceAst, Convert(New(constructorInfo, parameterEmiters), serviceType)));
            }

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
                constructorEmitter.Append(Assign(instanceAst, Convert(New(constructorInfo, parameterEmiters), serviceType)));

                constructorEmitter.InvokeBaseConstructor(baseConstructor);
            }

            return serviceType.IsAbstract
                ? ProxyAlways(classEmitter, servicesAst, instanceAst, propertyInfos, proxyMethods)
                : Proxy(classEmitter, servicesAst, instanceAst, propertyInfos, proxyMethods);
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
}
