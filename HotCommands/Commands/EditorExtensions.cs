using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System.Linq;

namespace HotCommands
{
    static class EditorExtensions
    {
        internal static int MoveMemberUp(this IWpfTextView textView)
        {
            var caretPosition = textView.Caret.Position.BufferPosition.Position;
            var syntaxRoot = textView.TextSnapshot.GetOpenDocumentInCurrentContextWithChanges().GetSyntaxRootAsync().Result;
            var currMember = syntaxRoot.FindMemberDeclarationAt(caretPosition);

            // If cursor is outside the MemberDeclaration consider parent as a Current Member.
            if (!currMember.ContainsCaretPosition(caretPosition))
            {
                currMember = currMember?.Parent.AncestorsAndSelf().OfType<MemberDeclarationSyntax>().FirstOrDefault();
            }

            if (currMember == null || currMember.Parent == null) return VSConstants.S_OK;

            //Find the Previous Declaration Member from caret Position
            var prevMember = syntaxRoot.FindMemberDeclarationAt(currMember.FullSpan.Start - 1);
            while (prevMember.IsRootNodeof(currMember) && prevMember.FullSpan.Start>0)            
            {
                prevMember = syntaxRoot.FindMemberDeclarationAt(prevMember.FullSpan.Start - 1);
                if (prevMember == null) return VSConstants.S_OK; ;
            }

            //If previous member has any nested member declaration, get the nearest/closest member declaration
            var nestedMembers = prevMember.ChildNodes().OfType<MemberDeclarationSyntax>();
            while (nestedMembers.Count() > 0)
            {
                prevMember = nestedMembers.Last();
                nestedMembers = prevMember.ChildNodes().OfType<MemberDeclarationSyntax>();
            }

            textView.SwapMembers(currMember, prevMember, true);

            return VSConstants.S_OK;
        }
        

        internal static int MoveMemberDown(this IWpfTextView textView)
        {
            var caretPosition = textView.Caret.Position.BufferPosition.Position;
            var syntaxRoot = textView.TextSnapshot.GetOpenDocumentInCurrentContextWithChanges().GetSyntaxRootAsync().Result;
            var currMember = syntaxRoot.FindMemberDeclarationAt(caretPosition);

            // If cursor is outside the MemberDeclaration consider parent as a Current Member.
            if (!currMember.ContainsCaretPosition(caretPosition))
            {
                currMember = currMember?.Parent.AncestorsAndSelf().OfType<MemberDeclarationSyntax>().FirstOrDefault();
            }

            if (currMember == null || currMember.Parent == null) return VSConstants.S_OK;

            //Find the Next Declaration Member from caret Position
            var nextMember = syntaxRoot.FindMemberDeclarationAt(currMember.FullSpan.End + 1);
            while (nextMember.IsRootNodeof(currMember))
            {
                nextMember = syntaxRoot.FindMemberDeclarationAt(nextMember.FullSpan.End + 1);
                if (nextMember == null) return VSConstants.S_OK;
            }

            //If previous member has any nesteed member declaration, get the nearest/closest member declaration
            var nestedMembers = nextMember.ChildNodes().OfType<MemberDeclarationSyntax>();
            while (nestedMembers.Count() > 0)
            {
                nextMember = nestedMembers.First();
                nestedMembers = nextMember.ChildNodes().OfType<MemberDeclarationSyntax>();
            }

            textView.SwapMembers(currMember, nextMember, false);

            return VSConstants.S_OK;
        }

        internal static void SwapMembers(this IWpfTextView textView, MemberDeclarationSyntax member1, MemberDeclarationSyntax member2, bool isMoveUp)
        {
            int newCaretPosition;
            var editor = textView.TextSnapshot.TextBuffer.CreateEdit();
            var caretIndent = textView.Caret.Position.BufferPosition.Position - member1.FullSpan.Start;            
                        
            editor.Delete(member1.FullSpan.Start, member1.FullSpan.Length);
            if(isMoveUp)
            {
                editor.Insert(member2.FullSpan.Start, member1.GetText().ToString());
                newCaretPosition = member2.FullSpan.Start + caretIndent;
            }
            else
            {
                editor.Insert(member2.FullSpan.End, member1.GetText().ToString());
                newCaretPosition = member2.FullSpan.End + caretIndent - member1.FullSpan.Length;
            }
            
            editor.Apply();           
            textView.Caret.MoveTo(new SnapshotPoint(textView.TextSnapshot, newCaretPosition));
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

        private static bool IsRootNodeof(this SyntaxNode member, SyntaxNode currentMember)
        {
            while (currentMember.Parent != null)
            {
                if (currentMember.Parent.Equals(member))
                {
                    return true;
                }
                currentMember = currentMember.Parent;
            }
            return false;
        }
    }
}
