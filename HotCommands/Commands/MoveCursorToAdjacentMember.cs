using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.Text;
using Microsoft.CodeAnalysis;
using System.Linq;
using System;
using Microsoft.VisualStudio.Text.Operations;

namespace HotCommands
{
    class MoveCursorToAdjacentMember : Command<MoveCursorToAdjacentMember>
    {
        public static int MoveToNextMember(IWpfTextView textView, IEditorOperations editorOperations)
        {
            return MoveToAdjacentMember(textView, editorOperations, up: false);
        }
        public static int MoveToPreviousMember(IWpfTextView textView, IEditorOperations editorOperations)
        {
            return MoveToAdjacentMember(textView, editorOperations, up: true);
        }

        public static int MoveToAdjacentMember(
            IWpfTextView textView,
            IEditorOperations editorOperations,
            bool up)
        {
            var syntaxRoot = textView.TextSnapshot.GetOpenDocumentInCurrentContextWithChanges().GetSyntaxRootAsync().Result;
            var position = textView.Caret.Position.BufferPosition.Position;
            var currMember = FindDeclarationAt(syntaxRoot, position);
            // Check if outside of all namespaces and classes
            if (currMember == null)
            {
                var children = syntaxRoot.ChildNodes().OfType<MemberDeclarationSyntax>();
                if (children.Count() < 1)
                {
                    return VSConstants.S_OK;
                }
                if (up)
                {
                    var last = children.Last();
                    if (position > last.SpanStart)
                    {
                        var node = last.DescendantNodesAndSelf().OfType<MemberDeclarationSyntax>().LastOrDefault();
                        MoveCursor(textView, node, editorOperations);
                        return VSConstants.S_OK;
                    }
                }
                else
                {
                    var first = children.First();
                    if (position < first.SpanStart)
                    {
                        MoveCursor(textView, first, editorOperations);
                        return VSConstants.S_OK;
                    }
                }
                return VSConstants.S_OK;
            }
            // Check if inside a method
            // Includes all annotations and comments (trivia??)
            if ((position < GetCorrectPosition(currMember) && !up) ||
                (position > GetCorrectPosition(currMember) && up))
            {
                MoveCursor(textView, currMember, editorOperations);
                return VSConstants.S_OK;
            }
            MemberDeclarationSyntax MoveToNode = null;
            // Check if inside Namespace or class
            if (isContainer(currMember))
            {
                int posInContainer = getPosInContainer(currMember, position);
                // The current member is a container. We need to deal with one of four (ish) possibilities:
                // 1. We are at the top (or it is empty) and moving up
                if (posInContainer <= 0 && up)
                {
                    MoveToNode = getPrevious(currMember);
                }
                // 2. We are at the bottom (or it is empty) and moving down
                if (posInContainer >= 0 && !up)
                {
                    var node = getNext(currMember);
                    if (node != currMember)
                    {
                        MoveCursor(textView, node, editorOperations);
                    }
                    return VSConstants.S_OK;
                }
                // 3. We are at the top and moving down
                if (posInContainer == -1 && !up)
                {
                    // go to first child member declaration
                    MoveToNode = currMember.DescendantNodes().OfType<MemberDeclarationSyntax>().FirstOrDefault();
                }
                // 4. WE are at the bottom and moving up
                else if (posInContainer == 1 && up)
                {
                    // go to the last member
                    MoveToNode = currMember.DescendantNodes().OfType<MemberDeclarationSyntax>().LastOrDefault();
                }
            }
            // Else we are not in a container
            else
            {
                if (up)
                {
                    MoveToNode = getPrevious(currMember);
                }
                else  // down
                {
                    var node = getNext(currMember);
                    if (node != currMember)
                    {
                        MoveCursor(textView, node, editorOperations);
                    }
                    return VSConstants.S_OK;
                }
            }
            MoveCursor(textView, MoveToNode, editorOperations);
            return VSConstants.S_OK;
        }

