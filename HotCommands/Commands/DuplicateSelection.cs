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

        private ITextBufferUndoManagerProvider UndoProvider;


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

        public static int HandleCommand(IWpfTextView textView, IClassifier classifier,
            IOleCommandTarget commandTarget, IEditorOperations editorOperations,
            ITextBufferUndoManagerProvider undoManagerProvider, bool isDuplicateLines = false, bool isReversed = false)
        {
            // Use cases:
            // Single or Multiple carets
            // For each caret/selection (in SelectedSpans):
            // - No-text selection
            // - Single-line selection
            // - Multi-line selection
            // - Selection that ends on the first char of the line

            // Create a single transaction so the user can Undo all operations in one go.
            ITextBufferUndoManager undoManager = undoManagerProvider.GetTextBufferUndoManager(textView.TextBuffer);
            ITextUndoTransaction transaction = undoManager.TextBufferUndoHistory.CreateTransaction("Duplicate Selection");

            // This bool is to handle an annoying edge case where the selection would be expanded
            // by the inserted text because the insertion happens at the end of the selection.
            bool isEdgeCase_InsertExpandsSelection = false;

            IMultiSelectionBroker selectionBroker = textView.GetMultiSelectionBroker();
            IReadOnlyList<Selection> selections = selectionBroker.AllSelections;
            Selection primarySelection = selectionBroker.PrimarySelection;
            ITextSnapshot afterEditSnapshot;
            using (ITextEdit edit = textView.TextBuffer.CreateEdit()) // Perform compound operation.
            {
                foreach (Selection selection in selections)
                {
                    SnapshotSpan selectionSpan = selection.Extent.SnapshotSpan;
                    bool isDuplicateLinesForThisSelection = isDuplicateLines || selectionSpan.Length == 0; // When selection length is zero we treat it as duplicate lines command (or should we not?).
                    
                    SnapshotSpan duplicationSpan;
                    bool isMissingNewLine;
                    if (isDuplicateLinesForThisSelection)
                    {
                        duplicationSpan = GetContainingLines(selectionSpan);
                        isMissingNewLine = duplicationSpan.End == edit.Snapshot.Length;
                    }
                    else
                    {
                        duplicationSpan = selectionSpan;
                        isMissingNewLine = false;
                    }

                    string textToInsert = duplicationSpan.GetText();

                    SnapshotPoint insertPos;
                    if (isReversed)
                    {
                        insertPos = duplicationSpan.End;

                        isEdgeCase_InsertExpandsSelection |= selectionSpan.End == insertPos;

                        if (isMissingNewLine) textToInsert = Environment.NewLine + textToInsert;
                    }
                    else
                    {
                        insertPos = duplicationSpan.Start;

                        if (isMissingNewLine) textToInsert = textToInsert + Environment.NewLine;
                    }

                    edit.Insert(insertPos, textToInsert);
                }

                afterEditSnapshot = edit.Apply();
            }

            if (isEdgeCase_InsertExpandsSelection)
            {
                // Translate selections to newest snapshot with negative tracking mode for the
                // end point so that they are not expanded due to the recent insertions.
                var newSelections = new Selection[selections.Count];
                int primarySelectionIndex = 0;
                for (int i = 0; i < newSelections.Length; i++)
                {
                    Selection selection = selections[i];
                    if (primarySelection.Equals(selection)) primarySelectionIndex = i;
                    newSelections[i] = TranslateTo(
                        selection, afterEditSnapshot,
                        GetPointTrackingMode(selection.InsertionPoint),
                        GetPointTrackingMode(selection.AnchorPoint),
                        GetPointTrackingMode(selection.ActivePoint)
                    );

                    PointTrackingMode GetPointTrackingMode(VirtualSnapshotPoint point)
                    {
                        if (point.Position == point.Position.Snapshot.Length) return PointTrackingMode.Negative;
                        return point <= selection.Extent.Start ? PointTrackingMode.Positive : PointTrackingMode.Negative;
                    }
                }

                selectionBroker.SetSelectionRange(newSelections, newSelections[primarySelectionIndex]);
            }

            // Complete the transaction
            transaction.Complete();

            return VSConstants.S_OK;
        }

        /// <summary>
        /// Transforms a selection to a target snapshot using the provided tracking rules.
        /// TODO: Move to utility class and make into extension?
        /// </summary>
        public static Selection TranslateTo(Selection selection, ITextSnapshot targetSnapshot, PointTrackingMode insertionPointTracking, PointTrackingMode anchorPointTracking, PointTrackingMode activePointTracking)
        {
            return new Selection
            (
                selection.InsertionPoint.TranslateTo(targetSnapshot, insertionPointTracking),
                selection.AnchorPoint.TranslateTo(targetSnapshot, anchorPointTracking),
                selection.ActivePoint.TranslateTo(targetSnapshot, activePointTracking),
                selection.InsertionPointAffinity
            );
        }

        /// <summary>
        /// Expands span to include all lines it touches.
        /// Spans ending on first char of a new line does not count as touching that line.
        /// Includes any trailing new-line chars (CR/LF).
        /// TODO: Move to utility class and make into extension?
        /// </summary>
        /// <returns> The lines that contains this span. </returns>
        public static SnapshotSpan GetContainingLines(SnapshotSpan span)
        {
            var firstLine = span.Start.GetContainingLine();
            SnapshotPoint linesStart = firstLine.Start;
            SnapshotPoint linesEnd;
            if (span.Length == 0)
            {
                linesEnd = firstLine.EndIncludingLineBreak;
            }
            else
            {
                var lastLine = span.End.GetContainingLine();
                linesEnd = span.End == lastLine.Start ?
                    lastLine.Start
                    :
                    lastLine.EndIncludingLineBreak;
            }
            return new SnapshotSpan(linesStart, linesEnd);
        }
    }
}