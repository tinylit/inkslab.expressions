using Xunit;
using System;
using System.Reflection;

namespace Inkslab.Expressions.Tests
{
    public class EmitterInternalTests
    {
        private readonly ModuleEmitter _mod = new ModuleEmitter($"EIT_{Guid.NewGuid():N}");

        #region ConstructorEmitter

        [Fact]
        public void ConstructorEmitter_DefineDefaultConstructor()
        {
            var te = _mod.DefineType($"CEIB_{Guid.NewGuid():N}", TypeAttributes.Public);
            te.DefineDefaultConstructor();
            var type = te.CreateType();
            Assert.NotNull(Activator.CreateInstance(type));
        }

        [Fact]
        public void ConstructorEmitter_Attributes()
        {
            var te = _mod.DefineType($"CEA_{Guid.NewGuid():N}", TypeAttributes.Public);
            var ct = te.DefineConstructor(MethodAttributes.Public);
            ct.Append(Expression.Default(typeof(void)));
            Assert.Equal(MethodAttributes.Public, ct.Attributes & MethodAttributes.Public);
        }

        [Fact]
        public void ConstructorEmitter_GetParameters_Empty()
        {
            var te = _mod.DefineType($"CEGP_{Guid.NewGuid():N}", TypeAttributes.Public);
            var ct = te.DefineConstructor(MethodAttributes.Public);
            ct.Append(Expression.Default(typeof(void)));
            Assert.Empty(ct.GetParameters());
        }

        [Fact]
        public void ConstructorEmitter_WithParam()
        {
            var te = _mod.DefineType($"CEWP_{Guid.NewGuid():N}", TypeAttributes.Public);
            var ct = te.DefineConstructor(MethodAttributes.Public);
            var p = ct.DefineParameter(typeof(int), ParameterAttributes.None, "x");
            ct.Append(Expression.Default(typeof(void)));
            Assert.Single(ct.GetParameters());
            te.CreateType();
        }

        [Fact]
        public void ConstructorEmitter_CallingConventions()
        {
            var te = _mod.DefineType($"CECC_{Guid.NewGuid():N}", TypeAttributes.Public);
            var ct = te.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis);
            ct.Append(Expression.Default(typeof(void)));
            Assert.Equal(CallingConventions.HasThis, ct.Conventions);
            te.CreateType();
        }

        #endregion

        #region MethodEmitter

