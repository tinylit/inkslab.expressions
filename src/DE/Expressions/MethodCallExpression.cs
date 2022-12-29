using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;

namespace Delta.Expressions
{
    /// <summary>
    /// 调用表达式。
    /// </summary>
    [DebuggerDisplay("{DebuggerView}")]
    public class MethodCallExpression : Expression
    {
        private readonly MethodInfo methodInfo;
        private readonly Expression instanceAst;
        private readonly Expression[] arguments;

        private static Type GetReturnType(Expression instanceAst, MethodInfo methodInfo, Expression[] arguments)
        {
            if (methodInfo is null)
            {
                throw new ArgumentNullException(nameof(methodInfo));
            }

            if (arguments is null)
            {
                throw new ArgumentNullException(nameof(arguments));
            }

            if (methodInfo.ContainsGenericParameters)
            {
                throw new AstException($"类型“{methodInfo.DeclaringType}.{methodInfo.Name}”包含未指定的泛型参数！");
            }

            if (methodInfo.IsStatic ^ (instanceAst is null))
            {
                if (methodInfo.IsStatic)
                {
                    throw new AstException($"方法“{methodInfo.Name}”是静态的，不能指定实例！");
                }
                else
                {
                    throw new AstException($"方法“{methodInfo.Name}”不是静态的，必须指定实例！");
                }
            }

            var parameterInfos = methodInfo.IsGenericMethod
                ? methodInfo.GetGenericMethodDefinition()
                    .GetParameters()
                : methodInfo.GetParameters();

            if (arguments.Length != parameterInfos.Length)
            {
                throw new AstException("方法参数不匹配!");
            }

            if (methodInfo is DynamicMethod dynamicMethod)
            {
                if (parameterInfos.Zip(arguments, (x, y) =>
                 {
                     return EmitUtils.EqualSignatureTypes(x.ParameterType, y.RuntimeType) || x.ParameterType.IsAssignableFrom(y.RuntimeType);
                 }).All(x => x))
                {
                    return dynamicMethod.ReturnType;
                }
            }
            else if (parameterInfos.Zip(arguments, (x, y) =>
            {
                return x.ParameterType == y.RuntimeType || x.ParameterType.IsAssignableFrom(y.RuntimeType);

            }).All(x => x))
            {
                return methodInfo.ReturnType;
            }

            throw new AstException("方法参数类型不匹配!");
        }

        /// <summary>
        /// 静态无参函数调用。
        /// </summary>
        /// <param name="methodInfo">函数。</param>
        internal MethodCallExpression(MethodInfo methodInfo) : this(null, methodInfo, EmptyAsts)
        {
        }

        /// <summary>
        /// 静态函数调用。
        /// </summary>
        /// <param name="methodInfo">函数。</param>
        /// <param name="arguments">参数。</param>
        internal MethodCallExpression(MethodInfo methodInfo, Expression[] arguments) : this(null, methodInfo, arguments)
        {
        }

        /// <summary>
        /// 无参函数调用。
        /// </summary>
        /// <param name="instanceAst">实例。</param>
        /// <param name="methodInfo">函数。</param>
        internal MethodCallExpression(Expression instanceAst, MethodInfo methodInfo) : this(instanceAst, methodInfo, EmptyAsts)
        {
        }

        /// <summary>
        /// 函数调用。
        /// </summary>
        /// <param name="instanceAst">实例。</param>
        /// <param name="methodInfo">函数。</param>
        /// <param name="arguments">参数。</param>
        internal MethodCallExpression(Expression instanceAst, MethodInfo methodInfo, Expression[] arguments) : base(GetReturnType(instanceAst, methodInfo, arguments))
        {
            this.instanceAst = instanceAst is null || methodInfo.DeclaringType == instanceAst.RuntimeType
                ? instanceAst
                : Convert(instanceAst, methodInfo.DeclaringType);

            this.methodInfo = methodInfo;
            this.arguments = arguments;
        }

        [DebuggerHidden]
        private string DebuggerView
        {
            get
            {
                var sb = new StringBuilder();

                if (instanceAst is null)
                {
                    sb.Append(methodInfo.DeclaringType.Name);
                }
                else
                {
                    sb.Append(instanceAst);
                }

                sb.Append('.')
                    .Append(methodInfo.Name)
                    .Append('(')
                    .Append("{ ")
                    .Append("...args");

                return sb.Append(')').ToString();
            }
        }

        /// <summary>
        /// 加载数据。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            if (!methodInfo.IsStatic)
            {
                instanceAst.Load(ilg);
            }

            foreach (var item in arguments)
            {
                if (item is ParameterExpression parameterAst && parameterAst.IsByRef) //? 仅加载参数位置。
                {
                    switch (parameterAst.Position)
                    {
                        case 0:
                            ilg.Emit(OpCodes.Ldarg_0);
                            break;
                        case 1:
                            ilg.Emit(OpCodes.Ldarg_1);
                            break;
                        case 2:
                            ilg.Emit(OpCodes.Ldarg_2);
                            break;
                        case 3:
                            ilg.Emit(OpCodes.Ldarg_3);
                            break;
                        default:
                            if (parameterAst.Position < byte.MaxValue)
                            {
                                ilg.Emit(OpCodes.Ldarg_S, parameterAst.Position);

                                break;
                            }

                            ilg.Emit(OpCodes.Ldarg, parameterAst.Position);
                            break;
                    }
                }
                else
                {
                    item.Load(ilg);
                }
            }

            if (methodInfo is DynamicMethod dynamicMethod)
            {
                if (methodInfo.IsStatic || methodInfo.DeclaringType.IsValueType)
                {
                    ilg.Emit(OpCodes.Call, dynamicMethod.RuntimeMethod);
                }
                else
                {
                    ilg.Emit(OpCodes.Callvirt, dynamicMethod.RuntimeMethod);
                }
            }
            else
            {
                if (methodInfo.IsStatic || methodInfo.DeclaringType.IsValueType)
                {
                    ilg.Emit(OpCodes.Call, methodInfo);
                }
                else
                {
                    ilg.Emit(OpCodes.Callvirt, methodInfo);
                }
            }
        }
    }
}
