using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.CodeAnalysis.Text;

namespace HotCommands
{
    /// <summary>
    /// Command handler for ToggleComment
    /// </summary>
    internal sealed class MoveMemberUp
    {
        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="MoveMethodUp"/> class.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private MoveMemberUp(Package package)
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
        public static MoveMemberUp Instance
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
            Instance = new MoveMemberUp(package);
        }

        public int HandleCommand(IWpfTextView textView)
        {
            //Get the Syntax Root 
            var syntaxRoot = textView.TextSnapshot.GetOpenDocumentInCurrentContextWithChanges().GetSyntaxRootAsync().Result;

            //Find the Current Declaration Member from caret Position
            var currMember = syntaxRoot.FindMemberDeclarationAt(textView.Caret.Position.BufferPosition.Position);
            if (currMember == null || currMember.Parent == null) return VSConstants.S_OK;

            //Find the Previous Declaration Member from caret Position
            var prevMember = syntaxRoot.FindMemberDeclarationAt(currMember.FullSpan.Start - 1);
            
            //If current and previous declaration member belongs to same Parent, then Swap the members
            if (currMember.Parent.Equals(prevMember?.Parent))
            {
                textView.SwapMembers(currMember, prevMember);
            }

            return VSConstants.S_OK;
        }        
    }
}
