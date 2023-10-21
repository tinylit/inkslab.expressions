using System;
using System.Reflection;

namespace Inkslab.Emitters
{
    /// <summary>
    /// 类。
    /// </summary>
    public sealed class ClassEmitter : AbstractTypeEmitter
    {
        /// <summary>
        /// 在此模块中用指定的名称为私有类型构造 TypeBuilder。
        /// </summary>
        /// <param name="moduleEmitter">模块。</param>
        /// <param name="name">类型的完整路径，其中包括命名空间。 name 不能包含嵌入的 null。</param>
        public ClassEmitter(ModuleEmitter moduleEmitter, string name) : base(moduleEmitter, name)
        {

        }

        /// <summary>
        /// 在给定类型名称和类型特性的情况下，构造 TypeBuilder。
        /// </summary>
        /// <param name="moduleEmitter">模块。</param>
        /// <param name="name">类型的完整路径。 name 不能包含嵌入的 null。</param>
        /// <param name="attributes">已定义类型的属性。</param>
        public ClassEmitter(ModuleEmitter moduleEmitter, string name, TypeAttributes attributes) : base(moduleEmitter, name, attributes)
        {

        }

        /// <summary>
        /// 在给定类型名称、类型特性和已定义类型扩展的类型的情况下，构造 TypeBuilder。
        /// </summary>
        /// <param name="moduleEmitter">模块。</param>
        /// <param name="name">类型的完整路径。 name 不能包含嵌入的 null。</param>
        /// <param name="attributes">与类型关联的属性。</param>
        /// <param name="baseType">已定义类型扩展的类型。</param>
        public ClassEmitter(ModuleEmitter moduleEmitter, string name, TypeAttributes attributes, Type baseType) : base(moduleEmitter, name, attributes, baseType)
        {

        }

        /// <summary>
        /// 在给定类型名称、特性、已定义类型扩展的类型和已定义类型实现的接口的情况下，构造 TypeBuilder。
        /// </summary>
        /// <param name="moduleEmitter">模块。</param>
        /// <param name="name">类型的完整路径。 name 不能包含嵌入的 null。</param>
        /// <param name="attributes">与类型关联的特性。</param>
        /// <param name="baseType">已定义类型扩展的类型。</param>
        /// <param name="interfaces">类型实现的接口列表。</param>
        public ClassEmitter(ModuleEmitter moduleEmitter, string name, TypeAttributes attributes, Type baseType, Type[] interfaces) : base(moduleEmitter, name, attributes, baseType, interfaces)
        {

        }

        /// <summary>
        /// 创建类型。
        /// </summary>
        /// <returns></returns>
        public Type CreateType() => Emit();
    }
}
