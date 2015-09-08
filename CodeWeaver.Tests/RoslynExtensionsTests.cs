using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeWeaver.Vsix;

namespace CodeWeaver.Tests
{
    [TestClass]
    public class RoslynExtensionsTests
    {
        [TestMethod]
        public void GetMethodDeclarationFromTokenTests()
        {
            string sourceCode = @"class MyClass
{
    public void Toto()
    {

    }

//#Marker1

    public void Toto1()
    {
    //#Marker2

    }

    public void Toto2()
    {

    }
    //#Marker3

    public void Toto3()
    {
        {
            Console.WriteLine(12);
        }//#Marker4
    }

}
";
            var tree = CSharpSyntaxTree.ParseText(sourceCode);
            var token = tree.GetRoot().FindTrivia(sourceCode.IndexOf("//#Marker1", StringComparison.Ordinal));
            Assert.IsNull(token.GetMethodDeclarationFromTrivia());

            token = tree.GetRoot().FindTrivia(sourceCode.IndexOf("//#Marker2", StringComparison.Ordinal));
            Assert.IsNotNull(token.GetMethodDeclarationFromTrivia());
            token = tree.GetRoot().FindTrivia(sourceCode.IndexOf("//#Marker3", StringComparison.Ordinal));
            Assert.IsNull(token.GetMethodDeclarationFromTrivia());
            token = tree.GetRoot().FindTrivia(sourceCode.IndexOf("//#Marker1", StringComparison.Ordinal) - 3);
            Assert.IsNull(token.GetMethodDeclarationFromTrivia());
            token = tree.GetRoot().FindTrivia(sourceCode.IndexOf("//#Marker4", StringComparison.Ordinal));
            Assert.IsNotNull(token.GetMethodDeclarationFromTrivia());

        }
    }
}
