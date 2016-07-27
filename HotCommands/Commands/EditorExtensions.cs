using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System.Linq;
using System;

namespace HotCommands
{
    static class EditorExtensions
    {

        internal static int MoveMemberUp(this IWpfTextView textView)
        {
            var position = textView.Caret.Position.BufferPosition.Position;
            var syntaxRoot = textView.TextSnapshot.GetOpenDocumentInCurrentContextWithChanges().GetSyntaxRootAsync().Result;
            MoveDirection direction = MoveDirection.Up;

            // Find the Current member, and exit if it is outside the container            
            var currMember = syntaxRoot.FindMemberDeclarationAt(position);
            if (!currMember.ContainsPosition(position))
            {
                currMember = currMember?.Parent.AncestorsAndSelf().OfType<MemberDeclarationSyntax>().FirstOrDefault();
            }
            if (currMember == null || currMember.Parent == null) return VSConstants.S_OK;

            //Find Previous member
            var prevMember = syntaxRoot.FindMemberDeclarationAt(currMember.FullSpan.Start - 1);
            if (prevMember.IsContainerType())
            {
                if (prevMember.Equals(currMember.Parent))
                {
                    //Move Methods/Properties/Enum/Constructor/..
                    if (!currMember.IsContainerType())
                    {
                        while (prevMember.IsRootNodeof(currMember) || prevMember.IsKind(SyntaxKind.NamespaceDeclaration)) //untill valid
                        {
                            prevMember = syntaxRoot.FindMemberDeclarationAt(prevMember.FullSpan.Start - 1);
                            prevMember.MovetoLastChildMember();
                            if (prevMember == null) return VSConstants.S_OK;
                        }
                        direction = prevMember.IsContainerType() ? MoveDirection.MiddlefromBottom : MoveDirection.Down;
                    }
                }
                else  //prev member is Sibling
                {
                    prevMember.MovetoLastChildMember();
                    direction = prevMember.IsContainerType() ? MoveDirection.MiddlefromBottom : MoveDirection.Down;
                }
            }

            textView.SwapMembers(currMember, prevMember, direction);
            return VSConstants.S_OK;
        }

        internal static int MoveMemberDown(this IWpfTextView textView)
        {
            var position = textView.Caret.Position.BufferPosition.Position;
            var syntaxRoot = textView.TextSnapshot.GetOpenDocumentInCurrentContextWithChanges().GetSyntaxRootAsync().Result;
            MoveDirection direction = MoveDirection.Down;

            // Find the Current member, and exit if it is outside the container
            var currMember = syntaxRoot.FindMemberDeclarationAt(position);
            if (!currMember.ContainsPosition(position))
            {
                currMember = currMember?.Parent.AncestorsAndSelf().OfType<MemberDeclarationSyntax>().FirstOrDefault();
            }
            if (currMember == null || currMember.Parent == null) return VSConstants.S_OK;

            //Find next member
            var nextMember = syntaxRoot.FindMemberDeclarationAt(currMember.FullSpan.End + 1);
            if (nextMember.IsContainerType())
            {
                if (nextMember.Equals(currMember.Parent))
                {
                    //Move Methods/Properties/Enum/Constructor/..
                    if (!currMember.IsContainerType())
                    {
                        while (nextMember.IsRootNodeof(currMember) || nextMember.IsKind(SyntaxKind.NamespaceDeclaration)) //untill valid
                        {
                            nextMember = syntaxRoot.FindMemberDeclarationAt(nextMember.FullSpan.End + 1);
                            nextMember.MovetoFirstChildMember();
                            if (nextMember == null) return VSConstants.S_OK;
                        }
                        direction = nextMember.IsContainerType() ? MoveDirection.MiddlefromTop : MoveDirection.Up;
                    }
                }
                else  //Next member is Sibling
                {
                    MovetoFirstChildMember(nextMember);
                    direction = nextMember.IsContainerType() ? MoveDirection.MiddlefromTop : MoveDirection.Up;
                }
            }

            textView.SwapMembers(currMember, nextMember, direction);
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
                newCaretPosition = (member1.SpanStart < member2.SpanStart) ? member2.FullSpan.End + caretIndent - member1.FullSpan.Length : member2.FullSpan.End + caretIndent;
            }
            else if (direction == MoveDirection.MiddlefromBottom)
            {
                var blockToken = member2.ChildTokens().FirstOrDefault(t => t.IsKind(SyntaxKind.CloseBraceToken));
                editor.Insert(blockToken.SpanStart - 1, member1.GetText().ToString());
                newCaretPosition = blockToken.SpanStart - 1 + caretIndent;
            }
            else if (direction == MoveDirection.MiddlefromTop)
            {
                var blockToken = member2.ChildTokens().FirstOrDefault(t => t.IsKind(SyntaxKind.CloseBraceToken));
                editor.Insert(blockToken.SpanStart - 1, member1.GetText().ToString());
                newCaretPosition = blockToken.SpanStart - 1 + caretIndent - member1.FullSpan.Length;
            }

            editor.Apply();
            textView.Caret.MoveTo(new SnapshotPoint(textView.TextSnapshot, newCaretPosition));


        }

        //TODO merge both method & remove return type
        private static void MovetoLastChildMember(this MemberDeclarationSyntax member)
        {
            var childMembers = member?.ChildNodes().OfType<MemberDeclarationSyntax>();
            while (childMembers?.Count() > 0 && !member.IsKind(SyntaxKind.EnumDeclaration))
            {
                member = childMembers.Last();
                childMembers = member.ChildNodes().OfType<MemberDeclarationSyntax>();
            }
        }

        private static void MovetoFirstChildMember(this MemberDeclarationSyntax member)
        {
            var childMembers = member?.ChildNodes().OfType<MemberDeclarationSyntax>();
            while (childMembers?.Count() > 0 && !member.IsKind(SyntaxKind.EnumDeclaration))
            {
                member = childMembers.First();
                childMembers = member.ChildNodes().OfType<MemberDeclarationSyntax>();
            }
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

        private static bool IsContainerType(this SyntaxNode node)
        {
            return node.IsKind(SyntaxKind.ClassDeclaration) || node.IsKind(SyntaxKind.StructDeclaration) || node.IsKind(SyntaxKind.InterfaceDeclaration) || node.IsKind(SyntaxKind.NamespaceDeclaration);
        }

        internal enum MoveDirection
        {
            Up,
            Down,
            MiddlefromBottom,
            MiddlefromTop
        }
    }
}
