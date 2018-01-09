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
    /// Command handler
    /// </summary>
    internal sealed class GoToLastEditLocation
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("2ce65dd2-2135-4a0b-a90a-468501fdfdc0");

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

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
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
                    //string caretMoveTypeEnumName = caretMoveType.GetType().GetEnumName(caretMoveType);
                }
            }

            // No edits found. Return -1
            return -1;
        }

        private static void PrintFields(Type bfNavServiceType)
        {
            FieldInfo[] fieldInfos = bfNavServiceType.GetFields();
            foreach (FieldInfo fieldInfo in fieldInfos)
            {
                string name = fieldInfo.Name;
                System.Diagnostics.Debug.WriteLine($"Field: {name}");
            }
        }
        private static void PrintProperties(Type objectType, BindingFlags bindingFlags)
        {
            var infos = objectType.GetProperties(bindingFlags);
            foreach (var info in infos)
            {
                string name = info.Name;
                System.Diagnostics.Debug.WriteLine($"Property: {name}");
            }
        }
    }
}
