using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.CodeAnalysis.Text;

namespace HotCommands
{
    /// <summary>
    /// Command handler for MoveMemberDown
    /// </summary>
    internal sealed class MoveMemberDown
    {
        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="MoveMemberDown"/> class.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private MoveMemberDown(Package package)
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
        public static MoveMemberDown Instance
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
            Instance = new MoveMemberDown(package);
        }

        public int HandleCommand(IWpfTextView textView)
        {
            //Get the Syntax Root 
            var syntaxRoot = textView.TextSnapshot.GetOpenDocumentInCurrentContextWithChanges().GetSyntaxRootAsync().Result;

            //Find the Current Declaration Member from caret Position
            var currMember = syntaxRoot.FindMemberDeclarationAt(textView.Caret.Position.BufferPosition.Position);
            if (currMember == null || currMember.Parent == null) return VSConstants.S_OK;

            //Find the Next Declaration Member from caret Position
            var nextMember = syntaxRoot.FindMemberDeclarationAt(currMember.FullSpan.End + 1);

            //If the current or previous member belongs to same Parent Member, then Swap the members
            if (currMember.Parent.Equals(nextMember.Parent))
            {
                textView.SwapMembers(currMember, nextMember);
            }

            return VSConstants.S_OK;
        }       
    }
}
