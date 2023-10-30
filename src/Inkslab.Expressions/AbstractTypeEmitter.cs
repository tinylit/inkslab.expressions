using Inkslab.Emitters;
using Inkslab.Expressions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Security;

namespace Inkslab
{
    /// <summary>
    /// 抽象类。
    /// </summary>
    public abstract class AbstractTypeEmitter
    {
        private bool initializedGenericType = false;
        private readonly List<Type> genericArguments = new List<Type>();
        private readonly Type baseType;
        private readonly Type[] interfaces;
        private readonly TypeBuilder typeBuilder;
        private readonly INamingScope namingScope;
        private readonly List<MethodEmitter> methods = new List<MethodEmitter>();
        private readonly List<AbstractTypeEmitter> abstracts = new List<AbstractTypeEmitter>();
        private readonly List<ConstructorEmitter> constructors = new List<ConstructorEmitter>();
        private readonly Dictionary<string, FieldEmitter> fields = new Dictionary<string, FieldEmitter>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, PropertyEmitter> properties = new Dictionary<string, PropertyEmitter>(StringComparer.OrdinalIgnoreCase);

        private readonly object lockObj = new object();

        /// <summary>
        /// 静态构造函数。
        /// </summary>
        public class TypeInitializerEmitter : BlockExpression
        {
            internal TypeInitializerEmitter()
            {
            }

            /// <summary>
            /// 发行。
            /// </summary>
            internal void Emit(ConstructorBuilder builder)
            {
                var attributes = builder.MethodImplementationFlags;

                if ((attributes & MethodImplAttributes.Runtime) != MethodImplAttributes.IL)
                {
                    return;
                }

                var ilg = builder.GetILGenerator();

                if (IsEmpty)
                {
                    ilg.Emit(OpCodes.Nop);
                }
                else
                {
                    base.Load(ilg);
                }

                ilg.Emit(OpCodes.Ret);
            }
        }

        /// <summary>
        /// 方法。
        /// </summary>
        private sealed class MethodOverrideEmitter : MethodEmitter
        {
            private readonly MethodBuilder methodBuilder;
            private readonly MethodInfo methodInfoDeclaration;

            public MethodOverrideEmitter(MethodBuilder methodBuilder, MethodInfo methodInfoDeclaration, Type returnType) : base(methodBuilder.Name, methodBuilder.Attributes, returnType)
            {
                this.methodBuilder = methodBuilder;
                this.methodInfoDeclaration = methodInfoDeclaration;
            }

            public override bool IsGenericMethod => methodInfoDeclaration.IsGenericMethod;

            public override Type[] GetGenericArguments() => methodBuilder.GetGenericArguments();

            public ParameterEmitter DefineParameter(ParameterInfo parameterInfo, Type parameterType)
            {
                var parameter = base.DefineParameter(parameterType, parameterInfo.Attributes, parameterInfo.Name);

                if (parameterInfo.HasDefaultValue)
                {
                    parameter.SetConstant(parameterInfo.DefaultValue);
                }

                foreach (var customAttribute in parameterInfo.CustomAttributes)
                {
                    parameter.SetCustomAttribute(customAttribute);
                }

                return parameter;
            }

            public override ParameterEmitter DefineParameter(Type parameterType, ParameterAttributes attributes, string name)
            {
                throw new AstException("重写方法不支持自定义参数!");
            }

            public override void Emit(TypeBuilder builder)
            {
                if (builder != methodBuilder.DeclaringType)
                {
                    throw new ArgumentException("方法声明类型和类型构造器不一致!", nameof(builder));
                }

                Emit(methodBuilder);

                if (methodInfoDeclaration.DeclaringType.IsInterface)
                {
                    builder.DefineMethodOverride(methodBuilder, methodInfoDeclaration);
                }
            }
        }

        /// <summary>
        /// 在此模块中用指定的名称为私有类型构造 TypeBuilder。
        /// </summary>
        /// <param name="moduleEmitter">模块。</param>
        /// <param name="name">类型的完整路径，其中包括命名空间。 name 不能包含嵌入的 null。</param>
        protected AbstractTypeEmitter(ModuleEmitter moduleEmitter, string name)
        {
            if (moduleEmitter is null)
            {
                throw new ArgumentNullException(nameof(moduleEmitter));
            }

            namingScope = moduleEmitter.BeginScope();
            typeBuilder = ModuleEmitter.DefineType(moduleEmitter, name);
        }

        /// <summary>
        /// 在给定类型名称和类型特性的情况下，构造 TypeBuilder。
        /// </summary>
        /// <param name="moduleEmitter">模块。</param>
        /// <param name="name">类型的完整路径。 name 不能包含嵌入的 null。</param>
        /// <param name="attributes">已定义类型的属性。</param>
        protected AbstractTypeEmitter(ModuleEmitter moduleEmitter, string name, TypeAttributes attributes) : this(moduleEmitter, name, attributes, null)
        {
        }

        /// <summary>
        /// 在给定类型名称、类型特性和已定义类型扩展的类型的情况下，构造 TypeBuilder。
        /// </summary>
        /// <param name="moduleEmitter">模块。</param>
        /// <param name="name">类型的完整路径。 name 不能包含嵌入的 null。</param>
        /// <param name="attributes">与类型关联的属性。</param>
        /// <param name="baseType">已定义类型扩展的类型。</param>
        protected AbstractTypeEmitter(ModuleEmitter moduleEmitter, string name, TypeAttributes attributes, Type baseType) : this(moduleEmitter, name, attributes, baseType, null)
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
        protected AbstractTypeEmitter(ModuleEmitter moduleEmitter, string name, TypeAttributes attributes, Type baseType, Type[] interfaces)
        {
            if (moduleEmitter is null)
            {
                throw new ArgumentNullException(nameof(moduleEmitter));
            }

            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (baseType is null)
            {
                if (interfaces?.Length > 0)
                {
                    if (interfaces.Any(x => x.IsGenericTypeDefinition))
                    {
                        AnalyzeGenericParameters(ref interfaces);

                        this.interfaces = interfaces;
                    }

                    typeBuilder = ModuleEmitter.DefineType(moduleEmitter, name, attributes, typeof(object), interfaces);
                }
                else
                {
                    typeBuilder = ModuleEmitter.DefineType(moduleEmitter, name, attributes);
                }
            }
            else if (baseType.IsGenericTypeDefinition)
            {
                this.baseType = baseType;

                AnalyzeGenericParameters(baseType, ref interfaces);

                this.interfaces = interfaces;

                typeBuilder = ModuleEmitter.DefineType(moduleEmitter, name, attributes);
            }
            else if (interfaces?.Length > 0)
            {
                if (interfaces.Any(x => x.IsGenericTypeDefinition))
                {
                    AnalyzeGenericParameters(baseType, ref interfaces);

                    this.interfaces = interfaces;

                    typeBuilder = ModuleEmitter.DefineType(moduleEmitter, name, attributes, baseType);
                }
                else
                {
                    typeBuilder = ModuleEmitter.DefineType(moduleEmitter, name, attributes, baseType, interfaces);
                }
            }
            else
            {
                typeBuilder = ModuleEmitter.DefineType(moduleEmitter, name, attributes, baseType);
            }

            namingScope = moduleEmitter.BeginScope();
        }

