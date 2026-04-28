using Inkslab.Expressions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Inkslab.Emitters
{
    /// <summary>
    /// 方法。
    /// </summary>
    [DebuggerDisplay("{DebuggerView}")]
    public class MethodEmitter : BlockExpression
    {
        private int _parameterIndex = 0;
        private MethodBuilder _methodBuilder;

        private readonly List<ParameterEmitter> _parameters = new List<ParameterEmitter>();
        private readonly List<CustomAttributeBuilder> _customAttributes = new List<CustomAttributeBuilder>();

        private class InitMethodEmitter : MethodEmitter
        {
            private readonly Type[] _typeArguments;
            private readonly MethodEmitter _methodEmitter;

            public InitMethodEmitter(MethodEmitter methodEmitter, Type[] typeArguments) : base(methodEmitter.DeclaringType, methodEmitter.Name, methodEmitter.Attributes, methodEmitter.RuntimeType)
            {
                this._methodEmitter = methodEmitter;
                this._typeArguments = typeArguments;
            }

            internal override MethodInfo Value => _methodEmitter.Value.MakeGenericMethod(_typeArguments);

            public override ParameterEmitter[] GetParameters() => _methodEmitter.GetParameters();

            public override bool IsGenericMethod => _methodEmitter.IsGenericMethod;

            public override Type[] GetGenericArguments() => _typeArguments;

            public override ParameterEmitter DefineParameter(Type parameterType, ParameterAttributes attributes, string name)
            {
                return _methodEmitter.DefineParameter(parameterType, attributes, name);
            }

            public override MethodEmitter MakeGenericMethod(params Type[] typeArguments)
            {
                return _methodEmitter.MakeGenericMethod(typeArguments);
            }

            public override void SetCustomAttribute(CustomAttributeBuilder customBuilder)
            {
                _methodEmitter.SetCustomAttribute(customBuilder);
            }
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="declaringType">声明类型。</param>
        /// <param name="name">方法的名称。</param>
        /// <param name="attributes">方法的属性。</param>
        /// <param name="returnType">方法的返回类型。</param>
        public MethodEmitter(AbstractTypeEmitter declaringType, string name, MethodAttributes attributes, Type returnType) : base(declaringType?.MapReturnTypeForEmit(returnType))
        {
            DeclaringType = declaringType;
            Name = name;
            Attributes = attributes;
        }

        /// <summary>
        /// 构造函数（接收已预定义的 <see cref="MethodBuilder"/>）。
        /// </summary>
        /// <remarks>
        /// 供已持有外部定义的方法构造器的派生类（如重写方法）使用；
        /// 预赋值后 <see cref="DefineMethod"/> 将因幂等检查自动跳过创建逻辑，
        /// 且 <see cref="Value"/> 从构造期起即可返回有效 <see cref="MethodInfo"/>。
        /// </remarks>
        /// <param name="declaringType">声明类型。</param>
        /// <param name="methodBuilder">已预定义的方法构造器。</param>
        /// <param name="returnType">方法的返回类型。</param>
        protected MethodEmitter(AbstractTypeEmitter declaringType, MethodBuilder methodBuilder, Type returnType) : base(declaringType?.MapReturnTypeForEmit(returnType))
        {
            if (methodBuilder is null)
            {
                throw new ArgumentNullException(nameof(methodBuilder));
            }

            DeclaringType = declaringType;
            Name = methodBuilder.Name;
            Attributes = methodBuilder.Attributes;
            this._methodBuilder = methodBuilder;
        }

        [DebuggerHidden]
        private string DebuggerView
        {
            get
            {
                var sb = new StringBuilder();

                if ((Attributes & MethodAttributes.Public) == MethodAttributes.Public)
                {
                    sb.Append("public");
                }
                else
                {
                    sb.Append("private");
                }

                sb.Append(" ")
                    .Append(RuntimeType.Name)
                    .Append(" ")
                    .Append(Name)
                    .Append('(');

                bool flag = false;

                foreach (ParameterEmitter parameter in _parameters)
                {
                    if (flag)
                    {
                        sb.Append(", ");
                    }
                    else
                    {
                        flag = true;
                    }

                    sb.Append(parameter.ParameterType.Name)
                        .Append(" ")
                        .Append(parameter.ParameterName);
                }

                sb.Append(')')
                    .AppendLine()
                    .Append('{')
                    .Append(" /*TODO:somethings ...*/")
                    .AppendLine()
                    .Append('}');

                return sb.ToString();
            }
        }

        /// <summary>
        /// 是否为泛型方法。
        /// </summary>
        public virtual bool IsGenericMethod => false;

        /// <summary>
        /// 返回值类型。
        /// </summary>
        public Type ReturnType => RuntimeType;

        /// <summary>
        /// 泛型参数。
        /// </summary>
        /// <returns></returns>
        public virtual Type[] GetGenericArguments() => null;

        /// <summary>
        /// 成员。
        /// </summary>
        /// <remarks>
        /// 若在调用 <see cref="DefineMethod"/> 之前访问（例如另一个方法体发射期间前向引用），
        /// 将自动使用 <see cref="DeclaringType"/> 的 <see cref="TypeBuilder"/> 进行惰性定义。
        /// </remarks>
        [DebuggerHidden]
        internal virtual MethodInfo Value
        {
            get
            {
                if (_methodBuilder is null)
                {
                    DefineMethod(DeclaringType.Value);
                }

                var declaringType = _methodBuilder.DeclaringType;

                if (declaringType.IsGenericType)
                {
                    return TypeBuilder.GetMethod(declaringType, _methodBuilder);
                }

                return _methodBuilder;
            }
        }

        /// <summary>
        /// 声明类型。
        /// </summary>
        public AbstractTypeEmitter DeclaringType { get; }

        /// <summary>
        /// 方法的名称。
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 是否为静态方法。
        /// </summary>
        public bool IsStatic => (Attributes & MethodAttributes.Static) == MethodAttributes.Static;

        /// <summary>
        /// 方法的属性。
        /// </summary>
        public MethodAttributes Attributes { get; }

        /// <summary>
        /// 参数。
        /// </summary>
        public virtual ParameterEmitter[] GetParameters() => _parameters.ToArray();

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
        /// <param name="strParamName">名称。</param>
        /// <returns></returns>
        public ParameterEmitter DefineParameter(Type parameterType, string strParamName) => DefineParameter(parameterType, ParameterAttributes.None, strParamName);

        /// <summary>
        /// 声明参数。
        /// </summary>
        /// <param name="parameterType">参数类型。</param>
        /// <param name="attributes">属性。</param>
        /// <param name="name">名称。</param>
        /// <returns></returns>
        public virtual ParameterEmitter DefineParameter(Type parameterType, ParameterAttributes attributes, string name)
        {
            var parameter = new ParameterEmitter(DeclaringType.MapReturnTypeForEmit(parameterType), (Attributes & MethodAttributes.Static) == MethodAttributes.Static ? _parameterIndex++ : ++_parameterIndex, attributes, name);

            _parameters.Add(parameter);

            return parameter;
        }

        /// <summary>
        /// 返回使用指定泛型类型参数从当前泛型方法定义构造的泛型方法。
        /// </summary>
        /// <param name="typeArguments">表示泛型方法的类型参数的 <see cref="Type"/> 对象的数组。</param>
        /// <returns>一个 <see cref="MethodEmitter"/>，它表示使用指定泛型类型参数从当前泛型方法定义构造的泛型方法。</returns>
        public virtual MethodEmitter MakeGenericMethod(params Type[] typeArguments)
        {
            if (typeArguments?.Length > 0)
            {
                return new InitMethodEmitter(this, typeArguments);
            }

            return this;
        }

        /// <summary>
        /// 自定义标记。
        /// </summary>
        /// <typeparam name="TAttribute">标记类型。</typeparam>
        public void SetCustomAttribute<TAttribute>() where TAttribute : Attribute, new() => SetCustomAttribute(EmitUtils.CreateCustomAttribute<TAttribute>());

        /// <summary>
        /// 设置属性标记。
        /// </summary>
        /// <param name="attributeData">属性。</param>
        public void SetCustomAttribute(CustomAttributeData attributeData)
        {
            if (attributeData is null)
            {
                throw new ArgumentNullException(nameof(attributeData));
            }

            SetCustomAttribute(EmitUtils.CreateCustomAttribute(attributeData));
        }

        /// <summary>
        /// 设置属性标记。
        /// </summary>
        /// <param name="customBuilder">属性。</param>
        public virtual void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            if (customBuilder is null)
            {
                throw new ArgumentNullException(nameof(customBuilder));
            }

            _customAttributes.Add(customBuilder);
        }

        /// <summary>
        /// 发行方法。
        /// </summary>
        /// <param name="methodBuilder">方法。</param>
        protected virtual void Emit(MethodBuilder methodBuilder)
        {
            this._methodBuilder = methodBuilder ?? throw new ArgumentNullException(nameof(methodBuilder));

            foreach (var parameter in _parameters)
            {
                parameter.Emit(methodBuilder.DefineParameter(parameter.Position, parameter.Attributes, parameter.ParameterName));
            }

            foreach (var customAttribute in _customAttributes)
            {
                methodBuilder.SetCustomAttribute(customAttribute);
            }

            var ilg = methodBuilder.GetILGenerator();

            Load(methodBuilder.GetILGenerator());

            ilg.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// 在 <see cref="TypeBuilder"/> 上预定义方法（仅声明签名，不发射方法体）。
        /// </summary>
        /// <remarks>
        /// 该方法用于支持方法之间的相互引用：在所有方法体发射之前先完成全部 <see cref="MethodBuilder"/> 的定义，
        /// 这样 <see cref="MethodCallEmitter"/> 在发射时即可拿到目标 <see cref="MethodInfo"/>。
        /// 通过接收 <see cref="MethodBuilder"/> 的保护构造函数创建的实例会因幂等检查自动跳过。
        /// </remarks>
        /// <param name="builder">类型构造器。</param>
        public void DefineMethod(TypeBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (_methodBuilder is not null)
            {
                return;
            }

            int index = 0;

            var parameterTypes = new Type[_parameters.Count];

            foreach (var parameterEmitter in _parameters)
            {
                parameterTypes[index++] = parameterEmitter.RuntimeType;
            }

            _methodBuilder = builder.DefineMethod(Name, Attributes, CallingConventions.Standard, RuntimeType, parameterTypes);
        }

        /// <summary>
        /// 发行。
        /// </summary>
        /// <param name="builder">构造器。</param>
        public virtual void Emit(TypeBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            DefineMethod(builder);

            Emit(_methodBuilder);
        }

        /// <summary>
        /// 发行。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            var label = new Label(LabelKind.Return);

            MarkLabel(label);

            if (IsVoid)
            {
                base.Load(ilg);

                label.MarkLabel(ilg);
            }
            else
            {
                var variable = Variable(ReturnType);

                StoredLocal(variable);

                base.Load(ilg);

                if (!IsClosed)
                {
                    variable.Storage(ilg);
                }

                label.MarkLabel(ilg);

                variable.Load(ilg);
            }
        }
    }
}
