using Xunit;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Newtonsoft.Json;

namespace Inkslab.Expressions.Tests
{
    /// <summary>
    /// 单元测试。
    /// </summary>
    public class UnitTest1
    {
        /// <summary>
        /// 类型成员。
        /// </summary>
        public class Member
        {
            /// <summary>
            /// 成员名称。
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// 成员代码。
            /// </summary>
            public string Code { get; set; }

            /// <summary>
            /// 成员类型。
            /// </summary>
            public MemberType MemberType { get; set; }

            /// <summary>
            /// 成员级别。
            /// </summary>
            public int Level { get; set; }

            /// <summary>
            /// 是否必需。
            /// </summary>
            public bool Required { get; set; }
        }

        /// <summary>
        /// 类型成员类型。
        /// </summary>
        public enum MemberType
        {
            /// <summary>
            /// 布尔类型，表示 true 或 false 值。
            /// </summary>
            Boolean = 1,

            /// <summary>
            /// 字符类型，表示 0 到 65535 之间的无符号 16 位整数，对应 Unicode 字符集。
            /// </summary>
            Char = 2,

            /// <summary>
            /// 有符号 8 位整数，值范围为 -128 到 127。
            /// </summary>
            SByte = 3,

            /// <summary>
            /// 无符号 8 位整数，值范围为 0 到 255。
            /// </summary>
            Byte = 4,

            /// <summary>
            /// 有符号 16 位整数，值范围为 -32768 到 32767。
            /// </summary>
            Int16 = 5,

            /// <summary>
            /// 无符号 16 位整数，值范围为 0 到 65535。
            /// </summary>
            UInt16 = 6,

            /// <summary>
            /// 有符号 32 位整数，值范围为 -2147483648 到 2147483647。
            /// </summary>
            Int32 = 7,

            /// <summary>
            /// 无符号 32 位整数，值范围为 0 到 4294967295。
            /// </summary>
            UInt32 = 8,

            /// <summary>
            /// 有符号 64 位整数，值范围为 -9223372036854775808 到 9223372036854775807。
            /// </summary>
            Int64 = 9,

            /// <summary>
            /// 无符号 64 位整数，值范围为 0 到 18446744073709551615。
            /// </summary>
            UInt64 = 10,

            /// <summary>
            /// 单精度浮点类型，值范围约为 1.5 × 10⁻⁴⁵ 到 3.4 × 10³⁸，精度为 7 位数字。
            /// </summary>
            Single = 11,

            /// <summary>
            /// 双精度浮点类型，值范围约为 5.0 × 10⁻³²⁴ 到 1.7 × 10³⁰⁸，精度为 15-16 位数字。
            /// </summary>
            Double = 12,

            /// <summary>
            /// 十进制类型，值范围为 1.0 × 10⁻²⁸ 到约 7.9 × 10²⁸，有效数字为 28-29 位。
            /// </summary>
            Decimal = 13,

            /// <summary>
            /// 日期时间类型，表示日期和时间值。
            /// </summary>
            DateTime = 14,

            /// <summary>
            /// 字符串类型，表示 Unicode 字符串。
            /// </summary>
            String = 15,

            /// <summary>
            /// 枚举类型。
            /// </summary>
            Enum = 16,

            /// <summary>
            /// 数组类型。
            /// </summary>
            Array = 17,

            /// <summary>
            /// 通用对象类型，表示未被其他 TypeCode 明确表示的任何引用或值类型。
            /// </summary>
            Object = 18
        }

        private readonly ModuleEmitter _emitter = new ModuleEmitter($"Inkslab.Expressions.Tests.DynamicModule.{Guid.NewGuid():N}");

        /// <summary>
        /// 测试方法。
        /// </summary>
        [Fact]
        public void Test1()
        {
            var type = CreateType(new Member[]
                    {
                        new Member { Name = "Id", Code = "Id", MemberType = MemberType.Int32, Level = 0, Required = true },
                        new Member { Name = "Name", Code = "Name", MemberType = MemberType.String, Level = 0, Required = true },
                        new Member { Name = "Address", Code = "Address", MemberType = MemberType.Object, Level = 0, Required = true },
                        new Member { Name = "Address.Street", Code = "Address.Street", MemberType = MemberType.String, Level = 1, Required = true },
                        new Member { Name = "Address.City", Code = "Address.City", MemberType = MemberType.String, Level = 1, Required = true },
                        new Member { Name = "Type", Code = "Type", MemberType = MemberType.Enum, Level = 0, Required = true }
                    });

            Assert.NotNull(type);

            //生成Json序列化代码测试
            var json = "{\"Id\":1,\"Name\":\"Test\",\"Address\":{\"Street\":\"123 Main St\",\"City\":\"Metropolis\"},\"Type\":1}";

            var instance = JsonConvert.DeserializeObject(json, type);

            var value = JsonConvert.SerializeObject(instance);

            Assert.Equal(json, value);
        }

