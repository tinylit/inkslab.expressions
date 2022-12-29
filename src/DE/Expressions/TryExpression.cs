using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Text;

namespace Delta.Expressions
{
    /// <summary>
    /// 捕获异常。
    /// </summary>
    [DebuggerDisplay("{DebuggerView}")]
    public class TryExpression : BlockExpression
    {
        private readonly Expression finallyAst;
        private readonly List<CatchExpression> catchAsts;

        /// <summary>
        /// 异常处理。
        /// </summary>
        public interface IErrorHandler
        {
            /// <summary>
            /// 添加表达式。
            /// </summary>
            /// <param name="code">表达式。</param>
            IErrorHandler Append(Expression code);
        }

        [DebuggerHidden]
        private string DebuggerView
        {
            get
            {
                var sb = new StringBuilder();

                sb.Append("try { //TODO:somethings }");

                foreach (var catchAst in catchAsts)
                {
                    sb.AppendLine()
                        .Append(catchAst);
                }

                if (finallyAst is not null)
                {
                    sb.AppendLine()
                        .Append('{')
                        .AppendLine()
                        .Append(finallyAst)
                        .AppendLine()
                        .Append('}');
                }

                return sb.ToString();
            }
        }

        /// <summary>
        /// 捕获异常。
        /// </summary>
        private class CatchExpression : BlockExpression, IErrorHandler
        {
            private class CatchBlockAst : Expression
            {
                public CatchBlockAst(Type returnType) : base(returnType)
                {
                }

                public override void Load(ILGenerator ilg)
                {
                    ilg.BeginCatchBlock(RuntimeType);
                }
            }

            private readonly Type exceptionType;
            private readonly VariableExpression variable;

            public CatchExpression(Type returnType, Type exceptionType) : base(returnType)
            {
                if (exceptionType is null)
                {
                    throw new ArgumentNullException(nameof(exceptionType));
                }

                if (exceptionType == typeof(Exception) || exceptionType.IsAssignableFrom(typeof(Exception)))
                {
                    this.exceptionType = exceptionType;
                }
                else
                {
                    throw new AstException($"变量类型“{exceptionType}”未继承“{typeof(Exception)}”异常基类!");
                }
            }

            public CatchExpression(Type returnType, VariableExpression variable) : base(returnType)
            {
                if (variable is null)
                {
                    throw new ArgumentNullException(nameof(variable));
                }

                this.exceptionType = variable.RuntimeType;

                if (exceptionType == typeof(Exception) || exceptionType.IsAssignableFrom(typeof(Exception)))
                {
                    this.variable = variable;
                }
                else
                {
                    throw new AstException($"变量类型“{exceptionType}”未继承“{typeof(Exception)}”异常基类!");
                }
            }

            /// <summary>
            /// 生成。
            /// </summary>
            /// <param name="ilg">指令。</param>
            public override void Load(ILGenerator ilg)
            {
                if (variable is null)
                {
                    ilg.BeginCatchBlock(exceptionType);
                }
                else
                {
                    Assign(variable, new CatchBlockAst(exceptionType))
                        .Load(ilg);
                }

                ilg.Emit(OpCodes.Nop);

                base.Load(ilg);
            }

            IErrorHandler IErrorHandler.Append(Expression code)
            {
                Append(code);

                return this;
            }

            public override string ToString()
            {
                var sb = new StringBuilder();

                sb.Append("catch(");

                if (variable is not null)
                {
                    sb.Append(variable);
                }
                else
                {
                    sb.Append(exceptionType.Name);
                }

                sb.Append(')')
                    .Append("{ /*TODO:somethings...*/}");

                return sb.ToString();
            }
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="returnType">返回结果。</param>
        internal TryExpression(Type returnType) : base(returnType)
        {
            catchAsts = new List<CatchExpression>();
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="returnType">返回结果。</param>
        /// <param name="finallyAst">一定会执行的代码。</param>
        internal TryExpression(Type returnType, Expression finallyAst) : base(returnType)
        {
            this.finallyAst = finallyAst ?? throw new ArgumentNullException(nameof(finallyAst));

            catchAsts = new List<CatchExpression>();
        }

        /// <summary>
        /// 捕获任意异常。
        /// </summary>
        /// <returns></returns>
        public IErrorHandler Catch() => Catch(typeof(Exception));

        /// <summary>
        /// 捕获“<paramref name="variable"/>.RuntimeType”异常，并将异常赋值给指定变量。
        /// </summary>
        /// <returns></returns>
        public IErrorHandler Catch(VariableExpression variable) => Catch(RuntimeType, variable);

        /// <summary>
        /// 捕获指定类型异常。
        /// </summary>
        /// <param name="exceptionType">异常类型。</param>
        /// <returns></returns>
        public IErrorHandler Catch(Type exceptionType) => Catch(RuntimeType, exceptionType);

        /// <summary>
        /// 捕获指定类型的异常。
        /// </summary>
        /// <param name="returnType">返回类型。</param>
        /// <param name="exceptionType">异常类型。</param>
        /// <returns></returns>
        public IErrorHandler Catch(Type returnType, Type exceptionType)
        {
            if (exceptionType is null)
            {
                throw new ArgumentNullException(nameof(exceptionType));
            }

            var catchAst = new CatchExpression(returnType, exceptionType);

            catchAsts.Add(catchAst);

            return catchAst;
        }

        /// <summary>
        /// 捕获“<paramref name="variable"/>.RuntimeType”的异常，并将异常赋值给指定变量。
        /// </summary>
        /// <param name="returnType">返回类型。</param>
        /// <param name="variable">变量。</param>
        /// <returns></returns>
        public IErrorHandler Catch(Type returnType, VariableExpression variable)
        {
            if (variable is null)
            {
                throw new ArgumentNullException(nameof(variable));
            }

            var catchAst = new CatchExpression(returnType, variable);

            catchAsts.Add(catchAst);

            return catchAst;
        }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            if (catchAsts.Count == 0 && finallyAst is null)
            {
                throw new AstException("表达式残缺，未设置“catch”代码块和“finally”代码块至少设置其一！");
            }

            ilg.BeginExceptionBlock();

            base.Load(ilg);

            if (catchAsts.Count > 0)
            {
                foreach (var catchAst in catchAsts)
                {
                    catchAst.Load(ilg);
                }

                ilg.Emit(OpCodes.Nop);
            }

            if (finallyAst != null)
            {
                ilg.BeginFinallyBlock();

                finallyAst.Load(ilg);

                ilg.Emit(OpCodes.Nop);
            }

            ilg.EndExceptionBlock();
        }
    }
}
