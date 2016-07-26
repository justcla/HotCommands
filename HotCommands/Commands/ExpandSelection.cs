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
            //Get the Syntax Root 
            var syntaxRoot = textView.TextSnapshot.GetOpenDocumentInCurrentContextWithChanges().GetSyntaxRootAsync().Result;
            var startPosition = textView.Selection.Start.Position;
            var endPosition = textView.Selection.End.Position;
            var length = endPosition - startPosition;

            var caretLocation = new TextSpan(startPosition, length);
            var node = syntaxRoot.FindNode(caretLocation);

            if (node.SpanStart < startPosition || node.Span.End < endPosition)
            {
                // node itself not fully selected, select it first
                SetSelection(textView, node);
            }
            else
            {
                SetSelection(textView, node.Parent);
            }

            return VSConstants.S_OK;
        }

        private static void SetSelection(IWpfTextView textView, SyntaxNode node)
        {
            if (node == null)
            {
                return;
            }

            var snapshot = textView.TextSnapshot;

            textView.Selection.Select(new SnapshotSpan(snapshot, node.SpanStart, node.Span.Length), false);
            textView.Caret.MoveTo(new SnapshotPoint(snapshot, node.Span.End));
        }
    }
}
