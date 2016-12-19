using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using OleInterop = Microsoft.VisualStudio.OLE.Interop;

namespace HotCommands
{
    /// <summary>
    /// Command handler for JoinLines
    /// </summary>
    internal sealed class JoinLines
    {
        public static int HandleCommand(IWpfTextView textView, IClassifier classifier, OleInterop.IOleCommandTarget commandTarget, IEditorOperations editorOperations)
        {
            // TODO: Handle UNDO management. This should occur as a single undo-able transaction in the Undo history
            editorOperations.MoveToEndOfLine(false);
            editorOperations.Delete();
            editorOperations.DeleteHorizontalWhiteSpace();

            return VSConstants.S_OK;
        }

    }
}
