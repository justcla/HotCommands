using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;

namespace HotCommands
{
    /// <summary>
    /// Command handler for MoveMemberDown
    /// </summary>
    internal sealed class MoveMemberDown
    {
        private readonly Package package;

        private MoveMemberDown(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this.package = package;
        }

        public static MoveMemberDown Instance
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

        public static void Initialize(Package package)
        {
            Instance = new MoveMemberDown(package);
        }

        public int HandleCommand(IWpfTextView textView,IEditorOperations editorOperations)
        {
            return textView.MoveMemberDown(editorOperations);
        }

    }
}
