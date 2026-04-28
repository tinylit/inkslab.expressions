using System;
using System.Reflection;
using Xunit;

namespace Inkslab.Expressions.Tests
{
    /// <summary>
    /// <see cref="TypeCompiler.GetField"/> 的字段名匹配回归测试。
    /// 修复前实现仅按字段类型匹配，当目标类型存在多个相同类型字段时，
    /// 会错误地返回第一个，导致动态生成代码访问到错误的字段。
    /// </summary>
    public class TypeCompilerGetFieldTests
    {
        // 含两个相同类型但不同名字段的泛型类型——专门用于复现修复前的 bug。
        private class Holder<T>
        {
#pragma warning disable CS0649 // 字段未赋值，仅用于反射
            public T First;
            public T Second;
            public int Counter;
            public int Counter2;
#pragma warning restore CS0649
        }

        [Fact]
        public void GetField_GenericType_ResolvesFieldByName_NotJustByType()
        {
            Type closedType = typeof(Holder<int>);

            // 关键回归点：两个字段同类型 (int)，仅名字不同。
            // 修复前会因为只比类型而都返回第一个匹配的字段。
            FieldInfo refCounter = closedType.GetField(nameof(Holder<int>.Counter));
            FieldInfo refCounter2 = closedType.GetField(nameof(Holder<int>.Counter2));

            FieldInfo resolvedCounter = TypeCompiler.GetField(closedType, refCounter);
            FieldInfo resolvedCounter2 = TypeCompiler.GetField(closedType, refCounter2);

            Assert.Equal(nameof(Holder<int>.Counter), resolvedCounter.Name);
            Assert.Equal(nameof(Holder<int>.Counter2), resolvedCounter2.Name);
            Assert.NotEqual(resolvedCounter.Name, resolvedCounter2.Name);
        }

        [Fact]
        public void GetField_NonGenericType_ReturnsOriginalField()
        {
            FieldInfo field = typeof(Holder<int>).GetField(nameof(Holder<int>.Counter));

            FieldInfo resolved = TypeCompiler.GetField(typeof(string), field);

            // 非泛型目标类型时，按既有契约直接返回原始字段。
            Assert.Same(field, resolved);
        }

        [Fact]
        public void GetField_NullArguments_Throws()
        {
            FieldInfo field = typeof(Holder<int>).GetField(nameof(Holder<int>.Counter));

            Assert.Throws<ArgumentNullException>(() => TypeCompiler.GetField(null, field));
            Assert.Throws<ArgumentNullException>(() => TypeCompiler.GetField(typeof(Holder<int>), null));
        }
    }
}