        /// <summary>
        /// 创建匿名类型的构造函数。
        /// </summary>
        /// <param name="typeEmitter">匿名类型的所属类型。</param>
        /// <param name="name">匿名类型名称。</param>
        protected AbstractTypeEmitter(AbstractTypeEmitter typeEmitter, string name) : this(typeEmitter, name, TypeAttributes.NotPublic)
        {
            namingScope = typeEmitter.BeginScope();
            typeBuilder = DefineType(typeEmitter, name);
        }

        /// <summary>
        /// 创建匿名类型的构造函数。
        /// </summary>
        /// <param name="typeEmitter">匿名类型的所属类型。</param>
        /// <param name="name">匿名类型名称。</param>
        /// <param name="attributes">匿名函数类型。</param>
        protected AbstractTypeEmitter(AbstractTypeEmitter typeEmitter, string name, TypeAttributes attributes) : this(typeEmitter, name, attributes, null)
        {
        }

        /// <summary>
        /// 创建匿名类型的构造函数。
        /// </summary>
        /// <param name="typeEmitter">匿名类型的所属类型。</param>
        /// <param name="name">匿名类型名称。</param>
        /// <param name="attributes">匿名函数类型。</param>
        /// <param name="baseType">匿名函数基类。</param>
        protected AbstractTypeEmitter(AbstractTypeEmitter typeEmitter, string name, TypeAttributes attributes, Type baseType) : this(typeEmitter, name, attributes, baseType, Type.EmptyTypes)
        {
        }

        /// <summary>
        /// 创建匿名类型的构造函数。
        /// </summary>
        /// <param name="typeEmitter">匿名类型的所属类型。</param>
        /// <param name="name">匿名类型名称。</param>
        /// <param name="attributes">匿名函数类型。</param>
        /// <param name="baseType">匿名函数基类。</param>
        /// <param name="interfaces">匿名函数实现接口。</param>
        protected AbstractTypeEmitter(AbstractTypeEmitter typeEmitter, string name, TypeAttributes attributes, Type baseType, Type[] interfaces)
        {
            if (typeEmitter is null)
            {
                throw new ArgumentNullException(nameof(typeEmitter));
            }

            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (baseType is null)
            {
                if (interfaces?.Length > 0)
                {
                    if (interfaces.Any(x => x.IsGenericTypeDefinition))
                    {
                        AnalyzeGenericParameters(ref interfaces);

                        this.interfaces = interfaces;
                    }

                    typeBuilder = DefineType(typeEmitter, name, attributes, typeof(object), interfaces);

                }
                else
                {
                    typeBuilder = DefineType(typeEmitter, name, attributes);
                }
            }
            else if (baseType.IsGenericTypeDefinition)
            {
                this.baseType = baseType;

                AnalyzeGenericParameters(baseType, ref interfaces);

                this.interfaces = interfaces;

                typeBuilder = DefineType(typeEmitter, name, attributes);
            }
            else if (interfaces?.Length > 0)
            {
                if (interfaces.Any(x => x.IsGenericTypeDefinition))
                {
                    AnalyzeGenericParameters(baseType, ref interfaces);

                    this.interfaces = interfaces;

                    typeBuilder = DefineType(typeEmitter, name, attributes, baseType);
                }
                else
                {
                    typeBuilder = DefineType(typeEmitter, name, attributes, baseType, interfaces);
                }
            }
            else
            {
                typeBuilder = DefineType(typeEmitter, name, attributes, baseType);
            }

            namingScope = typeEmitter.BeginScope();

            typeEmitter.abstracts.Add(this);
        }

        /// <summary>
        /// 开始名称范围。
        /// </summary>
        /// <returns></returns>
        public INamingScope BeginScope() => namingScope.BeginScope();

        private string GetUniqueName(string name) => namingScope.GetUniqueName(name);

