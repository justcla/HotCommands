//------------------------------------------------------------------------------
// <copyright file="FormatCode.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.Operations;
using OleInterop = Microsoft.VisualStudio.OLE.Interop;

namespace HotCommands
{
    /// <summary>
    /// Command handler for FormatCode
    /// </summary>
    internal sealed class GoToLastEditLocation : Command<GoToLastEditLocation>
    {
        IWpfTextView lastView;
        int lastLocation;

        public int HandleCommand(IWpfTextView textView, OleInterop.IOleCommandTarget commandTarget)
        {
            Guid cmdGroup = VSConstants.VSStd2K;
            
            return VSConstants.S_OK;
        }

        private void TextBuffer_Changed(object sender, TextContentChangedEventArgs e)
        {
            lastLocation = e.Changes.GetEnumerator().Current.NewSpan.Start;   
        }

        public void newFile(IWpfTextView textView)
        {
            textView.TextBuffer.Changed += TextBuffer_Changed;
            textView.MouseHover += TextView_MouseHover;
        }

        private void TextView_MouseHover(object sender, MouseHoverEventArgs e)
        {
            this.lastView = (IWpfTextView) e.View;
        }

        private bool IsCursorOnly(IWpfTextView textView)
        {
            if (textView.Selection.SelectedSpans.Count > 1) return false;
            // Only one selection. Check if there is any selected content.
            return textView.Selection.SelectedSpans[0].Length == 0;
        }
    }
}