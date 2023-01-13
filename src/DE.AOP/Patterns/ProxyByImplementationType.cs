using Delta.Emitters;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Delta.AOP.Patterns
{
    using static Delta.Expression;

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

            foreach (var constructorInfo in implementationType.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
            {
                var constructorEmitter = classEmitter.DefineConstructor(constructorInfo.Attributes);

                var parameterInfos = constructorInfo.GetParameters();
                var parameterEmiters = new Expression[parameterInfos.Length];

                for (int i = 0; i < parameterInfos.Length; i++)
                {
                    parameterEmiters[i] = constructorEmitter.DefineParameter(parameterInfos[i]);
                }

                constructorEmitter.Append(Assign(instanceAst, Convert(New(constructorInfo, parameterEmiters), serviceType)));
            }

            return OverrideType(instanceAst, classEmitter, serviceType, implementationType);
        }

        protected virtual Type ResolveIsClass()
        {
            string name = string.Concat(serviceType.Name, "Override");

            var classEmitter = moduleEmitter.DefineType(name, TypeAttributes.Public | TypeAttributes.Class, implementationType);

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
            }

            return OverrideType(classEmitter, serviceType, implementationType);
        }
    }
}
