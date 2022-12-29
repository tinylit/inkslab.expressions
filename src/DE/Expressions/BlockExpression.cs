using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Delta.Expressions
{
    /// <summary>
    /// 代码块。
    /// </summary>
    public class BlockExpression : Expression
    {
        private readonly List<Expression> codes;

        private bool isReadOnly = false;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="returnType">返回值类型。</param>
        internal BlockExpression(Type returnType) : base(returnType)
        {
            codes = new List<Expression>();
        }

        /// <summary>
        /// 是否为空。
        /// </summary>
        public bool IsEmpty => codes.Count == 0;

        /// <summary>
        /// 添加代码。
        /// </summary>
        /// <param name="code">代码。</param>
        /// <returns>当前代码块。</returns>
        public virtual BlockExpression Append(Expression code)
        {
            if (code is null)
            {
                throw new ArgumentNullException(nameof(code));
            }

            if (isReadOnly)
            {
                throw new AstException("当前代码块已作为其它代码块的一部分，不能进行修改!");
            }

            if (code is GotoExpression)
            {
                if (IsEmpty)
                {
                    throw new AstException("栈顶部的无数据!");
                }

                int index = codes.Count - 1;

                Expression lastCode = codes[index];

                if (lastCode is GotoExpression || lastCode is ReturnExpression)
                {
                    return this;
                }

                if (lastCode.RuntimeType != typeof(void))
                {
                    codes.Add(Nop);
                }
            }
            else if (code is ReturnExpression @return)
            {
                int index = codes.Count - 1;

                Expression lastCode = codes[index];

                if (lastCode is GotoExpression || lastCode is ReturnExpression)
                {
                    return this;
                }

                if (lastCode.RuntimeType == RuntimeType)
                {
                    goto label_core;
                }

                if (@return.IsReturnVoid)
                {
                    if (RuntimeType == typeof(void))
                    {
                        codes.Add(Nop);

                        goto label_core;
                    }
                    else
                    {
                        throw new AstException($"需要一个类型可转换为“{RuntimeType}”的对象！");
                    }
                }

                if (EmitUtils.EqualSignatureTypes(lastCode.RuntimeType, RuntimeType) || lastCode.RuntimeType.IsAssignableFrom(RuntimeType))
                {
                    codes[index] = Convert(lastCode, RuntimeType);

                    goto label_core;
                }

                throw new AstException($"无法将类型“{lastCode.RuntimeType}”隐式转换为“{RuntimeType}”!");
            }
            else if (code is BlockExpression blockAst)
            {
                blockAst.isReadOnly = true;
            }
label_core:
            codes.Add(code);

            return this;
        }

        /// <summary>
        /// 发行。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            foreach (var code in codes)
            {
                code.Load(ilg);
            }
        }
    }
}