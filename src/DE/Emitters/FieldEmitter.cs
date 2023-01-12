using Delta.Expressions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace Delta.Emitters
{
    /// <summary>
    /// 字段。
    /// </summary>
    [DebuggerDisplay("{RuntimeType.Name} {Name}")]
    public class FieldEmitter : MemberExpression
    {
        private FieldBuilder builder;
        private object defaultValue;
        private bool hasDefaultValue = false;
        private readonly List<CustomAttributeBuilder> customAttributes = new List<CustomAttributeBuilder>();

        /// <summary>
        /// 字段。
        /// </summary>
        /// <param name="name">字段的名称。</param>
        /// <param name="returnType">字段的返回类型。</param>
        /// <param name="attributes">字段的属性。</param>
        public FieldEmitter(string name, Type returnType, FieldAttributes attributes) : this(name, returnType, attributes, (attributes & FieldAttributes.Static) == FieldAttributes.Static)
        {
        }

        private FieldEmitter(string name, Type returnType, FieldAttributes attributes, bool isStatic) : base(returnType)
        {
            Name = name;
            Attributes = attributes;
        }

        /// <summary>
        /// 字段的名称。
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 字段的属性。
        /// </summary>
        public FieldAttributes Attributes { get; }

        /// <inheritdoc/>
        public override bool CanWrite => true;

        /// <inheritdoc/>
        public override bool IsStatic => (Attributes & FieldAttributes.Static) == FieldAttributes.Static;

        /// <summary>
        /// 设置默认值。
        /// </summary>
        /// <param name="defaultValue">默认值。</param>
        public void SetConstant(object defaultValue)
        {
            hasDefaultValue = true;

            this.defaultValue = EmitUtils.SetConstantOfType(defaultValue, RuntimeType);
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

        /// <inheritdoc/>
        public override void Load(ILGenerator ilg)
        {
            if (IsStatic)
            {
                ilg.Emit(OpCodes.Ldsfld, builder);
            }
            else
            {
                ilg.Emit(OpCodes.Ldarg_0);

                ilg.Emit(OpCodes.Ldfld, builder);
            }
        }

        /// <inheritdoc/>
        protected override void Assign(ILGenerator ilg, Expression value)
        {
            if (IsStatic)
            {
                value.Load(ilg);

                ilg.Emit(OpCodes.Stsfld, builder);
            }
            else
            {
                ilg.Emit(OpCodes.Ldarg_0);

                value.Load(ilg);

                ilg.Emit(OpCodes.Stfld, builder);
            }
        }

        /// <summary>
        /// 发行。
        /// </summary>
        /// <param name="builder">属性构造器。</param>
        public void Emit(FieldBuilder builder)
        {
            this.builder = builder ?? throw new ArgumentNullException(nameof(builder));

            if (hasDefaultValue)
            {
                builder.SetConstant(defaultValue);
            }

            foreach (var item in customAttributes)
            {
                builder.SetCustomAttribute(item);
            }
        }
    }
}
