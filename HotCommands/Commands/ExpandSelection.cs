//------------------------------------------------------------------------------
// <copyright file="ExpandSelection.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Globalization;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text.Editor;

namespace HotCommands
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class ExpandSelection : Command<ExpandSelection>
    {
        public override int HandleCommand(IWpfTextView textView)
        {
            // Show a message box to prove we were here
            string message = string.Format(CultureInfo.CurrentCulture, "Inside {0}.HandleCommand()", this.GetType().FullName);
            string title = "ExpandSelection";
            VsShellUtilities.ShowMessageBox(
                this.ServiceProvider,
                message,
                title,
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

            // TODO: Implement Expand Selection logic

            return VSConstants.S_OK;
        }
    }
}
