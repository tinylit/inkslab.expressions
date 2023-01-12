using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;

namespace Delta.Emitters
{
    /// <summary>
    /// 创建新实例。
    /// </summary>
    [DebuggerDisplay("new {RuntimeType.Name}(...args)")]
    public class NewInstanceEmitter : Expression
    {
        private readonly ConstructorEmitter constructorEmitter;
        private readonly Expression[] parameters;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="constructorEmitter">构造函数。</param>
        internal NewInstanceEmitter(ConstructorEmitter constructorEmitter) : this(constructorEmitter, new Expression[0])
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="constructorEmitter">构造函数。</param>
        /// <param name="parameters">参数。</param>
        internal NewInstanceEmitter(ConstructorEmitter constructorEmitter, params Expression[] parameters) : base(constructorEmitter.RuntimeType)
        {
            ArgumentsCheck(constructorEmitter, parameters);

            this.constructorEmitter = constructorEmitter;
            this.parameters = parameters;
        }

        private static void ArgumentsCheck(ConstructorEmitter constructorEmitter, Expression[] parameters)
        {
            var parameterInfos = constructorEmitter.GetParameters();

            if (parameters?.Length != parameterInfos.Length)
            {
                throw new AstException("指定参数和构造函数参数个数不匹配!");
            }

            if (!parameterInfos.Zip(parameters, (x, y) =>
            {
                return EmitUtils.IsAssignableFromSignatureTypes(x.ParameterType, y.RuntimeType);

            }).All(x => x))
            {
                throw new AstException("指定参数和构造函数参数类型不匹配!");
            }
        }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            if (parameters?.Length > 0)
            {
                foreach (var parameter in parameters)
                {
                    parameter.Load(ilg);
                }
            }

            ilg.Emit(OpCodes.Newobj, constructorEmitter.Value);
        }
    }
}
