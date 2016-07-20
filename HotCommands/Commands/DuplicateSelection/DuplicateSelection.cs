using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
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

            if (isSingleLine)
            {
                editorOperations.CopySelection();
                editorOperations.MoveToNextCharacter(false);
                editorOperations.Paste();
                editorOperations.MoveToPreviousCharacter(false);
            }
            else
            {
                ITextSelection selection = textView.Selection;
                var virtualBufferPosition = editorOperations.TextView.Caret.Position.VirtualBufferPosition;
                bool isReversed = selection.IsReversed;
                string text = selectedText;
                ITextSnapshot textSnapshot = textView.TextSnapshot;
                ITrackingPoint trackingPoint = textSnapshot.CreateTrackingPoint(virtualBufferPosition.Position, PointTrackingMode.Negative);
                List<ITrackingSpan> list = new List<ITrackingSpan>();
                foreach (SnapshotSpan snapshotSpan in selection.SelectedSpans)
                    list.Add(textSnapshot.CreateTrackingSpan(snapshotSpan, SpanTrackingMode.EdgeExclusive));
                if (!selection.IsEmpty)
                    selection.Clear();
                int offset = 0;
                if (virtualBufferPosition.IsInVirtualSpace)
                {
                    offset = editorOperations.GetWhitespaceForVirtualSpace(virtualBufferPosition).Length;
                }
                textView.Caret.MoveTo(virtualBufferPosition.TranslateTo(textView.TextSnapshot));
                editorOperations.InsertText(text);
                SnapshotPoint insertionPoint = trackingPoint.GetPoint(textView.TextSnapshot);
                if (offset != 0)
                {
                    insertionPoint = insertionPoint.Add(offset);
                }
                VirtualSnapshotPoint virtualSnapshotPoint1 = new VirtualSnapshotPoint(insertionPoint);
                VirtualSnapshotPoint virtualSnapshotPoint2 = new VirtualSnapshotPoint(insertionPoint.Add(text.Length));
                if (isReversed)
                    editorOperations.SelectAndMoveCaret(virtualSnapshotPoint2, virtualSnapshotPoint1, TextSelectionMode.Stream);
                else
                    editorOperations.SelectAndMoveCaret(virtualSnapshotPoint1, virtualSnapshotPoint2, TextSelectionMode.Stream);
            }

            return VSConstants.S_OK;
        }
    }
}