using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeWeaver.Vsix
{
    public static class RoslynExtensions
    {

        public static T GetNodeFromToken<T>(this SyntaxTrivia trivia) where T : SyntaxNode
        {
            //trivia is before the first token of T, so it's before
            if (trivia.Token.Parent is T && trivia.Token.HasLeadingTrivia && trivia.Token.LeadingTrivia.Contains(trivia)) return null;
            //if trivia is after the last token
            if (trivia.Token.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.CloseBraceToken)
                && trivia.Token.HasTrailingTrivia
                && trivia.Token.TrailingTrivia.Contains(trivia))
            {
                SyntaxNode n = trivia.Token.Parent;

                while (true)
                {
                    if (n is T) return null;
                    if (n == null) return null;
                    var nextParent = n.Parent;
                    //if n is not the last, trailing trivias are not outside
                    if (nextParent != null && nextParent.ChildNodesAndTokens().Last() != n) break;
                    n = nextParent;
                }
            }
            var node = trivia.Token.Parent;
            if (node == null) return null;
            while (node != null)
            {
                var result = node as T;
                if (node != null) return result;
                node = node.Parent;
            }

            return null;
        }

        public static MethodDeclarationSyntax GetMethodDeclarationFromTrivia(this SyntaxTrivia trivia)
        {
            return GetNodeFromToken<MethodDeclarationSyntax>(trivia);
        }

        public static ClassDeclarationSyntax GetClassDeclarationFromTrivia(this SyntaxTrivia trivia)
        {
            return GetNodeFromToken<ClassDeclarationSyntax>(trivia);
        }
    }
}
