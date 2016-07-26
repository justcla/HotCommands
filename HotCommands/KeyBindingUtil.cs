using System;
using Microsoft.VisualStudio.Shell;
using EnvDTE;

namespace HotCommands
{
    class KeyBindingUtil
    {
        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private static Package package;

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package thePackage)
        {
            if (thePackage == null)
            {
                throw new ArgumentNullException("thePackage");
            }

            package = thePackage;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return package;
            }
        }

        /// <summary>
        /// Note: Calling this is redundant if the original KeyBinding works in the vsct file.
        /// However, sometimes there is another command bound to the desired keybinding.
        /// In those cases, explicitly defining the key binding here is usually more effective.
        /// </summary>
        internal void UpdateKeyBindings()
        {
            // Make sure we're not using the Default keyboard mapping scheme
            DTE dte = (DTE)ServiceProvider.GetService(typeof(DTE));
            Properties props = dte.Properties["Environment", "Keyboard"];
            Property prop = props.Item("SchemeName");
            prop.Value = "MyBindings.vsk";

            EnvDTE.Commands cmds = dte.Commands;

            // Add a binding for ExpandSelection(TextEditor)
            {
                Command cmdToggleComment = cmds.Item("Edit.ToggleComment");
                const string toggleCommentKeyBinding = "Text Editor::Ctrl+/";
                cmdToggleComment.Bindings = (object)AppendKeyboardBinding(cmdToggleComment, toggleCommentKeyBinding);  // Note: This overrides any key bindings already assigned to this command
            }

            // Add a binding for ExpandSelection(TextEditor)
            {
                Command cmdExpandSelection = cmds.Item("1023dc3d-550c-46b8-a3ec-c6b03431642c", 0x1022); // Edit.ExpandSelection
                const string expandSelectionKeyBinding = "Text Editor::Ctrl+W";
                object[] newBindings = SingleKeyboardBinding(expandSelectionKeyBinding);
                cmdExpandSelection.Bindings = (object)newBindings;
            }

            {
                Command cmdToggleComment = cmds.Item("Edit.DuplicateSelection");
                const string toggleCommentKeyBinding = "Text Editor::Ctrl+D";
                cmdToggleComment.Bindings = AppendKeyboardBinding(cmdToggleComment, toggleCommentKeyBinding);

                cmdToggleComment = cmds.Item("Edit.DuplicateAndSelectOriginal");
                const string toggleCommentKeyReverseBinding = "Text Editor::Ctrl+Shift+D";
                cmdToggleComment.Bindings = AppendKeyboardBinding(cmdToggleComment, toggleCommentKeyReverseBinding);
            }
        }

        private static object[] SingleKeyboardBinding(string keyboardBindingDefn)
        {
            return new object[] { keyboardBindingDefn };
        }

        private static object[] AppendKeyboardBinding(Command command, string keyboardBindingDefn)
        {
            object[] oldBindings = (object[])command.Bindings;

            // Check that keyboard binding is not already there
            for (int i = 0; i < oldBindings.Length; i++)
            {
                if (keyboardBindingDefn.Equals(oldBindings[i]))
                {
                    // Exit early and return the existing bindings array if new keyboard binding is already there
                    return oldBindings;
                }
            }

            // Build new array with all the old bindings, plus the new one.
            object[] newBindings = new object[oldBindings.Length + 1];
            Array.Copy(oldBindings, newBindings, oldBindings.Length);
            newBindings[newBindings.Length - 1] = keyboardBindingDefn;
            return newBindings;
        }

    }
}
