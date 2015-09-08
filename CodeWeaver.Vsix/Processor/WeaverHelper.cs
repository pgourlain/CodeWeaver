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
        #region private methods

        private static WeaveEditorResult InternalWeaveOrUnWeaveFromEditor(Func<DocumentWeaver, SyntaxNode, SyntaxTree> weaveOrunwaveFun)
        {
            Document doc;
            IWpfTextView wpfView;
            var trivia = GetCaretTrivia(out doc, out wpfView);
            if (trivia == null) return WeaveEditorResult.ActiveDocumentGotFocusFailed;

            var syntaxTree = doc.GetSyntaxTreeAsync().Result;
            SyntaxNode mOrc = trivia.Value.GetMethodDeclarationFromTrivia();
            if (mOrc == null) mOrc = trivia.Value.GetClassDeclarationFromTrivia();
            if (mOrc == null) return WeaveEditorResult.NothingToWeave;
            var weaver = new DocumentWeaver(syntaxTree);
            var newSyntaxTree = weaveOrunwaveFun(weaver, mOrc);
            if (newSyntaxTree == null) return WeaveEditorResult.AllReadyWeaveOrUnweave;
            var root = newSyntaxTree.GetRoot();
            var newDoc = Formatter.FormatAsync(doc.WithSyntaxRoot(root)).Result;
            wpfView.TextBuffer.GetWorkspace().TryApplyChanges(newDoc.Project.Solution);

            return WeaveEditorResult.Ok;
        }

        private static SyntaxTrivia? GetCaretTrivia(out Document doc, out IWpfTextView wpfView)
        {
            doc = null;
            var dte = (EnvDTE.DTE)Package.GetGlobalService(typeof(EnvDTE.DTE));
            wpfView = VSTools.GetWpfTextView(dte, dte.ActiveDocument);
            if (wpfView == null)
            {
                return null;
            }
            CaretPosition position = wpfView.Caret.Position;

            var point = position.BufferPosition;
            //extensions in 'Microsoft.CodeAnalysis.EditorFeatures.Text'
            doc = point.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (doc == null) return null;
            var root = doc.GetSyntaxRootAsync().Result;
            if (root == null) return null;
            var token = root.FindTrivia(point.Position);
            return token;
        }

        private static SyntaxTrivia? GetCaretTrivia()
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
            var trivia = GetCaretTrivia();
            if (trivia == null) return false;
            SyntaxNode mOrc = trivia.Value.GetMethodDeclarationFromTrivia();
            return mOrc != null;
        }

        public static bool CaretIsInClass()
        {
            var trivia = GetCaretTrivia();
            if (trivia == null) return false;
            SyntaxNode mOrc = trivia.Value.GetClassDeclarationFromTrivia();
            return mOrc != null;
        }
    }
}
