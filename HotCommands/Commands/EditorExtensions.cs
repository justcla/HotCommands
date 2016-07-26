using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text.Editor;
using System.Linq;

namespace HotCommands
{
    static class EditorExtensions
    {
        internal static int MoveCurrentMemberUp(this IWpfTextView textView)
        {
            //Get the Syntax Root 
            var syntaxRoot = textView.TextSnapshot.GetOpenDocumentInCurrentContextWithChanges().GetSyntaxRootAsync().Result;

            //Find the Current Declaration Member from caret Position
            var currMember = syntaxRoot.FindMemberDeclarationAt(textView.Caret.Position.BufferPosition.Position);

            // If cursor is before start of MemberDeclaration consider parent as a Current Member
            if (!currMember.ContainsCaretPosition(textView.Caret.Position.BufferPosition.Position))
            {
                currMember = currMember?.Parent.AncestorsAndSelf().OfType<MemberDeclarationSyntax>().FirstOrDefault();
            }

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

        internal static int MoveMemberDown(this IWpfTextView textView)
        {
            //Get the Syntax Root 
            var syntaxRoot = textView.TextSnapshot.GetOpenDocumentInCurrentContextWithChanges().GetSyntaxRootAsync().Result;

            //Find the Current Declaration Member from caret Position
            var currMember = syntaxRoot.FindMemberDeclarationAt(textView.Caret.Position.BufferPosition.Position);

            // If cursor is before start of MemberDeclaration consider parent as a Current Member
            if (!currMember.ContainsCaretPosition(textView.Caret.Position.BufferPosition.Position))
            {
                currMember = currMember?.Parent.AncestorsAndSelf().OfType<MemberDeclarationSyntax>().FirstOrDefault();
            }

            if (currMember == null || currMember.Parent == null) return VSConstants.S_OK;

            //Find the Next Declaration Member from caret Position
            var nextMember = syntaxRoot.FindMemberDeclarationAt(currMember.FullSpan.End + 1);

            //If the current or previous member belongs to same Parent Member, then Swap the members
            if (currMember.Parent.Equals(nextMember?.Parent))
            {
                textView.SwapMembers(currMember, nextMember);
            }

            return VSConstants.S_OK;
        }

        internal static void SwapMembers(this IWpfTextView textView, MemberDeclarationSyntax member1, MemberDeclarationSyntax member2)
        {
            var content = member2.GetText().ToString();
            var editor = textView.TextSnapshot.TextBuffer.CreateEdit();
            editor.Delete(member2.FullSpan.Start, member2.FullSpan.Length);

            if(member1.SpanStart> member2.SpanStart) // Moving UP
            {
                editor.Insert(member1.FullSpan.End, content);
            }
            else // Moving Down
            {
                editor.Insert(member1.FullSpan.Start, content);
            }
            editor.Apply();
        }

        internal static MemberDeclarationSyntax FindMemberDeclarationAt(this SyntaxNode root, int position)
        {
            if (position > root.FullSpan.End || position < root.FullSpan.Start) return null;
            var token = root.FindToken(position, false);
            var member = token.Parent.AncestorsAndSelf().OfType<MemberDeclarationSyntax>().FirstOrDefault();

            //If the caret is at EnumDeclaration, entire EnumMemberDeclaration as a Member declaration
            member = member.IsKind(SyntaxKind.EnumMemberDeclaration) ? member.Parent.AncestorsAndSelf().OfType<MemberDeclarationSyntax>().FirstOrDefault() : member;
            return member;
        }

        internal static bool ContainsCaretPosition(this MemberDeclarationSyntax currMember, int caretPosition)
        {
            var trivia = currMember?.GetFirstToken().LeadingTrivia;
            foreach (var t in trivia)
            {
                if (t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) || t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia) || t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia))
                {
                    break;
                }
                if (t.IsKind(SyntaxKind.EndOfLineTrivia) && t.Span.Start <= caretPosition && t.Span.End >= caretPosition)
                {
                    return false;
                }
            }
            return true;
        }

    }
}
