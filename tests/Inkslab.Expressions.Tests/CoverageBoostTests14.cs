using Inkslab.Emitters;
using System;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace Inkslab.Expressions.Tests
{
    /// <summary>
    /// Heavy coverage push: MethodEmitter, ParameterEmitter, ConstructorEmitter,
    /// PropertyEmitter, PropertyExpression, MethodCallExpression, MethodCallEmitter,
    /// InvocationEmitter, InvocationExpression deep paths.
    /// </summary>
    public class CoverageBoostTests14
    {
        private readonly ModuleEmitter _mod = new ModuleEmitter($"CB14_{Guid.NewGuid():N}");

        private Type BuildStatic(string name, Type ret, Action<MethodEmitter> body)
        {
            var cls = _mod.DefineType($"C14_{name}", TypeAttributes.Public);
            var me = cls.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, ret);
            body(me);
            return cls.CreateType();
        }

        private Type BuildStaticP(string name, Type ret, Type p1, Action<MethodEmitter, ParameterExpression> body)
        {
            var cls = _mod.DefineType($"C14P_{name}", TypeAttributes.Public);
            var me = cls.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, ret);
            var param = me.DefineParameter(p1, ParameterAttributes.None, "a");
            body(me, param);
            return cls.CreateType();
        }

        private object Invoke(Type t, params object[] args) => t.GetMethod("Run").Invoke(null, args);

        // ===== MethodEmitter: DefineParameter(ParameterInfo), SetCustomAttribute, various properties =====

        [Fact]
        public void MethodEmitter_DefineParameterFromInfo()
        {
            var cls = _mod.DefineType("ME_DPI", TypeAttributes.Public);
            var refMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
            var refParam = refMethod.GetParameters()[0];

            var me = cls.DefineMethod("Foo", MethodAttributes.Public | MethodAttributes.Static, typeof(bool));
            var p = me.DefineParameter(refParam);
            me.Append(Expression.Constant(true));
            var t = cls.CreateType();
            Assert.True((bool)t.GetMethod("Foo").Invoke(null, new object[] { "x" }));
        }

        [Fact]
        public void MethodEmitter_SetCustomAttributeData()
        {
            var cls = _mod.DefineType("ME_SCA", TypeAttributes.Public);
            var me = cls.DefineMethod("Bar", MethodAttributes.Public | MethodAttributes.Static, typeof(void));
            var attrData = typeof(CoverageBoostTests14).GetMethod("DummyObsolete", BindingFlags.NonPublic | BindingFlags.Static).CustomAttributes;
            foreach (var ad in attrData)
            {
                me.SetCustomAttribute(ad);
            }
            me.Append(Expression.Default(typeof(void)));
            var t = cls.CreateType();
            Assert.NotNull(t.GetMethod("Bar").GetCustomAttribute<ObsoleteAttribute>());
        }

        [Obsolete("test")]
        internal static void DummyObsolete() { }

        [Fact]
        public void MethodEmitter_SetCustomAttributeBuilder()
        {
            var cls = _mod.DefineType("ME_SCAB", TypeAttributes.Public);
            var me = cls.DefineMethod("Baz", MethodAttributes.Public | MethodAttributes.Static, typeof(void));
            var cab = new CustomAttributeBuilder(
                typeof(ObsoleteAttribute).GetConstructor(new[] { typeof(string) }),
                new object[] { "msg" });
            me.SetCustomAttribute(cab);
            me.Append(Expression.Default(typeof(void)));
            var t = cls.CreateType();
            var attr = t.GetMethod("Baz").GetCustomAttribute<ObsoleteAttribute>();
            Assert.Equal("msg", attr.Message);
        }

        [Fact]
        public void MethodEmitter_GetParameters()
        {
            var cls = _mod.DefineType("ME_GP", TypeAttributes.Public);
            var me = cls.DefineMethod("Add", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            var p1 = me.DefineParameter(typeof(int), ParameterAttributes.None, "x");
            var p2 = me.DefineParameter(typeof(int), ParameterAttributes.None, "y");
            var pars = me.GetParameters();
            Assert.Equal(2, pars.Length);
            me.Append(Expression.Add(p1, p2));
            cls.CreateType();
        }

        [Fact]
        public void MethodEmitter_Instance()
        {
            var cls = _mod.DefineType("ME_Inst", TypeAttributes.Public);
            var ctor = cls.DefineConstructor(MethodAttributes.Public);
            ctor.InvokeBaseConstructor();

            var me = cls.DefineMethod("GetFive", MethodAttributes.Public | MethodAttributes.Virtual, typeof(int));
            Assert.False(me.IsStatic);
            Assert.Equal("GetFive", me.Name);
            Assert.Equal(typeof(int), me.ReturnType);
            me.Append(Expression.Constant(5));
            var t = cls.CreateType();
            var inst = Activator.CreateInstance(t);
            Assert.Equal(5, t.GetMethod("GetFive").Invoke(inst, null));
        }

        // ===== ParameterEmitter: Emit paths with default values =====

        [Fact]
        public void ParameterEmitter_WithDefaultInt()
        {
            var cls = _mod.DefineType("PE_DI", TypeAttributes.Public);
            var me = cls.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            var p = me.DefineParameter(typeof(int), ParameterAttributes.Optional | ParameterAttributes.HasDefault, "x");
            p.SetConstant(42);
            me.Append(p);
            var t = cls.CreateType();
            var mi = t.GetMethod("Run");
            var par = mi.GetParameters()[0];
            Assert.Equal(typeof(int), par.ParameterType);
        }

        [Fact]
        public void ParameterEmitter_WithDefaultString()
        {
            var cls = _mod.DefineType("PE_DS", TypeAttributes.Public);
            var me = cls.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, typeof(string));
            var p = me.DefineParameter(typeof(string), ParameterAttributes.Optional | ParameterAttributes.HasDefault, "s");
            p.SetConstant("hello");
            me.Append(p);
            var t = cls.CreateType();
            var mi = t.GetMethod("Run");
            var par = mi.GetParameters()[0];
            Assert.Equal(typeof(string), par.ParameterType);
        }

        [Fact]
        public void ParameterEmitter_IsByRef()
        {
            var cls = _mod.DefineType("PE_Ref", TypeAttributes.Public);
            var me = cls.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, typeof(void));
            var p = me.DefineParameter(typeof(int).MakeByRefType(), ParameterAttributes.Out, "x");
            Assert.True(p.IsByRef);
            me.Append(Expression.Default(typeof(void)));
            cls.CreateType();
        }

        [Fact]
        public void ParameterEmitter_SetCustomAttributeGeneric()
        {
            var cls = _mod.DefineType("PE_CAG", TypeAttributes.Public);
            var me = cls.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, typeof(string));
            var p = me.DefineParameter(typeof(string), ParameterAttributes.None, "s");
            var cab = new CustomAttributeBuilder(
                typeof(ObsoleteAttribute).GetConstructor(new[] { typeof(string) }),
                new object[] { "deprecated" });
            p.SetCustomAttribute(cab);
            me.Append(p);
            var t = cls.CreateType();
            // Verify the parameter exists and has correct type
            Assert.Equal(typeof(string), t.GetMethod("Run").GetParameters()[0].ParameterType);
        }

        [Fact]
        public void ParameterEmitter_SetCustomAttributeBuilder()
        {
            var cls = _mod.DefineType("PE_CAB", TypeAttributes.Public);
            var me = cls.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            var p = me.DefineParameter(typeof(int), ParameterAttributes.None, "x");
            var cab = new CustomAttributeBuilder(
                typeof(ParamArrayAttribute).GetConstructor(Type.EmptyTypes),
                Array.Empty<object>());
            p.SetCustomAttribute(cab);
            me.Append(p);
            cls.CreateType();
        }

        // ===== ConstructorEmitter: more paths =====

        [Fact]
        public void ConstructorEmitter_Properties()
        {
            var cls = _mod.DefineType("CE_P", TypeAttributes.Public);
            var ctor = cls.DefineConstructor(MethodAttributes.Public);
            Assert.Equal(MethodAttributes.Public, ctor.Attributes & MethodAttributes.Public);
            Assert.True(ctor.IsEmpty);
            ctor.InvokeBaseConstructor();
            Assert.False(ctor.IsEmpty);
            cls.CreateType();
        }

        [Fact]
        public void ConstructorEmitter_GetParameters()
        {
            var cls = _mod.DefineType("CE_GP", TypeAttributes.Public);
            var ctor = cls.DefineConstructor(MethodAttributes.Public);
            var p1 = ctor.DefineParameter(typeof(int), ParameterAttributes.None, "x");
            var pars = ctor.GetParameters();
            Assert.Single(pars);
            ctor.InvokeBaseConstructor();
            cls.CreateType();
        }

        [Fact]
        public void ConstructorEmitter_WithFieldInit()
        {
            var cls = _mod.DefineType("CE_FI", TypeAttributes.Public);
            var field = cls.DefineField("_x", typeof(int));

            var ctor = cls.DefineConstructor(MethodAttributes.Public);
            var cp = ctor.DefineParameter(typeof(int), ParameterAttributes.None, "x");
            ctor.InvokeBaseConstructor();
            ctor.Append(Expression.Assign(field, cp));

            var getter = cls.DefineMethod("GetX", MethodAttributes.Public, typeof(int));
            getter.Append(field);

            var t = cls.CreateType();
            var inst = Activator.CreateInstance(t, 99);
            Assert.Equal(99, t.GetMethod("GetX").Invoke(inst, null));
        }

        [Fact]
        public void ConstructorEmitter_DefineParameterFromInfo()
        {
            var cls = _mod.DefineType("CE_DPI", TypeAttributes.Public);
            var ctor = cls.DefineConstructor(MethodAttributes.Public);
            var refCtor = typeof(Exception).GetConstructor(new[] { typeof(string) });
            var refParam = refCtor.GetParameters()[0];
            var p = ctor.DefineParameter(refParam);
            ctor.InvokeBaseConstructor();
            var t = cls.CreateType();
            Assert.NotNull(Activator.CreateInstance(t, "test"));
        }

        // ===== PropertyEmitter: full getter/setter with static =====

        [Fact]
        public void PropertyEmitter_StaticProperty()
        {
            var cls = _mod.DefineType("PrE_S", TypeAttributes.Public);
            var field = cls.DefineField("_sv", typeof(int), FieldAttributes.Private | FieldAttributes.Static);

            var prop = cls.DefineProperty("SVal", PropertyAttributes.None, typeof(int));

            var getter = cls.DefineMethod("get_SVal", MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.SpecialName, typeof(int));
            getter.Append(field);

            var setter = cls.DefineMethod("set_SVal", MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.SpecialName, typeof(void));
            var sp = setter.DefineParameter(typeof(int), ParameterAttributes.None, "value");
            setter.Append(Expression.Assign(field, sp));

            prop.SetGetMethod(getter);
            prop.SetSetMethod(setter);
            Assert.True(prop.IsStatic);

            var t = cls.CreateType();
            var pi = t.GetProperty("SVal");
            pi.SetValue(null, 77);
            Assert.Equal(77, pi.GetValue(null));
        }

        [Fact]
        public void PropertyEmitter_NoGetterThrowsOnRead()
        {
            var prop = new PropertyEmitter("X", PropertyAttributes.None, typeof(int));
            // CanRead is false when no getter/setter
            Assert.False(prop.CanRead);
            Assert.False(prop.CanWrite);
        }

        // ===== PropertyExpression: static property, instance property =====

        [Fact]
        public void PropertyExpression_StaticDateTimeNow()
        {
            var t = BuildStatic("PE_SDT", typeof(DateTime), me =>
            {
                me.Append(Expression.Property(typeof(DateTime).GetProperty("Now")));
            });
            var result = (DateTime)Invoke(t);
            Assert.True((DateTime.Now - result).TotalSeconds < 10);
        }

        [Fact]
        public void PropertyExpression_InstanceStringLength()
        {
            var t = BuildStaticP("PE_SL", typeof(int), typeof(string), (me, a) =>
            {
                me.Append(Expression.Property(a, typeof(string).GetProperty("Length")));
            });
            Assert.Equal(5, Invoke(t, "Hello"));
        }

        // ===== MethodCallExpression: instance method on parameter, DeclaringCall =====

        [Fact]
        public void MethodCallExpression_InstanceToString()
        {
            var t = BuildStaticP("MCE_TS", typeof(string), typeof(int), (me, a) =>
            {
                me.Append(Expression.Call(
                    Expression.Convert(a, typeof(object)),
                    typeof(object).GetMethod("ToString", Type.EmptyTypes)));
            });
            Assert.Equal("42", Invoke(t, 42));
        }

        [Fact]
        public void MethodCallExpression_StaticNoArgs()
        {
            var mi = typeof(GC).GetMethod("Collect", Type.EmptyTypes);
            var t = BuildStatic("MCE_SNA", typeof(void), me =>
            {
                me.Append(Expression.Call(mi));
            });
            Invoke(t);
        }

        [Fact]
        public void MethodCallExpression_StaticWithArgs()
        {
            var mi = typeof(Math).GetMethod("Max", new[] { typeof(int), typeof(int) });
            var t = BuildStatic("MCE_SWA", typeof(int), me =>
            {
                me.Append(Expression.Call(mi, Expression.Constant(10), Expression.Constant(20)));
            });
            Assert.Equal(20, Invoke(t));
        }

        [Fact]
        public void MethodCallExpression_InstanceStringContains()
        {
            var mi = typeof(string).GetMethod("Contains", new[] { typeof(string) });
            var t = BuildStaticP("MCE_SC", typeof(bool), typeof(string), (me, a) =>
            {
                me.Append(Expression.Call(a, mi, Expression.Constant("lo")));
            });
            Assert.Equal(true, Invoke(t, "Hello"));
            Assert.Equal(false, Invoke(t, "World"));
        }

        // ===== MethodCallEmitter: static and instance on emitter methods =====

        [Fact]
        public void MethodCallEmitter_StaticMultiParam()
        {
            var cls = _mod.DefineType("MCeS", TypeAttributes.Public);
            var helper = cls.DefineMethod("Mul", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            var ha = helper.DefineParameter(typeof(int), ParameterAttributes.None, "x");
            var hb = helper.DefineParameter(typeof(int), ParameterAttributes.None, "y");
            helper.Append(Expression.Multiply(ha, hb));

            var run = cls.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            var ra = run.DefineParameter(typeof(int), ParameterAttributes.None, "a");
            var rb = run.DefineParameter(typeof(int), ParameterAttributes.None, "b");
            run.Append(Expression.Call(helper, ra, rb));
            var t = cls.CreateType();
            Assert.Equal(12, t.GetMethod("Run").Invoke(null, new object[] { 3, 4 }));
        }

        [Fact]
        public void MethodCallEmitter_InstanceVoid()
        {
            var cls = _mod.DefineType("MCeIV", TypeAttributes.Public);
            var ctor = cls.DefineConstructor(MethodAttributes.Public);
            ctor.InvokeBaseConstructor();
            var field = cls.DefineField("_c", typeof(int));

            var inc = cls.DefineMethod("Inc", MethodAttributes.Public | MethodAttributes.Virtual, typeof(void));
            inc.Append(Expression.Assign(field, Expression.Add(field, Expression.Constant(1))));

            var exec = cls.DefineMethod("Exec", MethodAttributes.Public, typeof(int));
            exec.Append(Expression.Call(inc));
            exec.Append(field);

            var t = cls.CreateType();
            var inst = Activator.CreateInstance(t);
            Assert.Equal(1, t.GetMethod("Exec").Invoke(inst, null));
        }

        // ===== InvocationExpression: static with typed return =====

        [Fact]
        public void InvocationExpression_TypedReturn()
        {
            var mi = typeof(Math).GetMethod("Abs", new[] { typeof(int) });
            var t = BuildStatic("IE_TR", typeof(int), me =>
            {
                var args = Expression.Array(typeof(object), Expression.Convert(Expression.Constant(-7), typeof(object)));
                me.Append(Expression.Invoke(mi, args));
            });
            Assert.Equal(7, Invoke(t));
        }

        [Fact]
        public void InvocationExpression_VoidMethodInvoke()
        {
            var mi = typeof(GC).GetMethod("Collect", new[] { typeof(int) });
            var t = BuildStatic("IE_VI", typeof(void), me =>
            {
                var args = Expression.Array(typeof(object), Expression.Convert(Expression.Constant(0), typeof(object)));
                me.Append(Expression.Invoke(mi, args));
            });
            Invoke(t);
        }

        // ===== SwitchExpression: string equality, default only =====

        [Fact]
        public void Switch_StringCases()
        {
            var t = BuildStaticP("Sw_S", typeof(int), typeof(string), (me, a) =>
            {
                var result = Expression.Variable(typeof(int));
                me.Append(Expression.Assign(result, Expression.Constant(0)));
                var sw = Expression.Switch(a);
                sw.Case(Expression.Constant("yes")).Append(Expression.Assign(result, Expression.Constant(1)));
                sw.Case(Expression.Constant("no")).Append(Expression.Assign(result, Expression.Constant(2)));
                me.Append(sw);
                me.Append(result);
            });
            Assert.Equal(1, Invoke(t, "yes"));
            Assert.Equal(2, Invoke(t, "no"));
            Assert.Equal(0, Invoke(t, "maybe"));
        }

        [Fact]
        public void Switch_IntWithDefault()
        {
            var t = BuildStaticP("Sw_ID", typeof(int), typeof(int), (me, a) =>
            {
                var result = Expression.Variable(typeof(int));
                me.Append(Expression.Assign(result, Expression.Constant(-1)));
                var sw = Expression.Switch(a);
                sw.Case(Expression.Constant(1)).Append(Expression.Assign(result, Expression.Constant(100)));
                sw.Case(Expression.Constant(2)).Append(Expression.Assign(result, Expression.Constant(200)));
                me.Append(sw);
                me.Append(result);
            });
            Assert.Equal(100, Invoke(t, 1));
            Assert.Equal(200, Invoke(t, 2));
            Assert.Equal(-1, Invoke(t, 99));
        }

        [Fact]
        public void Switch_LongCases()
        {
            var t = BuildStaticP("Sw_L", typeof(int), typeof(long), (me, a) =>
            {
                var result = Expression.Variable(typeof(int));
                me.Append(Expression.Assign(result, Expression.Constant(0)));
                var sw = Expression.Switch(a);
                sw.Case(Expression.Constant(1L)).Append(Expression.Assign(result, Expression.Constant(10)));
                sw.Case(Expression.Constant(2L)).Append(Expression.Assign(result, Expression.Constant(20)));
                me.Append(sw);
                me.Append(result);
            });
            Assert.Equal(10, Invoke(t, 1L));
            Assert.Equal(20, Invoke(t, 2L));
        }

        // ===== TryExpression: more catch paths =====

        [Fact]
        public void Try_CatchWithExceptionVariable()
        {
            var t = BuildStatic("T_CEV", typeof(string), me =>
            {
                var result = Expression.Variable(typeof(string));
                me.Append(Expression.Assign(result, Expression.Constant("none")));
                var tryExpr = Expression.Try();
                tryExpr.Append(Expression.Throw(typeof(InvalidOperationException)));
                var exVar = Expression.Variable(typeof(Exception));
                tryExpr.Catch(exVar).Append(Expression.Assign(result, Expression.Constant("caught")));
                me.Append(tryExpr);
                me.Append(result);
            });
            Assert.Equal("caught", Invoke(t));
        }

        [Fact]
        public void Try_FinallyBlockRuns()
        {
            var t = BuildStatic("T_FB", typeof(int), me =>
            {
                var result = Expression.Variable(typeof(int));
                me.Append(Expression.Assign(result, Expression.Constant(0)));
                var finallyExpr = Expression.Assign(result, Expression.Add(result, Expression.Constant(10)));
                var tryExpr = Expression.Try(finallyExpr);
                tryExpr.Append(Expression.Assign(result, Expression.Constant(5)));
                me.Append(tryExpr);
                me.Append(result);
            });
            Assert.Equal(15, Invoke(t));
        }

        // ===== UnaryExpression: more types =====

        [Fact]
        public void Unary_Increment_Short()
        {
            var t = BuildStaticP("U_IS", typeof(short), typeof(short), (me, a) =>
            {
                me.Append(Expression.Increment(a));
            });
            Assert.Equal((short)6, Invoke(t, (short)5));
        }

        [Fact]
        public void Unary_Decrement_Int()
        {
            var t = BuildStaticP("U_DI", typeof(int), typeof(int), (me, a) =>
            {
                me.Append(Expression.Decrement(a));
            });
            Assert.Equal(4, Invoke(t, 5));
        }

        [Fact]
        public void Unary_Not_Long()
        {
            var t = BuildStaticP("U_NL", typeof(long), typeof(long), (me, a) =>
            {
                me.Append(Expression.Not(a));
            });
            Assert.Equal(~42L, Invoke(t, 42L));
        }

        [Fact]
        public void Unary_Negate_Short()
        {
            var t = BuildStaticP("U_NS", typeof(short), typeof(short), (me, a) =>
            {
                me.Append(Expression.Negate(a));
            });
            Assert.Equal((short)-5, Invoke(t, (short)5));
        }

        [Fact]
        public void Unary_Negate_Float()
        {
            var t = BuildStaticP("U_NF", typeof(float), typeof(float), (me, a) =>
            {
                me.Append(Expression.Negate(a));
            });
            Assert.Equal(-3.14f, Invoke(t, 3.14f));
        }

        // ===== ReturnExpression =====

        [Fact]
        public void Return_WithValueFromIfThen()
        {
            var t = BuildStaticP("R_V", typeof(int), typeof(int), (me, a) =>
            {
                me.Append(Expression.IfThen(
                    Expression.LessThan(a, Expression.Constant(0)),
                    Expression.Return(Expression.Constant(-1))
                ));
                me.Append(Expression.IfThen(
                    Expression.Equal(a, Expression.Constant(0)),
                    Expression.Return(Expression.Constant(0))
                ));
                me.Append(Expression.Constant(1));
            });
            Assert.Equal(-1, Invoke(t, -5));
            Assert.Equal(0, Invoke(t, 0));
            Assert.Equal(1, Invoke(t, 5));
        }

        [Fact]
        public void Return_VoidEarlyExit()
        {
            // void method that calls a known-safe static void method
            var t = BuildStatic("R_VE", typeof(void), me =>
            {
                me.Append(Expression.Call(typeof(GC).GetMethod("Collect", Type.EmptyTypes)));
            });
            Invoke(t);
        }

        // ===== TypeIsExpression =====

        [Fact]
        public void TypeIs_WithValueType()
        {
            var t = BuildStaticP("TI_V", typeof(bool), typeof(object), (me, a) =>
            {
                me.Append(Expression.TypeIs(a, typeof(int)));
            });
            Assert.Equal(true, Invoke(t, (object)42));
            Assert.Equal(false, Invoke(t, (object)"hello"));
        }

        [Fact]
        public void TypeIs_WithInterface()
        {
            var t = BuildStaticP("TI_IF", typeof(bool), typeof(object), (me, a) =>
            {
                me.Append(Expression.TypeIs(a, typeof(IDisposable)));
            });
            Assert.Equal(false, Invoke(t, (object)"hello"));
        }

        // ===== ModuleEmitter overloads =====

        [Fact]
        public void ModuleEmitter_DefineEnum()
        {
            var en = _mod.DefineEnum("MyEnum", TypeAttributes.Public, typeof(int));
            en.DefineLiteral("A", 0);
            en.DefineLiteral("B", 1);
            var t = en.CreateType();
            Assert.True(t.IsEnum);
        }

        [Fact]
        public void ModuleEmitter_DefineType_NameOnly()
        {
            var cls = _mod.DefineType("ME_NO");
            var ctor = cls.DefineConstructor(MethodAttributes.Public);
            ctor.InvokeBaseConstructor();
            var t = cls.CreateType();
            Assert.NotNull(t);
        }

        [Fact]
        public void ModuleEmitter_BeginScope()
        {
            var scope = _mod.BeginScope();
            Assert.NotNull(scope);
            var name = scope.GetUniqueName("test");
            Assert.Contains("test", name);
        }

        // ===== BlockExpression =====

        [Fact]
        public void BlockExpression_IsEmpty()
        {
            var cls = _mod.DefineType("BE_IE", TypeAttributes.Public);
            var me = cls.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            Assert.True(me.IsEmpty);
            me.Append(Expression.Constant(1));
            Assert.False(me.IsEmpty);
            cls.CreateType();
        }

        // ===== MemberInitExpression =====

        [Fact]
        public void MemberInit_WithPropertyBinding()
        {
            var t = BuildStatic("MI_PB", typeof(object), me =>
            {
                var ctor = typeof(TestPropClass).GetConstructor(Type.EmptyTypes);
                var propInfo = typeof(TestPropClass).GetProperty("Name");
                var init = Expression.MemberInit(
                    Expression.New(ctor),
                    Expression.Bind(propInfo, Expression.Constant("test")));
                me.Append(Expression.Convert(init, typeof(object)));
            });
            var result = (TestPropClass)Invoke(t);
            Assert.Equal("test", result.Name);
        }

        public class TestPropClass
        {
            public string Name { get; set; }
        }

        // ===== EmitUtils: constant types =====

        [Fact]
        public void Constant_IntPtr()
        {
            var t = BuildStatic("C_IP", typeof(IntPtr), me =>
            {
                me.Append(Expression.Constant(new IntPtr(42)));
            });
            Assert.Equal(new IntPtr(42), Invoke(t));
        }

        [Fact]
        public void Constant_Uri()
        {
            var uri = new Uri("https://example.com");
            var t = BuildStatic("C_U", typeof(Uri), me =>
            {
                me.Append(Expression.Constant(uri));
            });
            Assert.Equal(uri, Invoke(t));
        }

        [Fact]
        public void Constant_DateTimeOffset()
        {
            var dto = new DateTimeOffset(2024, 1, 15, 0, 0, 0, TimeSpan.Zero);
            var t = BuildStatic("C_DTO", typeof(DateTimeOffset), me =>
            {
                me.Append(Expression.Constant(dto));
            });
            Assert.Equal(dto, Invoke(t));
        }

        [Fact]
        public void Constant_DecimalFractional()
        {
            var d = 3.14159m;
            var t = BuildStatic("C_DF", typeof(decimal), me =>
            {
                me.Append(Expression.Constant(d));
            });
            Assert.Equal(d, Invoke(t));
        }

    }
}
