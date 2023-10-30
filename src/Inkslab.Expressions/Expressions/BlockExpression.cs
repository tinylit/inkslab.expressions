using Inkslab.Emitters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace Inkslab.Expressions
{
    /// <summary>
    /// 代码块。
    /// </summary>
    public class BlockExpression : Expression
    {
        private readonly List<Expression> codes;
        private class NopExpression : Expression
        {
            /// <summary>
            /// 单例。
            /// </summary>
            public static NopExpression Instance = new NopExpression();

            /// <summary>
            /// 构造函数。
            /// </summary>
            private NopExpression() : base(typeof(void))
            {
            }

            /// <summary>
            /// 加载。
            /// </summary>
            /// <param name="ilg">指令。</param>
            public override void Load(ILGenerator ilg) => ilg.Emit(OpCodes.Nop);
        }

        private bool isReadOnly = false;

        internal BlockExpression()
        {
            codes = new List<Expression>();
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="blockType">返回值类型。</param>
        internal BlockExpression(Type blockType) : base(blockType)
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

            bool checkFlag = true;

            if (code is GotoExpression || code is ReturnExpression || code is BreakExpression || code is ContinueExpression || code is ThrowExpression)
            {
                checkFlag = false;

                if (IsEmpty)
                {
                    goto label_core;
                }

                int index = codes.Count - 1;

                Expression lastCode = codes[index];

                if (lastCode is GotoExpression || lastCode is ReturnExpression || lastCode is BreakExpression || lastCode is ContinueExpression || lastCode is ThrowExpression)
                {
                    return this;
                }
            }
            else if (code is BlockExpression blockAst)
            {
                blockAst.isReadOnly = true;
            }

label_core:
            codes.Add(code);

            if (checkFlag)
            {
                if (IsVoid)
                {
                    if (!code.IsVoid)
                    {
                        codes.Add(NopExpression.Instance);
                    }
                }
                else if (code.IsVoid || RuntimeType == code.RuntimeType)
                {

                }
                else if (RuntimeType.IsGenericType || RuntimeType.IsGenericParameter || code.RuntimeType.IsGenericType || code.RuntimeType.IsGenericParameter || !RuntimeType.IsAssignableFrom(code.RuntimeType)) // 解决泛型类调用“IsAssignableFrom”方法异常。
                {
                    codes.Add(NopExpression.Instance);
                }
            }

            return this;
        }

        /// <summary>
        /// 标记标签。
        /// </summary>
        /// <param name="label">标签。</param>
        protected internal override void MarkLabel(Label label)
        {
            if (label is null)
            {
                throw new ArgumentNullException(nameof(label));
            }

            foreach (Expression node in codes)
            {
                node.MarkLabel(label);
            }
        }

        /// <summary>
        /// 将数据存储到变量中。
        /// </summary>
        /// <param name="variable">变量。</param>
        protected internal override void StoredLocal(VariableExpression variable)
        {
            foreach (Expression node in codes)
            {
                node.StoredLocal(variable);
            }
        }

        /// <inheritdoc/>
        protected internal override bool DetectionResult(Type returnType)
        {
            if (IsEmpty)
            {
                return false;
            }

            var last = codes.Last();

            if (last is ThrowExpression)
            {
                return true;
            }

            return last.DetectionResult(returnType);
        }

        /// <summary>
        /// 发行。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            if (IsVoid)
            {
                goto label_core;
            }

            if (IsEmpty)
            {
                throw new NotSupportedException("并发所有代码路径都有返回值！");
            }

            var last = codes.Last();

            if (last is GotoExpression || last is ReturnExpression || last is BreakExpression || last is ContinueExpression || last is ThrowExpression || last.DetectionResult(RuntimeType))
            {
                goto label_core;
            }

            throw new NotSupportedException("并发所有代码路径都有返回值！");

label_core:
            foreach (var code in codes)
            {
                code.Load(ilg);
            }
        }
    }
}