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
            Func<int> _getEnabled;
            public MyMenuCommand(EventHandler handler, CommandID id, Func<int> getEnabled) 
                : base (handler, id)
            {
                _getEnabled = getEnabled;
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
                    if (_getEnabled != null)
                    {
                        var result = _getEnabled();
                        if ((result >= 0))
                            this.Enabled = (result > 0);
                    }
                    return base.OleStatus;
                }
            }

            public override bool Supported
            {
                get
                {
                    Trace.WriteLine("MyMenuCommand.Supported");
                    return base.Supported;
                }

                set
                {
                    base.Supported = value;
                }
            }

            public override bool Visible
            {
                get
                {
                    Trace.WriteLine("MyMenuCommand.Visible");
                    return base.Visible;
                }

                set
                {
                    base.Visible = value;
                }
            }
            protected override void OnCommandChanged(EventArgs e)
            {
                //base.OnCommandChanged(e);
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
                var menuItem = new MyMenuCommand(this.OnMenuClick, menuCommandID, this.GetIsEnabled);
                commandService.AddCommand(menuItem);
            }
        }

        protected virtual int GetIsEnabled()
        {
            return -1;
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
