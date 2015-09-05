using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeWeaver.Vsix.Processor
{
    class Result_pgo_Replacement : CSharpSyntaxRewriter
    {
        MethodDeclarationSyntax _m;
        ExpressionSyntax _loggerExpression = SyntaxFactory.ParseExpression(DocumentWeaver.LoggerFullName);
        public Result_pgo_Replacement(MethodDeclarationSyntax m)
        {
            _m = m;
        }
        /*
            var result = tryStatements;
            AssignmentExpressionSyntax assign = null;
            StatementSyntax assignStm = null;
            ExpressionStatementSyntax current = null;
            List<StatementSyntax> l = new List<StatementSyntax>(tryStatements);
            foreach (var stm in l.ToArray())
            {
                current = stm as ExpressionStatementSyntax;
                if (current != null && current.Expression is AssignmentExpressionSyntax)
                {
                    var ex = (AssignmentExpressionSyntax)current.Expression;
                    if (ex.Left is IdentifierNameSyntax && ((IdentifierNameSyntax)ex.Left).Identifier.Text == RESULTMARKER)
                    {
                        assign = ex;
                        assignStm = current;
                        continue;
                    }
                }
                if (assign != null && stm is ReturnStatementSyntax)
                {
                    var ret = (ReturnStatementSyntax)stm;
                    var ident = ret.Expression as IdentifierNameSyntax;
                    if (ident != null && ident.Identifier.Text == RESULTMARKER)
                    {
                        var returnVar = SyntaxFactory.ReturnStatement(assign.Right);
                        l[l.IndexOf(ret)] = returnVar;
                        l.Remove(assignStm);
                    }
                }
            }
            result = new SyntaxList<StatementSyntax>().AddRange(l);
            return result;
        */
        ExpressionSyntax foundAssignment;
        public override SyntaxNode VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            if (node.Expression is AssignmentExpressionSyntax)
            {
                var ex = (AssignmentExpressionSyntax)node.Expression;
                if (ex.Left is IdentifierNameSyntax && ((IdentifierNameSyntax)ex.Left).Identifier.Text == DocumentWeaver.RESULTMARKER)
                {
                    foundAssignment = ex.Right;
                    //to delete this statement
                    return null;
                }
            }
            if (node.Expression is InvocationExpressionSyntax)
            {
                var newNode = VisitInvocationExpression((InvocationExpressionSyntax)node.Expression);
                if (newNode == null) return null;
            }
            return base.VisitExpressionStatement(node);
        }

        public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            MemberAccessExpressionSyntax subExpression = node.Expression as MemberAccessExpressionSyntax;
            if (subExpression != null && subExpression.Expression !=null && DocumentWeaver.LoggerFullName.Equals(subExpression.Expression.ToString()))
            {
                return null;
            }
            //remove all WeaverReport.xxxx
            return base.VisitInvocationExpression(node);
        }
        public override SyntaxNode VisitReturnStatement(ReturnStatementSyntax node)
        {
            if (foundAssignment != null)
            {
                var ident = node.Expression as IdentifierNameSyntax;
                if (ident != null && ident.Identifier.Text == DocumentWeaver.RESULTMARKER)
                {
                    var result = SyntaxFactory.ReturnStatement(foundAssignment);
                    foundAssignment = null;
                    return result;
                }
            }
            return base.VisitReturnStatement(node);
        }
    }
}
