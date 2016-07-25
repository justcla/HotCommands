using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.CodeAnalysis;

namespace HotCommands.Commands
{
    class MoveCursorToPreviousMember
    {

        public int MoveCursorToMember(IWpfTextView textView, bool up)
        {
            // Get the Syntax Root
            var syntaxRoot = textView.TextSnapshot.GetOpenDocumentInCurrentContextWithChanges().GetSyntaxRootAsync().Result;

            // Find the Current Declaration Member from caret Position
            var currMember = syntaxRoot.FindMemberDeclarationAt(textView.Caret.Position.BufferPosition.Position);
            if (currMember == null || currMember.Parent == null)
            {
                return VSConstants.S_OK;
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
                textView.Caret.MoveTo(new SnapshotPoint(textView.TextSnapshot, adjacentMember.FullSpan.Start));
            }

            return VSConstants.S_OK;
        }
    }
}
