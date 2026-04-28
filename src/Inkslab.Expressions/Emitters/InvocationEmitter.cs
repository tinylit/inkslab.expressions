using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace Inkslab.Emitters
{
    /// <summary>
    /// 调用。
    /// </summary>
    [DebuggerDisplay("Invoke({methodAst},...args)")]
    public class InvocationEmitter : Expression
    {
        private readonly Expression _instanceAst;
        private readonly MethodEmitter _methodEmitter;
        private readonly Expression _arguments;

        private static readonly MethodInfo _invokeMethod = typeof(MethodBase).GetMethod(nameof(MethodBase.Invoke), new Type[2] { typeof(object), typeof(object[]) });

        /// <summary>
        /// 静态方法调用。
        /// </summary>
        /// <param name="methodEmitter">方法。</param>
        /// <param name="arguments">调用参数。</param>
        internal InvocationEmitter(MethodEmitter methodEmitter, Expression arguments) : this(null, methodEmitter, arguments)
        {
        }

        /// <summary>
        /// 方法调用。
        /// </summary>
        /// <param name="instanceAst">实例。</param>
        /// <param name="methodEmitter">方法。</param>
        /// <param name="arguments">调用参数。</param>
        internal InvocationEmitter(Expression instanceAst, MethodEmitter methodEmitter, Expression arguments) : base(typeof(object))
        {
            if (methodEmitter is null)
            {
                throw new ArgumentNullException(nameof(methodEmitter));
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

            _instanceAst = instanceAst;
            _methodEmitter = methodEmitter;
            _arguments = arguments;
        }
        /// <summary>
        /// 加载数据。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            EmitUtils.EmitConstantOfType(ilg, _methodEmitter, typeof(MethodInfo));

            if (_methodEmitter.IsStatic)
            {
                ilg.Emit(OpCodes.Ldnull);
            }
            else
            {
                _instanceAst.Load(ilg);
            }

            _arguments.Load(ilg);

            ilg.Emit(OpCodes.Call, _invokeMethod);

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
