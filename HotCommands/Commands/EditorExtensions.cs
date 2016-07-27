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
            var position = textView.Caret.Position.BufferPosition.Position;
            var syntaxRoot = textView.TextSnapshot.GetOpenDocumentInCurrentContextWithChanges().GetSyntaxRootAsync().Result;

            //Find the Current member
            var currMember = syntaxRoot.FindMemberDeclarationAt(position);            
            if (!currMember.ContainsPosition(position))
            {
                currMember = currMember?.Parent.AncestorsAndSelf().OfType<MemberDeclarationSyntax>().FirstOrDefault();
            }

            // If current member is outside the container, exit
            if (currMember == null || currMember.Parent == null)
            {
                return VSConstants.S_OK;
            }
            
            //Find the previous member
            var prevMember = syntaxRoot.FindMemberDeclarationAt(currMember.FullSpan.Start - 1);

            //if prev member is not a Container and Type declaration
            if (!prevMember.Equals(currMember.Parent) && prevMember.IsTypeDeclaration()) // find nested member
            {
                var nestedMembers = prevMember.ChildNodes().OfType<MemberDeclarationSyntax>();
                while (nestedMembers.Count() > 0)
                {
                    prevMember = nestedMembers.Last();
                    nestedMembers = prevMember.ChildNodes().OfType<MemberDeclarationSyntax>();
                }
                if (prevMember.IsTypeDeclaration())
                {
                    //If prevoius member is empty type declaration, place inside the type
                    textView.SwapMembers(currMember, prevMember, MoveDirection.Middle);
                }
                else
                {
                    //Move at end of the previous nested Member declaration
                    textView.SwapMembers(currMember, prevMember, MoveDirection.Down);
                }
            }
            else
            {
                textView.SwapMembers(currMember, prevMember, MoveDirection.Up);
            }

            return VSConstants.S_OK;
        }


        internal static int MoveMemberDown(this IWpfTextView textView)
        {
            var caretPosition = textView.Caret.Position.BufferPosition.Position;
            var syntaxRoot = textView.TextSnapshot.GetOpenDocumentInCurrentContextWithChanges().GetSyntaxRootAsync().Result;
            var currMember = syntaxRoot.FindMemberDeclarationAt(caretPosition);

            // If cursor is outside the MemberDeclaration consider parent as a Current Member.
            if (!currMember.ContainsPosition(caretPosition))
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

            //If Next member is not a Enum, and it has a nesteed member declaration, get the nearest/closest member declaration
            if (!nextMember.IsKind(SyntaxKind.EnumDeclaration))
            {
                var nestedMembers = nextMember.ChildNodes().OfType<MemberDeclarationSyntax>();
                while (nestedMembers.Count() > 0)
                {
                    nextMember = nestedMembers.First();
                    nestedMembers = nextMember.ChildNodes().OfType<MemberDeclarationSyntax>();
                }
            }


            //textView.SwapMembers(currMember, nextMember, false);

            return VSConstants.S_OK;
        }

        internal static void SwapMembers(this IWpfTextView textView, MemberDeclarationSyntax member1, MemberDeclarationSyntax member2, MoveDirection direction)
        {
            int newCaretPosition = 0;
            var editor = textView.TextSnapshot.TextBuffer.CreateEdit();
            var caretIndent = textView.Caret.Position.BufferPosition.Position - member1.FullSpan.Start;

            editor.Delete(member1.FullSpan.Start, member1.FullSpan.Length);
            if (direction == MoveDirection.Up)
            {
                editor.Insert(member2.FullSpan.Start, member1.GetText().ToString());
                newCaretPosition = member2.FullSpan.Start + caretIndent;
            }
            else if (direction == MoveDirection.Down)
            {
                editor.Insert(member2.FullSpan.End, member1.GetText().ToString());
                //if member 1 is above member2 case1 or case2
                newCaretPosition = (member1.SpanStart <member2.SpanStart) ? member2.FullSpan.End + caretIndent - member1.FullSpan.Length : member2.FullSpan.End + caretIndent;
            }
            else if (direction == MoveDirection.Middle)
            {
                var blockToken = member2.ChildTokens().FirstOrDefault(t => t.IsKind(SyntaxKind.CloseBraceToken));
                editor.Insert(blockToken.SpanStart - 1, member1.GetText().ToString());
                newCaretPosition = blockToken.SpanStart - 1 + caretIndent;
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

        internal static bool ContainsPosition(this MemberDeclarationSyntax currMember, int caretPosition)
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

        private static bool IsTypeDeclaration(this SyntaxNode node)
        {
            return node.IsKind(SyntaxKind.ClassDeclaration) || node.IsKind(SyntaxKind.StructDeclaration) || node.IsKind(SyntaxKind.InterfaceDeclaration);
        }

        internal enum MoveDirection
        {
            Up,
            Down,
            Middle
        }
    }
}
