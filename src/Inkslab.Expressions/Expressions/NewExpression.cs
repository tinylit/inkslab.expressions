using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Inkslab.Expressions
{
    /// <summary>
    /// 创建新实例。
    /// </summary>
    [DebuggerDisplay("new {RuntimeType.Name}(...args)")]
    public class NewExpression : Expression
    {
        private readonly ConstructorInfo _constructorInfo;
        private readonly Expression[] _parameters;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="constructorInfo">构造函数。</param>
        internal NewExpression(ConstructorInfo constructorInfo) : this(constructorInfo, new Expression[0])
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="constructorInfo">构造函数。</param>
        /// <param name="parameters">参数。</param>
        internal NewExpression(ConstructorInfo constructorInfo, params Expression[] parameters) : base(constructorInfo.DeclaringType)
        {
            _constructorInfo = constructorInfo;
            _parameters = parameters;
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="instanceType">实例类型。</param>
        public NewExpression(Type instanceType) : this(instanceType.GetConstructor(Type.EmptyTypes)) { }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="instanceType">实例类型。</param>
        /// <param name="parameters">参数。</param>
        public NewExpression(Type instanceType, params Expression[] parameters) : this(parameters?.Length > 0 ? instanceType.GetConstructor(parameters.Select(x => x.RuntimeType).ToArray()) : instanceType.GetConstructor(Type.EmptyTypes), parameters) { }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            if (_parameters?.Length > 0)
            {
                foreach (var parameter in _parameters)
                {
                    parameter.Load(ilg);
                }
            }

            ilg.Emit(OpCodes.Newobj, _constructorInfo);
        }
    }
}
