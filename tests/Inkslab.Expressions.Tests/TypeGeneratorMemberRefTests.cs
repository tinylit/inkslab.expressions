using System;
using Xunit;

namespace Inkslab.Expressions.Tests
{
    /// <summary>
    /// ?? <see cref="TypeGenerator"/> ? <see cref="EnumMemberType.MemberRef"/> ????????
    /// MemberRef ? <see cref="MemberInDto.DefaultValue"/> ?????? <see cref="MemberInDto.Id"/>????????
    /// </summary>
    public class TypeGeneratorMemberRefTests
    {
        private static TypeGenerator CreateGenerator()
            => new(new ModuleEmitter($"MemberRefTests_{Guid.NewGuid():N}"));

        private static MemberInDto Member(long id, string code, EnumMemberType type, int level,
            long? defaultRefId = null, string defaultValue = null, string name = null, bool required = false)
            => new()
            {
                Id = id,
                Name = name ?? code,
                Code = code,
                MemberType = type,
                Level = level,
                Required = required,
                DefaultValue = defaultRefId.HasValue ? defaultRefId.Value.ToString() : defaultValue
            };

        [Fact]
        public void Root_MemberRef_Mirrors_Sibling_Value()
        {
            MemberInDto[] members =
            {
                Member(1, "Source", EnumMemberType.Int32, 0),
                Member(2, "Mirror", EnumMemberType.MemberRef, 0, defaultRefId: 1)
            };

            Type type = CreateGenerator().CreateType("Sample", members);

            Assert.True(typeof(IMemberRefBinder).IsAssignableFrom(type));

            object instance = Activator.CreateInstance(type);
            type.GetProperty("Source")!.SetValue(instance, 42);

            ((IMemberRefBinder)instance).BindMemberRefs();

            object mirror = type.GetProperty("Mirror")!.GetValue(instance);
            Assert.Equal(42, mirror);
            Assert.Equal(typeof(int?), type.GetProperty("Mirror")!.PropertyType);
        }

        [Fact]
        public void Nested_MemberRef_Reads_Ancestor_Value()
        {
            MemberInDto[] members =
            {
                Member(1, "Header", EnumMemberType.String, 0, name: "Header"),
                Member(2, "Body", EnumMemberType.Object, 0),
                Member(3, "Body.Title", EnumMemberType.MemberRef, 1, defaultRefId: 1)
            };

            Type type = CreateGenerator().CreateType("Sample", members);

            object instance = Activator.CreateInstance(type);
            type.GetProperty("Header")!.SetValue(instance, "hello");

            Type bodyType = type.GetProperty("Body")!.PropertyType;
            object body = Activator.CreateInstance(bodyType);
            type.GetProperty("Body")!.SetValue(instance, body);

            ((IMemberRefBinder)instance).BindMemberRefs();

            object title = bodyType.GetProperty("Title")!.GetValue(body);
            Assert.Equal("hello", title);
            Assert.Equal(typeof(string), bodyType.GetProperty("Title")!.PropertyType);
        }

        [Fact]
        public void MemberRef_In_Array_Element_Reads_Root_Sibling()
        {
            MemberInDto[] members =
            {
                Member(1, "Owner", EnumMemberType.String, 0),
                Member(2, "Items", EnumMemberType.Array, 0),
                Member(3, "Items.Owner", EnumMemberType.MemberRef, 1, defaultRefId: 1),
                Member(4, "Items.Index", EnumMemberType.Int32, 1)
            };

            Type type = CreateGenerator().CreateType("Sample", members);

            object instance = Activator.CreateInstance(type);
            type.GetProperty("Owner")!.SetValue(instance, "alice");

            Type arrayType = type.GetProperty("Items")!.PropertyType;
            Type elementType = arrayType.GetElementType()!;
            Array array = Array.CreateInstance(elementType, 2);

            object item0 = Activator.CreateInstance(elementType);
            elementType.GetProperty("Index")!.SetValue(item0, 1);
            array.SetValue(item0, 0);

            object item1 = Activator.CreateInstance(elementType);
            elementType.GetProperty("Index")!.SetValue(item1, 2);
            array.SetValue(item1, 1);

            type.GetProperty("Items")!.SetValue(instance, array);

            ((IMemberRefBinder)instance).BindMemberRefs();

            Assert.Equal("alice", elementType.GetProperty("Owner")!.GetValue(item0));
            Assert.Equal("alice", elementType.GetProperty("Owner")!.GetValue(item1));
        }

