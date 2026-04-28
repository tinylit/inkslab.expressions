using System;
using System.Collections.Concurrent;
using System.Reflection;
using SLExpr = System.Linq.Expressions.Expression;
using SLExpression = System.Linq.Expressions.Expression;

namespace Inkslab.Intercept
{
    /// <summary>
    /// 实现接口的调用。
    /// </summary>
    public class ImplementInvocation : IInvocation
    {
        // 将 MethodInfo 编译为强类型委托并缓存，避免每次调用都走 MethodInfo.Invoke 的反射调用栈。
        // Key 使用 MethodInfo（被声明类型唯一标识），LazyThreadSafetyMode 默认为 ExecutionAndPublication。
        private static readonly ConcurrentDictionary<MethodInfo, Func<object, object[], object>> _invokerCache
            = new ConcurrentDictionary<MethodInfo, Func<object, object[], object>>();

        private readonly object _target;
        private readonly Func<object, object[], object> _invoker;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="target">目标对象。</param>
        /// <param name="methodInfo">调用方法。</param>
        /// <exception cref="ArgumentNullException">参数 <paramref name="target"/> 或 <paramref name="methodInfo"/> 为 null。</exception>
        public ImplementInvocation(object target, MethodInfo methodInfo)
        {
            this._target = target ?? throw new ArgumentNullException(nameof(target));
            if (methodInfo is null)
            {
                throw new ArgumentNullException(nameof(methodInfo));
            }

            _invoker = _invokerCache.GetOrAdd(methodInfo, BuildInvoker);
        }

        /// <inheritdoc/>
        public object Invoke(object[] parameters) => _invoker.Invoke(_target, parameters);

        /// <summary>
        /// 为 <paramref name="method"/> 构建签名为 <c>(object target, object[] args) =&gt; object</c> 的强类型委托。
        /// 泛型方法定义不能 Compile（必须先 MakeGenericMethod），此处回退到 MethodInfo.Invoke。
        /// </summary>
        private static Func<object, object[], object> BuildInvoker(MethodInfo method)
        {
            // 泛型方法定义无法直接通过表达式树编译，只能继续使用反射。
            if (method.IsGenericMethodDefinition || method.ContainsGenericParameters)
            {
                return (instance, args) => method.Invoke(instance, args);
            }

            var parameterInfos = method.GetParameters();

            if (Array.Exists(parameterInfos, p => p.ParameterType.IsByRef))
            {
                // ref/out 参数无法在表达式树里安全装拆箱，回退到反射路径。
                return (instance, args) => method.Invoke(instance, args);
            }

            try
            {
                var targetParam = SLExpr.Parameter(typeof(object), "target");
                var argsParam = SLExpr.Parameter(typeof(object[]), "args");

                var argExprs = new SLExpression[parameterInfos.Length];

                for (int i = 0; i < parameterInfos.Length; i++)
                {
                    var paramType = parameterInfos[i].ParameterType;

                    var argAccess = SLExpr.ArrayIndex(argsParam, SLExpr.Constant(i));
                    
                    argExprs[i] = SLExpr.Convert(argAccess, paramType);
                }

                SLExpression instanceExpr = method.IsStatic
                    ? null
                    : SLExpr.Convert(targetParam, method.DeclaringType);

                SLExpression call = SLExpr.Call(instanceExpr, method, argExprs);

                SLExpression body = method.ReturnType == typeof(void)
                    ? SLExpr.Block(call, SLExpr.Constant(null, typeof(object)))
                    : SLExpr.Convert(call, typeof(object));

                return SLExpr.Lambda<Func<object, object[], object>>(body, targetParam, argsParam).Compile();
            }
            catch
            {
                // 任何编译失败都回退到反射，保证功能不退化。
                return (instance, args) => method.Invoke(instance, args);
            }
        }
    }
}