        private static TypeAttributes MakeNestedTypeAttributes(TypeAttributes attributes)
        {
            if ((attributes & TypeAttributes.Public) == TypeAttributes.Public)
            {
                attributes ^= TypeAttributes.Public;
                attributes |= TypeAttributes.NestedPublic;
            }

            if ((attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic)
            {
                attributes ^= TypeAttributes.NotPublic;
                attributes |= TypeAttributes.NestedPrivate;
            }

            if ((attributes & TypeAttributes.Class) != TypeAttributes.Class
                && (attributes & TypeAttributes.AnsiClass) != TypeAttributes.AnsiClass
                && (attributes & TypeAttributes.AutoClass) != TypeAttributes.AutoClass
                && (attributes & TypeAttributes.UnicodeClass) != TypeAttributes.UnicodeClass
                && (attributes & TypeAttributes.Interface) != TypeAttributes.Interface)
            {
                attributes |= TypeAttributes.Class;
            }

            return attributes | TypeAttributes.Sealed;
        }

        private static TypeBuilder DefineType(AbstractTypeEmitter emitter, string name) => emitter.typeBuilder.DefineNestedType(emitter.GetUniqueName(name));
        private static TypeBuilder DefineType(AbstractTypeEmitter emitter, string name, TypeAttributes attr) => emitter.typeBuilder.DefineNestedType(emitter.GetUniqueName(name), MakeNestedTypeAttributes(attr));
        private static TypeBuilder DefineType(AbstractTypeEmitter emitter, string name, TypeAttributes attr, Type parent) => emitter.typeBuilder.DefineNestedType(emitter.GetUniqueName(name), MakeNestedTypeAttributes(attr), parent);
        private static TypeBuilder DefineType(AbstractTypeEmitter emitter, string name, TypeAttributes attr, Type parent, Type[] interfaces) => emitter.typeBuilder.DefineNestedType(emitter.GetUniqueName(name), MakeNestedTypeAttributes(attr), parent, interfaces);

        private GenericTypeParameterBuilder[] DefineGenericParameters()
        {
            if (typeBuilder.DeclaringType?.IsGenericType ?? false)
            {
                genericArguments.InsertRange(0, typeBuilder.DeclaringType.GetGenericArguments());
            }

            if (genericArguments.Count == 0)
            {
                return Array.Empty<GenericTypeParameterBuilder>();
            }

            var names = new string[genericArguments.Count];

            for (int i = 0; i < genericArguments.Count; i++)
            {
                names[i] = genericArguments[i].Name;
            }

            var typeParameterBuilders = typeBuilder.DefineGenericParameters(names.ToArray());

            for (int i = 0; i < genericArguments.Count; i++)
            {
                var g = genericArguments[i];
                var t = typeParameterBuilders[i];

                t.SetGenericParameterAttributes(g.GenericParameterAttributes);

                if (g is not GenericTypeParameterBuilder)
                {
                    t.SetInterfaceConstraints(AdjustGenericConstraints(typeParameterBuilders, g.GetGenericParameterConstraints()));
                }

                //? 避免重复约束。 T2 where T, T, new()
                if (g.BaseType is null || g.BaseType.IsGenericParameter)
                {
                    continue;
                }

                if (g.BaseType == typeof(object))
                {
                    continue;
                }

                if (HasGenericParameter(g.BaseType))
                {
                    t.SetBaseTypeConstraint(MakeGenericParameter(g.BaseType, typeParameterBuilders));
                }
                else
                {
                    t.SetBaseTypeConstraint(g.BaseType);
                }
            }

            if (baseType is null || !baseType.IsGenericTypeDefinition)
            {
                typeBuilder.SetParent(baseType ?? typeof(object));

                if (interfaces?.Length > 0)
                {
                    foreach (var interfaceType in interfaces)
                    {
                        typeBuilder.AddInterfaceImplementation(MakeGenericType(interfaceType, typeParameterBuilders));
                    }
                }
            }
            else if (interfaces is null || interfaces.Length == 0)
            {
                var serviceType = MakeGenericType(baseType, typeParameterBuilders);

                typeBuilder.SetParent(serviceType);

                foreach (var interfaceType in baseType.GetInterfaces())
                {
                    typeBuilder.AddInterfaceImplementation(MakeGenericType(interfaceType, typeParameterBuilders));
                }
            }
            else
            {
                var serviceType = MakeGenericType(baseType, typeParameterBuilders);

                typeBuilder.SetParent(serviceType);

                foreach (var interfaceType in baseType.GetInterfaces())
                {
                    typeBuilder.AddInterfaceImplementation(MakeGenericType(interfaceType, typeParameterBuilders));
                }

                foreach (var interfaceType in interfaces)
                {
                    typeBuilder.AddInterfaceImplementation(MakeGenericType(interfaceType, typeParameterBuilders));
                }
            }

            return typeParameterBuilders;
        }

        private static Type MakeGenericType(Type serviceType, GenericTypeParameterBuilder[] typeParameterBuilders)
        {
            if (serviceType.IsGenericTypeDefinition)
            {
                int offset = 0;

                var genericArguments = serviceType.GetGenericArguments();

                for (int i = 0; i < genericArguments.Length; i++)
                {
                    if (genericArguments[i].IsGenericParameter)
                    {
                        genericArguments[i] = typeParameterBuilders[i - offset];
                    }
                    else
                    {
                        offset--;
                    }
                }

                return serviceType.MakeGenericType(genericArguments);
            }

            return serviceType;
        }

        private void AnalyzeGenericParameters(ref Type[] interfaces)
        {
            if (interfaces is null || interfaces.Length == 0)
            {

            }
            else if (interfaces.Length == 1)
            {
                Type interfaceType = interfaces[0];

                if (interfaceType.IsGenericTypeDefinition)
                {
                    genericArguments.AddRange(interfaceType.GetGenericArguments());
                }
            }
            else
            {
                List<Type> independentTypes = new List<Type>(interfaces.Length);

                Type[] simpleTypes = Array.FindAll(interfaces, type => !type.IsGenericTypeDefinition);
                Type[] genericTypes = Array.FindAll(interfaces, type => type.IsGenericTypeDefinition);

                independentTypes.AddRange(genericTypes);

                foreach (var interfaceType in genericTypes)
                {
                    Array.ForEach(interfaceType.GetInterfaces(), type => independentTypes.Remove(type));
                }

                if (independentTypes.Count > 1)
                {
                    throw new AstException("无法分析多个没有继承关系的泛型定义接口，请自己实现泛型关系！");
                }

                genericArguments.AddRange(independentTypes.SelectMany(x => x.GetGenericArguments()));

                independentTypes.AddRange(simpleTypes);

                interfaces = independentTypes.ToArray();
            }
        }

        private void AnalyzeGenericParameters(Type baseType, ref Type[] interfaces)
        {
            if (baseType is null || !baseType.IsGenericTypeDefinition)
            {
                AnalyzeGenericParameters(ref interfaces);
            }
            else if (interfaces is null || interfaces.Length == 0)
            {
                if (baseType.IsGenericTypeDefinition)
                {
                    genericArguments.AddRange(baseType.GetGenericArguments());
                }
            }
            else
            {
                if (Array.Exists(interfaces, x => x.IsGenericTypeDefinition))
                {
                    Type[] simpleTypes = Array.FindAll(interfaces, type => !type.IsGenericTypeDefinition);
                    Type[] genericTypes = Array.FindAll(interfaces, type => type.IsGenericTypeDefinition);

                    List<Type> independentTypes = new List<Type>(genericTypes.Length);

                    independentTypes.AddRange(genericTypes);

                    Type[] baseInterfaceTypes = baseType.GetInterfaces();

                    Array.ForEach(baseInterfaceTypes, type => independentTypes.Remove(type));

                    if (independentTypes.Count > 1)
                    {
                        throw new AstException("无法分析多个没有继承关系的泛型定义接口，请自己实现泛型关系！");
                    }

                    genericArguments.AddRange(baseType.GetGenericArguments());

                    independentTypes.AddRange(simpleTypes);

                    // 移除基础类型有的简单类型。
                    Array.ForEach(baseInterfaceTypes, type => independentTypes.Remove(type));

                    interfaces = independentTypes.ToArray();
                }
                else
                {
                    genericArguments.AddRange(baseType.GetGenericArguments());
                }
            }
        }

        /// <summary>
        /// 静态构造函数。
        /// </summary>
        public TypeInitializerEmitter TypeInitializer = new TypeInitializerEmitter();

        /// <summary>
        /// 是否为泛型类。
        /// </summary>
        public bool IsGenericType => genericArguments.Count > 0;

        /// <summary>
        /// 泛型参数。
        /// </summary>
        /// <returns></returns>
        public Type[] GetGenericArguments()
        {
            CheckGenericParameters();

            return typeBuilder.GetGenericArguments();
        }

        /// <summary>
        /// 类名称。
        /// </summary>
        public string Name => typeBuilder.Name;

        /// <summary>
        /// 父类型。
        /// </summary>
        public Type BaseType
        {
            get
            {
                if (typeBuilder.IsInterface)
                {
                    return typeof(object);
                }

                return baseType ?? typeBuilder.BaseType;
            }
        }

        /// <summary>
        /// 接口。
        /// </summary>
        /// <returns></returns>
        public Type[] GetInterfaces() => interfaces ?? Type.EmptyTypes;

        /// <summary>
        /// 当前类型。
        /// </summary>
        [DebuggerHidden]
        internal Type Value => typeBuilder;

        private void CheckGenericParameters()
        {
            if (initializedGenericType)
            {
                return;
            }

            lock (lockObj)
            {
                if (initializedGenericType)
                {
                    return;
                }

                initializedGenericType = true;

                DefineGenericParameters();
            }
        }

        /// <summary>
        /// 声明泛型。
        /// </summary>
        /// <param name="genericTypeArguments">泛型参数数组。</param>
        /// <exception cref="ArgumentNullException">参数 <paramref name="genericTypeArguments"/> 为 null。</exception>
        /// <exception cref="InvalidOperationException">类型已经定义了泛型约束。</exception>
        /// <exception cref="NotSupportedException">数组约束 <see cref="Type.IsGenericParameter"/> 为 false。</exception>
        /// <returns>指定 <paramref name="genericTypeArguments"/> 泛型数组与类的泛型顺序对应的泛型类型数组。</returns>
        public GenericTypeParameterBuilder[] DefineGenericParameters(Type[] genericTypeArguments)
        {
            if (genericTypeArguments is null)
            {
                throw new ArgumentNullException(nameof(genericTypeArguments));
            }

            if (initializedGenericType)
            {
                throw new InvalidOperationException("类型已经定义了泛型约束！");
            }

            lock (lockObj)
            {
                if (initializedGenericType)
                {
                    throw new InvalidOperationException("类型已经定义了泛型约束！");
                }

                initializedGenericType = true;

                for (int i = 0; i < genericTypeArguments.Length; i++)
                {
                    var genericType = genericTypeArguments[i];

                    if (genericType.IsGenericParameter)
                    {
                        genericArguments.Add(genericType);
                    }
                    else
                    {
                        throw new NotSupportedException($"类型“{genericType}”不是泛型类型参数！");
                    }
                }

                if (genericTypeArguments.Length == 0)
                {
                    return Array.Empty<GenericTypeParameterBuilder>();
                }

                var typeParameterBuilders = DefineGenericParameters();

                if (typeParameterBuilders.Length == genericTypeArguments.Length)
                {
                    return typeParameterBuilders;
                }

                var typeParameters = new GenericTypeParameterBuilder[genericTypeArguments.Length];

                Array.Copy(typeParameterBuilders, typeParameterBuilders.Length - genericTypeArguments.Length, typeParameters, 0, genericTypeArguments.Length);

                return typeParameters;
            }
        }

        /// <summary>
        /// 创建字段。
        /// </summary>
        /// <param name="fieldInfo">字段。</param>
        /// <returns></returns>
        public FieldEmitter DefineField(FieldInfo fieldInfo)
        {
            var fieldEmitter = DefineField(fieldInfo.Name, fieldInfo.FieldType, fieldInfo.Attributes);

            if ((fieldInfo.Attributes & FieldAttributes.HasDefault) == FieldAttributes.HasDefault)
            {
                fieldEmitter.SetConstant(fieldInfo.GetRawConstantValue());
            }

            foreach (var attributeData in fieldInfo.CustomAttributes)
            {
                fieldEmitter.SetCustomAttribute(attributeData);
            }

            return fieldEmitter;
        }

        /// <summary>
        /// 创建字段。
        /// </summary>
        /// <param name="name">名称。</param>
        /// <param name="fieldType">类型。</param>
        /// <returns></returns>
        public FieldEmitter DefineField(string name, Type fieldType) => DefineField(name, fieldType, true);

        /// <summary>
        /// 创建字段。
        /// </summary>
        /// <param name="name">名称。</param>
        /// <param name="fieldType">类型。</param>
        /// <param name="serializable">能否序列化。</param>
        /// <returns></returns>
        public FieldEmitter DefineField(string name, Type fieldType, bool serializable)
        {
            var atts = FieldAttributes.Private;

            if (!serializable)
            {
                atts |= FieldAttributes.NotSerialized;
            }

            return DefineField(name, fieldType, atts);
        }

        /// <summary>
        /// 创建字段。
        /// </summary>
        /// <param name="name">名称。</param>
        /// <param name="fieldType">类型。</param>
        /// <param name="atts">属性。</param>
        /// <returns></returns>
        public FieldEmitter DefineField(string name, Type fieldType, FieldAttributes atts)
        {
            name = namingScope.GetUniqueName(name);

            var fieldEmitter = new FieldEmitter(name, fieldType, atts);

            fields.Add(name, fieldEmitter);

            return fieldEmitter;
        }

        /// <summary>
        /// 创建属性。
        /// </summary>
        /// <param name="name">名称。</param>
        /// <param name="attributes">属性。</param>
        /// <param name="propertyType">类型。</param>
        /// <returns></returns>
        public PropertyEmitter DefineProperty(string name, PropertyAttributes attributes, Type propertyType) => DefineProperty(name, attributes, propertyType, null);

        /// <summary>
        /// 创建属性。
        /// </summary>
        /// <param name="name">名称。</param>
        /// <param name="attributes">属性。</param>
        /// <param name="propertyType">类型。</param>
        /// <param name="arguments">参数。</param>
        /// <returns></returns>
        public PropertyEmitter DefineProperty(string name, PropertyAttributes attributes, Type propertyType, Type[] arguments)
        {
            var propEmitter = new PropertyEmitter(name, attributes, propertyType, arguments);
            properties.Add(name, propEmitter);
            return propEmitter;
        }

        /// <summary>
        /// 创建方法。
        /// </summary>
        /// <param name="name">名称。</param>
        /// <param name="attrs">属性。</param>
        /// <param name="returnType">类型。</param>
        /// <returns></returns>
        public MethodEmitter DefineMethod(string name, MethodAttributes attrs, Type returnType)
        {
            var member = new MethodEmitter(name, attrs, returnType);

            methods.Add(member);

            return member;
        }

        private static bool IsInternal(MethodBase method)
        {
            return method.IsAssembly || (method.IsFamilyAndAssembly && !method.IsFamilyOrAssembly);
        }

        private static MethodAttributes ObtainAttributes(MethodInfo methodInfo)
        {
            var attributes = MethodAttributes.Virtual;

            if (methodInfo.IsFinal || methodInfo.DeclaringType.IsInterface)
            {
                attributes |= MethodAttributes.NewSlot;
            }

            if (methodInfo.IsPublic)
            {
                attributes |= MethodAttributes.Public;
            }

            if (methodInfo.IsHideBySig)
            {
                attributes |= MethodAttributes.HideBySig;
            }

            if (IsInternal(methodInfo))
            {
                attributes |= MethodAttributes.Assembly;
            }

            if (methodInfo.IsFamilyAndAssembly)
            {
                attributes |= MethodAttributes.FamANDAssem;
            }
            else if (methodInfo.IsFamilyOrAssembly)
            {
                attributes |= MethodAttributes.FamORAssem;
            }
            else if (methodInfo.IsFamily)
            {
                attributes |= MethodAttributes.Family;
            }

            if (methodInfo.IsSpecialName)
            {
                attributes |= MethodAttributes.SpecialName;
            }

            return attributes;
        }

        /// <summary>
        /// 定义重写方法。
        /// </summary>
        /// <param name="methodInfoDeclaration">被重写的方法。</param>
        /// <returns></returns>
        public MethodEmitter DefineMethodOverride(ref MethodInfo methodInfoDeclaration)
        {
            CheckGenericParameters();

            var parameterInfos = methodInfoDeclaration.GetParameters();

            var parameterTypes = new Type[parameterInfos.Length];

            for (int i = 0; i < parameterInfos.Length; i++)
            {
                parameterTypes[i] = parameterInfos[i].ParameterType;
            }

            var methodBuilder = typeBuilder.DefineMethod(methodInfoDeclaration.Name, ObtainAttributes(methodInfoDeclaration), CallingConventions.Standard);

            var genericArguments = Type.EmptyTypes;
            Type returnType = methodInfoDeclaration.ReturnType;
            Type runtimeType = methodInfoDeclaration.ReturnType;
            ParameterInfo returnParameter = methodInfoDeclaration.ReturnParameter;
            GenericTypeParameterBuilder[] newGenericParameters = new GenericTypeParameterBuilder[0];

            MethodInfo methodInfoOriginal = methodInfoDeclaration;

            Type declaringType = methodInfoOriginal.DeclaringType;

            if (HasGenericParameter(declaringType))
            {
                bool hasDeclaringTypes = false;

                Type declaringTypeEmit = typeBuilder;

                Type typeDefinition = declaringType.GetGenericTypeDefinition();

                if (declaringType.IsClass)
                {
                    while ((declaringTypeEmit = declaringTypeEmit.BaseType) != null)
                    {
                        if (declaringTypeEmit.IsGenericType && declaringTypeEmit.GetGenericTypeDefinition() == typeDefinition)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    foreach (var interfaceType in declaringTypeEmit.GetInterfaces())
                    {
                        if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeDefinition)
                        {
                            declaringTypeEmit = interfaceType;

                            break;
                        }
                    }
                }

                Type[] declaringTypes = declaringType.GetGenericArguments();

                Type[] declaringTypeParameters = declaringTypeEmit.GetGenericArguments();

                if (methodInfoOriginal.IsGenericMethod)
                {
                    genericArguments = methodInfoOriginal.GetGenericArguments();

                    newGenericParameters = methodBuilder.DefineGenericParameters(genericArguments.Select(x => x.Name).ToArray());

                    for (int i = 0; i < genericArguments.Length; i++)
                    {
                        var g = genericArguments[i];
                        var t = newGenericParameters[i];

                        t.SetGenericParameterAttributes(g.GenericParameterAttributes);

                        t.SetInterfaceConstraints(AdjustGenericConstraints(newGenericParameters, methodInfoOriginal, genericArguments, g.GetGenericParameterConstraints()));

                        //? 避免重复约束。 T2 where T, T, new()
                        if (g.BaseType.IsGenericParameter)
                        {
                            continue;
                        }

                        if (HasGenericParameter(g.BaseType))
                        {
                            t.SetBaseTypeConstraint(MakeGenericParameter(g.BaseType, genericArguments, declaringTypeParameters, newGenericParameters));
                        }
                        else
                        {
                            t.SetBaseTypeConstraint(g.BaseType);
                        }
                    }

                    for (int i = 0; i < parameterTypes.Length; i++)
                    {
                        if (HasGenericParameter(parameterTypes[i]))
                        {
                            parameterTypes[i] = MakeGenericParameter(parameterTypes[i], genericArguments, declaringTypeParameters, newGenericParameters);
                        }
                    }

                    var methodInfoGeneric = methodInfoOriginal.MakeGenericMethod(newGenericParameters);

                    if (hasDeclaringTypes = HasGenericParameter(returnType, declaringTypes))
                    {
                        runtimeType = MakeGenericParameter(returnType, newGenericParameters, declaringTypeParameters);

                        returnType = MakeGenericParameter(returnType, genericArguments, declaringTypeParameters, newGenericParameters);
                    }
                    else if (HasGenericParameter(returnType))
                    {
                        runtimeType = methodInfoGeneric.ReturnType;

                        returnType = MakeGenericParameter(returnType, genericArguments, declaringTypeParameters, newGenericParameters);
                    }

                    methodInfoDeclaration = new DynamicMethod(methodInfoOriginal, methodInfoGeneric, declaringType.MakeGenericType(declaringTypeParameters), runtimeType, declaringTypeParameters, hasDeclaringTypes);
                }
                else
                {
                    for (int i = 0; i < parameterTypes.Length; i++)
                    {
                        if (HasGenericParameter(parameterTypes[i]))
                        {
                            parameterTypes[i] = MakeGenericParameter(parameterTypes[i], declaringTypeParameters);
                        }
                    }

                    if (hasDeclaringTypes = HasGenericParameter(returnType, declaringTypes))
                    {
                        runtimeType = returnType = MakeGenericParameter(returnType, declaringTypeParameters);
                    }
                    else if (HasGenericParameter(returnType))
                    {
                        returnType = MakeGenericParameter(returnType, declaringTypeParameters);
                    }

                    methodInfoDeclaration = new DynamicMethod(methodInfoOriginal, declaringType, runtimeType, declaringTypeParameters, hasDeclaringTypes);
                }
            }
            else if (methodInfoOriginal.IsGenericMethod)
            {
                genericArguments = methodInfoOriginal.GetGenericArguments();

                newGenericParameters = methodBuilder.DefineGenericParameters(genericArguments.Select(x => x.Name).ToArray());

                for (int i = 0; i < genericArguments.Length; i++)
                {
                    var g = genericArguments[i];

                    var t = newGenericParameters[i];

                    t.SetGenericParameterAttributes(g.GenericParameterAttributes);

                    t.SetInterfaceConstraints(AdjustGenericConstraints(newGenericParameters, methodInfoOriginal, genericArguments, g.GetGenericParameterConstraints()));

                    //? 避免重复约束。 T2 where T, T, new()
                    if (g.BaseType.IsGenericParameter)
                    {
                        continue;
                    }

                    if (HasGenericParameter(g.BaseType))
                    {
                        t.SetBaseTypeConstraint(MakeGenericParameter(g.BaseType, genericArguments));
                    }
                    else
                    {
                        t.SetBaseTypeConstraint(g.BaseType);
                    }
                }

                for (int i = 0; i < parameterTypes.Length; i++)
                {
                    if (HasGenericParameter(parameterTypes[i]))
                    {
                        parameterTypes[i] = MakeGenericParameter(parameterTypes[i], newGenericParameters);
                    }
                }

                if (HasGenericParameter(returnType))
                {
                    runtimeType = methodInfoOriginal.ReturnType;

                    returnType = MakeGenericParameter(returnType, newGenericParameters);
                }

                methodInfoDeclaration = new DynamicMethod(methodInfoOriginal, methodInfoOriginal.MakeGenericMethod(newGenericParameters), methodInfoOriginal.DeclaringType, runtimeType, Type.EmptyTypes, false);
            }

            var overrideEmitter = new MethodOverrideEmitter(methodBuilder, methodInfoOriginal, returnType);

            for (int i = 0; i < parameterInfos.Length; i++)
            {
                overrideEmitter.DefineParameter(parameterInfos[i], parameterTypes[i]);
            }

            Type[] returnRequiredCustomModifiers;
            Type[] returnOptionalCustomModifiers;
            Type[][] parametersRequiredCustomModifiers;
            Type[][] parametersOptionalCustomModifiers;

            returnRequiredCustomModifiers = returnParameter.GetRequiredCustomModifiers();
            Array.Reverse(returnRequiredCustomModifiers);

            returnOptionalCustomModifiers = returnParameter.GetOptionalCustomModifiers();
            Array.Reverse(returnOptionalCustomModifiers);

            int parameterCount = parameterInfos.Length;
            parametersRequiredCustomModifiers = new Type[parameterCount][];
            parametersOptionalCustomModifiers = new Type[parameterCount][];
            for (int i = 0; i < parameterCount; ++i)
            {
                parametersRequiredCustomModifiers[i] = parameterInfos[i].GetRequiredCustomModifiers();
                Array.Reverse(parametersRequiredCustomModifiers[i]);

                parametersOptionalCustomModifiers[i] = parameterInfos[i].GetOptionalCustomModifiers();
                Array.Reverse(parametersOptionalCustomModifiers[i]);
            }

            methodBuilder.SetSignature(
                returnType,
                returnRequiredCustomModifiers,
                returnOptionalCustomModifiers,
                parameterTypes,
                parametersRequiredCustomModifiers,
                parametersOptionalCustomModifiers);

            methods.Add(overrideEmitter);

            return overrideEmitter;
        }

        /// <summary>
        /// 声明构造函数。
        /// </summary>
        /// <param name="attributes">属性。</param>
        /// <returns></returns>
        public ConstructorEmitter DefineConstructor(MethodAttributes attributes) => DefineConstructor(attributes, CallingConventions.Standard);

        /// <summary>
        /// 声明构造函数。
        /// </summary>
        /// <param name="attributes">属性。</param>
        /// <param name="conventions">调用约定。</param>
        /// <returns></returns>
        public ConstructorEmitter DefineConstructor(MethodAttributes attributes, CallingConventions conventions)
        {
            var member = new ConstructorEmitter(typeBuilder, attributes, conventions);
            constructors.Add(member);
            return member;
        }

        /// <summary>
        /// 创建默认构造函数。
        /// </summary>
        /// <returns></returns>
        public void DefineDefaultConstructor()
        {
            constructors.Add(new ConstructorEmitter(typeBuilder, MethodAttributes.Public));
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

            typeBuilder.SetCustomAttribute(EmitUtils.CreateCustomAttribute(attributeData));
        }

        /// <summary>
        /// 自定义标记。
        /// </summary>
        /// <param name="attribute">标记。</param>
        public void DefineCustomAttribute(CustomAttributeBuilder attribute)
        {
            typeBuilder.SetCustomAttribute(attribute);
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
        /// 在此模块中用指定的名称为私有类型构造 TypeBuilder。
        /// </summary>
        /// <param name="name">类型的完整路径，其中包括命名空间。 name 不能包含嵌入的 null。</param>
        /// <returns>具有指定名称的私有类型。</returns>
        [SecuritySafeCritical]
        public NestedClassEmitter DefineNestedType(string name) => new NestedClassEmitter(this, name);

        /// <summary>
        /// 在给定类型名称和类型特性的情况下，构造 TypeBuilder。
        /// </summary>
        /// <param name="name">类型的完整路径。 name 不能包含嵌入的 null。</param>
        /// <param name="attr">已定义类型的属性。</param>
        /// <returns>用所有请求的特性创建的 TypeBuilder。</returns>
        [SecuritySafeCritical]
        public NestedClassEmitter DefineNestedType(string name, TypeAttributes attr) => new NestedClassEmitter(this, name, attr);

        /// <summary>
        /// 在给定类型名称、类型特性和已定义类型扩展的类型的情况下，构造 TypeBuilder。
        /// </summary>
        /// <param name="name">类型的完整路径。 name 不能包含嵌入的 null。</param>
        /// <param name="attr">与类型关联的属性。</param>
        /// <param name="parent">已定义类型扩展的类型。</param>
        /// <returns>用所有请求的特性创建的 TypeBuilder。</returns>
        [SecuritySafeCritical]
        public NestedClassEmitter DefineNestedType(string name, TypeAttributes attr, Type parent) => new NestedClassEmitter(this, name, attr, parent);

        /// <summary>
        /// 在给定类型名称、特性、已定义类型扩展的类型和已定义类型实现的接口的情况下，构造 TypeBuilder。
        /// </summary>
        /// <param name="name">类型的完整路径。 name 不能包含嵌入的 null。</param>
        /// <param name="attr">与类型关联的特性。</param>
        /// <param name="parent">已定义类型扩展的类型。</param>
        /// <param name="interfaces">类型实现的接口列表。</param>
        /// <returns>用所有请求的特性创建的 TypeBuilder。</returns>
        [ComVisible(true)]
        [SecuritySafeCritical]
        public NestedClassEmitter DefineNestedType(string name, TypeAttributes attr, Type parent, Type[] interfaces) => new NestedClassEmitter(this, name, attr, parent, interfaces);

        /// <summary>
        /// 发行。
        /// </summary>
        protected virtual Type Emit()
        {
            CheckGenericParameters();

            foreach (FieldEmitter emitter in fields.Values)
            {
                emitter.Emit(typeBuilder.DefineField(emitter.Name, emitter.RuntimeType, emitter.Attributes));
            }

            if (!typeBuilder.IsInterface && constructors.Count == 0)
            {
                DefineDefaultConstructor();
            }

            foreach (var emitter in abstracts)
            {
                emitter.Emit();
            }

            foreach (ConstructorEmitter emitter in constructors)
            {
                var parameters = emitter.GetParameters();

                var parameterTypes = new Type[parameters.Length];

                for (int i = 0; i < parameters.Length; i++)
                {
                    parameterTypes[i] = parameters[i].RuntimeType;
                }

                emitter.Emit(typeBuilder.DefineConstructor(emitter.Attributes, emitter.Conventions, parameterTypes));
            }

            foreach (MethodEmitter emitter in methods)
            {
                emitter.Emit(typeBuilder);
            }

            foreach (PropertyEmitter emitter in properties.Values)
            {
                emitter.Emit(typeBuilder.DefineProperty(emitter.Name, emitter.Attributes, emitter.RuntimeType, emitter.ParameterTypes));
            }

            TypeInitializer.Emit(typeBuilder.DefineTypeInitializer());

#if NETSTANDARD2_0_OR_GREATER
            return typeBuilder.CreateTypeInfo().AsType();
#else
            return typeBuilder.CreateType();
#endif
        }

        private static Type AdjustConstraintToNewGenericParameters(Type constraint, GenericTypeParameterBuilder[] newGenericParameters)
        {
            if (constraint.IsGenericType)
            {
                var genericArgumentsOfConstraint = constraint.GetGenericArguments();

                for (var i = 0; i < genericArgumentsOfConstraint.Length; ++i)
                {
                    genericArgumentsOfConstraint[i] = AdjustConstraintToNewGenericParameters(genericArgumentsOfConstraint[i], newGenericParameters);
                }

                if (!constraint.IsGenericTypeDefinition)
                {
                    constraint = constraint.GetGenericTypeDefinition();
                }

                return constraint.MakeGenericType(genericArgumentsOfConstraint);
            }
            else
            {
                return constraint;
            }
        }

        private static Type[] AdjustGenericConstraints(GenericTypeParameterBuilder[] newGenericParameters, Type[] constraints)
        {
            Type[] adjustedConstraints = new Type[constraints.Length];

            for (var i = 0; i < constraints.Length; i++)
            {
                adjustedConstraints[i] = AdjustConstraintToNewGenericParameters(constraints[i], newGenericParameters);
            }

            return adjustedConstraints;
        }

        private static Type AdjustConstraintToNewGenericParameters(
            Type constraint, MethodInfo methodToCopyGenericsFrom, Type[] originalGenericParameters,
            GenericTypeParameterBuilder[] newGenericParameters)
        {
            if (constraint.IsGenericType)
            {
                var genericArgumentsOfConstraint = constraint.GetGenericArguments();

                for (var i = 0; i < genericArgumentsOfConstraint.Length; ++i)
                {
                    genericArgumentsOfConstraint[i] =
                        AdjustConstraintToNewGenericParameters(genericArgumentsOfConstraint[i], methodToCopyGenericsFrom,
                                                               originalGenericParameters, newGenericParameters);
                }
                return constraint.GetGenericTypeDefinition().MakeGenericType(genericArgumentsOfConstraint);
            }
            else if (constraint.IsGenericParameter)
            {
                if (constraint.DeclaringMethod is null)
                {
                    Trace.Assert(constraint.DeclaringType.IsGenericTypeDefinition);
                    Trace.Assert(methodToCopyGenericsFrom.DeclaringType.IsGenericType
                                 && constraint.DeclaringType == methodToCopyGenericsFrom.DeclaringType.GetGenericTypeDefinition(),
                                 "When a generic method parameter has a constraint on a generic type parameter, the generic type must be the declaring typer of the method.");

                    var index = Array.IndexOf(constraint.DeclaringType.GetGenericArguments(), constraint);
                    Trace.Assert(index != -1, "The generic parameter comes from the given type.");

                    var genericArguments = methodToCopyGenericsFrom.DeclaringType.GetGenericArguments();

                    return genericArguments[index]; // these are the actual, concrete types
                }
                else
                {
                    var index = Array.IndexOf(originalGenericParameters, constraint);
                    Trace.Assert(index != -1,
                                 "When a generic method parameter has a constraint on another method parameter, both parameters must be declared on the same method.");
                    return newGenericParameters[index];
                }
            }
            else
            {
                return constraint;
            }
        }

        private static Type[] AdjustGenericConstraints(GenericTypeParameterBuilder[] newGenericParameters, MethodInfo methodInfo, Type[] originalGenericArguments, Type[] constraints)
        {
            Type[] adjustedConstraints = new Type[constraints.Length];
            for (var i = 0; i < constraints.Length; i++)
            {
                adjustedConstraints[i] = AdjustConstraintToNewGenericParameters(constraints[i], methodInfo, originalGenericArguments, newGenericParameters);
            }
            return adjustedConstraints;
        }

        private static bool HasGenericParameter(Type type)
        {
            if (type.IsGenericParameter)
            {
                return true;
            }

            if (type.IsGenericType)
            {
                return Array.Exists(type.GetGenericArguments(), HasGenericParameter);
            }

            if (type.IsArray || type.IsByRef)
            {
                return HasGenericParameter(type.GetElementType());
            }

            return false;
        }

        private static bool HasGenericParameter(Type type, Type[] declaringTypes)
        {
            if (type.IsGenericParameter)
            {
                return Array.IndexOf(declaringTypes, type) > -1;
            }

            if (type.IsGenericType)
            {
                return Array.Exists(type.GetGenericArguments(), x => HasGenericParameter(x, declaringTypes));
            }

            if (type.IsArray || type.IsByRef)
            {
                return HasGenericParameter(type.GetElementType(), declaringTypes);
            }

            return false;
        }

        private static Type MakeGenericParameter(Type type, Type[] typeParameterBuilders)
        {
            if (type.IsGenericParameter)
            {
                return typeParameterBuilders[type.GenericParameterPosition];
            }

            if (type.IsGenericType)
            {
                bool flag = false;

                var genericArguments = type.GetGenericArguments();

                for (int i = 0; i < genericArguments.Length; i++)
                {
                    if (HasGenericParameter(genericArguments[i]))
                    {
                        flag = true;
                        genericArguments[i] = MakeGenericParameter(genericArguments[i], typeParameterBuilders);
                    }

                }

                return flag
                    ? type.GetGenericTypeDefinition().MakeGenericType(genericArguments)
                    : type;
            }

            if (type.IsArray)
            {
                Type elementType = MakeGenericParameter(type.GetElementType(), typeParameterBuilders);
                int rank = type.GetArrayRank();

                return rank == 1
                    ? elementType.MakeArrayType()
                    : elementType.MakeArrayType(rank);
            }

            if (type.IsByRef)
            {
                Type elementType = MakeGenericParameter(type.GetElementType(), typeParameterBuilders);

                return elementType.MakeByRefType();
            }

            return type;
        }

        private static Type MakeGenericParameter(Type type, Type[] genericArguments, Type[] typeParameterBuilders)
        {
            if (type.IsGenericParameter)
            {
                if (Array.IndexOf(genericArguments, type) > -1)
                {
                    return type;
                }

                return typeParameterBuilders[type.GenericParameterPosition];
            }

            if (type.IsGenericType)
            {
                var genericArguments2 = type.GetGenericArguments();

                for (int i = 0; i < genericArguments.Length; i++)
                {
                    genericArguments2[i] = MakeGenericParameter(genericArguments[i], genericArguments, typeParameterBuilders);
                }

                return type.GetGenericTypeDefinition().MakeGenericType(genericArguments);
            }

            if (type.IsArray)
            {
                Type elementType = MakeGenericParameter(type.GetElementType(), genericArguments, typeParameterBuilders);
                int rank = type.GetArrayRank();

                return rank == 1
                    ? elementType.MakeArrayType()
                    : elementType.MakeArrayType(rank);
            }

            if (type.IsByRef)
            {
                Type elementType = MakeGenericParameter(type.GetElementType(), genericArguments, typeParameterBuilders);

                return elementType.MakeByRefType();
            }

            return type;
        }

        private static Type MakeGenericParameter(Type type, Type[] genericArguments, Type[] declaringTypeParameters, GenericTypeParameterBuilder[] newGenericParameters)
        {
            if (type.IsGenericParameter)
            {
                if (Array.IndexOf(genericArguments, type) > -1)
                {
                    return newGenericParameters[type.GenericParameterPosition];
                }

                return declaringTypeParameters[type.GenericParameterPosition];
            }

            if (type.IsGenericType)
            {
                Debug.Assert(type.IsGenericTypeDefinition == false);

                bool flag = false;

                var genericArguments2 = type.GetGenericArguments();

                for (int i = 0; i < genericArguments2.Length; i++)
                {
                    if (HasGenericParameter(genericArguments2[i]))
                    {
                        genericArguments2[i] = MakeGenericParameter(genericArguments2[i], genericArguments, declaringTypeParameters, newGenericParameters);
                    }

                }

                return flag
                    ? type.GetGenericTypeDefinition().MakeGenericType(genericArguments2)
                    : type;
            }

            if (type.IsArray)
            {
                Type elementType = MakeGenericParameter(type.GetElementType(), genericArguments, declaringTypeParameters, newGenericParameters);
                int rank = type.GetArrayRank();

                return rank == 1
                    ? elementType.MakeArrayType()
                    : elementType.MakeArrayType(rank);
            }

            if (type.IsByRef)
            {
                Type elementType = MakeGenericParameter(type.GetElementType(), genericArguments, declaringTypeParameters, newGenericParameters);

                return elementType.MakeByRefType();
            }

            return type;
        }

        /// <summary>
        /// 是否已创建。
        /// </summary>
        /// <returns></returns>
        public bool IsCreated() => typeBuilder.IsCreated();
    }
}
