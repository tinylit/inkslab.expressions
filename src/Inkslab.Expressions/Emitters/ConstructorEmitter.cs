using Inkslab.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Inkslab.Emitters
{
    /// <summary>
    /// 构造函数。
    /// </summary>
    public class ConstructorEmitter : Expression
    {
        private static readonly ParameterEmitter[] _emptyParameters = new ParameterEmitter[0];

        private int _parameterIndex = 0;
        private readonly TypeBuilder _typeBuilder;
        private readonly BlockExpression _blockAst = new BlockExpression();
        private readonly List<ParameterEmitter> _parameterEmitters = new List<ParameterEmitter>();

        private class ConstructorExpression : Expression
        {
            private readonly ConstructorInfo _constructor;
            private readonly Expression[] _parameters;

            public ConstructorExpression(ConstructorInfo constructor) : base(constructor.DeclaringType)
            {
                _constructor = constructor ?? throw new ArgumentNullException(nameof(constructor));
            }
            public ConstructorExpression(ConstructorInfo constructor, Expression[] parameters) : this(constructor)
            {
                ArgumentsCheck(constructor, parameters);

                _parameters = parameters;
            }

            private static void ArgumentsCheck(ConstructorInfo constructorInfo, Expression[] arguments)
            {
                var parameterInfos = constructorInfo.GetParameters();

                if (arguments?.Length != parameterInfos.Length)
                {
                    throw new AstException("指定参数和构造函数参数个数不匹配!");
                }

                if (!parameterInfos.Zip(arguments, (x, y) => new { x.ParameterType, y.RuntimeType })
                    .All(x => EmitUtils.IsAssignableFromSignatureTypes(x.ParameterType, x.RuntimeType)))
                {
                    throw new AstException("指定参数和构造函数参数类型不匹配!");
                }
            }

            public override void Load(ILGenerator ilg)
            {
                ilg.Emit(OpCodes.Ldarg_0);

                if (_parameters?.Length > 0)
                {
                    foreach (var expression in _parameters)
                    {
                        expression.Load(ilg);
                    }
                }

                ilg.Emit(OpCodes.Call, _constructor);
            }
        }

        private class InitConstructorEmitter : ConstructorEmitter
        {
            private readonly Type[] _typeArguments;
            private readonly ConstructorEmitter _constructorEmitter;

            public InitConstructorEmitter(ConstructorEmitter constructorEmitter, Type[] typeArguments) : base(constructorEmitter._typeBuilder, constructorEmitter.Attributes, constructorEmitter.Conventions)
            {
                _constructorEmitter = constructorEmitter;
                _typeArguments = typeArguments;
            }

            internal override ConstructorInfo Value
            {
                get
                {
                    var constructorBuilder = _constructorEmitter._constructorBuilder ?? throw new NotImplementedException();

                    var typeBuilder = _constructorEmitter._typeBuilder;

                    var declaringType = typeBuilder.DeclaringType;

                    if (declaringType is null || !declaringType.IsGenericType)
                    {
                        return TypeBuilder.GetConstructor(typeBuilder.MakeGenericType(_typeArguments), constructorBuilder);
                    }

                    var genericArguments = declaringType.GetGenericArguments();

                    var typeGenericArguments = new Type[genericArguments.Length + _typeArguments.Length];

                    System.Array.Copy(genericArguments, typeGenericArguments, genericArguments.Length);

                    System.Array.Copy(_typeArguments, 0, typeGenericArguments, genericArguments.Length, _typeArguments.Length);

                    return TypeBuilder.GetConstructor(typeBuilder.MakeGenericType(typeGenericArguments), constructorBuilder);
                }
            }

            public override ParameterEmitter[] GetParameters() => _constructorEmitter.GetParameters();

            public override ParameterEmitter DefineParameter(Type parameterType, ParameterAttributes attributes, string parameterName)
            {
                return _constructorEmitter.DefineParameter(parameterType, attributes, parameterName);
            }
            public override void InvokeBaseConstructor(ConstructorInfo constructor, params Expression[] parameters)
            {
                _constructorEmitter.InvokeBaseConstructor(constructor, parameters);
            }
            public override ConstructorEmitter MakeGenericConstructor(params Type[] typeArguments)
            {
                return _constructorEmitter.MakeGenericConstructor(typeArguments);
            }
        }

        private ConstructorBuilder _constructorBuilder;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="typeBuilder">父类型。</param>
        /// <param name="attributes">属性。</param>
        public ConstructorEmitter(TypeBuilder typeBuilder, MethodAttributes attributes) : this(typeBuilder, attributes, CallingConventions.Standard)
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="typeBuilder">父类型。</param>
        /// <param name="attributes">属性。</param>
        /// <param name="conventions">调用约定。</param>
        public ConstructorEmitter(TypeBuilder typeBuilder, MethodAttributes attributes, CallingConventions conventions) : base(typeBuilder)
        {
            _typeBuilder = typeBuilder;
            Attributes = attributes;
            Conventions = conventions;
        }

        /// <summary>
        /// 方法的名称。
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 方法的属性。
        /// </summary>
        public MethodAttributes Attributes { get; }

        /// <summary>
        /// 调用约定。
        /// </summary>
        public CallingConventions Conventions { get; }

        /// <summary>
        /// 成员。
        /// </summary>
        internal virtual ConstructorInfo Value
        {
            get
            {
                if (_constructorBuilder is null)
                {
                    throw new NotImplementedException();
                }

                var declaringType = _typeBuilder.DeclaringType;

                if (declaringType is null || !declaringType.IsGenericType)
                {
                    return _constructorBuilder;
                }

                return TypeBuilder.GetConstructor(_typeBuilder.MakeGenericType(declaringType.GetGenericArguments()), _constructorBuilder);
            }
        }

        private bool _initializedConstructor = false;
        private ParameterEmitter[] _parameters = _emptyParameters;

        /// <summary>
        /// 参数。
        /// </summary>
        public virtual ParameterEmitter[] GetParameters()
        {
            if (_parameterEmitters.Count > _parameters.Length)
            {
                _parameters = _parameterEmitters.ToArray();
            }

            return _parameters;
        }

        /// <summary>
        /// 声明参数。
        /// </summary>
        /// <param name="parameterInfo">参数。</param>
        /// <returns></returns>
        public ParameterEmitter DefineParameter(ParameterInfo parameterInfo)
        {
            var parameter = DefineParameter(parameterInfo.ParameterType, parameterInfo.Attributes, parameterInfo.Name);

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

        /// <summary>
        /// 声明参数。
        /// </summary>
        /// <param name="parameterType">参数类型。</param>
        /// <param name="attributes">属性。</param>
        /// <param name="parameterName">名称。</param>
        /// <returns></returns>
        public virtual ParameterEmitter DefineParameter(Type parameterType, ParameterAttributes attributes, string parameterName)
        {
            var parameter = new ParameterEmitter(parameterType, ++_parameterIndex, attributes, parameterName);

            _parameterEmitters.Add(parameter);

            return parameter;
        }

        /// <summary>
        /// 返回使用指定泛型类型参数从当前泛型方法定义构造的构造函数。
        /// </summary>
        /// <param name="typeArguments">表示泛型方法的类型参数的 <see cref="Type"/> 对象的数组。</param>
        /// <returns>一个 <see cref="ConstructorEmitter"/>，它表示使用指定泛型类型参数从当前泛型方法定义构造的构造函数。</returns>
        public virtual ConstructorEmitter MakeGenericConstructor(params Type[] typeArguments)
        {
            if (typeArguments?.Length > 0)
            {
                return new InitConstructorEmitter(this, typeArguments);
            }

            return this;
        }

        /// <summary>
        /// 调用父类构造函数。
        /// </summary>
        public void InvokeBaseConstructor()
        {
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var type = _typeBuilder.BaseType ?? typeof(object);

            if (type.IsGenericParameter)
            {
                type = type.GetGenericTypeDefinition();
            }

            InvokeBaseConstructor(type.GetConstructor(flags, null, Type.EmptyTypes, null) ?? throw new NotSupportedException($"“{type.Name}”不具备无参构造函数！"));
        }

        /// <summary>
        /// 调用父类构造函数。
        /// </summary>
        /// <param name="constructor">构造函数。</param>
        public void InvokeBaseConstructor(ConstructorInfo constructor) => InvokeBaseConstructor(constructor, System.Array.Empty<Expression>());

        /// <summary>
        /// 调用父类构造函数。
        /// </summary>
        /// <param name="constructor">构造函数。</param>
        /// <param name="parameters">参数。</param>
        public virtual void InvokeBaseConstructor(ConstructorInfo constructor, params Expression[] parameters)
        {
            Append(new ConstructorExpression(constructor, parameters ?? System.Array.Empty<Expression>()));

            _initializedConstructor = true;
        }

        /// <summary>
        /// 是否为空。
        /// </summary>
        public bool IsEmpty => _blockAst.IsEmpty;

        /// <summary>
        /// 添加代码。
        /// </summary>
        /// <param name="code">代码。</param>
        /// <returns>当前代码块。</returns>
        public BlockExpression Append(Expression code) => _blockAst.Append(code);

        /// <summary>
        /// 发行。
        /// </summary>
        public void Emit(ConstructorBuilder constructorBuilder)
        {
            if (constructorBuilder is null)
            {
                throw new ArgumentNullException(nameof(constructorBuilder));
            }

            _constructorBuilder = constructorBuilder;

            var attributes = constructorBuilder.MethodImplementationFlags;

            if ((attributes & MethodImplAttributes.Runtime) != MethodImplAttributes.IL)
            {
                return;
            }

            if (!_initializedConstructor)
            {
                InvokeBaseConstructor();
            }

            foreach (var parameter in _parameterEmitters)
            {
                parameter.Emit(constructorBuilder.DefineParameter(parameter.Position, parameter.Attributes, parameter.ParameterName));
            }

            var ilg = constructorBuilder.GetILGenerator();

            Load(ilg);

            ilg.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// 发行。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            var label = new Label(LabelKind.Return);

            MarkLabel(label);

            _blockAst.Load(ilg);

            label.MarkLabel(ilg);
        }
    }
}
