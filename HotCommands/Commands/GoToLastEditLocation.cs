using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Globalization;
using System.Reflection;
using Microsoft.VisualStudio.Platform.WindowManagement.Navigation;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

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

        private void AlertCommandName()
        {
            string title = "GoToLastEditLocation";
            string message = string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", this.GetType().FullName);
            VsShellUtilities.ShowMessageBox(this.ServiceProvider, message, title, OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        private void NavigateToLastEditPosition()
        {
            PropertyInfo instanceInfo = typeof(BackForwardNavigationService).GetProperty("Instance", BindingFlags.NonPublic | BindingFlags.Static);
            BackForwardNavigationService backForwardNavigationService = (BackForwardNavigationService)instanceInfo.GetValue(null);

            // Get the Items property from the instance of BFNavService
            ReadOnlyObservableCollection<NavigationItem> items = backForwardNavigationService.Items;

            // Find the last edit
            int navIndexOfLastEditLocation = GetNavIndexOfLastEdit(items);
            // If any edit found, navigate to it. Otherwise, no-op.
            if (navIndexOfLastEditLocation >= 0)
            {
                backForwardNavigationService.NavigateTo(navIndexOfLastEditLocation);
            }
        }

        /// <summary>
        /// Returns the index in the Back-Forward Navigation Items of the last edit, or -1 if no edits found.
        /// </summary>
        private static int GetNavIndexOfLastEdit(ReadOnlyObservableCollection<NavigationItem> items)
        {
            // Iterate backward through the navItems looking for one with CaretType property of DestructiveCaretMove = 0x0002;
            for (int navIndex = items.Count-1; navIndex >= 0; navIndex--)
            {
                NavigationItem navItem = items[navIndex];
                object navigationContext = navItem.NavigationContext;
                Type navContextType = navigationContext.GetType();
                if (navContextType.Name.Contains("GoBackMarker"))
                {
                    object goBackMarker = navigationContext;        // renaming just for fun
                    // Get the CaretType property via reflection
                    PropertyInfo caretMoveTypeInfo = navContextType.GetProperty("CaretMoveType");
                    object caretMoveType = caretMoveTypeInfo.GetValue(goBackMarker, null);
                    //caretMoveType.ToString(); eg. "NonDestructiveCaretMove, ArbitraryLocation"
                    string caretMoveTypes = caretMoveType.ToString();
                    if (caretMoveTypes.Contains("DestructiveCaretMove") 
                        && !caretMoveTypes.Contains("NonDestructiveCaretMove"))  // Hack: Make sure it's not found because of this flag.
                    {
                        // Found an edit location.
                        return navIndex;
                    }
                }
            }

            // No edits found. Return -1
            return -1;
        }

    }
}