        [Fact]
        public void MemberRef_Chain_Resolves_Through_Multiple_Hops()
        {
            MemberInDto[] members =
            {
                Member(1, "A", EnumMemberType.Int32, 0),
                Member(2, "B", EnumMemberType.MemberRef, 0, defaultRefId: 1),
                Member(3, "C", EnumMemberType.MemberRef, 0, defaultRefId: 2)
            };

            Type type = CreateGenerator().CreateType("Sample", members);

            object instance = Activator.CreateInstance(type);
            type.GetProperty("A")!.SetValue(instance, 7);

            ((IMemberRefBinder)instance).BindMemberRefs();

            Assert.Equal(7, type.GetProperty("B")!.GetValue(instance));
            Assert.Equal(7, type.GetProperty("C")!.GetValue(instance));
        }

        [Fact]
        public void MemberRef_To_Missing_Target_Throws()
        {
            MemberInDto[] members =
            {
                Member(1, "Mirror", EnumMemberType.MemberRef, 0, defaultRefId: 9999)
            };

            Assert.Throws<InvalidOperationException>(() => CreateGenerator().CreateType("Sample", members));
        }

        [Fact]
        public void MemberRef_Without_DefaultValue_Throws()
        {
            MemberInDto[] members =
            {
                Member(1, "Source", EnumMemberType.Int32, 0),
                Member(2, "Mirror", EnumMemberType.MemberRef, 0)
            };

            Assert.Throws<InvalidOperationException>(() => CreateGenerator().CreateType("Sample", members));
        }

        [Fact]
        public void MemberRef_DefaultValue_Not_Long_Throws()
        {
            MemberInDto[] members =
            {
                Member(1, "Source", EnumMemberType.Int32, 0),
                Member(2, "Mirror", EnumMemberType.MemberRef, 0, defaultValue: "Source")
            };

            Assert.Throws<InvalidOperationException>(() => CreateGenerator().CreateType("Sample", members));
        }

        [Fact]
        public void MemberRef_Circular_Chain_Throws()
        {
            MemberInDto[] members =
            {
                Member(1, "A", EnumMemberType.MemberRef, 0, defaultRefId: 2),
                Member(2, "B", EnumMemberType.MemberRef, 0, defaultRefId: 1)
            };

            Assert.Throws<InvalidOperationException>(() => CreateGenerator().CreateType("Sample", members));
        }

        [Fact]
        public void Nested_MemberRef_Same_Type_Is_Reused()
        {
            MemberInDto[] members =
            {
                Member(1, "Primary", EnumMemberType.Object, 0),
                Member(2, "Primary.Name", EnumMemberType.String, 1),
                Member(3, "Mirror", EnumMemberType.MemberRef, 0, defaultRefId: 1)
            };

            Type type = CreateGenerator().CreateType("Sample", members);

            Type primaryType = type.GetProperty("Primary")!.PropertyType;
            Type mirrorType = type.GetProperty("Mirror")!.PropertyType;

            Assert.Same(primaryType, mirrorType);

            object instance = Activator.CreateInstance(type);
            object primary = Activator.CreateInstance(primaryType);
            primaryType.GetProperty("Name")!.SetValue(primary, "x");
            type.GetProperty("Primary")!.SetValue(instance, primary);

            ((IMemberRefBinder)instance).BindMemberRefs();

            object mirror = type.GetProperty("Mirror")!.GetValue(instance);
            Assert.Same(primary, mirror);
        }

