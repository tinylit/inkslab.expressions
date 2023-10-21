using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Inkslab
{
    /// <summary>
    /// 类型扩展。
    /// </summary>
    public static class TypeCompiler
    {
        /// <summary>
        /// 获取构造函数。
        /// </summary>
        /// <param name="type">类型。</param>
        /// <param name="constructor">构造函数。</param>
        /// <returns></returns>
        public static ConstructorInfo GetConstructor(Type type, ConstructorInfo constructor)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (constructor is null)
            {
                throw new ArgumentNullException(nameof(constructor));
            }

            if (HasTypeParameterBuilder(type))
            {
                return TypeBuilder.GetConstructor(type, constructor);
            }

            if (type.IsGenericType)
            {
                return GetConstructor(constructor, type);
            }

            return constructor;
        }

        private static ConstructorInfo GetConstructor(ConstructorInfo referenceInfo, Type implementationType)
        {
            BindingFlags bindingFlags = BindingFlags.Instance;

            if (referenceInfo.IsPublic)
            {
                bindingFlags |= BindingFlags.Public;
            }
            else
            {
                bindingFlags |= BindingFlags.NonPublic;
            }

            if (referenceInfo.IsStatic)
            {
                bindingFlags |= BindingFlags.Static;
            }
            else
            {
                bindingFlags |= BindingFlags.Instance;
            }

            var parameterInfos = referenceInfo.GetParameters();

            foreach (var constructorInfo in implementationType.GetConstructors(bindingFlags))
            {
                var parameters = constructorInfo.GetParameters();

                if (parameters.Length != parameterInfos.Length)
                {
                    continue;
                }

                for (int i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].ParameterType == parameterInfos[i].ParameterType)
                    {
                        continue;
                    }

                    goto label_continue;
                }

                return constructorInfo;
label_continue:
                continue;
            }

            throw new MissingMemberException(implementationType.FullName, referenceInfo.Name);
        }

        /// <summary>
        /// 获取字段。
        /// </summary>
        /// <param name="type">类型。</param>
        /// <param name="field">字段。</param>
        /// <returns></returns>
        public static FieldInfo GetField(Type type, FieldInfo field)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (field is null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            if (HasTypeParameterBuilder(type))
            {
                return TypeBuilder.GetField(type, field);
            }

            if (type.IsGenericType)
            {
                return GetField(field, type);
            }

            return field;
        }

        private static FieldInfo GetField(FieldInfo referenceInfo, Type implementationType)
        {
            BindingFlags bindingFlags = BindingFlags.Instance;

            if (referenceInfo.IsPublic)
            {
                bindingFlags |= BindingFlags.Public;
            }
            else
            {
                bindingFlags |= BindingFlags.NonPublic;
            }

            if (referenceInfo.IsStatic)
            {
                bindingFlags |= BindingFlags.Static;
            }
            else
            {
                bindingFlags |= BindingFlags.Instance;
            }

            foreach (var fieldInfo in implementationType.GetFields(bindingFlags))
            {
                if (EmitUtils.EqualSignatureTypes(fieldInfo.FieldType, referenceInfo.FieldType))
                {
                    return fieldInfo;
                }
            }

            throw new MissingMemberException(implementationType.FullName, referenceInfo.Name);
        }

        /// <summary>
        /// 获取方法。
        /// </summary>
        /// <param name="type">类型。</param>
        /// <param name="methodInfo">方法。</param>
        /// <returns></returns>
        public static MethodInfo GetMethod(Type type, MethodInfo methodInfo)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (methodInfo is null)
            {
                throw new ArgumentNullException(nameof(methodInfo));
            }

            if (HasTypeParameterBuilder(type))
            {
                return TypeBuilder.GetMethod(type, methodInfo);
            }

            if (type.IsGenericType)
            {
                return GetMethod(methodInfo, type);
            }

            return methodInfo;
        }

        private static MethodInfo GetMethod(MethodInfo referenceInfo, Type implementationType)
        {
            BindingFlags bindingFlags = BindingFlags.Instance;

            if (referenceInfo.IsPublic)
            {
                bindingFlags |= BindingFlags.Public;
            }
            else
            {
                bindingFlags |= BindingFlags.NonPublic;
            }

            if (referenceInfo.IsStatic)
            {
                bindingFlags |= BindingFlags.Static;
            }
            else
            {
                bindingFlags |= BindingFlags.Instance;
            }

            var parameterInfos = referenceInfo.GetParameters();

            foreach (var methodInfo in implementationType.GetMethods(bindingFlags))
            {
                if (methodInfo.Name != referenceInfo.Name)
                {
                    continue;
                }

                if (methodInfo.IsGenericMethod ^ referenceInfo.IsGenericMethod)
                {
                    continue;
                }

                var parameters = methodInfo.GetParameters();

                if (parameters.Length != parameterInfos.Length)
                {
                    continue;
                }

                for (int i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].ParameterType == parameterInfos[i].ParameterType)
                    {
                        continue;
                    }

                    goto label_continue;
                }

                return methodInfo;
