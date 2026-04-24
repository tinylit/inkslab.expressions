using Xunit;
using System;
using System.Collections.Generic;

namespace Inkslab.Expressions.Tests
{
    /// <summary>
    /// Internal API 覆盖率测试 — TypeCompiler。
    /// </summary>
    public class InternalCoverageTests
    {
        #region TypeCompiler

        [Fact]
        public void TypeCompiler_GetReturnType_Simple()
        {
            var mi = typeof(List<int>).GetMethod("Add");
            var rt = TypeCompiler.GetReturnType(mi, Type.EmptyTypes, Type.EmptyTypes);
            Assert.Equal(typeof(void), rt);
        }

        [Fact]
        public void TypeCompiler_GetReturnType_Generic()
        {
            var mi = typeof(List<int>).GetMethod("get_Item");
            var rt = TypeCompiler.GetReturnType(mi, Type.EmptyTypes, Type.EmptyTypes);
            Assert.Equal(typeof(int), rt);
        }

        [Fact]
        public void TypeCompiler_GetConstructor()
        {
            var ci = typeof(List<int>).GetConstructor(Type.EmptyTypes);
            var result = TypeCompiler.GetConstructor(typeof(List<int>), ci);
            Assert.NotNull(result);
        }

        [Fact]
        public void TypeCompiler_GetField()
        {
            var fi = typeof(string).GetField("Empty");
            var result = TypeCompiler.GetField(typeof(string), fi);
            Assert.NotNull(result);
        }

        [Fact]
        public void TypeCompiler_GetMethod()
        {
            var mi = typeof(List<int>).GetMethod("Add");
            var result = TypeCompiler.GetMethod(typeof(List<int>), mi);
            Assert.NotNull(result);
        }

        #endregion
    }
}
