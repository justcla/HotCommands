//------------------------------------------------------------------------------
// <copyright file="ExpandSelection.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Globalization;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.CodeAnalysis.Text;
using System.Linq;
using System.Collections;
using System.Windows;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace HotCommands
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class ExpandSelection
    {
        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpandSelection"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private ExpandSelection(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this.package = package;
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static ExpandSelection Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new ExpandSelection(package);
        }
        public int HandleCommand(IWpfTextView textView, bool expand)
        {
            if (expand)
            {
                return HandleCommandExpandTask(textView).Result;
            } 
            else
            {
                return HandleCommandShrinkTask(textView).Result;
            }
        }

        private async Task<int> HandleCommandShrinkTask(IWpfTextView textView)
        {
            var syntaxRoot = await textView.TextSnapshot.GetOpenDocumentInCurrentContextWithChanges().GetSyntaxRootAsync();

            var caretPos = textView.Caret.Position.BufferPosition;

            var startPosition = textView.Selection.Start.Position;
            var endPosition = textView.Selection.End.Position;
            var length = endPosition.Position - startPosition.Position;
            var selectionSpan = new TextSpan(startPosition.Position, length);

            List<TextSpan> spans = new List<TextSpan>();
            var trivia = syntaxRoot.FindTrivia(caretPos);
            var token = syntaxRoot.FindToken(caretPos);
            var node = syntaxRoot.FindNode(new TextSpan(caretPos.Position, 0));
            TextSpan currSelect = GetSyntaxSpan(trivia, token, node, new TextSpan(caretPos, 0));
            while(!IsOverlap(currSelect, startPosition.Position, endPosition.Position))
            {
                spans.Add(currSelect);
                trivia = syntaxRoot.FindTrivia(currSelect.Start);
                token = syntaxRoot.FindToken(currSelect.Start);
                node = syntaxRoot.FindNode(currSelect);
                currSelect = GetSyntaxSpan(trivia, token, node, currSelect);
            }

            if (spans.Count > 0)
            {
                TextSpan finalSpan =  spans.Skip(Math.Max(0, spans.Count - 2)).First();
                SetSelection(textView, finalSpan);
            }

            return VSConstants.S_OK;
        }

        private async Task<int> HandleCommandExpandTask(IWpfTextView textView)
        {
            var syntaxRoot = await textView.TextSnapshot.GetOpenDocumentInCurrentContextWithChanges().GetSyntaxRootAsync();
            var startPosition = textView.Selection.Start.Position;
            var endPosition = textView.Selection.End.Position;
            var length = endPosition.Position - startPosition.Position;
            var selectionSpan = new TextSpan(startPosition.Position, length);

            var trivia = syntaxRoot.FindTrivia(startPosition.Position);
            var token = syntaxRoot.FindToken(startPosition.Position);
            var node = syntaxRoot.FindNode(selectionSpan);

            TextSpan finalSpan = GetSyntaxSpan(trivia, token, node, selectionSpan);

            SetSelection(textView, finalSpan);
            return VSConstants.S_OK;
        }

        private static TextSpan GetSyntaxSpan(SyntaxTrivia trivia, SyntaxToken token, SyntaxNode node, TextSpan selection)
        {
            TextSpan finalSpan;
            int start = selection.Start;
            int end = selection.End;
            if (trivia.RawKind != 0) // in trivia
            {
                if (IsOverlap(trivia.Span, start, end) && (trivia.RawKind == 8542 || trivia.RawKind == 8541)) // in comment so grab comment
                {
                    finalSpan = trivia.Span;
                }
                else // not a comment or already selecting comment so get selection of next open/close bracket
                {
                    TextSpan innerBracketSpan = GetInnerBracketSpan(node);
                    while (innerBracketSpan.Equals(new TextSpan(0, 0)))
                    {
                        node = node.Parent;
                        innerBracketSpan = GetInnerBracketSpan(node);
                    }
                    // check that we are not selecting same area as current selection
                    finalSpan = (innerBracketSpan.Equals(selection)) ? node.Span : innerBracketSpan;
                }
            }
            else if (IsOverlap(token.Span, start, end)) // in token.
            {
                finalSpan = token.Span;
            }
            else // in node
            {
                node = (IsOverlap(node, start, end)) ? node : node.Parent;
                TextSpan innerBracketSpan = GetInnerBracketSpan(node);
                if (IsOverlap(innerBracketSpan, start, end))
                {
                    finalSpan = innerBracketSpan;
                }
                else
                {
                    finalSpan = node.Span;
                }
            }
            return finalSpan;
        }


        private static TextSpan GetInnerBracketSpan(SyntaxNode node)
        {
            var children = node.ChildNodesAndTokens();
            // node itself not fully selected, select it first
            var firstBracket = children.FirstOrDefault(x => x.RawKind == 8205);
            var lastBracket = children.LastOrDefault(x => x.RawKind == 8206);
            if (firstBracket.RawKind != 0 || lastBracket.RawKind != 0)
            {
                // We found an open and close brackets. Check and see if we need to only select the insides of the brackets
                int start = firstBracket.Span.End;
                int end;
                SyntaxTrivia lastEOL = lastBracket.GetLeadingTrivia().LastOrDefault(x => x.RawKind == 8539);
                if (lastEOL.RawKind == 8539)
                {
                    end = lastEOL.Span.Start;
                } else
                {
                    var nodeOrToken = lastBracket.GetPreviousSibling();
                    if(nodeOrToken.IsNode)
                    {
                        end = nodeOrToken.AsNode().Span.End;
                    }
                    else if (nodeOrToken.IsToken)
                    {
                        end = nodeOrToken.Span.End;
                    } else
                    {
                        return new TextSpan(0, 0);
                    }
                }
                return new TextSpan(start, end - start);
            }
            return new TextSpan(0, 0);
        }

        private static void SetSelection(IWpfTextView textView, TextSpan span)
        {
            if (span == null)
            {
                return;
            }

            var snapshot = textView.TextSnapshot;

            textView.Selection.Select(new SnapshotSpan(snapshot, span.Start, span.Length), false);
            //textView.Caret.MoveTo(new SnapshotPoint(snapshot, node.Span.End));
        }

        private static bool IsOverlap(TextSpan span, int startPosition, int endPosition)
        {
            return span.Start < startPosition && span.End > endPosition || 
                   (span.Start == startPosition && span.End > endPosition) ||
                   (span.Start < startPosition && span.End == endPosition);
        }

        private static bool IsOverlap(SyntaxNode node, int startPosition, int endPosition)
        {
            return node.SpanStart < startPosition && node.Span.End > endPosition || 
                   (node.SpanStart == startPosition && node.Span.End > endPosition) ||
                   (node.SpanStart < startPosition && node.Span.End == endPosition);
        }
    }
}