        /// <summary>
        /// 创建类型。
        /// </summary>
        private Type CreateType(Member[] members)
        {
            var typeEmitter = _emitter.DefineType($"DynamicType_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed);

            CreateType(typeEmitter, string.Empty, 0, members);

            return typeEmitter.CreateType();
        }

        private void CreateType(AbstractTypeEmitter typeEmitter, string namePrefix, int level, Member[] members)
        {
            var prefix = namePrefix + ".";

            var hashSet = new HashSet<string>();

            var currentLevelMembers = level == 0
                ? Array.FindAll(members, m => m.Level == level)
                : Array.FindAll(members, m => m.Level == level && m.Code.StartsWith(prefix));

            foreach (var member in currentLevelMembers)
            {
                var code = level == 0
                    ? member.Code
                    : member.Code[prefix.Length..];

                if (!hashSet.Add(code))
                {
                    throw new InvalidOperationException($"Duplicate member code detected: {code} at level {level}, full code is {member.Code}.");
                }

                var memberType = GetTypeFromMemberType(member.MemberType);

                if (member.MemberType == MemberType.Enum)
                {
                    // 枚举类型特殊处理，创建一个简单的枚举类型
                    var enumEmitter = _emitter.DefineEnum($"{code}_Enum_{Guid.NewGuid():N}", TypeAttributes.Public, typeof(int));

                    var boolEmitter = enumEmitter.DefineLiteral("Boolean", 1);

                    var charEmitter = enumEmitter.DefineLiteral("Char", 2);

                    memberType = enumEmitter.CreateType();
                }

                if (member.MemberType is MemberType.Array or MemberType.Object)
                {
                    var classEmitter = typeEmitter.DefineNestedType($"{code}_Type", TypeAttributes.NestedPublic | TypeAttributes.Class | TypeAttributes.Sealed);

                    CreateType(classEmitter, member.Code, level + 1, members);

                    var type = classEmitter.UncompiledType;

                    memberType = member.MemberType == MemberType.Array
                        ? type.MakeArrayType()
                        : type;
                }

                var fieldBuilder = typeEmitter.DefineField($"'<{code}>k__BackingField'", memberType, FieldAttributes.Private);

                fieldBuilder.SetCustomAttribute(new CustomAttributeBuilder(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute).GetConstructor(Type.EmptyTypes), Array.Empty<object>()));
                fieldBuilder.SetCustomAttribute(new CustomAttributeBuilder(typeof(System.Diagnostics.DebuggerBrowsableAttribute).GetConstructor(new[] { typeof(System.Diagnostics.DebuggerBrowsableState) }), new object[] { System.Diagnostics.DebuggerBrowsableState.Never }));

                var getterEmitter = typeEmitter.DefineMethod($"get_{code}", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, memberType);

                getterEmitter.Append(fieldBuilder);

                var setterEmitter = typeEmitter.DefineMethod($"set_{code}", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, typeof(void));

                var parameterEmitter = setterEmitter.DefineParameter(memberType, "value");

                setterEmitter.Append(Expression.Assign(fieldBuilder, parameterEmitter));

                var propertyBuilder = typeEmitter.DefineProperty(code, PropertyAttributes.None, memberType);

                if(member.Required)
                {
                    propertyBuilder.SetCustomAttribute(new CustomAttributeBuilder(typeof(System.ComponentModel.DataAnnotations.RequiredAttribute).GetConstructor(Type.EmptyTypes), Array.Empty<object>()));
                }

                propertyBuilder.SetGetMethod(getterEmitter);
                propertyBuilder.SetSetMethod(setterEmitter);
            }
        }

        private static Type GetTypeFromMemberType(MemberType memberType)
        {
            switch (memberType)
            {
                case MemberType.Boolean:
                    return typeof(bool);
                case MemberType.Char:
                    return typeof(char);
                case MemberType.SByte:
                    return typeof(sbyte);
                case MemberType.Byte:
                    return typeof(byte);
                case MemberType.Int16:
                    return typeof(short);
                case MemberType.UInt16:
                    return typeof(ushort);
                case MemberType.Int32:
                    return typeof(int);
                case MemberType.UInt32:
                    return typeof(uint);
                case MemberType.Int64:
                    return typeof(long);
                case MemberType.UInt64:
                    return typeof(ulong);
                case MemberType.Single:
                    return typeof(float);
                case MemberType.Double:
                    return typeof(double);
                case MemberType.Decimal:
                    return typeof(decimal);
                case MemberType.DateTime:
                    return typeof(DateTime);
                case MemberType.String:
                    return typeof(string);
                case MemberType.Enum:
                    return typeof(Enum);
                case MemberType.Array:
                    return typeof(Array);
                case MemberType.Object:
                default:
                    return typeof(object);
            }
        }
    }
}