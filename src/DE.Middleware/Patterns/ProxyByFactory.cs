using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;

namespace Delta.Middleware.Patterns
{
    using static Delta.Expression;

    class ProxyByFactory : ProxyByServiceType
    {
        private readonly ModuleEmitter moduleEmitter;
        private readonly Type serviceType;
        private readonly Func<IServiceProvider, object> implementationFactory;
        private readonly ServiceLifetime lifetime;

        public ProxyByFactory(ModuleEmitter moduleEmitter, Type serviceType, Func<IServiceProvider, object> implementationFactory, ServiceLifetime lifetime)
        {
            this.moduleEmitter = moduleEmitter;
            this.serviceType = serviceType;
            this.implementationFactory = implementationFactory;
            this.lifetime = lifetime;
        }

        public override ServiceDescriptor Ref()
        {
            var overrideType = OverrideType();

            return new ServiceDescriptor(serviceType, serviceProvider =>
            {
                var instance = implementationFactory.Invoke(serviceProvider);

                return Activator.CreateInstance(overrideType, instance);

            }, lifetime);
        }

        protected virtual Type OverrideType()
        {
            if (serviceType.IsSealed)
            {
                throw new NotSupportedException("无法代理密封类!");
            }

            if (serviceType.IsInterface)
            {
                return ResolveIsInterface();
            }

            var constructorInfo = serviceType.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null) ?? throw new NotSupportedException($"“{serviceType.Name}”不具备无参构造函数！");

            return ResolveIsClass(constructorInfo);
        }

        private Type ResolveIsInterface()
        {
            string name = string.Concat(serviceType.Name, "Override");

            var interfaces = serviceType.GetAllInterfaces();

            var classEmitter = moduleEmitter.DefineType(name, TypeAttributes.Public | TypeAttributes.Class, null, interfaces);

            var instanceAst = classEmitter.DefineField("____instance__", serviceType, FieldAttributes.Private | FieldAttributes.InitOnly | FieldAttributes.NotSerialized);

            var constructorEmitter = classEmitter.DefineConstructor(MethodAttributes.Public);

            var parameterEmitter = constructorEmitter.DefineParameter(serviceType, ParameterAttributes.None, "instance");

            constructorEmitter.Append(Assign(instanceAst, parameterEmitter));

            return OverrideType(instanceAst, classEmitter, serviceType, serviceType);
        }

        private Type ResolveIsClass(ConstructorInfo constructorInfo)
        {
            string name = string.Concat(serviceType.Name, "Override");

            var classEmitter = moduleEmitter.DefineType(name, TypeAttributes.Public | TypeAttributes.Class, serviceType);

            var constructorEmitter = classEmitter.DefineConstructor(MethodAttributes.Public);

            constructorEmitter.InvokeBaseConstructor(constructorInfo);

            var instanceAst = classEmitter.DefineField("____instance__", serviceType, FieldAttributes.Private | FieldAttributes.InitOnly | FieldAttributes.NotSerialized);

            var parameterEmitter = constructorEmitter.DefineParameter(serviceType, ParameterAttributes.None, "instance");

            constructorEmitter.Append(Assign(instanceAst, parameterEmitter));

            return OverrideType(instanceAst, classEmitter, serviceType, serviceType);
        }
    }
}
