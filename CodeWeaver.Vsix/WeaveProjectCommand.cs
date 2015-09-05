//------------------------------------------------------------------------------
// <copyright file="RunCodeWeaverOnProject.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using System.Globalization;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System.Linq;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using CodeWeaver.Vsix.Processor;

namespace CodeWeaver.Vsix
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class WeaveProjectCommand : SimpleCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        protected override void GetCmdId(out int commandId)
        {
            commandId = CommandId;
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        protected override void OnMenuClick(object sender, EventArgs e)
        {
            //string message = string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", this.GetType().FullName);
            //string title = "RunCodeWeaverOnProject";

            //http://www.codeproject.com/Articles/861548/Roslyn-Code-Analysis-in-Easy-Samples-Part
            //http://www.codeproject.com/Articles/857480/Roslyn-based-Simulated-Multiple-Inheritance-Usag
            //https://msdn.microsoft.com/en-us/magazine/dn904676.aspx
            //doc un peu pauvre...
            //http://www.coderesx.com/roslyn/html/22ACFE1E.htm

            WeaverHelper.WeaveOrUnWeave("Weaving", x => x.Weave());
        }
    }
}