        // @AI-Generated by Copilot 2026-04-28 19:30:00
        /// <summary>
        /// ?????root ? Outer ? Inner????? MemberRef ?? 2 ??????????
        /// </summary>
        [Fact]
        public void Deep_Nested_MemberRef_Reaches_Root_Sibling_Across_Two_Levels()
        {
            MemberInDto[] members =
            {
                Member(1, "RootName", EnumMemberType.String, 0),
                Member(2, "Outer", EnumMemberType.Object, 0),
                Member(3, "Outer.Inner", EnumMemberType.Object, 1),
                Member(4, "Outer.Inner.Echo", EnumMemberType.MemberRef, 2, defaultRefId: 1)
            };

            Type type = CreateGenerator().CreateType("Sample", members);

            object instance = Activator.CreateInstance(type);
            type.GetProperty("RootName")!.SetValue(instance, "deep");

            Type outerType = type.GetProperty("Outer")!.PropertyType;
            object outer = Activator.CreateInstance(outerType);
            type.GetProperty("Outer")!.SetValue(instance, outer);

            Type innerType = outerType.GetProperty("Inner")!.PropertyType;
            object inner = Activator.CreateInstance(innerType);
            outerType.GetProperty("Inner")!.SetValue(outer, inner);

            ((IMemberRefBinder)instance).BindMemberRefs();

            object echo = innerType.GetProperty("Echo")!.GetValue(inner);
            Assert.Equal("deep", echo);
        }

        /// <summary>
        /// ????????????A.B.Ref ?? A.C???? A.B ?????????????
        /// ??? deepObj ???? A ?? C ?????????????
        /// </summary>
        [Fact]
        public void MemberRef_Reads_Uncle_Branch_Value()
        {
            MemberInDto[] members =
            {
                Member(1, "A", EnumMemberType.Object, 0),
                Member(2, "A.B", EnumMemberType.Object, 1),
                Member(3, "A.C", EnumMemberType.Int32, 1),
                Member(4, "A.B.Ref", EnumMemberType.MemberRef, 2, defaultRefId: 3)
            };

            Type type = CreateGenerator().CreateType("Sample", members);

            object instance = Activator.CreateInstance(type);
            Type aType = type.GetProperty("A")!.PropertyType;
            object a = Activator.CreateInstance(aType);
            type.GetProperty("A")!.SetValue(instance, a);

            aType.GetProperty("C")!.SetValue(a, 100);

            Type bType = aType.GetProperty("B")!.PropertyType;
            object b = Activator.CreateInstance(bType);
            aType.GetProperty("B")!.SetValue(a, b);

            ((IMemberRefBinder)instance).BindMemberRefs();

            object refValue = bType.GetProperty("Ref")!.GetValue(b);
            Assert.Equal(100, refValue);
        }

        /// <summary>
        /// ?????? MemberRef ? 3 ??????????
        /// </summary>
        [Fact]
        public void Array_Element_Deep_MemberRef_Reaches_Root_Across_Three_Levels()
        {
            MemberInDto[] members =
            {
                Member(1, "RootSeq", EnumMemberType.Int32, 0),
                Member(2, "Items", EnumMemberType.Array, 0),
                Member(3, "Items.Inner", EnumMemberType.Object, 1),
                Member(4, "Items.Inner.Ref", EnumMemberType.MemberRef, 2, defaultRefId: 1)
            };

            Type type = CreateGenerator().CreateType("Sample", members);

            object instance = Activator.CreateInstance(type);
            type.GetProperty("RootSeq")!.SetValue(instance, 999);

            Type arrayType = type.GetProperty("Items")!.PropertyType;
            Type elementType = arrayType.GetElementType()!;
            Array array = Array.CreateInstance(elementType, 2);

            Type innerType = elementType.GetProperty("Inner")!.PropertyType;

            object item0 = Activator.CreateInstance(elementType);
            object inner0 = Activator.CreateInstance(innerType);
            elementType.GetProperty("Inner")!.SetValue(item0, inner0);
            array.SetValue(item0, 0);

            object item1 = Activator.CreateInstance(elementType);
            object inner1 = Activator.CreateInstance(innerType);
            elementType.GetProperty("Inner")!.SetValue(item1, inner1);
            array.SetValue(item1, 1);

            type.GetProperty("Items")!.SetValue(instance, array);

            ((IMemberRefBinder)instance).BindMemberRefs();

            Assert.Equal(999, innerType.GetProperty("Ref")!.GetValue(inner0));
            Assert.Equal(999, innerType.GetProperty("Ref")!.GetValue(inner1));
        }

