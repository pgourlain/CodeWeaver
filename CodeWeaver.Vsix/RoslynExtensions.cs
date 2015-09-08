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
            var token = trivia.Token;
            //trivia is before the first token of T, so it's before
            if (token.Parent is T && token.HasLeadingTrivia && token.LeadingTrivia.Contains(trivia)) return null;
            //if trivia is after the last token
            if (token.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.CloseBraceToken)
                && token.HasTrailingTrivia
                && token.TrailingTrivia.Contains(trivia))
            {
                SyntaxNode n = token.Parent;

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
            return token.GetNodeFromToken<T>();
            //var node = token.Parent;
            //if (node == null) return null;
            //while (node != null)
            //{
            //    var result = node as T;
            //    if (result != null) return result;
            //    node = node.Parent;
            //}

            //return null;
        }
        public static T GetNodeFromToken<T>(this SyntaxToken token) where T : SyntaxNode
        {
            var node = token.Parent;
            if (node == null) return null;
            while (node != null)
            {
                var result = node as T;
                if (result != null) return result;
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

        public static MethodDeclarationSyntax GetMethodDeclarationFromToken(this SyntaxToken token)
        {
            return GetNodeFromToken<MethodDeclarationSyntax>(token);
        }
        public static ClassDeclarationSyntax GetClassDeclarationFromToken(this SyntaxToken token)
        {
            return GetNodeFromToken<ClassDeclarationSyntax>(token);
        }
    }
}
