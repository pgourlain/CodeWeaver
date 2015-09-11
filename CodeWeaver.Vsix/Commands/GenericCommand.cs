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
    public delegate bool UpdateVisibleAndEnabledFunc<T, T1>(out T arg, out T1 arg1);
    internal abstract class SimpleCommand
    {
        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("e52f0deb-8e33-4714-9d18-cd65f35c84fd");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private Package package;
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
        protected abstract void GetCmdId(out int commandId);

        
        class MyMenuCommand : MenuCommand
        {
            UpdateVisibleAndEnabledFunc<bool, bool> _updateCmdFun;
            public MyMenuCommand(EventHandler handler, CommandID id, UpdateVisibleAndEnabledFunc<bool, bool> updateCmdFun) 
                : base (handler, id)
            {
                _updateCmdFun = updateCmdFun;
            }

            public override bool Enabled
            {
                get
                {
                    Trace.WriteLine("MyMenuCommand.Enabled");
                    return base.Enabled;
                }

                set
                {
                    base.Enabled = value;
                }
            }

            public override int OleStatus
            {
                get
                {
                    UpdateVisibleAndEnabled();
                    return base.OleStatus;
                }
            }

            protected override void OnCommandChanged(EventArgs e)
            {
                //base.OnCommandChanged(e);
            }

            private void UpdateVisibleAndEnabled()
            {
                if (_updateCmdFun != null)
                {
                    bool visible, enabled;
                    if ( _updateCmdFun(out visible, out enabled))
                    {
                        this.Enabled = enabled;
                        //seems that not working....
                        this.Visible = enabled;
                    }
                }
            }

        }

        internal void Initialize(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            this.package = package;

            var commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                int cmdId;
                GetCmdId(out cmdId);
                var menuCommandID = new CommandID(CommandSet, cmdId);
                var menuItem = new MyMenuCommand(this.OnMenuClick, menuCommandID, this.UpdateVisibleAndEnabled);
                commandService.AddCommand(menuItem);
            }
        }

        protected virtual bool UpdateVisibleAndEnabled(out bool visible, out bool enabled)
        {
            visible = false;
            enabled = false;
            return false;
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