        /// <summary>
        /// ????????????B.Ref ?? A.Source?A ? B ?????????????????
        /// </summary>
        [Fact]
        public void MemberRef_To_Sibling_Branch_Throws()
        {
            MemberInDto[] members =
            {
                Member(1, "A", EnumMemberType.Object, 0),
                Member(2, "A.Source", EnumMemberType.Int32, 1),
                Member(3, "B", EnumMemberType.Object, 0),
                Member(4, "B.Ref", EnumMemberType.MemberRef, 1, defaultRefId: 2)
            };

            Assert.Throws<InvalidOperationException>(() => CreateGenerator().CreateType("Sample", members));
        }

        /// <summary>
        /// ??? MemberRef?A.B.C ?????? A?A ? Object ??????? C ? A ???????
        /// </summary>
        [Fact]
        public void MemberRef_From_Grandchild_To_Ancestor_Object()
        {
            MemberInDto[] members =
            {
                Member(1, "A", EnumMemberType.Object, 0),
                Member(2, "A.B", EnumMemberType.Object, 1),
                Member(3, "A.B.Name", EnumMemberType.String, 2),
                Member(4, "A.B.C", EnumMemberType.MemberRef, 2, defaultRefId: 1)
            };

            Type type = CreateGenerator().CreateType("Sample", members);

            object instance = Activator.CreateInstance(type);

            Type aType = type.GetProperty("A")!.PropertyType;
            object a = Activator.CreateInstance(aType);
            type.GetProperty("A")!.SetValue(instance, a);

            Type bType = aType.GetProperty("B")!.PropertyType;
            object b = Activator.CreateInstance(bType);
            aType.GetProperty("B")!.SetValue(a, b);

            ((IMemberRefBinder)instance).BindMemberRefs();

            Type cType = bType.GetProperty("C")!.PropertyType;
            Assert.Same(aType, cType);

            object c = bType.GetProperty("C")!.GetValue(b);
            Assert.Same(a, c);
        }

        /// <summary>
        /// ??? MemberRef ??????????????A.B.C.Ref ?? A.Seq?? 2 ?????
        /// </summary>
        [Fact]
        public void MemberRef_From_Deep_Leaf_To_Mid_Ancestor_Scalar()
        {
            MemberInDto[] members =
            {
                Member(1, "A", EnumMemberType.Object, 0),
                Member(2, "A.Seq", EnumMemberType.Int32, 1),
                Member(3, "A.B", EnumMemberType.Object, 1),
                Member(4, "A.B.C", EnumMemberType.Object, 2),
                Member(5, "A.B.C.Ref", EnumMemberType.MemberRef, 3, defaultRefId: 2)
            };

            Type type = CreateGenerator().CreateType("Sample", members);

            object instance = Activator.CreateInstance(type);

            Type aType = type.GetProperty("A")!.PropertyType;
            object a = Activator.CreateInstance(aType);
            aType.GetProperty("Seq")!.SetValue(a, 321);
            type.GetProperty("A")!.SetValue(instance, a);

            Type bType = aType.GetProperty("B")!.PropertyType;
            object b = Activator.CreateInstance(bType);
            aType.GetProperty("B")!.SetValue(a, b);

            Type cType = bType.GetProperty("C")!.PropertyType;
            object c = Activator.CreateInstance(cType);
            bType.GetProperty("C")!.SetValue(b, c);

            ((IMemberRefBinder)instance).BindMemberRefs();

            Assert.Equal(321, cType.GetProperty("Ref")!.GetValue(c));
        }
        // @AI-Generated-End
    }
}
// @AI-Generated-End
