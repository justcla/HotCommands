﻿using System;
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

        public static int HandleCommand_DuplicateLines(IWpfTextView textView, IClassifier classifier,
            IOleCommandTarget commandTarget, IEditorOperations editorOperations,
            ITextBufferUndoManagerProvider undoManagerProvider, bool isCopyUp)
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
            ITextUndoTransaction transaction = undoManager.TextBufferUndoHistory.CreateTransaction("Duplicate Lines");

            // This bool is to handle an annoying edge case where the selection would be expanded
            // by the inserted text because the insertion happens at the end of the selection.
            bool isEdgeCase_InsertExpandsSelection = false;

            IMultiSelectionBroker broker = textView.GetMultiSelectionBroker();
            IReadOnlyList<Selection> selections = broker.AllSelections;
            Selection primarySelection = broker.PrimarySelection;
            // Hack: Work from the last selection upward, to avoid changing buffer positions with mutli-caret
            for (int i = selections.Count - 1; i >= 0; i--)
            {
                Selection selection = selections[i];

                SnapshotSpan selectionSpan = selection.Extent.SnapshotSpan;
                SnapshotSpan selectedLines = GetContainingLines(selectionSpan);
                string textToInsert = selectedLines.GetText();

                bool isEndOfFile = selectedLines.End == textView.TextSnapshot.Length;

                SnapshotPoint insertPos;
                if (isCopyUp)
                {
                    insertPos = selectedLines.End;

                    isEdgeCase_InsertExpandsSelection |= selectionSpan.End == insertPos;

                    // Case when there is no trailing new-line chars in the selected lines.
                    if (isEndOfFile) textToInsert = Environment.NewLine + textToInsert;
                }
                else // (isCopyDown)
                {
                    insertPos = selectedLines.Start;

                    // Case when there is no trailing new-line chars in the selected lines.
                    if (isEndOfFile) textToInsert = textToInsert + Environment.NewLine;
                }

                textView.TextBuffer.Insert(insertPos, textToInsert);
            }

            if (isEdgeCase_InsertExpandsSelection)
            {
                // Translate selections to newest snapshot with negative tracking mode for the
                // end point so that they are not expanded due to the recent insertions.
                var targetSnapshot = textView.TextSnapshot;
                var newSelections = new Selection[selections.Count];
                int primarySelectionIndex = 0;
                for (int i = 0; i < newSelections.Length; i++)
                {
                    Selection selection = selections[i];
                    if (primarySelection.Equals(selection)) primarySelectionIndex = i;
                    newSelections[i] = TranslateTo(
                        selection, targetSnapshot,
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

                broker.SetSelectionRange(newSelections, newSelections[primarySelectionIndex]);
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

        // Helped by source of Microsoft.VisualStudio.Text.Editor.DragDrop.DropHandlerBase.cs in assembly Microsoft.VisualStudio.Text.UI.Wpf, Version=14.0.0.0
        public static int HandleCommand(IWpfTextView textView, IClassifier classifier, IOleCommandTarget commandTarget, IEditorOperations editorOperations, bool shiftPressed = false)
        {
            //Guid cmdGroup = VSConstants.VSStd2K;
            string selectedText = editorOperations.SelectedText;
            ITrackingPoint trackingPoint = null;
            if (selectedText.Length == 0)
            {
                // if nothing is selected, we can consider the current line as a selection
                var virtualBufferPosition = editorOperations.TextView.Caret.Position.VirtualBufferPosition;
                trackingPoint = textView.TextSnapshot.CreateTrackingPoint(virtualBufferPosition.Position, PointTrackingMode.Negative);

                // Select all the text on the current line. Leaves caret at the start of the next line or end of line if last line.
                editorOperations.SelectLine(textView.Caret.ContainingTextViewLine, false);
                var text = editorOperations.SelectedText;
                // Clear the selection so new inserts will not overwrite the selected line. Caret stays at start of next line.
                editorOperations.ResetSelection();

                // Hack for Last Line: If last line of file, introduce a new line character then delete it after duplicating the line.
                var endOfFile = !EndsWithNewLine(text);
                if (endOfFile)
                {
                    // We are on the last line. Will need to insert a new line. Will be removed later.
                    editorOperations.InsertNewLine();
                }

                // Now we are at the beginning of the line we can insert the duplicate text.
                editorOperations.InsertText(text);

                // Clean up any newline character introduced by earlier hack
                if (endOfFile)
                {
                    editorOperations.Delete();
                }

                // Return the cursor to its original position, then move it down one line (unless doing reverse)
                textView.Caret.MoveTo(new VirtualSnapshotPoint(trackingPoint.GetPoint(textView.TextSnapshot)).TranslateTo(textView.TextSnapshot));
                if (!shiftPressed) editorOperations.MoveLineDown(false);
            }
            else
            {
                var selection = textView.Selection;
                var isReversed = selection.IsReversed;
                var text = selectedText;
                var textSnapshot = textView.TextSnapshot;
                var list = new List<ITrackingSpan>();
                //var shiftKeyPressed=textVie
                foreach (SnapshotSpan snapshotSpan in selection.SelectedSpans)
                {
                    list.Add(textSnapshot.CreateTrackingSpan(snapshotSpan, SpanTrackingMode.EdgeExclusive));
                }
                if (!selection.IsEmpty)
                {
                    selection.Clear();
                }


                // Case where there is just one selection:
                if (list.Count < 2)
                {
                    var offset = 0;
                    var virtualBufferPosition = editorOperations.TextView.Caret.Position.VirtualBufferPosition;
                    var point = editorOperations.TextView.Caret.Position.BufferPosition;
                    virtualBufferPosition = isReversed && !shiftPressed ? new VirtualSnapshotPoint(point.Add(text.Length))
                       : !isReversed && shiftPressed ? new VirtualSnapshotPoint(point.Add(-text.Length)) : virtualBufferPosition;

                    trackingPoint = textSnapshot.CreateTrackingPoint(virtualBufferPosition.Position, PointTrackingMode.Negative);
                    if (virtualBufferPosition.IsInVirtualSpace)
                    {
                        offset = editorOperations.GetWhitespaceForVirtualSpace(virtualBufferPosition).Length;
                    }
                    textView.Caret.MoveTo(virtualBufferPosition.TranslateTo(textView.TextSnapshot));
                    editorOperations.InsertText(text);
                    var insertionPoint = trackingPoint.GetPoint(textView.TextSnapshot);
                    if (offset != 0)
                    {
                        insertionPoint = insertionPoint.Add(offset);
                    }

                    var virtualSnapshotPoint1 = new VirtualSnapshotPoint(insertionPoint);
                    var virtualSnapshotPoint2 = new VirtualSnapshotPoint(insertionPoint.Add(text.Length));
                    if (isReversed)
                    {
                        editorOperations.SelectAndMoveCaret(virtualSnapshotPoint2, virtualSnapshotPoint1, TextSelectionMode.Stream);
                    }
                    else
                    {
                        editorOperations.SelectAndMoveCaret(virtualSnapshotPoint1, virtualSnapshotPoint2, TextSelectionMode.Stream);
                    }
                }
                // Handle Multi-selections:
                else
                {
                    var trackingPointOffsetList = new List<Tuple<ITrackingPoint, int, int>>();
                    //Insert Text!
                    if (isReversed) list.Reverse();
                    foreach (var trackingSpan in list)
                    {
                        SnapshotSpan span = trackingSpan.GetSpan(textSnapshot);
                        text = trackingSpan.GetText(textSnapshot);
                        int offset = 0;
                        SnapshotPoint insertionPoint = !isReversed ? trackingSpan.GetEndPoint(span.Snapshot) : trackingSpan.GetStartPoint(span.Snapshot);
                        var virtualBufferPosition = new VirtualSnapshotPoint(insertionPoint);
                        virtualBufferPosition = isReversed && !shiftPressed ? new VirtualSnapshotPoint(insertionPoint.Add(text.Length))
                           : !isReversed && shiftPressed ? new VirtualSnapshotPoint(insertionPoint.Add(-text.Length)) : virtualBufferPosition;


                        trackingPoint = textSnapshot.CreateTrackingPoint(virtualBufferPosition.Position, PointTrackingMode.Negative);
                        if (virtualBufferPosition.IsInVirtualSpace)
                        {
                            offset = editorOperations.GetWhitespaceForVirtualSpace(virtualBufferPosition).Length;
                        }
                        trackingPointOffsetList.Add(new Tuple<ITrackingPoint, int, int>(trackingPoint, offset, text.Length));
                        textView.Caret.MoveTo(virtualBufferPosition.TranslateTo(textView.TextSnapshot));
                        editorOperations.InsertText(text);
                    }
                    //Make Selections
                    {
                        var trackingPointOffset = trackingPointOffsetList.First();
                        var insertionPoint = trackingPointOffset.Item1.GetPoint(textView.TextSnapshot);
                        if (trackingPointOffset.Item2 != 0)
                        {
                            insertionPoint = insertionPoint.Add(trackingPointOffset.Item2);
                        }
                        var virtualSnapshotPoint1 = new VirtualSnapshotPoint(insertionPoint.Add(!isReversed ? 0 : trackingPointOffset.Item3));

                        trackingPointOffset = trackingPointOffsetList.Last();
                        insertionPoint = trackingPointOffset.Item1.GetPoint(textView.TextSnapshot);
                        if (trackingPointOffset.Item2 != 0)
                        {
                            insertionPoint = insertionPoint.Add(trackingPointOffset.Item2);
                        }
                        var virtualSnapshotPoint2 = new VirtualSnapshotPoint(insertionPoint.Add(isReversed ? 0 : trackingPointOffset.Item3));
                        editorOperations.SelectAndMoveCaret(virtualSnapshotPoint1, virtualSnapshotPoint2, TextSelectionMode.Box);
                    }
                }
            }

            return VSConstants.S_OK;
        }
        private static bool EndsWithNewLine(string text)
        {
            return text.Length > 0 &&
                (text[text.Length - 1] == '\n' || text[text.Length - 1] == '\r');
        }
    }
}