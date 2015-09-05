using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeWeaver.Vsix
{
    class VSTools
    {
        public static IEnumerable<string> SelectedProjectsFileName()
        {
            var explorer = ((EnvDTE80.DTE2)Package.GetGlobalService(typeof(EnvDTE.DTE))).ToolWindows.SolutionExplorer;
            var items = (object[])explorer.SelectedItems;
            List<string> l = new List<string>();
            foreach (EnvDTE.UIHierarchyItem item in items)
            {
                EnvDTE.Project project = (EnvDTE.Project)item.Object;
                //EnvDTE.Configuration config = project.ConfigurationManager.ActiveConfiguration;
                //string projectPath = Path.GetDirectoryName(project.FileName);
                //string outputPath = config.Properties.Item("OutputPath").Value.ToString();
                //string assemblyFileName = project.Properties.Item("OutputFileName").Value.ToString();
                l.Add(project.FileName);
            }
            return l;
        }

        internal static string SelectedSolution()
        {
            var dte = (EnvDTE.DTE)Package.GetGlobalService(typeof(EnvDTE.DTE));
            if (dte.Solution != null)
                return dte.Solution.FullName;
            return null;
        }

        public static IVsTextView GetTextView(EnvDTE.DTE dte, EnvDTE.Document document)
        {
            using (ServiceProvider sp = new ServiceProvider((Microsoft.VisualStudio.OLE.Interop.IServiceProvider)dte))
            {

                IVsUIHierarchy uiHierarchy;
                uint itemID;
                IVsWindowFrame windowFrame;

                VsShellUtilities.IsDocumentOpen(sp, document.FullName,
                                                Guid.Empty, out uiHierarchy,
                                                out itemID, out windowFrame);

                IVsTextView textView = VsShellUtilities.GetTextView(windowFrame);
                return textView;
            }
        }

        public static IWpfTextView GetWpfTextView(EnvDTE.DTE dte, IVsTextView viewAdapter)
        {
            using (ServiceProvider sp = new ServiceProvider((Microsoft.VisualStudio.OLE.Interop.IServiceProvider)dte))
            {
                var svc = (IVsEditorAdaptersFactoryService)sp.GetService(typeof(IVsEditorAdaptersFactoryService));
                return svc.GetWpfTextView(viewAdapter);
            }
        }
        public static IWpfTextView GetWpfTextView(EnvDTE.DTE dte, EnvDTE.Document document)
        {
            using (ServiceProvider sp = new ServiceProvider((Microsoft.VisualStudio.OLE.Interop.IServiceProvider)dte))
            {

                IVsUIHierarchy uiHierarchy;
                uint itemID;
                IVsWindowFrame windowFrame;

                VsShellUtilities.IsDocumentOpen(sp, document.FullName,
                                                Guid.Empty, out uiHierarchy,
                                                out itemID, out windowFrame);

                IVsTextView textView = VsShellUtilities.GetTextView(windowFrame);
                var componentModelService = (IComponentModel2)Package.GetGlobalService(typeof(SComponentModel));

                var svc = componentModelService.GetService<IVsEditorAdaptersFactoryService>();
                //var svc = (IVsEditorAdaptersFactoryService)sp.GetService(typeof(IVsEditorAdaptersFactoryService));
                return svc.GetWpfTextView(textView);
            }
        }
    }
}