        public static MemberDeclarationSyntax getNext(SyntaxNode current)
        {
            // First, go to the parent
            SyntaxNode Parent = current.Parent;
            if (Parent == null)
            {
                // We have reached the root node, there can be no siblings, and therefore there is no next
                return null;
            }
            bool foundSelf = false;
            // Iterate through parent children
            foreach (MemberDeclarationSyntax node in Parent.ChildNodes().OfType<MemberDeclarationSyntax>())
            {
                if (!foundSelf)
                {
                    // mark when we come accross our own node so that on the next one we can return it
                    foundSelf = node.Equals(current);
                }
                else
                {
                    // return the next node after we find ourselves
                    return node;
                }
            }

            // If we get here, the current node was the last declaration in the parent
            return getNext(Parent);
        }

        public static MemberDeclarationSyntax getPrevious(SyntaxNode current)
        {
            SyntaxNode parent = current.Parent;
            MemberDeclarationSyntax previous = null;
            foreach (MemberDeclarationSyntax node in parent.ChildNodes().OfType<MemberDeclarationSyntax>())
            {
                if (node.Equals(current))
                {
                    if (previous == null)
                    {
                        return parent.Parent != null ? (MemberDeclarationSyntax)parent : null;
                    }
                    if (isContainer(previous))
                    {
                        return previous.DescendantNodesAndSelf().OfType<MemberDeclarationSyntax>().LastOrDefault();
                    }
                    return previous;
                }
                previous = node;
            }
            // this should never happen. By definition the node will be found within its own parent's children
            return null;
        }

        public static void MoveCursor(IWpfTextView textView, MemberDeclarationSyntax node, IEditorOperations editorOperations)
        {
            if (node != null)
            {
                // move the cursor to the previous member
                textView.Caret.MoveTo(new SnapshotPoint(textView.TextSnapshot, GetCorrectPosition(node)));
                if (!CursorInView(textView))
                {
                    editorOperations.ScrollLineCenter();
                }
            }
        }

        private static bool CursorInView(IWpfTextView textView)
        {
            var caretPos = textView.Caret.Position.BufferPosition.Position;
            return caretPos < textView.ViewportBottom && caretPos > textView.ViewportTop;
        }

        private static int GetCorrectPosition(MemberDeclarationSyntax node)
        {
            var children = node.ChildNodesAndTokens().Where(x => x.RawKind != (int)Microsoft.CodeAnalysis.CSharp.SyntaxKind.AttributeList);
            return children.First().SpanStart;
        }

        private static bool isContainer(MemberDeclarationSyntax currMember)
        {
            return (currMember.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.ClassDeclaration)
                || currMember.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.NamespaceDeclaration)
                && currMember.DescendantNodes().OfType<MemberDeclarationSyntax>().Count() > 0);
        }

        /// <summary>
        /// Determines if we are before or after the members of this container (or if there are none)
        /// </summary>
        /// <param name="currMember">The namespace or class SyntxNode</param>
        /// <param name="position">The position in the text</param>
        /// <returns>-1 if position is before all members, 0 if there are none, 1 if it is after all members</returns>
        private static int getPosInContainer(MemberDeclarationSyntax currMember, int position)
        {
            var lastMember = currMember.DescendantNodes().OfType<MemberDeclarationSyntax>().LastOrDefault();
            if (lastMember == null)
            {
                return 0;
            }
            else if (position > lastMember.FullSpan.End)
            {
                return 1;
            }
            else
            {
                return -1;
            }
        }

        internal static MemberDeclarationSyntax FindDeclarationAt(SyntaxNode root, int position)
        {
            if (position > root.FullSpan.End || position < root.FullSpan.Start)
            {
                return null;
            }
            var token = root.FindToken(position, false);
            var member = token.Parent.AncestorsAndSelf().OfType<MemberDeclarationSyntax>().FirstOrDefault();
            return member;
        }
    }
}