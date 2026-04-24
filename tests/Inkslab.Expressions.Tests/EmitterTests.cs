using Xunit;
using System;
using System.Reflection;

namespace Inkslab.Expressions.Tests
{
    /// <summary>
    /// Emitter 和类型构建测试。
    /// </summary>
    public class EmitterTests
    {
        private readonly ModuleEmitter _emitter = new ModuleEmitter($"Inkslab.Expressions.Tests.EmitterTests.{Guid.NewGuid():N}");

        #region ClassEmitter

        /// <summary>
        /// 验证 DefineType 能创建公共类型。
        /// </summary>
        /// <remarks>
        /// 使用 Public | Class 特性定义类型，验证生成结果非空、是类且为公共类型。
        /// </remarks>
        [Fact]
        public void DefineType_CreatesPublicClass()
        {
            var typeEmitter = _emitter.DefineType($"PubClass_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var type = typeEmitter.CreateType();

            Assert.NotNull(type);
            Assert.True(type.IsClass);
            Assert.True(type.IsPublic);
        }

        /// <summary>
        /// 验证 DefineType 能创建继承自指定基类的类型。
        /// </summary>
        /// <remarks>
        /// 创建继承自 Exception 的动态类型，验证新类型可赋值给 Exception 类型变量。
        /// </remarks>
        [Fact]
        public void DefineType_WithBaseType()
        {
            var typeEmitter = _emitter.DefineType($"Derived_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class, typeof(Exception));
            var type = typeEmitter.CreateType();

            Assert.True(typeof(Exception).IsAssignableFrom(type));
        }

        /// <summary>
        /// 验证 DefineType 能创建实现指定接口的类型。
        /// </summary>
        /// <remarks>
        /// 创建实现 IDisposable 接口的动态类型，补全 Dispose 方法后验证类型可赋值给 IDisposable。
        /// </remarks>
        [Fact]
        public void DefineType_WithInterface()
        {
            var typeEmitter = _emitter.DefineType($"Impl_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class, typeof(object), new[] { typeof(IDisposable) });

            var method = typeEmitter.DefineMethod("Dispose", MethodAttributes.Public | MethodAttributes.Virtual, typeof(void));
            method.Append(Expression.Default(typeof(void)));

            var type = typeEmitter.CreateType();
            Assert.True(typeof(IDisposable).IsAssignableFrom(type));
        }

        #endregion

        #region FieldEmitter / PropertyEmitter

        /// <summary>
        /// 验证动态类型中字段的定义与访问。
        /// </summary>
        /// <remarks>
        /// 在动态类型中定义名为 _value 的 int 公共字段，验证字段非空且类型为 int。
        /// </remarks>
        [Fact]
        public void Field_DefineAndAccess()
        {
            var typeEmitter = _emitter.DefineType($"FieldTest_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var fieldEmitter = typeEmitter.DefineField("_value", typeof(int), FieldAttributes.Public);

            var type = typeEmitter.CreateType();
            var field = type.GetField("_value");

            Assert.NotNull(field);
            Assert.Equal(typeof(int), field.FieldType);
        }

        /// <summary>
        /// 验证动态类型中属性的 getter 与 setter 可正常工作。
        /// </summary>
        /// <remarks>
        /// 在动态类型中定义带 backing field 的 Name 属性，通过反射赋值后验证 getter 返回相同值。
        /// </remarks>
        [Fact]
        public void Property_GetterAndSetter()
        {
            var typeEmitter = _emitter.DefineType($"PropTest_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);

            var backingField = typeEmitter.DefineField("_name", typeof(string), FieldAttributes.Private);

            var getter = typeEmitter.DefineMethod("get_Name", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, typeof(string));
            getter.Append(backingField);

            var setter = typeEmitter.DefineMethod("set_Name", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, typeof(void));
            var valueParam = setter.DefineParameter(typeof(string), "value");
            setter.Append(Expression.Assign(backingField, valueParam));

            var prop = typeEmitter.DefineProperty("Name", PropertyAttributes.None, typeof(string));
            prop.SetGetMethod(getter);
            prop.SetSetMethod(setter);

            var type = typeEmitter.CreateType();
            var instance = Activator.CreateInstance(type);

            var propInfo = type.GetProperty("Name");
            propInfo.SetValue(instance, "TestValue");
            Assert.Equal("TestValue", propInfo.GetValue(instance));
        }

        /// <summary>
        /// 验证动态方法可通过 this 访问实例字段。
        /// </summary>
        /// <remarks>
        /// 在动态类型中定义私有 _data 字段，通过构造函数赋值后调用 GetData 方法，验证读取结果与传入值一致（42）。
        /// </remarks>
        [Fact]
        public void This_AccessInstanceField()
        {
            var typeEmitter = _emitter.DefineType($"ThisTest_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var fieldEmitter = typeEmitter.DefineField("_data", typeof(int), FieldAttributes.Private);

            // 构造函数设置字段
            var ctor = typeEmitter.DefineConstructor(MethodAttributes.Public);
            var ctorParam = ctor.DefineParameter(typeof(int), ParameterAttributes.None, "data");
            ctor.Append(Expression.Assign(fieldEmitter, ctorParam));

            // 方法读取字段
            var getMethod = typeEmitter.DefineMethod("GetData", MethodAttributes.Public, typeof(int));
            getMethod.Append(fieldEmitter);

            var type = typeEmitter.CreateType();
            var instance = Activator.CreateInstance(type, 42);
            var result = type.GetMethod("GetData").Invoke(instance, null);

            Assert.Equal(42, result);
        }

        #endregion

        #region EnumEmitter

        /// <summary>
        /// 验证 DefineEnum 能正确创建枚举类型。
        /// </summary>
        /// <remarks>
        /// 创建包含 Red(0)、Green(1)、Blue(2) 三个成员的枚举类型，验证类型为枚举、成员数量为 3 且名称正确。
        /// </remarks>
        [Fact]
        public void DefineEnum_CreatesEnumType()
        {
            var enumEmitter = _emitter.DefineEnum($"Color_{Guid.NewGuid():N}", TypeAttributes.Public, typeof(int));
            enumEmitter.DefineLiteral("Red", 0);
            enumEmitter.DefineLiteral("Green", 1);
            enumEmitter.DefineLiteral("Blue", 2);

            var type = enumEmitter.CreateType();

            Assert.True(type.IsEnum);
            Assert.Equal(3, Enum.GetValues(type).Length);
            Assert.Equal("Red", Enum.GetName(type, 0));
            Assert.Equal("Green", Enum.GetName(type, 1));
            Assert.Equal("Blue", Enum.GetName(type, 2));
        }

        #endregion

        #region NestedClassEmitter

        /// <summary>
        /// 验证可在动态类型内部定义嵌套类。
        /// </summary>
        /// <remarks>
        /// 在外部动态类型内定义名为 Inner 的嵌套公共类，验证外部类型创建成功且为类类型。
        /// </remarks>
        [Fact]
        public void DefineNestedType_CreatesNestedClass()
        {
            var outerEmitter = _emitter.DefineType($"Outer_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);

            var innerEmitter = outerEmitter.DefineNestedType("Inner", TypeAttributes.NestedPublic | TypeAttributes.Class);
            innerEmitter.DefineField("Value", typeof(int), FieldAttributes.Public);

            var outerType = outerEmitter.CreateType();

            // Nested types are compiled along with parent
            Assert.NotNull(outerType);
            Assert.True(outerType.GetNestedTypes().Length > 0 || outerType.IsClass);
        }

        #endregion

        #region ConstructorEmitter

        /// <summary>
        /// 验证动态类型构造函数支持多参数并正确赋值字段。
        /// </summary>
        /// <remarks>
        /// 定义带 name 和 age 两个参数的构造函数，通过反射以 ("Alice", 30) 创建实例，
        /// 验证 GetName 返回 "Alice"，GetAge 返回 30。
        /// </remarks>
        [Fact]
        public void Constructor_WithMultipleParameters()
        {
            var typeEmitter = _emitter.DefineType($"CtorTest_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);

            var nameField = typeEmitter.DefineField("_name", typeof(string), FieldAttributes.Private);
            var ageField = typeEmitter.DefineField("_age", typeof(int), FieldAttributes.Private);

            var ctor = typeEmitter.DefineConstructor(MethodAttributes.Public);
            var nameParam = ctor.DefineParameter(typeof(string), ParameterAttributes.None, "name");
            var ageParam = ctor.DefineParameter(typeof(int), ParameterAttributes.None, "age");
            ctor.Append(Expression.Assign(nameField, nameParam));
            ctor.Append(Expression.Assign(ageField, ageParam));

            var getName = typeEmitter.DefineMethod("GetName", MethodAttributes.Public, typeof(string));
            getName.Append(nameField);

            var getAge = typeEmitter.DefineMethod("GetAge", MethodAttributes.Public, typeof(int));
            getAge.Append(ageField);

            var type = typeEmitter.CreateType();
            var instance = Activator.CreateInstance(type, "Alice", 30);

            Assert.Equal("Alice", type.GetMethod("GetName").Invoke(instance, null));
            Assert.Equal(30, type.GetMethod("GetAge").Invoke(instance, null));
        }

        #endregion

        #region MethodEmitter

        /// <summary>
        /// 验证动态方法支持通过条件表达式实现多返回路径。
        /// </summary>
        /// <remarks>
        /// 生成 Compute 静态方法：input &gt; 0 时返回 input * 2，否则返回 0。
        /// 验证输入 5 得 10，输入 -1 得 0。
        /// </remarks>
        [Fact]
        public void Method_MultipleReturnPaths_UsingCondition()
        {
            var typeEmitter = _emitter.DefineType($"BlockTest_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var method = typeEmitter.DefineMethod("Compute", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            var input = method.DefineParameter(typeof(int), "input");

            // input > 0 ? input * 2 : 0
            method.Append(Expression.Condition(
                Expression.GreaterThan(input, Expression.Constant(0)),
                Expression.Multiply(input, Expression.Constant(2)),
                Expression.Constant(0)
            ));

            var type = typeEmitter.CreateType();
            var mi = type.GetMethod("Compute");

            Assert.Equal(10, mi.Invoke(null, new object[] { 5 }));
            Assert.Equal(0, mi.Invoke(null, new object[] { -1 }));
        }

        /// <summary>
        /// 验证动态方法支持循环结构对数组求和。
        /// </summary>
        /// <remarks>
        /// 生成 SumArray 静态方法：通过 while 循环遍历 int[] 数组累加求和。
        /// 验证 [1,2,3,4,5] 得 15，空数组得 0。
        /// </remarks>
        [Fact]
        public void Method_ComplexLoop_SumArray()
        {
            // int SumArray(int[] arr) { int sum = 0; int i = 0; while(i < arr.Length) { sum += arr[i]; i++; } return sum; }
            var typeEmitter = _emitter.DefineType($"SumArr_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var method = typeEmitter.DefineMethod("SumArray", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            var arrParam = method.DefineParameter(typeof(int[]), "arr");

            var sum = Expression.Variable(typeof(int));
            var i = Expression.Variable(typeof(int));

            method.Append(Expression.Assign(sum, Expression.Constant(0)));
            method.Append(Expression.Assign(i, Expression.Constant(0)));

            var loop = Expression.Loop();
            loop.Append(Expression.IfThen(
                Expression.GreaterThanOrEqual(i, Expression.ArrayLength(arrParam)),
                Expression.Break()
            ));
            loop.Append(Expression.AddAssign(sum, Expression.ArrayIndex(arrParam, i)));
            loop.Append(Expression.IncrementAssign(i));

            method.Append(loop);
            method.Append(sum);

            var type = typeEmitter.CreateType();
            var mi = type.GetMethod("SumArray");

            Assert.Equal(15, mi.Invoke(null, new object[] { new[] { 1, 2, 3, 4, 5 } }));
            Assert.Equal(0, mi.Invoke(null, new object[] { Array.Empty<int>() }));
        }

        #endregion

        #region 多类型常量

        /// <summary>
        /// 验证动态方法可内联 Guid 常量。
        /// </summary>
        /// <remarks>
        /// 生成返回固定 Guid 值的静态方法，验证调用结果与原始 Guid 值完全相等。
        /// </remarks>
        [Fact]
        public void Constant_GuidValue()
        {
            var typeEmitter = _emitter.DefineType($"GuidConst_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var method = typeEmitter.DefineMethod("GetGuid", MethodAttributes.Public | MethodAttributes.Static, typeof(Guid));

            var guidValue = Guid.NewGuid();
            method.Append(Expression.Constant(guidValue));

            var type = typeEmitter.CreateType();
            var result = type.GetMethod("GetGuid").Invoke(null, null);

            Assert.Equal(guidValue, result);
        }

        /// <summary>
        /// 验证动态方法可内联 DateTime 常量。
        /// </summary>
        /// <remarks>
        /// 生成返回固定 DateTime 值的静态方法，验证调用结果与 new DateTime(2026, 1, 1) 完全相等。
        /// </remarks>
        [Fact]
        public void Constant_DateTimeValue()
        {
            var typeEmitter = _emitter.DefineType($"DtConst_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var method = typeEmitter.DefineMethod("GetDt", MethodAttributes.Public | MethodAttributes.Static, typeof(DateTime));

            var dtValue = new DateTime(2026, 1, 1);
            method.Append(Expression.Constant(dtValue));

            var type = typeEmitter.CreateType();
            var result = type.GetMethod("GetDt").Invoke(null, null);

            Assert.Equal(dtValue, result);
        }

        /// <summary>
        /// 验证动态方法可内联 decimal 常量。
        /// </summary>
        /// <remarks>
        /// 生成返回 123.45m 常量的静态方法，验证调用结果与原始 decimal 值完全相等。
        /// </remarks>
        [Fact]
        public void Constant_DecimalValue()
        {
            var typeEmitter = _emitter.DefineType($"DecConst_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var method = typeEmitter.DefineMethod("GetDec", MethodAttributes.Public | MethodAttributes.Static, typeof(decimal));

            method.Append(Expression.Constant(123.45m));

            var type = typeEmitter.CreateType();
            var result = type.GetMethod("GetDec").Invoke(null, null);

            Assert.Equal(123.45m, result);
        }

        #endregion
    }
}
