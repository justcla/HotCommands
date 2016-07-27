using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using System;
using System.Linq;

namespace HotCommands
{
    static class EditorExtensions
    {

        internal static int MoveMemberUp(this IWpfTextView textView, IOleCommandTarget commandTarget, IEditorOperations editorOperations)
        {
            var position = textView.Caret.Position.BufferPosition.Position;
            var syntaxRoot = textView.TextSnapshot.GetOpenDocumentInCurrentContextWithChanges().GetSyntaxRootAsync().Result;
            MovePosition movePsoition = MovePosition.Top;

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
                            prevMember = prevMember.IsRootNodeof(currMember) ? prevMember : prevMember.GetNextChildMember(true);
                            if (prevMember == null) return VSConstants.S_OK;
                        }
                        movePsoition = prevMember.IsContainerType() ? MovePosition.MiddlefromBottom : MovePosition.Bottom;
                    }
                }
                else  //prev member is Sibling
                {
                    prevMember.GetNextChildMember(true);
                    movePsoition = prevMember.IsContainerType() ? MovePosition.MiddlefromBottom : MovePosition.Bottom;
                }
            }

            textView.SwapMembers(currMember, prevMember, movePsoition, MoveDirection.Up);
            editorOperations.ScrollLineCenter();
            FormatDocument(commandTarget);
            return VSConstants.S_OK;
        }

        internal static int MoveMemberDown(this IWpfTextView textView, IOleCommandTarget commandTarget, IEditorOperations editorOperations)
        {
            var position = textView.Caret.Position.BufferPosition.Position;
            var syntaxRoot = textView.TextSnapshot.GetOpenDocumentInCurrentContextWithChanges().GetSyntaxRootAsync().Result;
            MovePosition movePsoition = MovePosition.Bottom;

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
                            nextMember = nextMember.IsRootNodeof(currMember) ? nextMember : nextMember.GetNextChildMember(false);
                            if (nextMember == null) return VSConstants.S_OK;
                        }
                        movePsoition = nextMember.IsContainerType() ? MovePosition.MiddlefromTop : MovePosition.Top;
                    }
                }
                else  //Next member is Sibling
                {
                    nextMember.GetNextChildMember(false);
                    movePsoition = nextMember.IsContainerType() ? MovePosition.MiddlefromTop : MovePosition.Top;
                }
            }

            textView.SwapMembers(currMember, nextMember, movePsoition, MoveDirection.Down);
            editorOperations.ScrollLineCenter();
            FormatDocument(commandTarget);
            return VSConstants.S_OK;
        }

        private static void FormatDocument(IOleCommandTarget commandTarget)
        {
            Guid cmdGroup = VSConstants.VSStd2K;
            uint cmdID = (uint)VSConstants.VSStd2KCmdID.FORMATDOCUMENT;
            int hr = commandTarget.Exec(ref cmdGroup, cmdID, (uint)OLECMDEXECOPT.OLECMDEXECOPT_DODEFAULT, IntPtr.Zero, IntPtr.Zero);
        }

        internal static void SwapMembers(this IWpfTextView textView, MemberDeclarationSyntax member1, MemberDeclarationSyntax member2, MovePosition position, MoveDirection direction)
        {
            if (member1 == null || member2 == null) return;
            int newCaretPosition = 0;
            var editor = textView.TextSnapshot.TextBuffer.CreateEdit();
            var caretIndent = textView.Caret.Position.BufferPosition.Position - member1.FullSpan.Start;

            editor.Delete(member1.FullSpan.Start, member1.FullSpan.Length);
            if (position == MovePosition.Top)
            {
                editor.Insert(member2.FullSpan.Start, member1.GetText().ToString());
                newCaretPosition = direction == MoveDirection.Up ? (member2.FullSpan.Start + caretIndent) : (member2.FullSpan.Start + caretIndent - member1.FullSpan.Length);
            }
            else if (position == MovePosition.Bottom)
            {
                editor.Insert(member2.FullSpan.End, member1.GetText().ToString());
                newCaretPosition = direction == MoveDirection.Up ? (member2.FullSpan.End + caretIndent) : (member2.FullSpan.End + caretIndent - member1.FullSpan.Length);
            }
            else if (position == MovePosition.MiddlefromBottom)
            {
                var blockToken = member2.ChildTokens().FirstOrDefault(t => t.IsKind(SyntaxKind.CloseBraceToken));
                editor.Insert(blockToken.SpanStart - 1, member1.GetText().ToString());
                newCaretPosition = direction == MoveDirection.Up ? (blockToken.SpanStart - 1 + caretIndent) : (blockToken.SpanStart - 1 + caretIndent - member2.FullSpan.Length);
            }
            else if (position == MovePosition.MiddlefromTop)
            {
                var blockToken = member2.ChildTokens().FirstOrDefault(t => t.IsKind(SyntaxKind.OpenBraceToken));
                editor.Insert(blockToken.SpanStart + 1, member1.GetText().ToString());
                newCaretPosition = direction == MoveDirection.Up ? (blockToken.SpanStart + 1 + caretIndent) : blockToken.SpanStart + 1 + caretIndent - member1.FullSpan.Length;
            }

            editor.Apply();
            textView.Caret.MoveTo(new SnapshotPoint(textView.TextSnapshot, newCaretPosition));

        }

        private static MemberDeclarationSyntax GetNextChildMember(this MemberDeclarationSyntax member, bool moveFromBottom)
        {
            var childMembers = member?.ChildNodes().OfType<MemberDeclarationSyntax>();
            while (childMembers?.Count() > 0 && !member.IsKind(SyntaxKind.EnumDeclaration))
            {
                member = moveFromBottom ? childMembers.Last() : childMembers.First();
                childMembers = member.ChildNodes().OfType<MemberDeclarationSyntax>();
            }
            return member;
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

        internal enum MovePosition
        {
            Top,
            Bottom,
            MiddlefromBottom,
            MiddlefromTop
        }

        internal enum MoveDirection
        {
            Up,
            Down
        }
    }
}
