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
    /// 属性。
    /// </summary>
    [DebuggerDisplay("{DebuggerView}")]
    public class PropertyEmitter : MemberExpression
    {
        private MethodEmitter _Getter;
        private MethodEmitter _Setter;
        private readonly List<CustomAttributeBuilder> customAttributes = new List<CustomAttributeBuilder>();

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="name">属性的名称。</param>
        /// <param name="attributes">属性的特性。</param>
        /// <param name="returnType">属性的返回类型。</param>
        public PropertyEmitter(string name, PropertyAttributes attributes, Type returnType) : this(name, attributes, returnType, null)
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="name">属性的名称。</param>
        /// <param name="attributes">属性的特性。</param>
        /// <param name="returnType">属性的返回类型。</param>
        /// <param name="parameterTypes">属性的参数类型。</param>
        public PropertyEmitter(string name, PropertyAttributes attributes, Type returnType, Type[] parameterTypes) : base(returnType)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));

            Attributes = attributes;
            ParameterTypes = parameterTypes;
        }

        [DebuggerHidden]
        private string DebuggerView
        {
            get
            {
                var sb = new StringBuilder();

                if (_Getter is not null && (_Getter.Attributes & MethodAttributes.Public) == MethodAttributes.Public || _Setter is not null && (_Setter.Attributes & MethodAttributes.Public) == MethodAttributes.Public)
                {
                    sb.Append("public");
                }
                else
                {
                    sb.Append("private");
                }

                sb.Append(" ")
                    .Append(RuntimeType.Name)
                    .Append(Name)
                    .Append('{');

                if (_Getter is null || _Setter is null || (_Getter.Attributes & MethodAttributes.Public) == (_Setter.Attributes & MethodAttributes.Public))
                {
                    if (_Getter is not null)
                    {
                        sb.Append(" get;");
                    }

                    if (_Setter is not null)
                    {
                        sb.Append(" set;");
                    }
                }
                else
                {
                    if ((_Getter.Attributes & MethodAttributes.Public) != MethodAttributes.Public)
                    {
                        sb.Append("private");
                    }

                    sb.Append(" get;");

                    if ((_Setter.Attributes & MethodAttributes.Public) != MethodAttributes.Public)
                    {
                        sb.Append("private");
                    }

                    sb.Append(" set;");
                }

                return sb.Append(" }")
                         .ToString();
            }
        }

        /// <summary>
        /// 属性的名称。
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 属性的特性。
        /// </summary>
        public PropertyAttributes Attributes { private set; get; }

        /// <summary>
        /// 属性的参数类型。
        /// </summary>
        public Type[] ParameterTypes { get; }

        /// <inheritdoc/>
        public override bool CanRead => _Setter is not null;

        /// <inheritdoc/>
        public override bool CanWrite => _Setter is not null;

        private bool? isStatic;

        /// <inheritdoc/>
        public override bool IsStatic => isStatic ?? false;

        private object defaultValue;
        /// <summary>
        /// 默认值。
        /// </summary>
        public object DefaultValue
        {
            get => defaultValue;
            set
            {
                if (value is null)
                {
                    defaultValue = null;

                    Attributes &= ~PropertyAttributes.HasDefault;
                }
                else
                {
                    defaultValue = EmitUtils.SetConstantOfType(value, RuntimeType);

                    Attributes |= PropertyAttributes.HasDefault;
                }
            }
        }

        /// <summary>
        /// 设置Get方法。
        /// </summary>
        /// <param name="getter">数据获取器。</param>
        /// <returns></returns>
        public PropertyEmitter SetGetMethod(MethodEmitter getter)
        {
            if (getter is null)
            {
                throw new ArgumentNullException(nameof(getter));
            }

            if (isStatic.HasValue && isStatic.Value != getter.IsStatic)
            {
                throw new InvalidOperationException();
            }

            if (getter.IsStatic)
            {
                isStatic = true;
            }

            _Getter = getter;

            return this;
        }
        /// <summary>
        /// 创建Set方法。
        /// </summary>
        /// <param name="setter">数据设置器。</param>
        /// <returns></returns>
        public PropertyEmitter SetSetMethod(MethodEmitter setter)
        {
            if (setter is null)
            {
                throw new ArgumentNullException(nameof(setter));
            }

            if (isStatic.HasValue && isStatic.Value != setter.IsStatic)
            {
                throw new InvalidOperationException();
            }

            if (setter.IsStatic)
            {
                isStatic = true;
            }

            _Setter = setter;

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

            customAttributes.Add(EmitUtils.CreateCustomAttribute(attributeData));
        }

        /// <summary>
        /// 设置属性标记。
        /// </summary>
        /// <param name="customBuilder">属性。</param>
        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            if (customBuilder is null)
            {
                throw new ArgumentNullException(nameof(customBuilder));
            }

            customAttributes.Add(customBuilder);
        }

        /// <summary>
        /// 发行。
        /// </summary>
        /// <param name="builder">类型构造器。</param>
        public void Emit(PropertyBuilder builder)
        {
            if (_Getter is null && _Setter is null)
            {
                throw new InvalidOperationException("属性不能既不可读，也不可写!");
            }

            if (_Getter != null)
            {
                builder.SetGetMethod((MethodBuilder)_Getter.Value);
            }

            if (_Setter != null)
            {
                builder.SetSetMethod((MethodBuilder)_Setter.Value);
            }

            if (Attributes.HasFlag(PropertyAttributes.HasDefault))
            {
                builder.SetConstant(defaultValue);
            }

            foreach (var item in customAttributes)
            {
                builder.SetCustomAttribute(item);
            }
        }

        /// <summary>
        /// 获取成员数据。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            if (_Getter is null)
            {
                throw new AstException($"属性“{Name}”不可读!");
            }

            if (!IsStatic)
            {
                ilg.Emit(OpCodes.Ldarg_0);
            }

            ilg.Emit(OpCodes.Callvirt, _Getter.Value);
        }

        /// <summary>
        /// 赋值。
        /// </summary>
        /// <param name="ilg">指令。</param>
        /// <param name="value">值。</param>
        protected override void Assign(ILGenerator ilg, Expression value)
        {
            if (_Setter is null)
            {
                throw new AstException($"属性“{Name}”不可写!");
            }

            if (!IsStatic)
            {
                ilg.Emit(OpCodes.Ldarg_0);
            }

            value.Load(ilg);

            ilg.Emit(OpCodes.Call, _Setter.Value);
        }
    }
}
