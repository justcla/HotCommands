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

        public static void Initialize(Package package)
        {
            Instance = new DuplicateSelection(package);
        }

        private DuplicateSelection(Package package)
        {
            if (package == null)
                throw new ArgumentNullException(nameof(package));
            _package = package;
        }

        public int HandleCommand(IWpfTextView textView, IClassifier classifier, IOleCommandTarget commandTarget, IEditorOperations editorOperations)
        {
            Guid cmdGroup = VSConstants.VSStd2K;
            bool isSingleLine = false;
            var selectedText = editorOperations.SelectedText;
            if (selectedText.Length == 0)
            // if nothing is selected, we can consider the current line as a selection
            {
                editorOperations.SelectLine(textView.Caret.ContainingTextViewLine, false);
                isSingleLine = true;
            }
            var selStart = editorOperations.TextView.Caret.ContainingTextViewLine;
            var selEnd = editorOperations.TextView.Selection.End;
            editorOperations.CopySelection();
            // editorOperations.MoveToStartOfLine(false);
            editorOperations.MoveToNextCharacter(false);
            //var startPosition = textView.Caret.Position.VirtualBufferPosition;

            editorOperations.Paste(); 

            editorOperations.SelectAndMoveCaret(textView.Caret.Position.VirtualBufferPosition, textView.Caret.Position.VirtualBufferPosition);
            editorOperations.MoveToPreviousCharacter(true);
            editorOperations.MoveCaret(editorOperations.TextView.Caret.ContainingTextViewLine, 0, true);



            if (isSingleLine)
                editorOperations.MoveToEndOfLine(false);

            editorOperations.ResetSelection();
            return VSConstants.S_OK;
        }
    }
}