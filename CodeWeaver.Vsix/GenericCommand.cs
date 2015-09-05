using CodeWeaver.Vsix.Processor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.VisualStudio.Shell.ThreadedWaitDialogHelper;

namespace CodeWeaver.Vsix
{
    internal abstract class SimpleCommand
    {
        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        protected Package package;
        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        protected IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        protected abstract void GetCmdSetAndCmdId(out Guid commandSet, out int commandId);

        internal void Initialize(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this.package = package;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                Guid cmdSet;
                int cmdId;
                GetCmdSetAndCmdId(out cmdSet, out cmdId);
                var menuCommandID = new CommandID(cmdSet, cmdId);
                var menuItem = new MenuCommand(this.OnMenuClick, menuCommandID);
                commandService.AddCommand(menuItem);
            }
        }

        protected virtual void OnMenuClick(object sender, EventArgs e)
        {
        }

        protected void MessageBox(string message)
        {
            VsShellUtilities.ShowMessageBox(
                this.ServiceProvider,
                message,
                "CodeWeaver",
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
    internal class GenericCommand<TCommand> where TCommand : SimpleCommand, new()
    {
        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        public static TCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new TCommand();
            Instance.Initialize(package);
        }

    }
}
