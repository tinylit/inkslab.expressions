using Delta.Emitters;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace Delta.Expressions
{
    /// <summary>
    /// 调用。
    /// </summary>
    [DebuggerDisplay("Invoke({methodAst},...args)")]
    public class InvocationExpression : Expression
    {
        private readonly Expression instanceAst;
        private readonly Expression methodAst;
        private readonly Expression arguments;

        private static readonly MethodInfo InvokeMethod = typeof(MethodBase).GetMethod(nameof(MethodBase.Invoke), new Type[2] { typeof(object), typeof(object[]) });

        /// <summary>
        /// 静态方法调用。
        /// </summary>
        /// <param name="methodInfo">方法。</param>
        /// <param name="arguments">调用参数。</param>
        internal InvocationExpression(MethodInfo methodInfo, Expression arguments) : this(null, methodInfo, arguments)
        {
        }

        /// <summary>
        /// 方法调用。
        /// </summary>
        /// <param name="instanceAst">实例。</param>
        /// <param name="methodInfo">方法。</param>
        /// <param name="arguments">调用参数。</param>
        internal InvocationExpression(Expression instanceAst, MethodInfo methodInfo, Expression arguments) : base(methodInfo.ReturnType)
        {
            if (instanceAst is null)
            {
                throw new ArgumentNullException(nameof(instanceAst));
            }

            if (arguments is null)
            {
                throw new ArgumentNullException(nameof(arguments));
            }

            if (methodInfo is not DynamicMethod)
            {
                foreach (var genericArgumentType in methodInfo.GetGenericArguments())
                {
                    if (genericArgumentType.IsGenericParameter)
                    {
                        if (genericArgumentType is GenericTypeParameterBuilder)
                        {
                            continue;
                        }

                        throw new AstException($"类型“{methodInfo.DeclaringType}.{methodInfo.Name}”包含未指定的泛型参数！");
                    }
                }
            }

            if (methodInfo.IsStatic)
            {
                if (instanceAst is not null)
                {
                    throw new AstException($"方法“{methodInfo.Name}”是静态的，不能指定实例！");
                }
            }
            else if (instanceAst is null)
            {
                throw new AstException($"方法“{methodInfo.Name}”不是静态的，必须指定实例！");
            }
            else if (!methodInfo.DeclaringType.IsAssignableFrom(instanceAst.RuntimeType))
            {
                throw new AstException($"方法“{methodInfo.Name}”不属于实例(“{instanceAst.RuntimeType}”)！");
            }

            if (!arguments.RuntimeType.IsArray)
            {
                throw new ArgumentException("参数不是数组!", nameof(arguments));
            }

            if (arguments.RuntimeType != typeof(object[]))
            {
                throw new ArgumentException("参数不是“System.Object”数组!", nameof(arguments));
            }

            this.instanceAst = instanceAst;
            this.methodAst = Constant(methodInfo, typeof(MethodInfo));
            this.arguments = arguments;
        }

        /// <summary>
        /// 静态方法调用。
        /// </summary>
        /// <param name="methodAst">方法。</param>
        /// <param name="arguments">调用参数。</param>
        internal InvocationExpression(Expression methodAst, Expression arguments) : this(null, methodAst, arguments)
        {
        }

        /// <summary>
        /// 方法调用。
        /// </summary>
        /// <param name="instanceAst">实例。</param>
        /// <param name="methodAst">方法。</param>
        /// <param name="arguments">调用参数。</param>
        internal InvocationExpression(Expression instanceAst, Expression methodAst, Expression arguments) : base(typeof(object))
        {
            if (instanceAst is null)
            {
                throw new ArgumentNullException(nameof(instanceAst));
            }

            if (methodAst is null)
            {
                throw new ArgumentNullException(nameof(methodAst));
            }

            if (arguments is null)
            {
                throw new ArgumentNullException(nameof(arguments));
            }

            if (!arguments.RuntimeType.IsArray)
            {
                throw new ArgumentException("参数不是数组!", nameof(arguments));
            }

            if (arguments.RuntimeType != typeof(object[]))
            {
                throw new ArgumentException("参数不是“System.Object”数组!", nameof(arguments));
            }

            if (methodAst.RuntimeType == typeof(MethodInfo) || typeof(MethodInfo).IsAssignableFrom(methodAst.RuntimeType))
            {
                this.instanceAst = instanceAst;
                this.methodAst = methodAst is MethodEmitter ? Constant(methodAst, typeof(MethodInfo)) : methodAst;
                this.arguments = arguments;
            }
            else
            {
                throw new ArgumentException("参数不是方法!", nameof(methodAst));
            }
        }

        /// <summary>
        /// 加载数据。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            methodAst.Load(ilg);

            if (instanceAst is null)
            {
                ilg.Emit(OpCodes.Ldnull);
            }
            else
            {
                instanceAst.Load(ilg);
            }

            arguments.Load(ilg);

            ilg.Emit(OpCodes.Call, InvokeMethod);

            if (RuntimeType != typeof(object))
            {
                if (IsVoid)
                {
                    ilg.Emit(OpCodes.Pop);
                }
                else
                {
                    EmitUtils.EmitConvertToType(ilg, typeof(object), RuntimeType.IsByRef ? RuntimeType.GetElementType() : RuntimeType, true);
                }
            }
        }
    }
}
