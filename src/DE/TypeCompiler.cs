using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Delta
{
    /// <summary>
    /// 类型扩展。
    /// </summary>
    public static class TypeCompiler
    {
        /// <summary>
        /// 编译返回值。
        /// </summary>
        /// <param name="methodInfo">方法。</param>
        /// <param name="typeArguments">泛型参数。</param>
        /// <returns>方法返回值类型。</returns>
        public static Type MakeReturnType(MethodInfo methodInfo, params Type[] typeArguments)
        {
            if (methodInfo is null)
            {
                throw new ArgumentNullException(nameof(methodInfo));
            }

            var returnType = methodInfo.ReturnType;

            if (typeArguments is null || typeArguments.Length == 0)
            {
                return returnType;
            }

            if (ReturnTypeIsValid(returnType))
            {
                return returnType;
            }

            return MakeType(returnType, methodInfo.GetGenericArguments(), typeArguments);
        }

        private static bool ReturnTypeIsValid(Type type)
        {
            if (type.IsGenericParameter)
            {
                return type is GenericTypeParameterBuilder;
            }

            if (type.IsGenericType)
            {
                return Array.TrueForAll(type.GetGenericArguments(), ReturnTypeIsValid);
            }

            if (type.IsArray || type.IsByRef)
            {
                return ReturnTypeIsValid(type.GetElementType());
            }

            return true;
        }

        private static Type MakeType(Type returnType, Type[] genericArguments, Type[] typeArguments)
        {
            if (genericArguments.Length != typeArguments.Length)
            {
                throw new InvalidOperationException();
            }

            int indexOf = System.Array.IndexOf(genericArguments, returnType);

            if (indexOf > -1)
            {
                return typeArguments[indexOf];
            }

            if (returnType.IsGenericType)
            {
                return returnType.MakeGenericType(Array.ConvertAll(returnType.GetGenericArguments(), x => MakeType(x, genericArguments, typeArguments)));
            }

            if (returnType.IsArray)
            {
                var elementType = returnType.GetElementType();
                var arrayElementType = MakeType(elementType, genericArguments, typeArguments);

                if (elementType == arrayElementType)
                {
                    return returnType;
                }

                return arrayElementType.MakeArrayType(returnType.GetArrayRank());
            }

            if (returnType.IsByRef)
            {
                var elementType = returnType.GetElementType();
                var refElementType = MakeType(elementType, genericArguments, typeArguments);

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
