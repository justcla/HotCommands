using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using IServiceProvider = System.IServiceProvider;

namespace HotCommands
{
    internal sealed class SurroundWithBracket
    {
        private readonly Package package;

        public static SurroundWithBracket Instance { get; private set; }

        private IServiceProvider ServiceProvider => package;

        public static void Initialize(Package package)
        {
            Instance = new SurroundWithBracket(package);
        }

        private SurroundWithBracket(Package package)
        {
            if (package == null)
                throw new ArgumentNullException(nameof(package));
            this.package = package;
        }

        public int HandleCommand(IWpfTextView textView, IClassifier classifier, IOleCommandTarget commandTarget, IEditorOperations editorOperations)
        {
            Guid cmdGroup = VSConstants.VSStd2K;
            int start_position = SelectedPosition_Start(textView);
            commandTarget.Exec(ref cmdGroup, (uint)VSConstants.VSStd2KCmdID.CUT, (uint)OLECMDEXECOPT.OLECMDEXECOPT_PROMPTUSER, IntPtr.Zero, IntPtr.Zero);   // TODO: need show the list of constructs (if, if-else, for, while, do-while etc..)
            InsertSpan(textView, start_position, "if (condition) {");  // TODO: change this based on user selection

            commandTarget.Exec(ref cmdGroup, (uint)VSConstants.VSStd2KCmdID.RETURN, (uint)OLECMDEXECOPT.OLECMDEXECOPT_DONTPROMPTUSER, IntPtr.Zero, IntPtr.Zero);
            commandTarget.Exec(ref cmdGroup, (uint)VSConstants.VSStd2KCmdID.PASTE, (uint)OLECMDEXECOPT.OLECMDEXECOPT_DONTPROMPTUSER, IntPtr.Zero, IntPtr.Zero);
            commandTarget.Exec(ref cmdGroup, (uint)VSConstants.VSStd2KCmdID.RETURN, (uint)OLECMDEXECOPT.OLECMDEXECOPT_DONTPROMPTUSER, IntPtr.Zero, IntPtr.Zero);
            
            int end_position = SelectedPosition_End(textView);
            InsertSpan(textView, end_position, "}");

            editorOperations.Tabify();

            return VSConstants.S_OK;
        }

        private void InsertSpan(IWpfTextView textView, int position, string text)
        {
            using (ITextEdit edit = textView.TextBuffer.CreateEdit())
            {
                edit.Insert(position, text);
                edit.Apply();
            }
        }


        private int SelectedPosition_Start(IWpfTextView textView)
        {
            foreach (SnapshotSpan snapshotSpan in textView.Selection.SelectedSpans)
            {
                SnapshotSpan spanToCheck = snapshotSpan.Length == 0 ? new SnapshotSpan(textView.TextSnapshot, textView.Caret.ContainingTextViewLine.Extent.Span) : snapshotSpan;
                return spanToCheck.Start.Position;
            }
            return 0;
        }

        private int SelectedPosition_End(IWpfTextView textView)
        {
            int pos = default(int);
            foreach (SnapshotSpan snapshotSpan in textView.Selection.SelectedSpans)
            {
                SnapshotSpan spanToCheck = snapshotSpan.Length == 0 ? new SnapshotSpan(textView.TextSnapshot, textView.Caret.ContainingTextViewLine.Extent.Span) : snapshotSpan;
                pos = spanToCheck.End.Position;
            }
            return pos;
        }
    }
}
