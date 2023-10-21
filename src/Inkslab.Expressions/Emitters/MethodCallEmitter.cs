using Inkslab.Expressions;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Text;

namespace Inkslab.Emitters
{
    /// <summary>
    /// 调用表达式。
    /// </summary>
    [DebuggerDisplay("{DebuggerView}")]
    public class MethodCallEmitter : Expression
    {
        private readonly MethodEmitter methodEmitter;
        private readonly Expression[] arguments;

        private static Type GetReturnType(MethodEmitter methodEmitter, Expression[] arguments)
        {
            if (methodEmitter is null)
            {
                throw new ArgumentNullException(nameof(methodEmitter));
            }

            if (arguments is null)
            {
                throw new ArgumentNullException(nameof(arguments));
            }

            var parameterInfos = methodEmitter.GetParameters();

            if (arguments.Length != parameterInfos.Length)
            {
                throw new AstException("方法参数不匹配!");
            }

            if (parameterInfos.Zip(arguments, (x, y) =>
            {
                return EmitUtils.IsAssignableFromSignatureTypes(x.ParameterType, y.RuntimeType);

            }).All(x => x))
            {
                return methodEmitter.ReturnType;
            }

            throw new AstException("方法参数类型不匹配!");
        }

        /// <summary>
        /// 静态无参函数调用。
        /// </summary>
        /// <param name="methodEmitter">函数。</param>
        internal MethodCallEmitter(MethodEmitter methodEmitter) : this(methodEmitter, EmptyAsts)
        {
        }

        internal MethodCallEmitter(MethodEmitter methodEmitter, Expression[] arguments) : base(GetReturnType(methodEmitter, arguments))
        {
            this.methodEmitter = methodEmitter ?? throw new ArgumentNullException(nameof(methodEmitter));
            this.arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
        }

        [DebuggerHidden]
        private string DebuggerView
        {
            get
            {
                var sb = new StringBuilder();

                if (!methodEmitter.IsStatic)
                {
                    sb.Append("this.");
                }

                sb.Append(methodEmitter.Name)
                    .Append('(')
                    .Append("...args");

                return sb.Append(')').ToString();
            }
        }

        /// <inheritdoc/>
        public override void Load(ILGenerator ilg)
        {
            if (methodEmitter.IsStatic)
            {
                LoadArgs(ilg);

                ilg.Emit(OpCodes.Call, methodEmitter.Value);
            }
            else
            {
                ilg.Emit(OpCodes.Ldarg_0);

                LoadArgs(ilg);

                ilg.Emit(OpCodes.Callvirt, methodEmitter.Value);
            }
        }

        private void LoadArgs(ILGenerator ilg)
        {
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
        }
    }
}
