using System;
using System.Reflection;

namespace Inkslab.Intercept.Patterns
{
    using static Inkslab.Expression;

    abstract class ProxyByInstanceArgument : ProxyByServiceType
    {
        private readonly ModuleEmitter moduleEmitter;
        private readonly Type serviceType;

        public ProxyByInstanceArgument(ModuleEmitter moduleEmitter, Type serviceType)
        {
            this.moduleEmitter = moduleEmitter;
            this.serviceType = serviceType;
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

            var servicesAst = classEmitter.DefineField("____services__", typeof(IServiceProvider), FieldAttributes.Private | FieldAttributes.InitOnly | FieldAttributes.NotSerialized);
            var instanceAst = classEmitter.DefineField("____instance__", serviceType, FieldAttributes.Private | FieldAttributes.InitOnly | FieldAttributes.NotSerialized);

            var constructorEmitter = classEmitter.DefineConstructor(MethodAttributes.Public);

            var servicesEmitter = constructorEmitter.DefineParameter(typeof(IServiceProvider), ParameterAttributes.None, "services");
            var parameterEmitter = constructorEmitter.DefineParameter(serviceType, ParameterAttributes.None, "instance");

            constructorEmitter.Append(Assign(servicesAst, servicesEmitter));
            constructorEmitter.Append(Assign(instanceAst, parameterEmitter));

            return OverrideType(classEmitter, instanceAst, servicesAst, serviceType);
        }

        private Type ResolveIsClass(ConstructorInfo constructorInfo)
        {
            string name = string.Concat(serviceType.Name, "Override");

            var interfaces = serviceType.GetInterfaces();

            var classEmitter = moduleEmitter.DefineType(name, TypeAttributes.Public | TypeAttributes.Class, serviceType, interfaces);

            var servicesAst = classEmitter.DefineField("____services__", typeof(IServiceProvider), FieldAttributes.Private | FieldAttributes.InitOnly | FieldAttributes.NotSerialized);
            var instanceAst = classEmitter.DefineField("____instance__", serviceType, FieldAttributes.Private | FieldAttributes.InitOnly | FieldAttributes.NotSerialized);

            var constructorEmitter = classEmitter.DefineConstructor(MethodAttributes.Public);

            var servicesEmitter = constructorEmitter.DefineParameter(typeof(IServiceProvider), ParameterAttributes.None, "services");
            var parameterEmitter = constructorEmitter.DefineParameter(serviceType, ParameterAttributes.None, "instance");

            constructorEmitter.Append(Assign(servicesAst, servicesEmitter));
            constructorEmitter.Append(Assign(instanceAst, parameterEmitter));

            constructorEmitter.InvokeBaseConstructor(constructorInfo);

            return OverrideType(classEmitter, instanceAst, servicesAst, serviceType);
        }
    }
}
