using System.ComponentModel.DataAnnotations;

namespace Inkslab.Expressions.Tests
{
    /// <summary>
    /// 类型成员类型。
    /// </summary>
    public enum EnumMemberType
    {
        /// <summary>
        ///     布尔类型，表示 true 或 false 值。
        /// </summary>
        Boolean = 1,

        /// <summary>
        ///     字符类型，表示 0 到 65535 之间的无符号 16 位整数，对应 Unicode 字符集。
        /// </summary>
        Char = 2,

        /// <summary>
        ///     有符号 8 位整数，值范围为 -128 到 127。
        /// </summary>
        SByte = 3,

        /// <summary>
        ///     无符号 8 位整数，值范围为 0 到 255。
        /// </summary>
        Byte = 4,

        /// <summary>
        ///     有符号 16 位整数，值范围为 -32768 到 32767。
        /// </summary>
        Int16 = 5,

        /// <summary>
        ///     无符号 16 位整数，值范围为 0 到 65535。
        /// </summary>
        UInt16 = 6,

        /// <summary>
        ///     有符号 32 位整数，值范围为 -2147483648 到 2147483647。
        /// </summary>
        Int32 = 7,

        /// <summary>
        ///     无符号 32 位整数，值范围为 0 到 4294967295。
        /// </summary>
        UInt32 = 8,

        /// <summary>
        ///     有符号 64 位整数，值范围为 -9223372036854775808 到 9223372036854775807。
        /// </summary>
        Int64 = 9,

        /// <summary>
        ///     无符号 64 位整数，值范围为 0 到 18446744073709551615。
        /// </summary>
        UInt64 = 10,

        /// <summary>
        ///     单精度浮点类型，值范围约为 1.5 × 10⁻⁴⁵ 到 3.4 × 10³⁸，精度为 7 位数字。
        /// </summary>
        Single = 11,

        /// <summary>
        ///     双精度浮点类型，值范围约为 5.0 × 10⁻³²⁴ 到 1.7 × 10³⁰⁸，精度为 15-16 位数字。
        /// </summary>
        Double = 12,

        /// <summary>
        ///     十进制类型，值范围为 1.0 × 10⁻²⁸ 到约 7.9 × 10²⁸，有效数字为 28-29 位。
        /// </summary>
        Decimal = 13,

        /// <summary>
        ///     日期时间类型，表示日期和时间值。
        /// </summary>
        DateTime = 14,

        /// <summary>
        ///     字符串类型，表示 Unicode 字符串。
        /// </summary>
        String = 15,

        /// <summary>
        ///     枚举类型。
        /// </summary>
        Enum = 16,

        /// <summary>
        ///     数组类型。
        /// </summary>
        Array = 17,

        /// <summary>
        ///     通用对象类型，表示未被其他 TypeCode 明确表示的任何引用或值类型。
        /// </summary>
        Object = 18,

        /// <summary>
        ///    引用类型，表示一个引用类型的成员，包含成员的完整信息。
        /// </summary>
        MemberRef = 19
    }

    /// <summary>
    /// 类型成员。
    /// </summary>
    public class MemberInDto
    {
        /// <summary>
        ///     成员主键。
        /// </summary>
        public long Id { get; set; }
        /// <summary>
        ///     成员名称。
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     成员代码。
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        ///     成员描述。
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        ///     成员类型。
        /// </summary>
        public EnumMemberType MemberType { get; set; }

        /// <summary>
        ///     成员级别。
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        ///     是否必需。
        /// </summary>
        public bool Required { get; set; }

        /// <summary>
        ///     默认值。
        /// </summary>
        public string DefaultValue { get; set; }
    }
}