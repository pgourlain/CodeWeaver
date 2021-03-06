﻿//------------------------------------------------------------------------------
// <copyright file="RunCodeWeaverOnProjectPackage.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;

namespace CodeWeaver.Vsix
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [ProvideAutoLoad(Microsoft.VisualStudio.Shell.Interop.UIContextGuids.NoSolution)]
    [ProvideAutoLoad(Microsoft.VisualStudio.Shell.Interop.UIContextGuids.SolutionExists)]
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(CodeWeaverPackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class CodeWeaverPackage : Package, IOleCommandTarget
    {
        /// <summary>
        /// RunCodeWeaverOnProjectPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "429942cc-05af-4e2c-8593-79b0efec166c";

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            GenericCommand<WeaveProjectCommand>.Initialize(this);
            GenericCommand<UnWeaveProjectCommand>.Initialize(this);
            GenericCommand<WeaveEditorMethodCommand>.Initialize(this);
            GenericCommand<UnWeaveEditorMethodCommand>.Initialize(this);
            GenericCommand<WeaveEditorClassCommand>.Initialize(this);
            GenericCommand<UnWeaveEditorClassCommand>.Initialize(this);
            base.Initialize();
        }

        int IOleCommandTarget.Exec(ref Guid guidGroup, uint nCmdId, uint nCmdExcept, IntPtr pIn, IntPtr vOut)
        {
            IOleCommandTarget oleCommandTarget = (IOleCommandTarget)this.GetService(typeof(IOleCommandTarget));
            if (oleCommandTarget != null)
            {
                return oleCommandTarget.Exec(ref guidGroup, nCmdId, nCmdExcept, pIn, vOut);
            }
            return -2147221248;
        }
        int IOleCommandTarget.QueryStatus(ref Guid guidGroup, uint nCmdId, OLECMD[] nCmdExcept, IntPtr pCmdText)
        {
            IOleCommandTarget oleCommandTarget = (IOleCommandTarget)this.GetService(typeof(IOleCommandTarget));
            if (oleCommandTarget != null)
            {
                return oleCommandTarget.QueryStatus(ref guidGroup, nCmdId, nCmdExcept, pCmdText);
            }
            return -2147221248;
        }


        #endregion
    }
}
