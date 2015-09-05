using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeWeaver.Vsix.Processor
{
    class ReturnReplacement : CSharpSyntaxRewriter
    {
        MethodDeclarationSyntax _m;
        public bool modified = false;
        public ReturnReplacement(MethodDeclarationSyntax m)
        {
            _m = m;
        }
        int testForVisualizer()
        {
            int[] v = Enumerable.Range(0, 10).ToArray();

            foreach (var item in v.Where(x => { if (x == 34) return true; else return false; }))
            {
                if (item == 50) return 90;
                else if (item == 66) return 67;
            }
            return 456;
        }
        void test(int a, ref int b, out int c)
        {
            c = 23;
            var coucou = "ttoo";
            var toto = 1;
            Console.WriteLine(new Object[] { coucou, toto });
            foreach (var item in Enumerable.Range(0, 100))
            {
                if (item % 2 == 0) return;
            }
        }
        public override SyntaxNode VisitReturnStatement(ReturnStatementSyntax node)
        {
            if (!IsInLambda(node))
            {
                modified = true;
                return CreateNewReturnStm(node);
            }
            return base.VisitReturnStatement(node);
        }
        public override SyntaxNode VisitBlock(BlockSyntax node)
        {
            var newNode = (BlockSyntax)base.VisitBlock(node);
            //remove block in block
            if (newNode.Statements.OfType<BlockSyntax>().Any())
            {
                var l = new List<StatementSyntax>(newNode.Statements);
                for (int i = 0; i < l.Count; i++)
                {
                    if (l[i] is BlockSyntax)
                    {
                        var count = ((BlockSyntax)l[i]).Statements.Count;
                        foreach (var stm in ((BlockSyntax)l[i]).Statements.Reverse())
                        {
                            l.Insert(i, stm);
                        }
                        l.RemoveAt(i + count);
                    }
                }
                newNode = SyntaxFactory.Block(l);
            }
            return newNode;
        }

        private bool IsInLambda(ReturnStatementSyntax node)
        {
            SyntaxNode n = node;
            while (n != null)
            {
                if (n is LambdaExpressionSyntax) return true;
                n = n.Parent;
            }
            return false;
        }

        private StatementSyntax CreateNewReturnStm(ReturnStatementSyntax ret)
        {
            List<StatementSyntax> statements = new List<StatementSyntax>();
            var arg = DocumentWeaver.ParametersToArg(_m.ParameterList.Parameters, x => x.Modifiers.Count(y => y.Kind() == SyntaxKind.OutKeyword || y.Kind() == SyntaxKind.RefKeyword) > 0);
            if (arg != null)
                statements.Add(DocumentWeaver.ReportStatement("PushOutArgs", SyntaxFactory.Argument(arg)));
            if (DocumentWeaver.IsFunction(_m))
            {
                //pushResult
                var left = SyntaxFactory.IdentifierName(DocumentWeaver.RESULTMARKER);
                var newNode = SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, left, ret.Expression);
                var returnVar = SyntaxFactory.ReturnStatement(left);
                //specialvar = xxxx
                statements.Add(SyntaxFactory.ExpressionStatement(newNode));
                //pushresult (speacialvar)
                statements.Add(DocumentWeaver.ReportStatement("PushResult", SyntaxFactory.Argument(SyntaxFactory.IdentifierName(DocumentWeaver.RESULTMARKER))));
                //return specialvar
                statements.Add(returnVar);
            }
            else
            {
                //if void method there is no expression after return, so we add the same expression 'return;'
                statements.Add(ret);
            }
            return SyntaxFactory.Block(statements);
        }
    }
}
