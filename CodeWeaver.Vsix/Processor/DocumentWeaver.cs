using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeWeaver.Vsix.Processor
{
    class DocumentWeaver
    {
        const string MARKER = "//$PGO$/0X200X150X080X31/$PGO$";
        public const string RESULTMARKER = "__result_pgo_";
        const string EXCEPTIONVARNAME = "__e__";
        public const string LoggerFullName = "WeaverReport.Logger";

        Document _doc;
        public DocumentWeaver(Document doc)
        {
            if (doc == null) throw new ArgumentNullException("doc");
            _doc = doc;
        }
        /// <summary>
        /// weave code
        /// </summary>
        /// <returns></returns>
        public Document Weave(/* todo report method can be provided*/)
        {
            var root = _doc.GetSyntaxRootAsync().Result;
            var resultDoc = _doc;
            var foundMethods = root.DescendantNodes().OfType<MethodDeclarationSyntax>().Where(HasNotBeenWeaved);

            Dictionary<BlockSyntax, BlockSyntax> dico = new Dictionary<BlockSyntax, BlockSyntax>();
            foreach (var m in foundMethods)
            {
                //finally just to trace ref/out/and result
                BlockSyntax tryBlock = ReplaceReturn(m);
                var catchClause = CatchClause(m);
                //add try/catch
                var tryStatement = SyntaxFactory.TryStatement(
                    //try with marker in order be able to remove
                    SyntaxFactory.Token(SyntaxKind.TryKeyword)
                        .WithTrailingTrivia(SyntaxFactory.Comment(MARKER), SyntaxFactory.CarriageReturn),
                    tryBlock, 
                    //catch clauses
                    new SyntaxList<CatchClauseSyntax>().Add(catchClause),
                    //finally if needed
                    FinallyClause(m));
                var oldBody = m.Body;
                var bodyStatements = AddAdditionnalDeclaration(m, new SyntaxList<StatementSyntax>());
                //.Add(/*result declation for functions*/)
                bodyStatements = bodyStatements.Add(tryStatement);
                //update body to give a new one
                var newBody = m.Body.Update(m.Body.OpenBraceToken,
                    bodyStatements, 
                    m.Body.CloseBraceToken);
                dico.Add(oldBody, newBody);
            }
            var newRoot = root.ReplaceNodes(dico.Keys, (x, y) => dico[x]);
            var newDoc = resultDoc.WithSyntaxRoot(newRoot);
            resultDoc = Formatter.FormatAsync(newDoc).Result;
            return resultDoc;
        }

        public Document UnWeave()
        {
            var root = _doc.GetSyntaxRootAsync().Result;
            var resultDoc = _doc;
            var foundMethods = root.DescendantNodes().OfType<MethodDeclarationSyntax>().Where(HasBeenWeaved);

            Dictionary<BlockSyntax, BlockSyntax> dico = new Dictionary<BlockSyntax, BlockSyntax>();
            foreach (var m in foundMethods)
            {
                Func<SyntaxTriviaList, bool> hasMarker = (x) => x.Any(y => y.ToString() == MARKER);
                Func<TryStatementSyntax, bool> filterByMarker = (x) => { return x.TryKeyword.HasTrailingTrivia && hasMarker(x.TryKeyword.TrailingTrivia); };
                //search for try
                var trystatement = m.Body.Statements.OfType<TryStatementSyntax>().Where(filterByMarker).FirstOrDefault();
                if (trystatement != null)
                {
                    var oldBody = m.Body;
                    var newBody = Replace__result_pgo(m, trystatement.Block);
                    dico.Add(oldBody, newBody);
                }
            }
            var newRoot = root.ReplaceNodes(dico.Keys, (x, y) => dico[x]);
            var newDoc = resultDoc.WithSyntaxRoot(newRoot);
            resultDoc = Formatter.FormatAsync(newDoc).Result;
            return resultDoc;
        }

        #region private methods

        private BlockSyntax ReplaceReturn(MethodDeclarationSyntax m)
        {
            var rr = new ReturnReplacement(m);
            var blockSyntax = (BlockSyntax)rr.Visit(m.Body);
            return blockSyntax;
        }
        private BlockSyntax Replace__result_pgo(MethodDeclarationSyntax m, BlockSyntax toVisit)
        {
            var rr = new Result_pgo_Replacement(m);
            return (BlockSyntax)rr.Visit(toVisit);
        }
        private FinallyClauseSyntax FinallyClause(MethodDeclarationSyntax m)
        {
            return SyntaxFactory.FinallyClause().WithBlock(SyntaxFactory.Block(ReportStatement("EndReport")));
        }

        private CatchClauseSyntax CatchClause(MethodDeclarationSyntax m)
        {
            List<StatementSyntax> statements = new List<StatementSyntax>();
            //trace exception
            statements.Add(ReportStatement("PushException", SyntaxFactory.Argument(SyntaxFactory.IdentifierName("__e__"))));
            //rethrow it
            statements.Add(SyntaxFactory.ThrowStatement(SyntaxFactory.IdentifierName(EXCEPTIONVARNAME)));

            var type = SyntaxFactory.ParseTypeName("System.Exception");
            var identifier = SyntaxFactory.Identifier("__e__");
            var decl = SyntaxFactory.CatchDeclaration(type, identifier);
            return SyntaxFactory.CatchClause(decl, null, SyntaxFactory.Block(statements));
        }
        internal static bool IsFunction(MethodDeclarationSyntax m)
        {
            return !m.ReturnType.GetFirstToken().IsKind(SyntaxKind.VoidKeyword);
        }

        private SyntaxList<StatementSyntax> AddAdditionnalDeclaration(MethodDeclarationSyntax m, SyntaxList<StatementSyntax> bodyStatements)
        {
            SyntaxList<StatementSyntax> result = bodyStatements;
            //add result var in order to trace it
            if (IsFunction(m))
            {
                var localDeclarationstm = SyntaxFactory.LocalDeclarationStatement(
                    SyntaxFactory.VariableDeclaration(m.ReturnType)
                    .WithVariables(new SeparatedSyntaxList<VariableDeclaratorSyntax>().Add(SyntaxFactory.VariableDeclarator(RESULTMARKER)))
                    );
                result = bodyStatements.Add(localDeclarationstm);
            }
            //Add at start of void/function 'WeaverReport.Logger.BeginReport(fullname of void/function)'
            var stm = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(MethodFullName(m)));
            result = result.Add(ReportStatement("BeginReport",SyntaxFactory.Argument(stm)));
            var parameters = m.ParameterList.Parameters;
            var arg = ParametersToArg(parameters, x => !x.Modifiers.Any<SyntaxToken>(y => SyntaxKind.OutKeyword == y.Kind()));
            if (arg != null)
                result = result.Add(ReportStatement("PushArgs", SyntaxFactory.Argument(arg)));
            return result;
        }

        void test(int a, ref int b, out int c)
        {
            c = 23;
            var coucou = "ttoo";
            var toto = 1;
            Console.WriteLine(new Object[] { coucou, toto });
            //Console.WriteLine(new Object { coucou, toto });
        }

        internal static ExpressionSyntax ParametersToArg(SeparatedSyntaxList<ParameterSyntax> parameters, Func<ParameterSyntax, bool> paramFilter)
        {
            List<ExpressionSyntax> identifiers = new List<ExpressionSyntax>();
            Func<ParameterSyntax, bool> filter = null;
            if (paramFilter != null)
            {
                filter = paramFilter;
            }
            else
                filter = x => true;
            identifiers.AddRange(parameters.Where(filter).Select(x => SyntaxFactory.IdentifierName(x.Identifier.Text)));
            //if all identifers are 'out' and user should ignore them, so exit with null syntax
            if (identifiers.Count <= 0) return null;
            return SyntaxFactory.ArrayCreationExpression(SyntaxFactory.ArrayType(SyntaxFactory.IdentifierName("Object[]")),
                SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression, 
                SyntaxFactory.SeparatedList<ExpressionSyntax>(identifiers)));
        }

        /// <summary>
        /// returns fullname of a methoddeclaration
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        private string MethodFullName(MethodDeclarationSyntax m)
        {
            //may be a simplest way exists, but it works
            string result = m.Identifier.Text;
            var ancestors = m.Ancestors();
            SyntaxNode classOrStruct;
            var classe = ancestors.OfType<ClassDeclarationSyntax>().FirstOrDefault();
            if (classe != null)
            {
                result = classe.Identifier.Text + "." + result;
                classOrStruct = classe;
            }
            else
            {
                var structe = ancestors.OfType<StructDeclarationSyntax>().FirstOrDefault();
                result = structe.Identifier.Text + "." + result;
                classOrStruct = structe;
            }
            var ns = classOrStruct.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
            var nsString = ns?.Name.ToString();
            result = nsString + "." + result;
            return result;
        }

        private bool HasBeenWeaved(MethodDeclarationSyntax m)
        {
            //return true if m has not been weaved previously
            //find the MARKER
            var trystatements = m.Body.Statements.OfType<TryStatementSyntax>().Where(x => x.TryKeyword.HasTrailingTrivia).SelectMany(x =>x.TryKeyword.TrailingTrivia).Where(x => x.ToString() == MARKER);
            //return true if code has been weaved 
            return trystatements.Any();
        }

        private bool HasNotBeenWeaved(MethodDeclarationSyntax m)
        {
            return !HasBeenWeaved(m);
        }

        /// <summary>
        /// call statement to WeaverReport.Logger
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        internal static StatementSyntax ReportStatement(string methodName, params ArgumentSyntax[] args)
        {
            return SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.ParseExpression(LoggerFullName), SyntaxFactory.IdentifierName(methodName)),
                /*liste des args*/
                args!=null ? SyntaxFactory.ArgumentList().AddArguments(args) : null
                ));
            
        }
        #endregion
    }
}
