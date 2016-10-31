﻿//------------------------------------------------------------------------------
// <copyright file="FormatCode.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text.Editor;
using OleInterop = Microsoft.VisualStudio.OLE.Interop;

namespace HotCommands
{
    /// <summary>
    /// Command handler for FormatCode
    /// </summary>
    internal sealed class FormatCode : Command<FormatCode>
    {
        public int HandleCommand(IWpfTextView textView, OleInterop.IOleCommandTarget commandTarget)
        {
            Guid cmdGroup = VSConstants.VSStd2K;

            // Execute FormatSelection or FormatDocument depending on current state of selected code
            uint cmdID = IsCursorOnly(textView) ? (uint)VSConstants.VSStd2KCmdID.FORMATDOCUMENT : (uint)VSConstants.VSStd2KCmdID.FORMATSELECTION;
            commandTarget.Exec(ref cmdGroup, cmdID, (uint)OleInterop.OLECMDEXECOPT.OLECMDEXECOPT_DODEFAULT, IntPtr.Zero, IntPtr.Zero);

            return VSConstants.S_OK;
        }

        private bool IsCursorOnly(IWpfTextView textView)
        {
            if (textView.Selection.SelectedSpans.Count > 1) return false;
            // Only one selection. Check if there is any selected content.
            return textView.Selection.SelectedSpans[0].Length == 0;
        }
    }
}