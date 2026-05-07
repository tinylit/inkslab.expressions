using Inkslab.Expressions;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Text;

namespace Inkslab.Emitters
{
    /// <summary>
    /// 指定实例的方法调用表达式（对应跨类型 MethodEmitter 调用）。
    /// 静态方法或自动注入 this 的调用请使用 <see cref="MethodCallEmitter"/>。
    /// </summary>
    [DebuggerDisplay("{DebuggerView}")]
    public class InstanceMethodCallEmitter : Expression
    {
        private readonly Expression _instanceAst;
        private readonly MethodEmitter _methodEmitter;
        private readonly Expression[] _arguments;

        private static Type GetReturnType(Expression instanceAst, MethodEmitter methodEmitter, Expression[] arguments)
        {
            if (methodEmitter is null)
            {
                throw new ArgumentNullException(nameof(methodEmitter));
            }

            if (instanceAst is null)
            {
                throw new ArgumentNullException(nameof(instanceAst));
            }

            if (arguments is null)
            {
                throw new ArgumentNullException(nameof(arguments));
            }

            if (methodEmitter.IsStatic)
            {
                throw new AstException($"方法\"{methodEmitter.Name}\"是静态的，不能指定实例！");
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
        /// 指定实例的方法调用。
        /// </summary>
        /// <param name="instanceAst">实例表达式。</param>
        /// <param name="methodEmitter">方法发射器。需为非静态方法。</param>
        /// <param name="arguments">方法参数。</param>
        internal InstanceMethodCallEmitter(Expression instanceAst, MethodEmitter methodEmitter, Expression[] arguments)
            : base(GetReturnType(instanceAst, methodEmitter, arguments))
        {
            _instanceAst = instanceAst ?? throw new ArgumentNullException(nameof(instanceAst));
            _methodEmitter = methodEmitter ?? throw new ArgumentNullException(nameof(methodEmitter));
            _arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
        }

        [DebuggerHidden]
        private string DebuggerView
        {
            get
            {
                var sb = new StringBuilder();

                sb.Append(_instanceAst)
                    .Append('.')
                    .Append(_methodEmitter.Name)
                    .Append('(')
                    .Append("...args")
                    .Append(')');

                return sb.ToString();
            }
        }

        /// <inheritdoc/>
        public override void Load(ILGenerator ilg)
        {
            _instanceAst.Load(ilg);

            LoadArgs(ilg);

            ilg.Emit(OpCodes.Callvirt, _methodEmitter.Value);
        }

        private void LoadArgs(ILGenerator ilg)
        {
            foreach (var item in _arguments)
            {
                if (item is ParameterExpression parameterAst && parameterAst.IsByRef)
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
