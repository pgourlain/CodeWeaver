using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.VisualStudio.Shell.ThreadedWaitDialogHelper;

namespace CodeWeaver.Vsix.Processor
{
    class WeaverHelper
    {
        static protected IEnumerable<Project> FilterProjects(Solution originalSolution)
        {
            var selectedProjects = VSTools.SelectedProjectsFileName().ToArray();
            foreach (var item in originalSolution.Projects.Where(x => x.Language == "C#" && selectedProjects.Contains(x.FilePath)))
            {
                yield return item;
            }
        }

        public static void WeaveOrUnWeave(string rootText, Func<DocumentWeaver, Document> weaveOrunwaveFun)
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
                            var weaver = new DocumentWeaver(document);
                            var newDoc = weaveOrunwaveFun(weaver);
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
    }
}
