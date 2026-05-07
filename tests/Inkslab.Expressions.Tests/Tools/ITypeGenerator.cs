using System;

namespace Inkslab.Expressions.Tests
{
    /// <summary>
    ///     类型生成器接口。
    /// </summary>
    public interface ITypeGenerator
    {
        /// <summary>
        ///     创建类型。
        /// </summary>
        Type CreateType(string namePrefix, MemberInDto[] members, Type baseType = null);
    }
}