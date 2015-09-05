using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using CodeWeaver.Vsix.Processor;

namespace CodeWeaver.Tests
{
    [TestClass]
    public class DocumentWeaverTests
    {
        [TestMethod]
        public void WeaveTest()
        {
            var sourceCode = @"bool TyGet(out int a, out int b)
        {
            a = 123;
            b = 567;
            return false;
        }";
            var docWeaver = new DocumentWeaver(CSharpSyntaxTree.ParseText(sourceCode));
            var newSyntaxTree = docWeaver.Weave();
            var newCode = newSyntaxTree.ToString();
            Assert.IsNotNull(newCode);
            Assert.IsTrue(newCode.Contains("try//$PGO$/0X200X150X080X31/$PGO$\r"));
            Assert.IsTrue(newCode.Contains("WeaverReport.Logger.BeginReport"));
            Assert.IsTrue(newCode.Contains("WeaverReport.Logger.EndReport"));
            Assert.IsTrue(newCode.Contains("WeaverReport.Logger.PushOutArgs"));
            Assert.IsFalse(newCode.Contains("WeaverReport.Logger.PushArgs"));
            Assert.IsTrue(newCode.Contains("WeaverReport.Logger.PushResult"));
        }
    }
}
