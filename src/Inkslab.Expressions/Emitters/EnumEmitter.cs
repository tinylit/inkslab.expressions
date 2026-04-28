using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Security;

namespace Inkslab.Emitters
{
    /// <summary>
    /// 枚举发射器。
    /// </summary>
    public class EnumEmitter
    {
        private bool _isCreated = false;

        private readonly INamingScope _namingScope;

        private readonly EnumBuilder _enumBuilder;

        private readonly Dictionary<string, FieldEmitter> _fields = new Dictionary<string, FieldEmitter>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 在给定类型名称、类型特性和已定义类型扩展的类型的情况下，构造 TypeBuilder。
        /// </summary>
        /// <param name="moduleEmitter">模块。</param>
        /// <param name="name">类型的完整路径。 name 不能包含嵌入的 null。</param>
        /// <param name="attributes">与类型关联的属性。</param>
        /// <param name="underlyingType"></param>
        public EnumEmitter(ModuleEmitter moduleEmitter, string name, TypeAttributes attributes, Type underlyingType)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException($"'{nameof(name)}' cannot be null or empty.", nameof(name));
            }

            _namingScope = moduleEmitter.BeginScope();
            _enumBuilder = ModuleEmitter.DefineEnum(moduleEmitter, name, attributes, underlyingType);
        }

        /// <summary>
        /// 设置属性标记。
        /// </summary>
        [ComVisible(true)]
        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            _enumBuilder.SetCustomAttribute(con, binaryAttribute);
        }

        /// <summary>
        /// 自定义标记。
        /// </summary>
        /// <param name="attribute">标记。</param>
        public void DefineCustomAttribute(CustomAttributeBuilder attribute)
        {
            _enumBuilder.SetCustomAttribute(attribute);
        }

        /// <summary>
        /// 自定义标记。
        /// </summary>
        /// <typeparam name="TAttribute">标记类型。</typeparam>
        public void DefineCustomAttribute<TAttribute>() where TAttribute : Attribute, new() => DefineCustomAttribute(EmitUtils.CreateCustomAttribute<TAttribute>());

        /// <summary>
        /// 自定义标记。
        /// </summary>
        /// <param name="attributeData">标记信息参数。</param>
        public void DefineCustomAttribute(CustomAttributeData attributeData) => DefineCustomAttribute(EmitUtils.CreateCustomAttribute(attributeData));

        /// <summary>
        /// 定义枚举成员。
        /// </summary>
        /// <param name="name">枚举项。</param>
        /// <param name="value">枚举值。</param>
        /// <returns>字段。</returns>
        public FieldEmitter DefineLiteral(string name, object value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value), "枚举字面量类型不允许设置空值。");
            }

            name = _namingScope.GetUniqueName(name);

            var fieldEmitter = new FieldEmitter(name, _enumBuilder.UnderlyingSystemType, FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal)
            {
                DefaultValue = value
            };

            _fields.Add(name, fieldEmitter);

            return fieldEmitter;
        }

        /// <summary>
        /// 发行。
        /// </summary>
        public Type CreateType()
        {
            if (_isCreated)
            {
                throw new InvalidOperationException("枚举类型已创建，不能重复创建。");
            }

            _isCreated = true;

            foreach (FieldEmitter emitter in _fields.Values)
            {
                var fieldDefinition = _enumBuilder.DefineLiteral(emitter.Name, emitter.DefaultValue);

                emitter.Emit(fieldDefinition);
            }

#if NETSTANDARD2_0_OR_GREATER
            return _enumBuilder.CreateTypeInfo().AsType();
#else
            return _enumBuilder.CreateType();
#endif
        }

        /// <summary>
        /// 是否已创建。
        /// </summary>
        /// <returns></returns>
        public bool IsCreated() => _isCreated;
    }
}