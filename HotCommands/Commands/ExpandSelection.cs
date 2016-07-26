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
        public int HandleCommand(IWpfTextView textView)
        {
            return HandleCommandExpandTask(textView).Result;
        }

        private async Task<int> HandleCommandExpandTask(IWpfTextView textView)
        {
            //Get the Syntax Root 
            var syntaxRoot = await textView.TextSnapshot.GetOpenDocumentInCurrentContextWithChanges().GetSyntaxRootAsync();
            var startPosition = textView.Selection.Start.Position;
            var endPosition = textView.Selection.End.Position;
            var length = endPosition - startPosition;
            var selectionSpan = new TextSpan(startPosition.Position, length);
            TextSpan finalSpan;

            var trivia = syntaxRoot.FindTrivia(startPosition.Position);
            var token = syntaxRoot.FindToken(startPosition.Position);
            var node = syntaxRoot.FindNode(selectionSpan);

            if (trivia.RawKind == 8542 || trivia.RawKind == 8541) // in trivia
            {
                if (IsOverlap(trivia.Span, startPosition.Position, endPosition.Position))
                {
                    finalSpan = trivia.Span;
                }
                else
                {
                    var bracketSpan = GetInnerBracketSpan(trivia.Token.Parent);
                    if (IsOverlap(bracketSpan, startPosition.Position, endPosition.Position))
                    {
                        finalSpan = bracketSpan;
                    }
                    else
                    {
                        finalSpan = trivia.Token.Parent.Span;
                    }
                }
            }
            else if (IsOverlap(token.Span, startPosition.Position, endPosition.Position) && trivia.RawKind == 0) // in token. If we found a valid trivia, we dont want to parse the token next to cursor
            {
                finalSpan = token.Span;
            }
            else // in node
            {
                node = (IsOverlap(node, startPosition.Position, endPosition.Position)) ? node : node.Parent;
                var innerBracketSpan = GetInnerBracketSpan(node);
                if (IsOverlap(innerBracketSpan, startPosition.Position, endPosition.Position))
                {
                    finalSpan = innerBracketSpan;
                } else
                {
                    finalSpan = node.Span;
                }
            }

            SetSelection(textView, finalSpan);
            return VSConstants.S_OK;
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