        [Fact]
        public void MethodEmitter_Properties()
        {
            var te = _mod.DefineType($"MEP_{Guid.NewGuid():N}", TypeAttributes.Public);
            var me = te.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Static, typeof(string));
            me.Append(Expression.Constant("hi"));
            Assert.Equal("TestMethod", me.Name);
            Assert.True(me.IsStatic);
            Assert.Equal(typeof(string), me.ReturnType);
            Assert.False(me.IsGenericMethod);
            Assert.NotNull(me.DeclaringType);
        }

        [Fact]
        public void MethodEmitter_GetParameters()
        {
            var te = _mod.DefineType($"MEGP_{Guid.NewGuid():N}", TypeAttributes.Public);
            var me = te.DefineMethod("M", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            me.DefineParameter(typeof(int), "a");
            me.DefineParameter(typeof(string), "b");
            me.Append(Expression.Constant(0));
            Assert.Equal(2, me.GetParameters().Length);
        }

        [Fact]
        public void MethodEmitter_SetCustomAttribute()
        {
            var te = _mod.DefineType($"MECA_{Guid.NewGuid():N}", TypeAttributes.Public);
            te.DefineDefaultConstructor();
            var me = te.DefineMethod("M", MethodAttributes.Public | MethodAttributes.Static, typeof(void));
            me.SetCustomAttribute<ObsoleteAttribute>();
            me.Append(Expression.Default(typeof(void)));
            var type = te.CreateType();
            Assert.True(type.GetMethod("M").IsDefined(typeof(ObsoleteAttribute)));
        }

        #endregion

        #region PropertyEmitter

        [Fact]
        public void PropertyEmitter_Properties()
        {
            var te = _mod.DefineType($"PEP_{Guid.NewGuid():N}", TypeAttributes.Public);
            te.DefineDefaultConstructor();
            var prop = te.DefineProperty("Val", PropertyAttributes.None, typeof(int));
            var getter = te.DefineMethod("get_Val", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            getter.Append(Expression.Constant(0));
            prop.SetGetMethod(getter);
            Assert.Equal("Val", prop.Name);
            te.CreateType();
        }

        [Fact]
        public void PropertyEmitter_DefaultValue()
        {
            var te = _mod.DefineType($"PEDV_{Guid.NewGuid():N}", TypeAttributes.Public);
            te.DefineDefaultConstructor();
            var prop = te.DefineProperty("Val", PropertyAttributes.None, typeof(int));
            var getter = te.DefineMethod("get_Val", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            getter.Append(Expression.Constant(0));
            prop.SetGetMethod(getter);
            prop.DefaultValue = 42;
            Assert.Equal(42, prop.DefaultValue);
            prop.DefaultValue = null;
            Assert.Null(prop.DefaultValue);
            te.CreateType();
        }

        [Fact]
        public void PropertyEmitter_StaticProperty()
        {
            var te = _mod.DefineType($"PESP_{Guid.NewGuid():N}", TypeAttributes.Public);
            te.DefineDefaultConstructor();
            var field = te.DefineField("_sval", typeof(int), FieldAttributes.Private | FieldAttributes.Static);
            var prop = te.DefineProperty("SVal", PropertyAttributes.None, typeof(int));
            var getter = te.DefineMethod("get_SVal", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            getter.Append(field);
            prop.SetGetMethod(getter);
            Assert.True(prop.IsStatic);
            te.CreateType();
        }

        #endregion

        #region ParameterEmitter

        [Fact]
        public void ParameterEmitter_Properties()
        {
            var te = _mod.DefineType($"PAEP_{Guid.NewGuid():N}", TypeAttributes.Public);
            var me = te.DefineMethod("M", MethodAttributes.Public | MethodAttributes.Static, typeof(void));
            var pe = me.DefineParameter(typeof(int), "x");
            Assert.Equal(typeof(int), pe.ParameterType);
            Assert.Equal("x", pe.ParameterName);
            Assert.False(pe.IsByRef);
        }

        [Fact]
        public void ParameterEmitter_ByRef()
        {
            var te = _mod.DefineType($"PAEB_{Guid.NewGuid():N}", TypeAttributes.Public);
            var me = te.DefineMethod("M", MethodAttributes.Public | MethodAttributes.Static, typeof(void));
            var pe = me.DefineParameter(typeof(int).MakeByRefType(), "x");
            me.Append(Expression.Default(typeof(void)));
            Assert.True(pe.IsByRef);
        }

        [Fact]
        public void ParameterEmitter_Optional()
        {
            var te = _mod.DefineType($"PAEO_{Guid.NewGuid():N}", TypeAttributes.Public);
            te.DefineDefaultConstructor();
            var me = te.DefineMethod("M", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            var pe = me.DefineParameter(typeof(int), ParameterAttributes.Optional | ParameterAttributes.HasDefault, "x");
            pe.SetConstant(42);
            me.Append(pe);
            te.CreateType();
        }

        #endregion

        #region AbstractTypeEmitter

        [Fact]
        public void AbstractTypeEmitter_Properties()
        {
            var te = _mod.DefineType($"AP_{Guid.NewGuid():N}", TypeAttributes.Public);
            te.DefineDefaultConstructor();
            Assert.NotNull(te.Name);
            Assert.Equal(typeof(object), te.BaseType);
            Assert.False(te.IsGenericType);
            te.CreateType();
        }

        [Fact]
        public void AbstractTypeEmitter_IsCreated()
        {
            var te = _mod.DefineType($"IC_{Guid.NewGuid():N}", TypeAttributes.Public);
            Assert.False(te.IsCreated());
            te.DefineDefaultConstructor();
            te.CreateType();
            Assert.True(te.IsCreated());
        }

        [Fact]
        public void AbstractTypeEmitter_TypeInitializer()
        {
            var te = _mod.DefineType($"TI_{Guid.NewGuid():N}", TypeAttributes.Public);
            te.DefineDefaultConstructor();
            var ti = te.TypeInitializer;
            Assert.NotNull(ti);
        }

        [Fact]
        public void AbstractTypeEmitter_BeginScope()
        {
            var te = _mod.DefineType($"BS_{Guid.NewGuid():N}", TypeAttributes.Public);
            te.DefineDefaultConstructor();
            var scope = te.BeginScope();
            Assert.NotNull(scope);
            te.CreateType();
        }

        [Fact]
        public void AbstractTypeEmitter_DefineMethodOverride()
        {
            var te = _mod.DefineType($"DMO_{Guid.NewGuid():N}", TypeAttributes.Public);
            te.DefineDefaultConstructor();
            var toStringMethod = typeof(object).GetMethod("ToString");
            var overrideMethod = te.DefineMethodOverride(ref toStringMethod);
            overrideMethod.Append(Expression.Constant("overridden"));
            var type = te.CreateType();
            var obj = Activator.CreateInstance(type);
            Assert.Equal("overridden", obj.ToString());
        }

        [Fact]
        public void AbstractTypeEmitter_DefineField_Props()
        {
            var te = _mod.DefineType($"DFCA_{Guid.NewGuid():N}", TypeAttributes.Public);
            te.DefineDefaultConstructor();
            var field = te.DefineField("_val", typeof(int));
            Assert.Equal("_val", field.Name);
            Assert.Equal(typeof(int), field.RuntimeType);
        }

        [Fact]
        public void AbstractTypeEmitter_DefineProperty_WithIndexParams()
        {
            var te = _mod.DefineType($"DPI_{Guid.NewGuid():N}", TypeAttributes.Public);
            te.DefineDefaultConstructor();
            var prop = te.DefineProperty("Item", PropertyAttributes.None, typeof(int), new[] { typeof(int) });
            Assert.Equal("Item", prop.Name);
            var getter = te.DefineMethod("get_Item", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            getter.DefineParameter(typeof(int), "idx");
            getter.Append(Expression.Constant(0));
            prop.SetGetMethod(getter);
            te.CreateType();
        }

        #endregion

        #region ModuleEmitter

        [Fact]
        public void ModuleEmitter_DefineType_NameOnly()
        {
            var mod = new ModuleEmitter($"MDNO2_{Guid.NewGuid():N}");
            var te = mod.DefineType($"T_{Guid.NewGuid():N}");
            te.DefineDefaultConstructor();
            Assert.NotNull(te.CreateType());
        }

        [Fact]
        public void ModuleEmitter_DefineType_WithInterfaces()
        {
            var mod = new ModuleEmitter($"MDWI_{Guid.NewGuid():N}");
            var te = mod.DefineType($"T_{Guid.NewGuid():N}", TypeAttributes.Public, typeof(object), new[] { typeof(IDisposable) });
            te.DefineDefaultConstructor();
            var dispose = typeof(IDisposable).GetMethod("Dispose");
            var m = te.DefineMethodOverride(ref dispose);
            m.Append(Expression.Default(typeof(void)));
            var type = te.CreateType();
            Assert.True(typeof(IDisposable).IsAssignableFrom(type));
        }

        #endregion

        #region EnumEmitter

        [Fact]
        public void EnumEmitter_SetCustomAttribute()
        {
            var mod = new ModuleEmitter($"ESCA_{Guid.NewGuid():N}");
            var ee = mod.DefineEnum($"FE_{Guid.NewGuid():N}", TypeAttributes.Public, typeof(int));
            ee.DefineLiteral("None", 0);
            ee.DefineLiteral("A", 1);
            ee.DefineLiteral("B", 2);
            ee.DefineCustomAttribute<FlagsAttribute>();
            var type = ee.CreateType();
            Assert.True(type.IsEnum);
            Assert.True(type.IsDefined(typeof(FlagsAttribute)));
        }

        #endregion

        #region NestedClassEmitter

        [Fact]
        public void NestedClass_WithParent()
        {
            var te = _mod.DefineType($"NCP_{Guid.NewGuid():N}", TypeAttributes.Public);
            te.DefineDefaultConstructor();
            var nested = te.DefineNestedType("Inner2", TypeAttributes.NestedPublic, typeof(object));
            nested.DefineDefaultConstructor();
            te.CreateType();
        }

        [Fact]
        public void NestedClass_WithInterface()
        {
            var te = _mod.DefineType($"NCI_{Guid.NewGuid():N}", TypeAttributes.Public);
            te.DefineDefaultConstructor();
            var nested = te.DefineNestedType("Inner3", TypeAttributes.NestedPublic, typeof(object), new[] { typeof(IDisposable) });
            nested.DefineDefaultConstructor();
            var disposeMethod = typeof(IDisposable).GetMethod("Dispose");
            var overrideM = nested.DefineMethodOverride(ref disposeMethod);
            overrideM.Append(Expression.Default(typeof(void)));
            te.CreateType();
        }

        #endregion
    }
}
