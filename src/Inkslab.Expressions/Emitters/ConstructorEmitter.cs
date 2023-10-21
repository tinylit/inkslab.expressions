using Inkslab.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace Inkslab.Emitters
{
    /// <summary>
    /// 构造函数。
    /// </summary>
    public class ConstructorEmitter : BlockExpression
    {
        private static readonly ParameterEmitter[] EmptyParameters = new ParameterEmitter[0];

        private int parameterIndex = 0;
        private readonly TypeBuilder typeBuilder;
        private readonly List<ParameterEmitter> parameters = new List<ParameterEmitter>();

        private class ConstructorExpression : Expression
        {
            private readonly ConstructorInfo constructor;
            private readonly Expression[] parameters;

            public ConstructorExpression(ConstructorInfo constructor) : base(constructor.DeclaringType)
            {
                this.constructor = constructor ?? throw new ArgumentNullException(nameof(constructor));
            }
            public ConstructorExpression(ConstructorInfo constructor, Expression[] parameters) : this(constructor)
            {
                ArgumentsCheck(constructor, parameters);

                this.parameters = parameters;
            }

            private static void ArgumentsCheck(ConstructorInfo constructorInfo, Expression[] arguments)
            {
                var parameterInfos = constructorInfo.GetParameters();

                if (arguments?.Length != parameterInfos.Length)
                {
                    throw new AstException("指定参数和构造函数参数个数不匹配!");
                }

                if (!parameterInfos.Zip(arguments, (x, y) =>
                {
                    return EmitUtils.IsAssignableFromSignatureTypes(x.ParameterType, y.RuntimeType);

                }).All(x => x))
                {
                    throw new AstException("指定参数和构造函数参数类型不匹配!");
                }
            }

            public override void Load(ILGenerator ilg)
            {
                ilg.Emit(OpCodes.Ldarg_0);

                if (parameters?.Length > 0)
                {
                    foreach (var expression in parameters)
                    {
                        expression.Load(ilg);
                    }
                }

                ilg.Emit(OpCodes.Call, constructor);
            }
        }

        private class InitConstructorEmitter : ConstructorEmitter
        {
            private readonly Type[] typeArguments;
            private readonly ConstructorEmitter constructorEmitter;

            public InitConstructorEmitter(ConstructorEmitter constructorEmitter, Type[] typeArguments) : base(constructorEmitter.typeBuilder, constructorEmitter.Attributes, constructorEmitter.Conventions)
            {
                this.constructorEmitter = constructorEmitter;
                this.typeArguments = typeArguments;
            }

            internal override ConstructorInfo Value
            {
                get
                {
                    var constructorBuilder = constructorEmitter.constructorBuilder ?? throw new NotImplementedException();

                    var typeBuilder = constructorEmitter.typeBuilder;

                    var declaringType = typeBuilder.DeclaringType;

                    if (declaringType is null || !declaringType.IsGenericType)
                    {
                        return TypeBuilder.GetConstructor(typeBuilder.MakeGenericType(typeArguments), constructorBuilder);
                    }

                    var genericArguments = declaringType.GetGenericArguments();

                    var typeGenericArguments = new Type[genericArguments.Length + typeArguments.Length];

                    System.Array.Copy(genericArguments, typeGenericArguments, genericArguments.Length);

                    System.Array.Copy(typeArguments, 0, typeGenericArguments, genericArguments.Length, typeArguments.Length);

                    return TypeBuilder.GetConstructor(typeBuilder.MakeGenericType(typeGenericArguments), constructorBuilder);
                }
            }

            public override ParameterEmitter[] GetParameters() => constructorEmitter.GetParameters();

            public override ParameterEmitter DefineParameter(Type parameterType, ParameterAttributes attributes, string parameterName)
            {
                return constructorEmitter.DefineParameter(parameterType, attributes, parameterName);
            }
            public override void InvokeBaseConstructor(ConstructorInfo constructor, params Expression[] parameters)
            {
                constructorEmitter.InvokeBaseConstructor(constructor, parameters);
            }
            public override ConstructorEmitter MakeGenericConstructor(params Type[] typeArguments)
            {
                return constructorEmitter.MakeGenericConstructor(typeArguments);
            }
        }

        private ConstructorBuilder constructorBuilder;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="typeBuilder">父类型。</param>
        /// <param name="attributes">属性。</param>
        public ConstructorEmitter(TypeBuilder typeBuilder, MethodAttributes attributes) : this(typeBuilder, attributes, CallingConventions.Standard)
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="typeBuilder">父类型。</param>
        /// <param name="attributes">属性。</param>
        /// <param name="conventions">调用约定。</param>
        public ConstructorEmitter(TypeBuilder typeBuilder, MethodAttributes attributes, CallingConventions conventions) : base(typeBuilder)
        {
            this.typeBuilder = typeBuilder;
            Attributes = attributes;
            Conventions = conventions;
        }

        /// <summary>
        /// 方法的名称。
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 方法的属性。
        /// </summary>
        public MethodAttributes Attributes { get; }

        /// <summary>
        /// 调用约定。
        /// </summary>
        public CallingConventions Conventions { get; }

        /// <summary>
        /// 成员。
        /// </summary>
        internal virtual ConstructorInfo Value
        {
            get
            {
                if (constructorBuilder is null)
                {
                    throw new NotImplementedException();
                }

                var declaringType = typeBuilder.DeclaringType;

                if (declaringType is null || !declaringType.IsGenericType)
                {
                    return constructorBuilder;
                }

                return TypeBuilder.GetConstructor(typeBuilder.MakeGenericType(declaringType.GetGenericArguments()), constructorBuilder);
            }
        }

        private ParameterEmitter[] _parameters = EmptyParameters;

        /// <summary>
        /// 参数。
        /// </summary>
        public virtual ParameterEmitter[] GetParameters()
        {
            if (parameters.Count > _parameters.Length)
            {
                _parameters = parameters.ToArray();
            }

            return _parameters;
        }

        /// <summary>
        /// 声明参数。
        /// </summary>
        /// <param name="parameterInfo">参数。</param>
        /// <returns></returns>
        public ParameterEmitter DefineParameter(ParameterInfo parameterInfo)
        {
            var parameter = DefineParameter(parameterInfo.ParameterType, parameterInfo.Attributes, parameterInfo.Name);

            if (parameterInfo.HasDefaultValue)
            {
                parameter.SetConstant(parameterInfo.DefaultValue);
            }

            foreach (var customAttribute in parameterInfo.CustomAttributes)
            {
                parameter.SetCustomAttribute(customAttribute);
            }

            return parameter;
        }

        /// <summary>
        /// 声明参数。
        /// </summary>
        /// <param name="parameterType">参数类型。</param>
        /// <param name="attributes">属性。</param>
        /// <param name="parameterName">名称。</param>
        /// <returns></returns>
        public virtual ParameterEmitter DefineParameter(Type parameterType, ParameterAttributes attributes, string parameterName)
        {
            var parameter = new ParameterEmitter(parameterType, ++parameterIndex, attributes, parameterName);

            parameters.Add(parameter);

            return parameter;
        }

        /// <summary>
        /// 返回使用指定泛型类型参数从当前泛型方法定义构造的构造函数。
        /// </summary>
        /// <param name="typeArguments">表示泛型方法的类型参数的 <see cref="Type"/> 对象的数组。</param>
        /// <returns>一个 <see cref="ConstructorEmitter"/>，它表示使用指定泛型类型参数从当前泛型方法定义构造的构造函数。</returns>
        public virtual ConstructorEmitter MakeGenericConstructor(params Type[] typeArguments)
        {
            if (typeArguments?.Length > 0)
            {
                return new InitConstructorEmitter(this, typeArguments);
            }

            return this;
        }

        /// <summary>
        /// 调用父类构造函数。
        /// </summary>
        public void InvokeBaseConstructor()
        {
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var type = typeBuilder.BaseType;

            if (type.IsGenericParameter)
            {
                type = type.GetGenericTypeDefinition();
            }

            InvokeBaseConstructor(type.GetConstructor(flags, null, Type.EmptyTypes, null));
        }

        /// <summary>
        /// 调用父类构造函数。
        /// </summary>
        /// <param name="constructor">构造函数。</param>
        public void InvokeBaseConstructor(ConstructorInfo constructor) => InvokeBaseConstructor(constructor, System.Array.Empty<Expression>());

        /// <summary>
        /// 调用父类构造函数。
        /// </summary>
        /// <param name="constructor">构造函数。</param>
        /// <param name="parameters">参数。</param>
        public virtual void InvokeBaseConstructor(ConstructorInfo constructor, params Expression[] parameters) => Append(new ConstructorExpression(constructor, parameters ?? System.Array.Empty<Expression>()));

        /// <summary>
        /// 发行。
        /// </summary>
        public void Emit(ConstructorBuilder constructorBuilder)
        {
            if (constructorBuilder is null)
            {
                throw new ArgumentNullException(nameof(constructorBuilder));
            }

            this.constructorBuilder = constructorBuilder;

            var attributes = constructorBuilder.MethodImplementationFlags;

            if ((attributes & MethodImplAttributes.Runtime) != MethodImplAttributes.IL)
            {
                return;
            }

            if (IsEmpty)
            {
                InvokeBaseConstructor();
            }

            foreach (var parameter in parameters)
            {
                parameter.Emit(constructorBuilder.DefineParameter(parameter.Position, parameter.Attributes, parameter.ParameterName));
            }

            var ilg = constructorBuilder.GetILGenerator();

            Load(ilg);

            ilg.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// 发行。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            var label = new Label(LabelKind.Return);

            MarkLabel(label);

            base.Load(ilg);

            label.MarkLabel(ilg);
        }
    }
}
