using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace Inkslab
{
    internal class DynamicMethod : MethodInfo
    {
        private readonly Type _declaringType;
        private readonly Type _returnType;
        private readonly MethodInfo _methodInfoOriginal;
        private readonly Type[] _declaringTypeParameters;
        private readonly MethodInfo _methodInfoDeclaration;
        private readonly bool _hasDeclaringTypes;

        public DynamicMethod(MethodInfo methodInfoOriginal, MethodInfo methodInfoDeclaration, Type declaringType, Type returnType, Type[] declaringTypeParameters, bool hasDeclaringTypes)
        {
            this._methodInfoOriginal = methodInfoOriginal;
            this._methodInfoDeclaration = methodInfoDeclaration;
            this._declaringType = declaringType;
            this._returnType = returnType;
            this._declaringTypeParameters = declaringTypeParameters;
            this._hasDeclaringTypes = hasDeclaringTypes;
        }

        public DynamicMethod(MethodInfo methodInfoOriginal, Type declaringType, Type returnType, Type[] declaringTypeParameters, bool hasDeclaringTypes) : this(methodInfoOriginal, methodInfoOriginal, declaringType, returnType, declaringTypeParameters, hasDeclaringTypes)
        {
        }

        public MethodInfo RuntimeMethod => _methodInfoDeclaration;

        public override string Name => _methodInfoOriginal.Name;

        public override Type DeclaringType => _declaringType;

        public override Type ReflectedType => _methodInfoOriginal.ReflectedType;

        public override ParameterInfo ReturnParameter => _methodInfoOriginal.ReturnParameter;

        public override Type ReturnType => _returnType;

        public override MethodInfo GetBaseDefinition() => _methodInfoOriginal;

        public override ParameterInfo[] GetParameters() => _methodInfoOriginal.GetParameters();

#if NET45_OR_GREATER
        public override Delegate CreateDelegate(Type delegateType) => _methodInfoDeclaration.CreateDelegate(delegateType);

        public override Delegate CreateDelegate(Type delegateType, object target) => _methodInfoDeclaration.CreateDelegate(delegateType, target);
#endif

        public override Type[] GetGenericArguments() => _methodInfoOriginal.GetGenericArguments();

        public override ICustomAttributeProvider ReturnTypeCustomAttributes => _methodInfoOriginal.ReturnTypeCustomAttributes;

        public override RuntimeMethodHandle MethodHandle => _methodInfoOriginal.MethodHandle;

        public override MethodAttributes Attributes => _methodInfoOriginal.Attributes;

        public override CallingConventions CallingConvention => _methodInfoOriginal.CallingConvention;

        public override bool ContainsGenericParameters => _methodInfoOriginal.ContainsGenericParameters;

#if NET45_OR_GREATER
        public override IEnumerable<CustomAttributeData> CustomAttributes => _methodInfoOriginal.CustomAttributes;
#endif

        public override bool IsGenericMethod => _methodInfoOriginal.IsGenericMethod;
        public override bool IsGenericMethodDefinition => false;
        public override bool IsSecurityCritical => _methodInfoOriginal.IsSecurityCritical;
        public override bool IsSecuritySafeCritical => _methodInfoOriginal.IsSecuritySafeCritical;
        public override bool IsSecurityTransparent => _methodInfoOriginal.IsSecurityTransparent;
        public override MemberTypes MemberType => _methodInfoOriginal.MemberType;
        public override int MetadataToken => _methodInfoOriginal.MetadataToken;
#if NET45_OR_GREATER
        public override MethodImplAttributes MethodImplementationFlags => _methodInfoOriginal.MethodImplementationFlags;
#endif
        public override Module Module => _methodInfoOriginal.Module;

        public override object[] GetCustomAttributes(bool inherit) => _methodInfoOriginal.GetCustomAttributes(inherit);

        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => _methodInfoOriginal.GetCustomAttributes(attributeType, inherit);

        public override MethodImplAttributes GetMethodImplementationFlags() => _methodInfoOriginal.GetMethodImplementationFlags();

        public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) => _methodInfoDeclaration.Invoke(obj, invokeAttr, binder, parameters, culture);

        public override bool IsDefined(Type attributeType, bool inherit) => _methodInfoOriginal.IsDefined(attributeType, inherit);

        public override IList<CustomAttributeData> GetCustomAttributesData() => _methodInfoOriginal.GetCustomAttributesData();

        public override MethodInfo GetGenericMethodDefinition()
        {
            var methodInfoDeclaration = _methodInfoOriginal.GetGenericMethodDefinition();

            return new DynamicMethod(_methodInfoOriginal, methodInfoDeclaration, _declaringType, _returnType, _declaringTypeParameters, _hasDeclaringTypes);
        }

        public override bool Equals(object obj) => _methodInfoOriginal.Equals(obj);

        public override int GetHashCode() => _methodInfoOriginal.GetHashCode();

        public override MethodBody GetMethodBody() => _methodInfoOriginal.GetMethodBody();

        public override MethodInfo MakeGenericMethod(params Type[] typeArguments)
        {
            var methodInfoDeclaration = _methodInfoOriginal.IsGenericMethodDefinition
                ? _methodInfoOriginal.MakeGenericMethod(typeArguments)
                : _methodInfoOriginal.GetGenericMethodDefinition()
                    .MakeGenericMethod(typeArguments);

            var returnType = TypeCompiler.GetReturnType(methodInfoDeclaration, typeArguments, _declaringTypeParameters);

            return _hasDeclaringTypes
                ? new DynamicMethod(_methodInfoOriginal, methodInfoDeclaration, _declaringType, MakeGenericParameter(returnType, typeArguments, _declaringTypeParameters), _declaringTypeParameters, _hasDeclaringTypes)
                : new DynamicMethod(_methodInfoOriginal, methodInfoDeclaration, _declaringType, returnType, _declaringTypeParameters, _hasDeclaringTypes);
        }

        public override string ToString() => _methodInfoOriginal.ToString();

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
    }
}
