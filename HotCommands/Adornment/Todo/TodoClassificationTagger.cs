using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;

namespace HotCommands.Adornment
{
    public sealed class TodoClassificationTagger : ITagger<ClassificationTag>
    {
        private readonly ITextView m_View;
        private readonly ITextSearchService m_SearchService;
        private readonly IClassificationType m_Type;
        private NormalizedSnapshotSpanCollection m_CurrentSpans;

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged = delegate { };

        public TodoClassificationTagger (ITextView view, ITextSearchService searchService, IClassificationType type)
        {
            m_View = view;
            m_SearchService = searchService;
            m_Type = type;

            m_CurrentSpans = GetWordSpans(m_View.TextSnapshot);

            m_View.GotAggregateFocus += SetupSelectionChangedListener;
        }

        private void SetupSelectionChangedListener (object sender, EventArgs e)
        {
            if (m_View != null)
            {
                m_View.LayoutChanged += ViewLayoutChanged;
                m_View.GotAggregateFocus -= SetupSelectionChangedListener;
            }
        }

        private void ViewLayoutChanged (object sender, TextViewLayoutChangedEventArgs e)
        {
            if (e.OldSnapshot != e.NewSnapshot)
            {
                m_CurrentSpans = GetWordSpans(e.NewSnapshot);
                TagsChanged(this, new SnapshotSpanEventArgs(new SnapshotSpan(e.NewSnapshot, 0, e.NewSnapshot.Length)));
            }
        }

        private NormalizedSnapshotSpanCollection GetWordSpans (ITextSnapshot snapshot)
        {
            var wordSpans = new List<SnapshotSpan>();
            wordSpans.AddRange(FindAll(@"TODO", snapshot).Select(regionLine => regionLine.Start.GetContainingLine().Extent));
            return new NormalizedSnapshotSpanCollection(wordSpans);
        }

        private IEnumerable<SnapshotSpan> FindAll (string searchPattern, ITextSnapshot textSnapshot)
        {
            if (textSnapshot == null)
                return null;

            return m_SearchService.FindAll(
                new FindData(searchPattern, textSnapshot)
                {
                    FindOptions = FindOptions.WholeWord | FindOptions.Multiline | FindOptions.Wrap
                });
        }

        public IEnumerable<ITagSpan<ClassificationTag>> GetTags (NormalizedSnapshotSpanCollection spans)
        {
            if (spans == null || spans.Count == 0 || m_CurrentSpans.Count == 0)
                yield break;

            ITextSnapshot snapshot = m_CurrentSpans[0].Snapshot;
            spans = new NormalizedSnapshotSpanCollection(spans.Select(s => s.TranslateTo(snapshot, SpanTrackingMode.EdgeExclusive)));

            foreach (var span in NormalizedSnapshotSpanCollection.Intersection(m_CurrentSpans, spans))
            {
                int index = span.GetText().ToLower().IndexOf("todo", StringComparison.Ordinal);
                SnapshotPoint point = span.Start.Add(index);

                bool comment = EnsureComment(point.Position);
                if (!comment) continue;

                SnapshotSpan newSpan = new SnapshotSpan(span.Snapshot, new Span(point.Position, span.Length));
                yield return new TagSpan<ClassificationTag>(newSpan, new ClassificationTag(m_Type));
            }
        }

        private bool EnsureComment (int position)
        {
            Document document = m_View.TextSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            SyntaxNode rootNode = document.GetSyntaxRootAsync().Result;

            var commentTrivia = from t in rootNode.DescendantTrivia()
                                where t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia)
                                select t;
            SyntaxTrivia[] comments = commentTrivia as SyntaxTrivia[] ?? commentTrivia.ToArray();

            if (!comments.Any()) return false;

            foreach (var c in comments)
            {
                if (position >= c.Span.Start && position <= c.Span.End)
                    return true;
            }

            return false;
        }
    }
}