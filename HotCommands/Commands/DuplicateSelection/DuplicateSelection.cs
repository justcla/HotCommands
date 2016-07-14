using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using IServiceProvider = System.IServiceProvider;

namespace HotCommands.Commands
{
    internal sealed class DuplicateSelection
    {
        private readonly Package _package;

        public static DuplicateSelection Instance { get; private set; }

        private IServiceProvider ServiceProvider => _package;

        public static void Initialize (Package package)
        {
            Instance = new DuplicateSelection(package);
        }

        private DuplicateSelection (Package package)
        {
            if (package == null)
                throw new ArgumentNullException(nameof(package));
            _package = package;
        }

        public int HandleCommand (IWpfTextView textView, IClassifier classifier, IOleCommandTarget commandTarget, IEditorOperations editorOperations)
        {
            Guid cmdGroup = VSConstants.VSStd2K;
            bool isSingleLine = false;

            if (editorOperations.SelectedText.Length == 0)
            // if nothing is selected, we can consider the current line as a selection
            {
                editorOperations.SelectLine(textView.Caret.ContainingTextViewLine, false);
                isSingleLine = true;
            }

            editorOperations.CopySelection();
            editorOperations.MoveToStartOfLine(false);
            editorOperations.Paste();

            if (isSingleLine)
                editorOperations.MoveToEndOfLine(false);

            editorOperations.ResetSelection();
            return VSConstants.S_OK;
        }
    }
}