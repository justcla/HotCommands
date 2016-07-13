using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text.Editor;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

            MemberDeclarationSyntax member = null;
            MemberDeclarationSyntax nextMember = null;
            var cursorPosition = textView.Caret.ContainingTextViewLine.Extent.Span.Start;
            var members = CSharpSyntaxTree.ParseText(textView.TextSnapshot.GetText()).GetRoot().DescendantNodes().OfType<MemberDeclarationSyntax>();

            for (int i = 0; i <members.Count(); i++)
            {                
                member = members.ElementAt(i);
                if (cursorPosition >= member.FullSpan.Start && cursorPosition <= member.FullSpan.End && IsValidMember(member))
                {
                    nextMember = IsValidMember(members.ElementAtOrDefault(i + 1)) ? members.ElementAtOrDefault(i + 1) : null;                 
                    break;
                }
            }

            if (member == null || nextMember == null) return 0;
            var edit = textView.TextSnapshot.TextBuffer.CreateEdit();
            edit.Delete(nextMember.FullSpan.Start, nextMember.FullSpan.Length);
            edit.Insert(member.FullSpan.Start, nextMember.GetText().ToString());
            edit.Apply();
               
            return VSConstants.S_OK;
        }
        
        private bool IsValidMember(MemberDeclarationSyntax member)
        {
            if (member == null) return false;
            return member.IsKind(SyntaxKind.MethodDeclaration) || member.IsKind(SyntaxKind.PropertyDeclaration) || member.IsKind(SyntaxKind.FieldDeclaration);            
        }
    }
}
