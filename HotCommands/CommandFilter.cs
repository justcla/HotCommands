using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text.Editor;
using OLEConstants = Microsoft.VisualStudio.OLE.Interop.Constants;
using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace HotCommands
{
    internal sealed class CommandFilter : IOleCommandTarget
    {
        private readonly IWpfTextView textView;
        private readonly IClassifier classifier;
        private readonly SVsServiceProvider globalServiceProvider;

        public CommandFilter(IWpfTextView textView, IClassifierAggregatorService aggregatorFactory, SVsServiceProvider globalServiceProvider)
        {
            this.textView = textView;
            classifier = aggregatorFactory.GetClassifier(textView.TextBuffer);
            this.globalServiceProvider = globalServiceProvider;
        }

        public IOleCommandTarget Next { get; internal set; }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            // Command handling
            if (pguidCmdGroup == Constants.HotCommandsGuid)
            {
                // Dispatch to the correct command handler
                switch (nCmdID)
                {
                    case Constants.ToggleCommentCmdId:
                        return ToggleComment.Instance.HandleCommand(textView, classifier, GetShellCommandDispatcher());
                    case Constants.ExpandSelectionCmdId:
                        return ExpandSelection.Instance.HandleCommand(textView);
                }
            }

            // No commands called. Pass to next command handler.
            if (this.Next != null)
            {
                return this.Next.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
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
                        prgCmds[0].cmdf |= (uint)OLECMDF.OLECMDF_ENABLED;
                        return VSConstants.S_OK;
                }
            }

            if (this.Next != null)
            {
                return this.Next.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
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
