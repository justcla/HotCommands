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
using Roslyn.Utilities;
using System.Collections;
using System.Windows;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Outlining;

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
            var caretLocation = new TextSpan(textView.Caret.Position.BufferPosition.Position, 0);
            var node = syntaxRoot.FindNode(caretLocation);
            var snapSpan = new SnapshotSpan(new SnapshotPoint(textView.TextSnapshot, node.Parent.Span.Start), new SnapshotPoint(textView.TextSnapshot, node.Parent.Span.Start + node.Parent.Span.Length));
            textView.Selection.Select(snapSpan, false);
            textView.Caret.MoveTo(snapSpan.Start);

            /*
            //Find the Current Declaration Member from caret Position
            var currMember = syntaxRoot.FindMemberDeclarationAt(textView.Caret.Position.BufferPosition.Position);
            
            var currItem = textView.Caret.Position.BufferPosition.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (currMember == null || currMember.Parent == null) return VSConstants.S_OK;

            //Find the Next Declaration Member from caret Position
            var nextMember = syntaxRoot.FindMemberDeclarationAt(currMember.FullSpan.End + 1);

            //If the current or previous member belongs to same Parent Member, then Swap the members
            if (currMember.Parent.Equals(nextMember?.Parent))
            {
                textView.SwapMembers(currMember, nextMember);
            }

            */
            return VSConstants.S_OK;
        }
        /*
        public void SetSelection(
            this ITextView textView, VirtualSnapshotPoint anchorPoint, VirtualSnapshotPoint activePoint)
        {
            var isReversed = activePoint < anchorPoint;
            var start = isReversed ? activePoint : anchorPoint;
            var end = isReversed ? anchorPoint : activePoint;
            SetSelection(textView, new SnapshotSpan(start.Position, end.Position), isReversed);
        }

        public void SetSelection(
            this ITextView textView, SnapshotSpan span, bool isReversed = false)
        {
            //var spanInView = textView.GetSpanInView(span).Single();

        }*/
    }
}
