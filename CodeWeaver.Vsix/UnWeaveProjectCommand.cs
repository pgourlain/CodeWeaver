using CodeWeaver.Vsix.Processor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CodeWeaver.Vsix
{
    internal sealed class UnWeaveProjectCommand : SimpleCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0101;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("e52f0deb-8e33-4714-9d18-cd65f35c84fd");

        protected override void GetCmdSetAndCmdId(out Guid commandSet, out int commandId)
        {
            commandSet = CommandSet;
            commandId = CommandId;
        }

        protected override void OnMenuClick(object sender, EventArgs e)
        {
            WeaverHelper.WeaveOrUnWeave("UnWeaving", x => x.UnWeave());
        }
    }
}
