//------------------------------------------------------------------------------
// <copyright file="ToggleComment.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Language.StandardClassification;

namespace HotCommands
{
    /// <summary>
    /// Command handler for ToggleComment
    /// </summary>
    internal sealed class ToggleComment
    {
        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="ToggleComment"/> class.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private ToggleComment(Package package)
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
        public static ToggleComment Instance
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
                Instance = new ToggleComment(package);
        }

        public int HandleCommand(IWpfTextView textView, IClassifier classifier)
        {
            // Show a message box to prove we were here
            string message = string.Format(CultureInfo.CurrentCulture, "Inside {0}.HandleCommand()", GetType().FullName);
            string title = "ToggleComment";
            VsShellUtilities.ShowMessageBox(
                this.ServiceProvider,
                message,
                title,
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

            // Decide if all lines in the spam are currently commented
            bool allCommented = IsAllCommented(textView, classifier);

            return VSConstants.S_OK;
        }

        private Boolean IsAllCommented(IWpfTextView textView, IClassifier classifier)
        {
            ITextCaret caret = textView.Caret;
            ITextViewLine line = caret.ContainingTextViewLine;
            SnapshotSpan snapshotSpan = new SnapshotSpan(textView.TextSnapshot, line.Extent.Span);
            IList<ClassificationSpan> classificationSpans = classifier.GetClassificationSpans(snapshotSpan);
            foreach (var classification in classificationSpans)
            {
                var name = classification.ClassificationType.Classification.ToLower();
                if (!name.Contains(PredefinedClassificationTypeNames.Comment))        // "comment"
                {
                    return false;
                }
            }

            return true;
        }

    }
}
