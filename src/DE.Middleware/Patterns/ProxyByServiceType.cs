using Delta.Emitters;
using Delta.Expressions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Delta.Middleware.Patterns
{
    using static Delta.Expression;

    abstract class ProxyByServiceType : IProxyByPattern
    {
        private static readonly Type interceptAttributeType = typeof(InterceptAttribute);
        private static readonly Type noninterceptAttributeType = typeof(NoninterceptAttribute);
        private static readonly Type interceptContextType = typeof(InterceptContext);
        private static readonly ConstructorInfo interceptContextTypeCtor = interceptContextType.GetConstructor(new Type[] { typeof(object), typeof(MethodInfo), typeof(object) });

        private readonly ModuleEmitter moduleEmitter;
        private readonly Type serviceType;

        public ProxyByServiceType(ModuleEmitter moduleEmitter, Type serviceType)
        {
            this.moduleEmitter = moduleEmitter;
            this.serviceType = serviceType;
        }

        public void Config(HashSet<ServiceDescriptor> services)
        {
            throw new NotImplementedException();
        }

        public ServiceDescriptor Ref()
        {
            if (serviceType.IsSealed)
            {
                throw new NotSupportedException("无法代理密封类!");
            }

            if (serviceType.IsInterface)
            {
                return ResolveIsInterface();
            }
            else if (serviceType.IsSealed)
            {
                throw new NotSupportedException($"代理“{serviceType.FullName}”类是密封类!");
            }
            else if (serviceType.IsValueType)
            {
                throw new NotSupportedException($"代理“{serviceType.FullName}”类是值类型!");
            }
            else
            {
                return ResolveIsClass();
            }
        }

        public static MethodEmitter DefineMethodOverride(Expression instanceAst, ClassEmitter classEmitter, MethodInfo methodInfo, IList<CustomAttributeData> attributeDatas)
        {
            var overrideEmitter = classEmitter.DefineMethodOverride(ref methodInfo);

            var paramterEmitters = overrideEmitter.GetParameters();

            var interceptAttributes = new List<CustomAttributeData>();

            foreach (var attributeData in attributeDatas)
            {
                if (interceptAttributeType.IsAssignableFrom(attributeData.AttributeType))
                {
                    interceptAttributes.Add(attributeData);
                }
                else
                {
                    overrideEmitter.SetCustomAttribute(attributeData);
                }
            }

            Expression[] arguments = null;

            var variable = Variable(typeof(object[]));

            overrideEmitter.Append(Assign(variable, Array(paramterEmitters)));

            if (overrideEmitter.IsGenericMethod)
            {
                arguments = new Expression[] { instanceAst, Constant(methodInfo), variable };
            }
            else
            {
                var tokenEmitter = classEmitter.DefineField($"____token__{methodInfo.Name}", typeof(MethodInfo), FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly);

                classEmitter.TypeInitializer.Append(Assign(tokenEmitter, Constant(methodInfo)));

                arguments = new Expression[] { instanceAst, tokenEmitter, variable };
            }

            BlockExpression blockAst;

            if (paramterEmitters.Any(x => x.RuntimeType.IsByRef))
            {
                var finallyAst = Block(typeof(void));

                for (int i = 0; i < paramterEmitters.Length; i++)
                {
                    var paramterEmitter = paramterEmitters[i];

                    if (!paramterEmitter.IsByRef)
                    {
                        continue;
                    }

                    finallyAst.Append(Assign(paramterEmitter, Convert(ArrayIndex(variable, i), paramterEmitter.ParameterType)));
                }

                blockAst = Try(methodInfo.ReturnType, finallyAst);
            }
            else
            {
                blockAst = Block(methodInfo.ReturnType);
            }

            NewInstanceExpression contextAst = New(interceptContextTypeCtor, arguments);



            if (overrideEmitter.ReturnType.IsClass && typeof(Task).IsAssignableFrom(overrideEmitter.ReturnType))
            {
                if (overrideEmitter.ReturnType.IsGenericType)
                {
                    blockAst.Append(Call(InterceptAsyncGenericMethodCall.MakeGenericMethod(overrideEmitter.ReturnType.GetGenericArguments()), New(InterceptContextCtor, arguments)));
                }
                else
                {
                    blockAst.Append(Call(InterceptAsyncMethodCall, New(InterceptContextCtor, arguments)));
                }
            }
            else if (overrideEmitter.ReturnType == typeof(void))
            {
                blockAst.Append(Call(InterceptMethodCall, New(InterceptContextCtor, arguments)));
            }
            else
            {
                blockAst.Append(Call(InterceptGenericMethodCall.MakeGenericMethod(overrideEmitter.ReturnType), New(InterceptContextCtor, arguments)));
            }

            overrideEmitter.Append(blockAst);

            return overrideEmitter;
        }

        private ServiceDescriptor ResolveIsInterface()
        {
            throw new Exception();
        }

        private ServiceDescriptor ResolveIsClass()
        {
            throw new Exception();
        }
    }
}
