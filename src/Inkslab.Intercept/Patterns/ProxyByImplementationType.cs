using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;

namespace Inkslab.Intercept.Patterns
{
    using static Inkslab.Expression;

    class ProxyByImplementationType : ProxyByServiceType
    {
        private readonly ModuleEmitter moduleEmitter;
        private readonly Type serviceType;
        private readonly Type implementationType;
        private readonly ServiceLifetime lifetime;

        public ProxyByImplementationType(ModuleEmitter moduleEmitter, Type serviceType, Type implementationType, ServiceLifetime lifetime)
        {
            this.moduleEmitter = moduleEmitter;
            this.serviceType = serviceType;
            this.implementationType = implementationType;
            this.lifetime = lifetime;
        }

        public override ServiceDescriptor Ref()
        {
            var overrideType = OverrideType();

            return new ServiceDescriptor(serviceType, overrideType, lifetime);
        }

        protected virtual Type OverrideType()
        {
            if (serviceType.IsSealed)
            {
                throw new NotSupportedException("无法代理密封类!");
            }

            if (implementationType.IsSealed)
            {
                if (serviceType.IsInterface)
                {
                    return ResolveIsInterface();
                }

                throw new NotSupportedException($"代理“{serviceType.FullName}”类的实现类（“{implementationType.FullName}”）是密封类!");
            }
            else if (implementationType.IsInterface)
            {
                throw new NotSupportedException($"代理“{serviceType.FullName}”类的实现类（“{implementationType.FullName}”）是接口!");
            }
            else if (implementationType.IsAbstract)
            {
                throw new NotSupportedException($"代理“{serviceType.FullName}”类的实现类（“{implementationType.FullName}”）是抽象类!");
            }
            else if (implementationType.IsValueType)
            {
                throw new NotSupportedException($"代理“{serviceType.FullName}”类的实现类（“{implementationType.FullName}”）是值类型!");
            }
            else if (serviceType.IsInterface)
            {
                return ResolveIsInterface();
            }
            else
            {
                return ResolveIsClass();
            }
        }

        private Type ResolveIsInterface()
        {
            string name = string.Concat(serviceType.Name, "Override");

            var interfaces = serviceType.GetAllInterfaces();

            var classEmitter = moduleEmitter.DefineType(name, TypeAttributes.Public | TypeAttributes.Class, null, interfaces);

            var instanceAst = classEmitter.DefineField("____instance__", serviceType, FieldAttributes.Private | FieldAttributes.InitOnly | FieldAttributes.NotSerialized);
            var servicesAst = classEmitter.DefineField("____services__", typeof(IServiceProvider), FieldAttributes.Private | FieldAttributes.InitOnly | FieldAttributes.NotSerialized);

            foreach (var constructorInfo in implementationType.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
            {
                var constructorEmitter = classEmitter.DefineConstructor(constructorInfo.Attributes);

                var parameterInfos = constructorInfo.GetParameters();
                var parameterEmiters = new Expression[parameterInfos.Length];

                for (int i = 0; i < parameterInfos.Length; i++)
                {
                    parameterEmiters[i] = constructorEmitter.DefineParameter(parameterInfos[i]);
                }

                var servicesEmitter = constructorEmitter.DefineParameter(typeof(IServiceProvider), ParameterAttributes.None, "services");

                constructorEmitter.Append(Assign(instanceAst, Convert(New(constructorInfo, parameterEmiters), serviceType)));
                constructorEmitter.Append(Assign(servicesAst, servicesEmitter));
            }

            return OverrideType(classEmitter, instanceAst, servicesAst, serviceType);
        }

        private Type ResolveIsClass()
        {
            string name = string.Concat(serviceType.Name, "Override");

            var classEmitter = moduleEmitter.DefineType(name, TypeAttributes.Public | TypeAttributes.Class, implementationType);

            var servicesAst = classEmitter.DefineField("____services__", typeof(IServiceProvider), FieldAttributes.Private | FieldAttributes.InitOnly | FieldAttributes.NotSerialized);

            foreach (var constructorInfo in implementationType.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
            {
                var constructorEmitter = classEmitter.DefineConstructor(constructorInfo.Attributes);

                var parameterInfos = constructorInfo.GetParameters();
                var parameterEmiters = new Expression[parameterInfos.Length];

                for (int i = 0; i < parameterInfos.Length; i++)
                {
                    parameterEmiters[i] = constructorEmitter.DefineParameter(parameterInfos[i]);
                }

                constructorEmitter.InvokeBaseConstructor(constructorInfo, parameterEmiters);

                var servicesEmitter = constructorEmitter.DefineParameter(typeof(IServiceProvider), ParameterAttributes.None, "services");

                constructorEmitter.Append(Assign(servicesAst, servicesEmitter));
            }

            return OverrideType(classEmitter, servicesAst, serviceType);
        }
    }
}
