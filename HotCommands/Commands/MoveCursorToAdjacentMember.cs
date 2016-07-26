using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.CodeAnalysis;

namespace HotCommands
{
    class MoveCursorToAdjacentMember : Command<MoveCursorToAdjacentMember>
    {
        public static int MoveToNextMember(IWpfTextView textView)
        {
            return MoveToAdjacentMember(textView, up: false);
        }
        public static int MoveToPreviousMember(IWpfTextView textView)
        {
            return MoveToAdjacentMember(textView, up: true);
        }
        
        // Moves the cursor to either the previous or next member declaration, or does nothing if it is at the top/bottom already
        internal static int MoveToAdjacentMember(IWpfTextView textView, bool up)
        {
            // TODO there are some odd cases that crop up. For example, it counts a class declaration which is only in a namespace (not an inner class) and also seems to count the namespace.
            // It also doesn't behave well when above or below all of them. (if above, it should go to the first, and vice versa)
            // If it is at the last and you tell it to go next, it should not move. (and vice versa)
            // These do not work at the moment
            // Additionally, It does not always go to what I feel should be the beginning of a declaration. It goes to the beginning of the [attribbutes], not the access modifier.


            // Get the Syntax Root
            var syntaxRoot = textView.TextSnapshot.GetOpenDocumentInCurrentContextWithChanges().GetSyntaxRootAsync().Result;

            // Find the Current Declaration Member from caret Position
            var currMember = syntaxRoot.FindMemberDeclarationAt(textView.Caret.Position.BufferPosition.Position);
            if (currMember == null || currMember.Parent == null)
            {
                return Microsoft.VisualStudio.VSConstants.S_OK;
            }

            SyntaxNode adjacentMember;
            if (up)
            {
                adjacentMember = syntaxRoot.FindMemberDeclarationAt(currMember.FullSpan.Start - 1);
            }
            else
            {
                adjacentMember = syntaxRoot.FindMemberDeclarationAt(currMember.FullSpan.End + 1);
            }
            if (adjacentMember != null)
            {
                // move the cursor to the previous member
                textView.Caret.MoveTo(new SnapshotPoint(textView.TextSnapshot, adjacentMember.SpanStart));
            }

            return Microsoft.VisualStudio.VSConstants.S_OK;
        }
    }
}