label_continue:
                continue;
            }

            throw new MissingMethodException(implementationType.Name, referenceInfo.Name);
        }

        private static bool HasTypeParameterBuilder(Type type)
        {
            if (type.IsGenericParameter)
            {
                return type is GenericTypeParameterBuilder;
            }

            if (type.IsGenericType)
            {
                return Array.Exists(type.GetGenericArguments(), HasTypeParameterBuilder);
            }

            if (type.IsArray || type.IsByRef)
            {
                return HasTypeParameterBuilder(type.GetElementType());
            }

            return false;
        }

        /// <summary>
        /// 获取返回类型。
        /// </summary>
        /// <param name="methodInfo">方法。</param>
        /// <param name="methodGenericArguments">方法的泛型参数。</param>
        /// <param name="typeGenericArguments">类的泛型参数。</param>
        /// <returns></returns>
        public static Type GetReturnType(MethodInfo methodInfo, Type[] methodGenericArguments, Type[] typeGenericArguments)
        {
            if (methodInfo is null)
            {
                throw new ArgumentNullException(nameof(methodInfo));
            }

            typeGenericArguments ??= Array.Empty<Type>();

            methodGenericArguments ??= Array.Empty<Type>();

            var returnType = methodInfo.ReturnType;

            var declaringType = methodInfo.DeclaringType;

            if (declaringType.IsGenericType)
            {
                return TypeGetReturnType(returnType, methodGenericArguments, typeGenericArguments);
            }
            else if (methodInfo.IsGenericMethod)
            {
                return TypeGetReturnType(returnType, methodGenericArguments);
            }
            else
            {
                return returnType;
            }
        }

        private static Type TypeGetReturnType(Type returnType, Type[] methodGenericArguments)
        {
            if (returnType.IsGenericParameter)
            {
                return methodGenericArguments[returnType.GenericParameterPosition];
            }

            if (returnType.IsGenericType)
            {
                bool changeFlag = false;
                var myTypeArguments = returnType.GetGenericArguments();

                for (int i = 0; i < myTypeArguments.Length; i++)
                {
                    var typeArgument = myTypeArguments[i];

                    var makeTypeArgument = TypeGetReturnType(typeArgument, methodGenericArguments);

                    if (typeArgument != makeTypeArgument)
                    {
                        changeFlag = true;
                    }

                    myTypeArguments[i] = makeTypeArgument;
                }

                if (changeFlag)
                {
                    if (!returnType.IsGenericTypeDefinition)
                    {
                        returnType = returnType.GetGenericTypeDefinition();
                    }

                    return returnType.MakeGenericType(myTypeArguments);
                }

                return returnType;
            }

            if (returnType.IsArray)
            {
                var elementType = returnType.GetElementType();
                var arrayElementType = TypeGetReturnType(elementType, methodGenericArguments);

                if (elementType == arrayElementType)
                {
                    return returnType;
                }

                return arrayElementType.MakeArrayType(returnType.GetArrayRank());
            }

            if (returnType.IsByRef)
            {
                var elementType = returnType.GetElementType();
                var refElementType = TypeGetReturnType(elementType, methodGenericArguments);

                if (elementType == refElementType)
                {
                    return returnType;
                }

                return refElementType.MakeByRefType();
            }

            return returnType;
        }

        private static Type TypeGetReturnType(Type returnType, Type[] methodGenericArguments, Type[] typeGenericArguments)
        {
            if (returnType.IsGenericParameter)
            {
#if NETSTANDARD2_1_OR_GREATER
                if (returnType.IsGenericMethodParameter)
#else
                if (returnType.DeclaringMethod is not null)
#endif
                {
                    return methodGenericArguments[returnType.GenericParameterPosition];
                }

                return typeGenericArguments[returnType.GenericParameterPosition];
            }

            if (returnType.IsGenericType)
            {
                bool changeFlag = false;
                var myTypeArguments = returnType.GetGenericArguments();

                for (int i = 0; i < myTypeArguments.Length; i++)
                {
                    var typeArgument = myTypeArguments[i];

                    var makeTypeArgument = TypeGetReturnType(typeArgument, methodGenericArguments);

                    if (typeArgument != makeTypeArgument)
                    {
                        changeFlag = true;
                    }

                    myTypeArguments[i] = makeTypeArgument;
                }

                if (changeFlag)
                {
                    if (!returnType.IsGenericTypeDefinition)
                    {
                        returnType = returnType.GetGenericTypeDefinition();
                    }

                    return returnType.MakeGenericType(myTypeArguments);
                }

                return returnType;
            }

            if (returnType.IsArray)
            {
                var elementType = returnType.GetElementType();
                var arrayElementType = TypeGetReturnType(elementType, methodGenericArguments, typeGenericArguments);

                if (elementType == arrayElementType)
                {
                    return returnType;
                }

                return arrayElementType.MakeArrayType(returnType.GetArrayRank());
            }

            if (returnType.IsByRef)
            {
                var elementType = returnType.GetElementType();
                var refElementType = TypeGetReturnType(elementType, methodGenericArguments, typeGenericArguments);

                if (elementType == refElementType)
                {
                    return returnType;
                }

                return refElementType.MakeByRefType();
            }

            return returnType;
        }
    }
}
