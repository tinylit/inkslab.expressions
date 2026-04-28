using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Text;

namespace Inkslab.Expressions
{
    /// <summary>
    /// 捕获异常。
    /// </summary>
    [DebuggerDisplay("{DebuggerView}")]
    public class TryExpression : BlockExpression
    {
        private readonly Expression _finallyAst;
        private readonly List<CatchExpression> _catchAsts;

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

                foreach (var catchAst in _catchAsts)
                {
                    sb.AppendLine()
                        .Append(catchAst);
                }

                if (_finallyAst is not null)
                {
                    sb.AppendLine()
                        .Append('{')
                        .AppendLine()
                        .Append(_finallyAst)
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

            private readonly Type _exceptionType;
            private readonly VariableExpression _variable;

            public CatchExpression(Type exceptionType)
            {
                if (exceptionType is null)
                {
                    throw new ArgumentNullException(nameof(exceptionType));
                }

                if (exceptionType == typeof(Exception) || exceptionType.IsAssignableFrom(typeof(Exception)))
                {
                    _exceptionType = exceptionType;
                }
                else
                {
                    throw new AstException($"变量类型“{exceptionType}”未继承“{typeof(Exception)}”异常基类!");
                }
            }

            public CatchExpression(VariableExpression variable)
            {
                if (variable is null)
                {
                    throw new ArgumentNullException(nameof(variable));
                }

                _exceptionType = variable.RuntimeType;

                if (_exceptionType == typeof(Exception) || _exceptionType.IsAssignableFrom(typeof(Exception)))
                {
                    _variable = variable;
                }
                else
                {
                    throw new AstException($"变量类型“{_exceptionType}”未继承“{typeof(Exception)}”异常基类!");
                }
            }

            /// <summary>
            /// 生成。
            /// </summary>
            /// <param name="ilg">指令。</param>
            public override void Load(ILGenerator ilg)
            {
                if (_variable is null)
                {
                    ilg.BeginCatchBlock(_exceptionType);
                }
                else
                {
                    Assign(_variable, new CatchBlockAst(_exceptionType))
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

                if (_variable is not null)
                {
                    sb.Append(_variable);
                }
                else
                {
                    sb.Append(_exceptionType.Name);
                }

                sb.Append(')')
                    .Append("{ /*TODO:somethings...*/}");

                return sb.ToString();
            }
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        internal TryExpression()
        {
            _catchAsts = new List<CatchExpression>();
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="finallyAst">一定会执行的代码。</param>
        internal TryExpression(Expression finallyAst)
        {
            _finallyAst = finallyAst ?? throw new ArgumentNullException(nameof(finallyAst));

            _catchAsts = new List<CatchExpression>();
        }

        /// <summary>
        /// 捕获任意异常。
        /// </summary>
        /// <returns></returns>
        public IErrorHandler Catch() => Catch(typeof(Exception));

        /// <summary>
        /// 捕获指定类型的异常。
        /// </summary>
        /// <param name="exceptionType">异常类型。</param>
        /// <returns></returns>
        public IErrorHandler Catch(Type exceptionType)
        {
            if (exceptionType is null)
            {
                throw new ArgumentNullException(nameof(exceptionType));
            }

            var catchAst = new CatchExpression(exceptionType);

            _catchAsts.Add(catchAst);

            return catchAst;
        }

        /// <summary>
        /// 捕获“<paramref name="variable"/>.RuntimeType”的异常，并将异常赋值给指定变量。
        /// </summary>
        /// <param name="variable">变量。</param>
        /// <returns></returns>
        public IErrorHandler Catch(VariableExpression variable)
        {
            if (variable is null)
            {
                throw new ArgumentNullException(nameof(variable));
            }

            var catchAst = new CatchExpression(variable);

            _catchAsts.Add(catchAst);

            return catchAst;
        }

        /// <inheritdoc/>
        protected internal override void MarkLabel(Label label)
        {
            if (label is null)
            {
                throw new ArgumentNullException(nameof(label));
            }

            if (label.Kind == LabelKind.Return)
            {
                foreach (var @catch in _catchAsts)
                {
                    @catch.MarkLabel(label);
                }

                base.MarkLabel(label);

                _finallyAst?.MarkLabel(label);
            }
        }
        
        /// <inheritdoc/>
        protected internal override void StoredLocal(VariableExpression variable)
        {
            if (variable is null)
            {
                throw new ArgumentNullException(nameof(variable));
            }

            foreach (var @catch in _catchAsts)
            {
                @catch.StoredLocal(variable);
            }

            base.StoredLocal(variable);

            _finallyAst?.StoredLocal(variable);
        }
        /// <inheritdoc/>
        protected internal override bool DetectionResult(Type returnType)
        {
            foreach (var @catch in _catchAsts)
            {
                if (!@catch.DetectionResult(returnType))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            if (_catchAsts.Count == 0 && _finallyAst is null)
            {
                throw new AstException("表达式残缺，未设置“catch”代码块和“finally”代码块至少设置其一！");
            }

            ilg.BeginExceptionBlock();

            base.Load(ilg);

            if (_catchAsts.Count > 0)
            {
                foreach (var catchAst in _catchAsts)
                {
                    catchAst.Load(ilg);
                }

                ilg.Emit(OpCodes.Nop);
            }

            if (_finallyAst != null)
            {
                ilg.BeginFinallyBlock();

                _finallyAst.Load(ilg);

                if (!_finallyAst.IsVoid)
                {
                    ilg.Emit(OpCodes.Nop);
                }
            }

            ilg.EndExceptionBlock();
        }
    }
}
