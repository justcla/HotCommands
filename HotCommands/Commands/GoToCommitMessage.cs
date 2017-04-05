using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using OleInterop = Microsoft.VisualStudio.OLE.Interop;

namespace HotCommands
{
    /// <summary>
    /// Command handler for JoinLines
    /// </summary>
    internal sealed class GoToCommitMessage : Command<GoToCommitMessage>
    {
        public int HandleCommand(IWpfTextView textView, IClassifier classifier, OleInterop.IOleCommandTarget commandTarget, IEditorOperations editorOperations)
        {
            // Open Git Change Window
            DTE dte = (DTE)ServiceProvider.GetService(typeof(DTE));
            dte.ExecuteCommand("Team.Git.GoToGitChanges");

            //TODO: Put focus in Commit Message input box

            return VSConstants.S_OK;
        }

    }
}
