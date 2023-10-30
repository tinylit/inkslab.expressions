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
        private int parameterIndex = 0;
        private MethodBuilder methodBuilder;

        private readonly List<ParameterEmitter> parameters = new List<ParameterEmitter>();
        private readonly List<CustomAttributeBuilder> customAttributes = new List<CustomAttributeBuilder>();

        private class InitMethodEmitter : MethodEmitter
        {
            private readonly Type[] typeArguments;
            private readonly MethodEmitter methodEmitter;

            public InitMethodEmitter(MethodEmitter methodEmitter, Type[] typeArguments) : base(methodEmitter.Name, methodEmitter.Attributes, methodEmitter.RuntimeType)
            {
                this.methodEmitter = methodEmitter;
                this.typeArguments = typeArguments;
            }

            internal override MethodInfo Value => methodEmitter.Value.MakeGenericMethod(typeArguments);

            public override ParameterEmitter[] GetParameters() => methodEmitter.GetParameters();

            public override bool IsGenericMethod => methodEmitter.IsGenericMethod;

            public override Type[] GetGenericArguments() => typeArguments;

            public override ParameterEmitter DefineParameter(Type parameterType, ParameterAttributes attributes, string name)
            {
                return methodEmitter.DefineParameter(parameterType, attributes, name);
            }

            public override MethodEmitter MakeGenericMethod(params Type[] typeArguments)
            {
                return methodEmitter.MakeGenericMethod(typeArguments);
            }

            public override void SetCustomAttribute(CustomAttributeBuilder customBuilder)
            {
                methodEmitter.SetCustomAttribute(customBuilder);
            }
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="name">方法的名称。</param>
        /// <param name="attributes">方法的属性。</param>
        /// <param name="returnType">方法的返回类型。</param>
        public MethodEmitter(string name, MethodAttributes attributes, Type returnType) : base(returnType)
        {
            Name = name;
            Attributes = attributes;
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

                foreach (ParameterEmitter parameter in parameters)
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
        [DebuggerHidden]
        internal virtual MethodInfo Value
        {
            get
            {
                if (methodBuilder is null)
                {
                    throw new NotImplementedException();
                }

                var declaringType = methodBuilder.DeclaringType;

                if (declaringType.IsGenericType)
                {
                    return TypeBuilder.GetMethod(declaringType, methodBuilder);
                }

                return methodBuilder;
            }
        }

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
        public virtual ParameterEmitter[] GetParameters() => parameters.ToArray();

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
            var parameter = new ParameterEmitter(parameterType, (Attributes & MethodAttributes.Static) == MethodAttributes.Static ? parameterIndex++ : ++parameterIndex, attributes, name);

            parameters.Add(parameter);

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

            customAttributes.Add(customBuilder);
        }

        /// <summary>
        /// 发行方法。
        /// </summary>
        /// <param name="methodBuilder">方法。</param>
        protected virtual void Emit(MethodBuilder methodBuilder)
        {
            this.methodBuilder = methodBuilder ?? throw new ArgumentNullException(nameof(methodBuilder));

            foreach (var parameter in parameters)
            {
                parameter.Emit(methodBuilder.DefineParameter(parameter.Position, parameter.Attributes, parameter.ParameterName));
            }

            foreach (var customAttribute in customAttributes)
            {
                methodBuilder.SetCustomAttribute(customAttribute);
            }

            var ilg = methodBuilder.GetILGenerator();

            Load(methodBuilder.GetILGenerator());

            ilg.Emit(OpCodes.Ret);
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

            int index = 0;

            var parameterTypes = new Type[parameters.Count];

            foreach (var parameterEmitter in parameters)
            {
                parameterTypes[index++] = parameterEmitter.RuntimeType;
            }

            Emit(builder.DefineMethod(Name, Attributes, CallingConventions.Standard, RuntimeType, parameterTypes));
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

                label.MarkLabel(ilg);

                variable.Load(ilg);
            }
        }
    }
}
