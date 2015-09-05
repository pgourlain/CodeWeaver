using Microsoft.VisualStudio.Shell;
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
            var dte = (EnvDTE.DTE) Package.GetGlobalService(typeof(EnvDTE.DTE));
            if (dte.Solution != null)
                return dte.Solution.FullName;
            return null;
        }
    }
}
