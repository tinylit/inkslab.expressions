using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Inkslab.Expressions
{
    /// <summary>
    /// 元素遍历循环表达式（合并 <c>for</c> 与 <c>foreach</c>）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 选择策略（自上而下，命中即停止）：
    /// </para>
    /// <list type="number">
    ///   <item>
    ///     <description>
    ///       <c>for</c> 索引循环：源类型为一维数组，或同时具备 <c>int Count</c>/<c>int Length</c>
    ///       属性与 <c>this[int]</c> 索引器，且元素/索引器返回类型与循环变量类型完全一致。
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <c>foreach</c> 枚举器循环：优先选择元素类型与循环变量类型完全一致的
    ///       <see cref="IEnumerable{T}"/>；若源类型不存在该泛型实现，则回退到非泛型
    ///       <see cref="IEnumerable"/> 并在每轮迭代中将 <see cref="IEnumerator.Current"/>
    ///       强制转换到循环变量类型；若强转无法进行（在编译期已可断定，例如源仅实现
    ///       <see cref="IEnumerable{T}"/> 且 <c>T</c> 与循环变量类型不一致），
    ///       则抛出 <see cref="AstException"/>。
    ///     </description>
    ///   </item>
    /// </list>
    /// <para>
    /// 与 <see cref="LoopExpression"/> 一致：循环体内可使用 <see cref="BreakExpression"/> 跳出循环、
    /// <see cref="ContinueExpression"/> 进入下一轮迭代；<see cref="ReturnExpression"/> 仍可向外冒泡。
    /// </para>
    /// </remarks>
    public class ForEachExpression : BlockExpression
    {
        private enum Strategy
        {
            Array,
            Indexer,
            GenericEnumerable,
            NonGenericEnumerable
        }

        // 静态缓存：跨实例复用，避免每次构造都反射查找。
        private static readonly MethodInfo _nonGenericGetEnumerator = typeof(IEnumerable).GetMethod(nameof(IEnumerable.GetEnumerator));
        private static readonly MethodInfo _nonGenericCurrentGetter = typeof(IEnumerator).GetProperty(nameof(IEnumerator.Current)).GetGetMethod();
        private static readonly MethodInfo _moveNextMethod = typeof(IEnumerator).GetMethod(nameof(IEnumerator.MoveNext));
        private static readonly MethodInfo _disposeMethod = typeof(IDisposable).GetMethod(nameof(IDisposable.Dispose));

        private const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;

        private readonly VariableExpression _item;
        private readonly Expression _source;

        private readonly Strategy _strategy;
        private readonly Type _elementType;

        // Indexer 分支
        private readonly PropertyInfo _lengthProperty;
        private readonly MethodInfo _indexerGetter;

        // Enumerable 分支
        private readonly Type _enumeratorType;
        private readonly MethodInfo _getEnumeratorMethod;
        private readonly MethodInfo _getCurrentMethod;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="item">循环变量，每轮迭代写入当前元素。</param>
        /// <param name="source">迭代源表达式。</param>
        internal ForEachExpression(VariableExpression item, Expression source)
        {
            this._item = item ?? throw new ArgumentNullException(nameof(item));
            this._source = source ?? throw new ArgumentNullException(nameof(source));

            var sourceType = source.RuntimeType;
            var itemType = item.RuntimeType;

            // 1) for —— 一维数组（数组分支自洽：类型不一致直接报错）。
            if (sourceType.IsArray)
            {
                if (sourceType.GetArrayRank() != 1)
                {
                    throw new AstException($"foreach 暂不支持多维数组（{sourceType}）！");
                }

                var arrayElementType = sourceType.GetElementType();

                if (itemType != arrayElementType)
                {
                    throw new AstException($"循环变量类型“{itemType}”与数组元素类型“{arrayElementType}”不一致！");
                }

                _strategy = Strategy.Array;
                _elementType = arrayElementType;
                return;
            }

            // 后续分支需要遍历接口，仅取一次。
            var interfaces = sourceType.GetInterfaces();

            // 2) for —— 非数组：具备 int Count/Length 属性 + this[int] 且类型完全一致。
            if (TryResolveIndexer(sourceType, interfaces, itemType, out _lengthProperty, out _indexerGetter))
            {
                _strategy = Strategy.Indexer;
                _elementType = itemType;
                return;
            }

            // 3) foreach —— 优先 IEnumerable<itemType>。
            //    遍历接口时同时记录是否存在任意泛型枚举（用于步骤 4 的快速失败）。
            var matched = TryFindMatchingGenericEnumerable(sourceType, interfaces, itemType, out var hasOtherGenericEnumerable);

            if (matched is not null)
            {
                _elementType = itemType;
                _enumeratorType = typeof(IEnumerator<>).MakeGenericType(itemType);
                _getEnumeratorMethod = matched.GetMethod(nameof(IEnumerable<int>.GetEnumerator));
                _getCurrentMethod = _enumeratorType.GetProperty(nameof(IEnumerator<int>.Current)).GetGetMethod();
                _strategy = Strategy.GenericEnumerable;
                return;
            }

            // 4) 已暴露更具体的元素类型（IEnumerable<T> 中 T ≠ itemType）—— 不可强转。
            if (hasOtherGenericEnumerable)
            {
                throw new AstException($"循环变量类型“{itemType}”与源“{sourceType}”所暴露的 IEnumerable<T> 元素类型不一致，无法转化！");
            }

            // 5) foreach —— 仅有非泛型 IEnumerable，每轮迭代对 Current 强转。
            if (typeof(IEnumerable).IsAssignableFrom(sourceType))
            {
                _elementType = typeof(object);
                _enumeratorType = typeof(IEnumerator);
                _getEnumeratorMethod = _nonGenericGetEnumerator;
                _getCurrentMethod = _nonGenericCurrentGetter;
                _strategy = Strategy.NonGenericEnumerable;
                return;
            }

            throw new AstException($"类型“{sourceType}”不支持 foreach（必须是数组、IEnumerable 或 IEnumerable<T>，或具备 Count/Length 与 this[int]）！");
        }

        /// <summary>
        /// 循环变量。
        /// </summary>
        public VariableExpression Item => _item;

        /// <summary>
        /// 在 <paramref name="sourceType"/> 自身及其接口中查找 <c>int Count</c> 或 <c>int Length</c> 属性，以及
        /// 返回类型与 <paramref name="itemType"/> 一致的 <c>this[int]</c> 索引器。两者均命中时返回 <c>true</c>。
        /// </summary>
        private static bool TryResolveIndexer(Type sourceType, Type[] interfaces, Type itemType,
            out PropertyInfo lengthProperty, out MethodInfo indexerGetter)
        {
            // 自身
            lengthProperty = FindIntProperty(sourceType, "Count") ?? FindIntProperty(sourceType, "Length");
            indexerGetter = FindIntIndexerGetter(sourceType);

            // 接口（仅在缺失时补查，避免无谓反射）
            if (lengthProperty is null || indexerGetter is null)
            {
                foreach (var iface in interfaces)
                {
                    lengthProperty ??= FindIntProperty(iface, "Count") ?? FindIntProperty(iface, "Length");

                    indexerGetter ??= FindIntIndexerGetter(iface);

                    if (lengthProperty is not null && indexerGetter is not null)
                    {
                        break;
                    }
                }
            }

            if (lengthProperty is null || indexerGetter is null || indexerGetter.ReturnType != itemType)
            {
                lengthProperty = null;
                indexerGetter = null;
                return false;
            }

            return true;
        }

        /// <summary>
        /// 查找返回类型为 <see cref="int"/> 的公共实例属性（必须可读）。
        /// </summary>
        private static PropertyInfo FindIntProperty(Type type, string name)
        {
            var prop = type.GetProperty(name, PublicInstance, null, typeof(int), Type.EmptyTypes, null);

            if (prop is null || !prop.CanRead)
            {
                return null;
            }

            return prop;
        }

        /// <summary>
        /// 查找以 <see cref="int"/> 为参数的 <c>this[int]</c> 索引器 getter。
        /// </summary>
        private static MethodInfo FindIntIndexerGetter(Type type)
        {
            return type.GetMethod("get_Item", PublicInstance, null, new[] { typeof(int) }, null);
        }

        /// <summary>
        /// 在 <paramref name="sourceType"/> 自身及其接口中查找 <see cref="IEnumerable{T}"/>。优先返回 <c>T == itemType</c> 的实现；
        /// 若不存在精确匹配但存在其它 <c>T</c>，通过 <paramref name="hasOtherGenericEnumerable"/> 报告。
        /// </summary>
        private static Type TryFindMatchingGenericEnumerable(Type sourceType, Type[] interfaces, Type itemType,
            out bool hasOtherGenericEnumerable)
        {
            hasOtherGenericEnumerable = false;
            Type matched = null;

            // 源类型自身可能就是 IEnumerable<T>。
            if (sourceType.IsGenericType && sourceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                if (sourceType.GetGenericArguments()[0] == itemType)
                {
                    return sourceType;
                }

                hasOtherGenericEnumerable = true;
                matched = null;
            }

            foreach (var iface in interfaces)
            {
                if (!iface.IsGenericType || iface.GetGenericTypeDefinition() != typeof(IEnumerable<>))
                {
                    continue;
                }

                if (iface.GetGenericArguments()[0] == itemType)
                {
                    return iface;
                }

                hasOtherGenericEnumerable = true;
            }

            return matched;
        }

        /// <inheritdoc/>
        public override void Load(ILGenerator ilg)
        {
            if (IsEmpty)
            {
                throw new AstException("代码块为空！");
            }

            var breakLabel = new Label(LabelKind.Break);
            var continueLabel = new Label(LabelKind.Continue);

            base.MarkLabel(breakLabel);
            base.MarkLabel(continueLabel);

            switch (_strategy)
            {
                case Strategy.Array:
                    EmitArrayLoop(ilg, breakLabel, continueLabel);
                    break;
                case Strategy.Indexer:
                    EmitIndexerLoop(ilg, breakLabel, continueLabel);
                    break;
                case Strategy.GenericEnumerable:
                    EmitEnumeratorLoop(ilg, breakLabel, continueLabel, isGeneric: true);
                    break;
                case Strategy.NonGenericEnumerable:
                    EmitEnumeratorLoop(ilg, breakLabel, continueLabel, isGeneric: false);
                    break;
            }
        }

        private void EmitArrayLoop(ILGenerator ilg, Label breakLabel, Label continueLabel)
        {
            // 缓存数组到本地变量，避免重复求值。
            var arrayLocal = ilg.DeclareLocal(_source.RuntimeType);
            _source.Load(ilg);
            ilg.Emit(OpCodes.Stloc, arrayLocal);

            // index = 0
            var indexLocal = ilg.DeclareLocal(typeof(int));
            ilg.Emit(OpCodes.Ldc_I4_0);
            ilg.Emit(OpCodes.Stloc, indexLocal);

            var conditionLabel = ilg.DefineLabel();
            ilg.Emit(OpCodes.Br, conditionLabel);

            // body 起始：item = array[index]; <body>
            var bodyStartLabel = ilg.DefineLabel();
            ilg.MarkLabel(bodyStartLabel);

            ilg.Emit(OpCodes.Ldloc, arrayLocal);
            ilg.Emit(OpCodes.Ldloc, indexLocal);
            EmitLdelem(ilg, _elementType);
            _item.Storage(ilg);

            // 循环体
            base.Load(ilg);

            // continue：++index
            continueLabel.MarkLabel(ilg);
            ilg.Emit(OpCodes.Ldloc, indexLocal);
            ilg.Emit(OpCodes.Ldc_I4_1);
            ilg.Emit(OpCodes.Add);
            ilg.Emit(OpCodes.Stloc, indexLocal);

            // 条件：index < array.Length
            ilg.MarkLabel(conditionLabel);
            ilg.Emit(OpCodes.Ldloc, indexLocal);
            ilg.Emit(OpCodes.Ldloc, arrayLocal);
            ilg.Emit(OpCodes.Ldlen);
            ilg.Emit(OpCodes.Conv_I4);
            ilg.Emit(OpCodes.Blt, bodyStartLabel);

            // break 标签
            breakLabel.MarkLabel(ilg);
            ilg.Emit(OpCodes.Nop);
        }

        private void EmitIndexerLoop(ILGenerator ilg, Label breakLabel, Label continueLabel)
        {
            // 缓存源到本地变量，避免重复求值。
            var sourceLocal = ilg.DeclareLocal(_source.RuntimeType);
            _source.Load(ilg);
            ilg.Emit(OpCodes.Stloc, sourceLocal);

            // length = source.Count / source.Length
            var lengthLocal = ilg.DeclareLocal(typeof(int));
            LoadSourceForCall(ilg, sourceLocal);
            EmitCall(ilg, _lengthProperty.GetGetMethod());
            ilg.Emit(OpCodes.Stloc, lengthLocal);

            // index = 0
            var indexLocal = ilg.DeclareLocal(typeof(int));
            ilg.Emit(OpCodes.Ldc_I4_0);
            ilg.Emit(OpCodes.Stloc, indexLocal);

            var conditionLabel = ilg.DefineLabel();
            ilg.Emit(OpCodes.Br, conditionLabel);

            // body 起始：item = source[index];
            var bodyStartLabel = ilg.DefineLabel();
            ilg.MarkLabel(bodyStartLabel);

            LoadSourceForCall(ilg, sourceLocal);
            ilg.Emit(OpCodes.Ldloc, indexLocal);
            EmitCall(ilg, _indexerGetter);
            _item.Storage(ilg);

            // 循环体
            base.Load(ilg);

            // continue：++index
            continueLabel.MarkLabel(ilg);
            ilg.Emit(OpCodes.Ldloc, indexLocal);
            ilg.Emit(OpCodes.Ldc_I4_1);
            ilg.Emit(OpCodes.Add);
            ilg.Emit(OpCodes.Stloc, indexLocal);

            // 条件：index < length
            ilg.MarkLabel(conditionLabel);
            ilg.Emit(OpCodes.Ldloc, indexLocal);
            ilg.Emit(OpCodes.Ldloc, lengthLocal);
            ilg.Emit(OpCodes.Blt, bodyStartLabel);

            // break 标签
            breakLabel.MarkLabel(ilg);
            ilg.Emit(OpCodes.Nop);
        }

        private void EmitEnumeratorLoop(ILGenerator ilg, Label breakLabel, Label continueLabel, bool isGeneric)
        {
            var enumerator = ilg.DeclareLocal(_enumeratorType);

            // enumerator = source.GetEnumerator();
            _source.Load(ilg);

            if (_source.RuntimeType.IsValueType)
            {
                ilg.Emit(OpCodes.Box, _source.RuntimeType);
            }

            ilg.Emit(OpCodes.Callvirt, _getEnumeratorMethod);
            ilg.Emit(OpCodes.Stloc, enumerator);

            // try { ... } finally { dispose }
            ilg.BeginExceptionBlock();

            // continue：检查 MoveNext
            continueLabel.MarkLabel(ilg);
            ilg.Emit(OpCodes.Ldloc, enumerator);
            ilg.Emit(OpCodes.Callvirt, _moveNextMethod);

            var afterTestLabel = ilg.DefineLabel();
            ilg.Emit(OpCodes.Brtrue, afterTestLabel);

            // 跳到 break（位于 try 块内部，故 Br 合法）
            breakLabel.Goto(ilg);

            ilg.MarkLabel(afterTestLabel);

            // item = enumerator.Current; （非泛型分支需要从 object 强转到 itemType）
            ilg.Emit(OpCodes.Ldloc, enumerator);
            ilg.Emit(OpCodes.Callvirt, _getCurrentMethod);

            if (!isGeneric && _item.RuntimeType != typeof(object))
            {
                if (_item.RuntimeType.IsValueType)
                {
                    ilg.Emit(OpCodes.Unbox_Any, _item.RuntimeType);
                }
                else
                {
                    ilg.Emit(OpCodes.Castclass, _item.RuntimeType);
                }
            }

            _item.Storage(ilg);

            // 循环体
            base.Load(ilg);

            // 回到 continue
            continueLabel.Goto(ilg);

            // break 标签（仍在 try 块内）
            breakLabel.MarkLabel(ilg);
            ilg.Emit(OpCodes.Nop);

            // finally：释放枚举器
            ilg.BeginFinallyBlock();

            if (isGeneric)
            {
                // IEnumerator<T> 必然实现 IDisposable，直接调用即可。
                ilg.Emit(OpCodes.Ldloc, enumerator);
                ilg.Emit(OpCodes.Callvirt, _disposeMethod);
            }
            else
            {
                // if (enumerator is IDisposable d) d.Dispose();
                ilg.Emit(OpCodes.Ldloc, enumerator);
                ilg.Emit(OpCodes.Isinst, typeof(IDisposable));
                var disposable = ilg.DeclareLocal(typeof(IDisposable));
                ilg.Emit(OpCodes.Stloc, disposable);
                ilg.Emit(OpCodes.Ldloc, disposable);
                var endFinallyLabel = ilg.DefineLabel();
                ilg.Emit(OpCodes.Brfalse_S, endFinallyLabel);
                ilg.Emit(OpCodes.Ldloc, disposable);
                ilg.Emit(OpCodes.Callvirt, _disposeMethod);
                ilg.MarkLabel(endFinallyLabel);
            }

            ilg.EndExceptionBlock();
        }

        private void LoadSourceForCall(ILGenerator ilg, LocalBuilder sourceLocal)
        {
            if (_source.RuntimeType.IsValueType)
            {
                // 值类型走装箱后 callvirt，简单且与枚举器分支一致。
                ilg.Emit(OpCodes.Ldloc, sourceLocal);
                ilg.Emit(OpCodes.Box, _source.RuntimeType);
            }
            else
            {
                ilg.Emit(OpCodes.Ldloc, sourceLocal);
            }
        }

        private static void EmitCall(ILGenerator ilg, MethodInfo method)
        {
            if (method.IsVirtual && !method.IsFinal)
            {
                ilg.Emit(OpCodes.Callvirt, method);
            }
            else
            {
                ilg.Emit(OpCodes.Call, method);
            }
        }

        private static void EmitLdelem(ILGenerator ilg, Type elementType)
        {
            if (!elementType.IsValueType)
            {
                ilg.Emit(OpCodes.Ldelem_Ref);
                return;
            }

            switch (Type.GetTypeCode(elementType))
            {
                case TypeCode.SByte:
                case TypeCode.Boolean:
                    ilg.Emit(OpCodes.Ldelem_I1);
                    break;
                case TypeCode.Byte:
                    ilg.Emit(OpCodes.Ldelem_U1);
                    break;
                case TypeCode.Int16:
                    ilg.Emit(OpCodes.Ldelem_I2);
                    break;
                case TypeCode.Char:
                case TypeCode.UInt16:
                    ilg.Emit(OpCodes.Ldelem_U2);
                    break;
                case TypeCode.Int32:
                    ilg.Emit(OpCodes.Ldelem_I4);
                    break;
                case TypeCode.UInt32:
                    ilg.Emit(OpCodes.Ldelem_U4);
                    break;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    ilg.Emit(OpCodes.Ldelem_I8);
                    break;
                case TypeCode.Single:
                    ilg.Emit(OpCodes.Ldelem_R4);
                    break;
                case TypeCode.Double:
                    ilg.Emit(OpCodes.Ldelem_R8);
                    break;
                default:
                    ilg.Emit(OpCodes.Ldelem, elementType);
                    break;
            }
        }

        /// <inheritdoc/>
        protected internal override void MarkLabel(Label label)
        {
            // 与 LoopExpression 一致：仅放行 Return 标签向外冒泡，
            // Break / Continue 由本表达式自身消费。
            if (label.Kind == LabelKind.Return)
            {
                base.MarkLabel(label);
            }
        }
    }
}
