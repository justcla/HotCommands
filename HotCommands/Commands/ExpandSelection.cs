//------------------------------------------------------------------------------
// <copyright file="ExpandSelection.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using System.Globalization;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;

namespace HotCommands
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class ExpandSelection
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x1022;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("1023dc3d-550c-46b8-a3ec-c6b03431642c");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        private IVsMonitorSelection MonitorSelection;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpandSelection"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private ExpandSelection(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this.package = package;

            // Initialize the IVsMonitorSelection
            MonitorSelection = (IVsMonitorSelection)ServiceProvider.GetService(typeof(IVsMonitorSelection));

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new OleMenuCommand(this.MenuItemCallback, menuCommandID);
                menuItem.BeforeQueryStatus += MenuItem_BeforeQueryStatus;
                commandService.AddCommand(menuItem);
            }
        }

        private void MenuItem_BeforeQueryStatus(object sender, EventArgs e)
        {
            ((OleMenuCommand)sender).Visible = IsContextActive(VSConstants.VsEditorFactoryGuid.TextEditor_guid);
        }

        private bool IsContextActive(Guid contextGuid)
        {
            int pfActive;
            var result = MonitorSelection.IsCmdUIContextActive(GetContextCookie(contextGuid), out pfActive);
            var ready = result == VSConstants.S_OK && pfActive > 0;
            return ready;
        }

        private uint GetContextCookie(Guid contextGuid)
        {
            uint contextCookie;
            MonitorSelection.GetCmdUIContextCookie(contextGuid, out contextCookie);
            return contextCookie;
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static ExpandSelection Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new ExpandSelection(package);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            string message = string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", this.GetType().FullName);
            string title = "ExpandSelection";

            // Show a message box to prove we were here
            VsShellUtilities.ShowMessageBox(
                this.ServiceProvider,
                message,
                title,
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
