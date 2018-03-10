using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.ComponentModel.Design;

namespace HotCommands
{
    /// <summary>
    /// Command handler for GoToLastEditLocation
    /// </summary>
    internal sealed class GoToLastEditLocation
    {
        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="GoToLastEditLocation"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private GoToLastEditLocation(Package package)
        {
            this.package = package ?? throw new ArgumentNullException("package");

            if (this.ServiceProvider.GetService(typeof(IMenuCommandService)) is OleMenuCommandService globalCommandService)
            {
                var menuCommandID = new CommandID(Constants.HotCommandsGuid, (int)Constants.GoToLastEditLocationCmdId);
                var menuItem = new MenuCommand(this.MenuItemCallback, menuCommandID);
                globalCommandService.AddCommand(menuItem);
            }
        }

        public static GoToLastEditLocation Instance
        {
            get;
            private set;
        }

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
            Instance = new GoToLastEditLocation(package);
        }

        private void MenuItemCallback(object sender, EventArgs e)
        {
            NavigateToLastEditPosition();
        }

        private void NavigateToLastEditPosition()
        {
            System.Diagnostics.Debug.WriteLine("Navigate to last edit position.");

            // Open the file last editted
            string lastEditFilePath = LastEdit.LastEditFile;
            IWpfTextView textView = OpenLastEdittedFile(lastEditFilePath);
            if (textView == null)
            {
                // Unable to open the file. Do No-Op.
                System.Diagnostics.Debug.WriteLine($"Unable to open last editted file: {lastEditFilePath}");
                return;
            }

            // Navigate to the last edit caret position
            int lastEditPosn = LastEdit.LastEditPosn;
            SetCaretAtGivenPosition(textView, lastEditPosn);
        }

        private IWpfTextView OpenLastEdittedFile(string lastEditFilePath)
        {
            try
            {
                VsShellUtilities.OpenDocument(this.ServiceProvider, lastEditFilePath, VSConstants.LOGVIEWID_TextView,
                                      out IVsUIHierarchy hierarchy, out uint itemId, out IVsWindowFrame windowFrame,
                                      out IVsTextView vsTextView);
                return GetEditorAdaptorsFactoryService().GetWpfTextView(vsTextView);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Exception occurred trying to navigate set open file last editted.", e);
                return null;
            }
        }

        private static void SetCaretAtGivenPosition(IWpfTextView textView, int lastEditPosn)
        {
            try
            {
                // TODO: Check if position exists in file.
                textView.Caret.MoveTo(new SnapshotPoint(textView.TextSnapshot, lastEditPosn));
                textView.ViewScroller.EnsureSpanVisible(new SnapshotSpan(textView.TextSnapshot, lastEditPosn, 0), EnsureSpanVisibleOptions.None);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Exception occurred trying to set caret poistion to last edit location.", e);
            }
        }

        private static IVsEditorAdaptersFactoryService GetEditorAdaptorsFactoryService()
        {
            IComponentModel componentService = (IComponentModel)(Package.GetGlobalService(typeof(SComponentModel)));
            IVsEditorAdaptersFactoryService editorAdaptersFactoryService = componentService.GetService<IVsEditorAdaptersFactoryService>();
            return editorAdaptersFactoryService;
        }

    }
}
