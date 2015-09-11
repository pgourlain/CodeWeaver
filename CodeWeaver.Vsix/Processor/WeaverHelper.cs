using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.VisualStudio.Shell.ThreadedWaitDialogHelper;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeWeaver.Vsix.Processor
{
    enum WeaveEditorResult {
        Ok,
        ActiveDocumentGotFocusFailed,
        NothingToWeave,
        AllReadyWeaveOrUnweave,
    }
    class WeaverHelper
    {
        class CaretContent
        {
            public bool invalidDocument = false;
            public SyntaxToken token;
            public SyntaxTrivia trivia;
            public SyntaxNode GetMethodDeclarationFromTrivia()
            {
                if (trivia.IsKind(SyntaxKind.None))
                {
                    return token.GetMethodDeclarationFromToken();
                }
                return trivia.GetMethodDeclarationFromTrivia();
            }
            public SyntaxNode GetClassDeclarationFromTrivia()
            {
                if (trivia.IsKind(SyntaxKind.None))
                {
                    return token.GetClassDeclarationFromToken();
                }
                return trivia.GetClassDeclarationFromTrivia();
            }
        }

        internal static bool CaretIsInCSharpDocument()
        {
            var caretContent = GetCaretTrivia();
            return (caretContent != null && caretContent.invalidDocument == false);
        }
        #region private methods

        private static WeaveEditorResult InternalWeaveOrUnWeaveFromEditor(Func<DocumentWeaver, SyntaxNode, SyntaxTree> weaveOrunwaveFun)
        {
            Document doc;
            IWpfTextView wpfView;
            var caretContent = GetCaretTrivia(out doc, out wpfView);
            if (caretContent == null) return WeaveEditorResult.ActiveDocumentGotFocusFailed;

            var syntaxTree = doc.GetSyntaxTreeAsync().Result;
            SyntaxNode mOrc = caretContent.GetMethodDeclarationFromTrivia();
            if (mOrc == null) mOrc = caretContent.GetClassDeclarationFromTrivia();
            if (mOrc == null) return WeaveEditorResult.NothingToWeave;
            var weaver = new DocumentWeaver(syntaxTree);
            var newSyntaxTree = weaveOrunwaveFun(weaver, mOrc);
            if (newSyntaxTree == null) return WeaveEditorResult.AllReadyWeaveOrUnweave;
            var root = newSyntaxTree.GetRoot();
            var newDoc = Formatter.FormatAsync(doc.WithSyntaxRoot(root)).Result;
            wpfView.TextBuffer.GetWorkspace().TryApplyChanges(newDoc.Project.Solution);

            return WeaveEditorResult.Ok;
        }

        private static CaretContent GetCaretTrivia(out Document doc, out IWpfTextView wpfView)
        {
            doc = null;
            var dte = (EnvDTE.DTE)Package.GetGlobalService(typeof(EnvDTE.DTE));
            var activeDoc = dte.ActiveDocument;
            if (activeDoc == null)
            {
                //if an editor is host by a toolwindow
                activeDoc = dte.ActiveWindow != null ? dte.ActiveWindow.Document : null;
            }
            wpfView = VSTools.GetWpfTextView(dte, activeDoc);
            if (wpfView == null)
            {
                return null;
            }
            CaretPosition position = wpfView.Caret.Position;

            var point = position.BufferPosition;
            //extensions in 'Microsoft.CodeAnalysis.EditorFeatures.Text'
            doc = point.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (doc == null) return new CaretContent { invalidDocument = true};
            var root = doc.GetSyntaxRootAsync().Result;
            if (root == null) return null;
            var token = root.FindToken(point.Position);
            if (token.IsKind(SyntaxKind.None)) return null;

            var trivia = token.Parent.FindTrivia(point.Position);
            return new CaretContent { token = token, trivia = trivia };
        }

        private static CaretContent GetCaretTrivia()
        {
            Document doc;
            IWpfTextView wpfView;
            return GetCaretTrivia(out doc, out wpfView);
        }
        #endregion

        static protected IEnumerable<Project> FilterProjects(Solution originalSolution)
        {
            var selectedProjects = VSTools.SelectedProjectsFileName().ToArray();
            foreach (var item in originalSolution.Projects.Where(x => x.Language == "C#" && selectedProjects.Contains(x.FilePath)))
            {
                yield return item;
            }
        }

        public static void WeaveOrUnWeave(string rootText, Func<DocumentWeaver, SyntaxTree> weaveOrunwaveFun)
        {
            using (var session = StartWaitDialog("Weaving selected project(s)", "Start weaving", "Weaving selected project(s)"))
            {
                Report(session, "Opening solution with Roslyn");

                using (MSBuildWorkspace workspace = MSBuildWorkspace.Create())
                {
                    var originalSolution = workspace.OpenSolutionAsync(VSTools.SelectedSolution()).Result;
                    var newSolution = originalSolution;

                    foreach (var project in FilterProjects(originalSolution))
                    {
                        Report(session, rootText + " " + project.Name);
                        foreach (var documentid in project.DocumentIds)
                        {
                            var document = newSolution.GetDocument(documentid);
                            Report(session, rootText + " " + document.FilePath);
                            var weaver = new DocumentWeaver(document.GetSyntaxTreeAsync().Result);
                            var newRoot = weaveOrunwaveFun(weaver).GetRoot();
                            var newDoc = Formatter.FormatAsync(document.WithSyntaxRoot(newRoot)).Result;
                            newSolution = newDoc.Project.Solution;
                        }
                        if (workspace.TryApplyChanges(newSolution))
                        {
                            //
                            //MessageBox("Done successfully");
                            Trace.WriteLine("Changes are done for project " + project.Name);
                        }
                    }
                }
            }
        }

        static protected Session StartWaitDialog(string waitMessage, string progressText = null, string statusText = null)
        {
            var waitDialogSvc = (IVsThreadedWaitDialogFactory)Package.GetGlobalService(typeof(SVsThreadedWaitDialogFactory));
            var initialProgress = new ThreadedWaitDialogProgressData(waitMessage, progressText, statusText);
            return waitDialogSvc.StartWaitDialog("CodeWeaver", initialProgress);
        }

        static protected void Report(Session s, string progressText, string statusText = null)
        {
            var value = new ThreadedWaitDialogProgressData(null, progressText, statusText);
            s.Progress.Report(value);
        }

        public static void WeaveOrUnWeaveFromEditor(Func<DocumentWeaver, SyntaxNode, SyntaxTree> weaveOrunwaveFun)
        {
            //TODO: write to output pane the result
            switch(InternalWeaveOrUnWeaveFromEditor(weaveOrunwaveFun))
            {
                case WeaveEditorResult.ActiveDocumentGotFocusFailed:
                    break;
                case WeaveEditorResult.AllReadyWeaveOrUnweave:
                    break;
                case WeaveEditorResult.NothingToWeave:
                    break;
                case WeaveEditorResult.Ok:
                    //
                    break;
            }
        }

        public static bool CaretIsInMethod()
        {
            var caretContent = GetCaretTrivia();
            if (caretContent == null || caretContent.invalidDocument)
            {
                Trace.WriteLine("no trivia at caret position");
                return false;
            }
            SyntaxNode mOrc = caretContent.GetMethodDeclarationFromTrivia();
            if (mOrc!=null)
            {
                return true;
            }
            Trace.WriteLine("no method at caret position");
            return false;
        }

        public static bool CaretIsInClass()
        {
            var caretContent = GetCaretTrivia();
            if (caretContent == null || caretContent.invalidDocument)
            {
                Trace.WriteLine("no trivia at caret position");
                return false;
            }
            SyntaxNode mOrc = caretContent.GetClassDeclarationFromTrivia();
            if (mOrc != null)
            {
                return true;
            }
            Trace.WriteLine("no class at caret position");
            return false;
        }
    }
}
