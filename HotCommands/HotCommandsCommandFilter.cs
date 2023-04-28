using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text.Editor;
using OLEConstants = Microsoft.VisualStudio.OLE.Interop.Constants;
using System;
using HotCommands.Commands;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Operations;

namespace HotCommands
{
    internal sealed class HotCommandsCommandFilter : IOleCommandTarget
    {
        private readonly IWpfTextView textView;
        private readonly IClassifier classifier;
        private readonly SVsServiceProvider globalServiceProvider;
        private IEditorOperations editorOperations;
        private readonly ITextBufferUndoManagerProvider undoManagerProvider;
        private IVsStatusbar statusBarService;
        internal IVsStatusbar StatusBarService => this.statusBarService
            ?? (this.statusBarService = globalServiceProvider.GetService(typeof(SVsStatusbar)) as IVsStatusbar);

        public HotCommandsCommandFilter(IWpfTextView textView, IClassifierAggregatorService aggregatorFactory,
            SVsServiceProvider globalServiceProvider, IEditorOperationsFactoryService editorOperationsFactory,
            ITextBufferUndoManagerProvider undoProvider)
        {
            this.textView = textView;
            classifier = aggregatorFactory.GetClassifier(textView.TextBuffer);
            this.globalServiceProvider = globalServiceProvider;
            editorOperations = editorOperationsFactory.GetEditorOperations(textView);
            this.undoManagerProvider = undoProvider;
        }

        public IOleCommandTarget Next { get; internal set; }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            // Command handling
            if (pguidCmdGroup == Constants.HotCommandsGuid)
            {
                // Due to Async initialization, some Instances might be uninitialized and will return null.
                // Safely catch the NullReferenceException and report a message to the status bar
                try
                {
                    // Dispatch to the correct command handler
                    switch (nCmdID)
                    {
                        case Constants.ToggleCommentCmdId:
                            return ToggleComment.Instance.HandleCommand(textView, classifier, GetShellCommandDispatcher(), editorOperations);
                        case Constants.ExpandSelectionCmdId:
                            return ExpandSelection.Instance.HandleCommand(textView, true);
                        case Constants.ShrinkSelectionCmdId:
                            return ExpandSelection.Instance.HandleCommand(textView, false);
                        case Constants.FormatCodeCmdId:
                            return FormatCode.Instance.HandleCommand(textView, GetShellCommandDispatcher());
                        case Constants.DuplicateSelectionCmdId:
                            return DuplicateSelection.HandleCommand(textView, classifier, GetShellCommandDispatcher(), editorOperations, undoManagerProvider, isDuplicateLines: false, isReversed: false);
                        case Constants.DuplicateSelectionReverseCmdId:
                            return DuplicateSelection.HandleCommand(textView, classifier, GetShellCommandDispatcher(), editorOperations, undoManagerProvider, isDuplicateLines: false, isReversed: true);
                        case Constants.DuplicateLinesDownCmdId:
                            return DuplicateSelection.HandleCommand(textView, classifier, GetShellCommandDispatcher(), editorOperations, undoManagerProvider, isDuplicateLines: true, isReversed: false);
                        case Constants.DuplicateLinesUpCmdId:
                            return DuplicateSelection.HandleCommand(textView, classifier, GetShellCommandDispatcher(), editorOperations, undoManagerProvider, isDuplicateLines: true, isReversed: true);
                        case Constants.JoinLinesCmdId:
                            return JoinLines.HandleCommand(textView, classifier, GetShellCommandDispatcher(), editorOperations);
                        case Constants.MoveMemberUpCmdId:
                            return MoveMemberUp.Instance.HandleCommand(textView,GetShellCommandDispatcher(), editorOperations);
                        case Constants.MoveMemberDownCmdId:
                            return MoveMemberDown.Instance.HandleCommand(textView,GetShellCommandDispatcher(), editorOperations);
                        case Constants.GoToPreviousMemberCmdId:
                            return MoveCursorToAdjacentMember.MoveToPreviousMember(textView, editorOperations);
                        case Constants.GoToNextMemberCmdId:
                            return MoveCursorToAdjacentMember.MoveToNextMember(textView, editorOperations);
                    }
                    // Clear the Status Bar text
                    StatusBarService.Clear();
                }
                catch (NullReferenceException)
                {
                    // Most likely exception: System.NullReferenceException: 'Object reference not set to an instance of an object.'
                    // Resulting from the command being called before Async loading has taken place.
                    // Swallow the exception and hope it works the next time the user tries it.
                    // Print a message to the StatusBar
                    StatusBarService.SetText("HotCommands is still loading. Please try again soon.");
                }
            }

            // No commands called. Pass to next command handler.
            if (Next != null)
            {
                return Next.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            }
            return (int)OLEConstants.OLECMDERR_E_UNKNOWNGROUP;
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            // Command handling registration
            if (pguidCmdGroup == Constants.HotCommandsGuid && cCmds == 1)
            {
                switch (prgCmds[0].cmdID)
                {
                    case Constants.ToggleCommentCmdId:
                    case Constants.ExpandSelectionCmdId:
                    case Constants.FormatCodeCmdId:
                    case Constants.DuplicateLinesUpCmdId:
                    case Constants.DuplicateLinesDownCmdId:
                    case Constants.DuplicateSelectionCmdId:
                    case Constants.DuplicateSelectionReverseCmdId:
                    case Constants.MoveMemberUpCmdId:
                    case Constants.MoveMemberDownCmdId:
                    case Constants.GoToPreviousMemberCmdId:
                    case Constants.GoToNextMemberCmdId:
                        prgCmds[0].cmdf |= (uint)(OLECMDF.OLECMDF_ENABLED | OLECMDF.OLECMDF_SUPPORTED);
                        return VSConstants.S_OK;
                }
            }

            if (Next != null)
            {
                return Next.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
            }
            return (int)OLEConstants.OLECMDERR_E_UNKNOWNGROUP;
        }

        /// <summary>
        /// Get the SUIHostCommandDispatcher from the global service provider.
        /// </summary>
        private IOleCommandTarget GetShellCommandDispatcher()
        {
            return globalServiceProvider.GetService(typeof(SUIHostCommandDispatcher)) as IOleCommandTarget;
        }
    }
}
