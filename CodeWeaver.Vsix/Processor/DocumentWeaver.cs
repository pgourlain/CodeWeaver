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
    public class DocumentWeaver
    {
        const string MARKER = "//$PGO$/0X200X150X080X31/$PGO$";
        public const string RESULTMARKER = "__result_pgo_";
        const string EXCEPTIONVARNAME = "__e__";
        public const string LoggerFullName = "WeaverReport.Logger";

        SyntaxTree _doc;
        public DocumentWeaver(SyntaxTree doc)
        {
            if (doc == null) throw new ArgumentNullException("doc");
            _doc = doc;
        }
        /// <summary>
        /// weave code
        /// </summary>
        /// <returns></returns>
        public SyntaxTree Weave(/* todo report method can be provided*/)
        {
            var root = _doc.GetRootAsync().Result;
            var resultDoc = _doc;
            var foundMethods = root.DescendantNodes().OfType<MethodDeclarationSyntax>().Where(HasNotBeenWeaved);

            Dictionary<BlockSyntax, BlockSyntax> dico = new Dictionary<BlockSyntax, BlockSyntax>();
            foreach (var m in foundMethods)
            {
                BlockSyntax oldBody, newBody;
                WeaveMethod(m, out oldBody, out newBody);
                dico.Add(oldBody, newBody);
            }
            var newRoot = root.ReplaceNodes(dico.Keys, (x, y) => dico[x]);
            resultDoc = resultDoc.WithRootAndOptions(newRoot, _doc.Options);
            return resultDoc;
        }

        public SyntaxTree Weave(SyntaxNode y)
        {
            MethodDeclarationSyntax dcs = y as MethodDeclarationSyntax;
            if (dcs != null) return this.Weave(dcs);
            ClassDeclarationSyntax cds = y as ClassDeclarationSyntax;
            if (cds != null) return this.Weave(cds);
            return null;
        }

        public SyntaxTree UnWeave(SyntaxNode y)
        {
            MethodDeclarationSyntax dcs = y as MethodDeclarationSyntax;
            if (dcs != null) return this.UnWeave(dcs);
            ClassDeclarationSyntax cds = y as ClassDeclarationSyntax;
            if (cds != null) return this.UnWeave(cds);
            return null;
        }

        public SyntaxTree Weave(MethodDeclarationSyntax m)
        {
            var root = _doc.GetRootAsync().Result;
            var resultDoc = _doc;
            BlockSyntax oldBody, newBody;
            if (HasNotBeenWeaved(m))
            {
                WeaveMethod(m, out oldBody, out newBody);
                var newRoot = root.ReplaceNode(oldBody, newBody);
                resultDoc = resultDoc.WithRootAndOptions(newRoot, _doc.Options);
                return resultDoc;
            }
            return null;

        }
        public SyntaxTree UnWeave(MethodDeclarationSyntax m)
        {
            //return this.WeaveOrUnWeave(new MethodDeclarationSyntax[] { m }, UnWeaveMethod);
            var root = _doc.GetRootAsync().Result;
            var resultDoc = _doc;
            BlockSyntax oldBody, newBody;
            if (this.UnWeaveMethod(m, out oldBody, out newBody))
            {
                var newRoot = root.ReplaceNode(oldBody, newBody);
                resultDoc = resultDoc.WithRootAndOptions(newRoot, _doc.Options);
                return resultDoc;
            }
            return null;
        }

        public SyntaxTree Weave(ClassDeclarationSyntax c)
        {
            var foundMethods = c.ChildNodes().OfType<MethodDeclarationSyntax>().Where(HasNotBeenWeaved);
            return WeaveOrUnWeave(foundMethods, WeaveMethod);
        }

        public SyntaxTree UnWeave(ClassDeclarationSyntax c)
        {
            var foundMethods = c.ChildNodes().OfType<MethodDeclarationSyntax>().Where(HasBeenWeaved);
            return WeaveOrUnWeave(foundMethods, UnWeaveMethod);
        }

        public SyntaxTree UnWeave()
        {
            var root = _doc.GetRootAsync().Result;
            var foundMethods = root.DescendantNodes().OfType<MethodDeclarationSyntax>().Where(HasBeenWeaved);
            return WeaveOrUnWeave(foundMethods, UnWeaveMethod);
        }

        #region private methods
        private delegate TResult SpecialFunc<T, T1, T2, TResult>(T arg, out T1 arg1, out T2 arg2);

        private SyntaxTree WeaveOrUnWeave(IEnumerable<MethodDeclarationSyntax> foundMethods, SpecialFunc<MethodDeclarationSyntax, BlockSyntax, BlockSyntax, bool> weaveOrUnweave)
        {
            var root = _doc.GetRootAsync().Result;
            var resultDoc = _doc;

            Dictionary<BlockSyntax, BlockSyntax> dico = new Dictionary<BlockSyntax, BlockSyntax>();
            foreach (var m in foundMethods)
            {
                BlockSyntax oldBody, newBody;
                if (weaveOrUnweave(m, out oldBody, out newBody))
                {
                    dico.Add(oldBody, newBody);
                }
            }
            var newRoot = root.ReplaceNodes(dico.Keys, (x, y) => dico[x]);
            resultDoc = resultDoc.WithRootAndOptions(newRoot, _doc.Options);
            return resultDoc;
        }

        private bool UnWeaveMethod(MethodDeclarationSyntax m, out BlockSyntax oldBody, out BlockSyntax newBody)
        {
            oldBody = null;
            newBody = null;
            Func<SyntaxTriviaList, bool> hasMarker = (x) => x.Any(y => y.ToString() == MARKER);
            Func<TryStatementSyntax, bool> filterByMarker = (x) => { return x.TryKeyword.HasTrailingTrivia && hasMarker(x.TryKeyword.TrailingTrivia); };
            //search for try
            var trystatement = m.Body.Statements.OfType<TryStatementSyntax>().Where(filterByMarker).FirstOrDefault();
            if (trystatement != null)
            {
                oldBody = m.Body;
                newBody = Replace__result_pgo(m, trystatement.Block);
                return true;
            }

            return false;

        }
        private bool WeaveMethod(MethodDeclarationSyntax m, out BlockSyntax oldBody, out BlockSyntax newBody)
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
            oldBody = m.Body;
            var bodyStatements = AddAdditionnalDeclaration(m, new SyntaxList<StatementSyntax>());
            //.Add(/*result declation for functions*/)
            bodyStatements = bodyStatements.Add(tryStatement);
            //update body to give a new one
            newBody = m.Body.Update(m.Body.OpenBraceToken,
                bodyStatements,
                m.Body.CloseBraceToken);
            return true;
        }

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
                if (structe == null)
                {
                    //it's just a peace of code with out class or struct
                    return result;
                }
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
