﻿using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.Text;
using Microsoft.CodeAnalysis;
using System.Linq;
using System;

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

        public static int MoveToAdjacentMember(IWpfTextView textView, bool up)
        {
            var syntaxRoot = textView.TextSnapshot.GetOpenDocumentInCurrentContextWithChanges().GetSyntaxRootAsync().Result;
            var position = textView.Caret.Position.BufferPosition.Position;
            var currMember = FindDeclarationAt(syntaxRoot, position);
            if (currMember == null)
            {
                // We are not inside a definition
                return VSConstants.S_OK;
            }
            SyntaxNode MoveToNode = null;
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
                    MoveToNode = getNext(currMember);
                }
                // 1. We are at the top and moving down
                if (posInContainer == -1 && !up)
                {
                    // go to first child member declaration
                    MoveToNode = currMember.DescendantNodes().OfType<MemberDeclarationSyntax>().FirstOrDefault();
                }
                // 2. WE are at the bottom and moving up
                else if (posInContainer == 1 && up)
                {
                    // go to the last member
                    MoveToNode = currMember.DescendantNodes().OfType<MemberDeclarationSyntax>().LastOrDefault();
                }
            }
            else
            {
                if (up)
                {
                    MoveToNode = getPrevious(currMember);
                }
                else
                {
                    MoveToNode = getNext(currMember);
                }
            }        
            MoveCursor(textView, MoveToNode);
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
            foreach(MemberDeclarationSyntax node in Parent.ChildNodes().OfType< MemberDeclarationSyntax>())
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

        public static SyntaxNode getPrevious(SyntaxNode current)
        {
            SyntaxNode parent = current.Parent;
            MemberDeclarationSyntax previous = null;
            foreach(MemberDeclarationSyntax node in parent.ChildNodes().OfType<MemberDeclarationSyntax>())
            {
                if (node.Equals(current))
                {
                    if (previous == null)
                    {
                        return parent;
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

        public static void MoveCursor(IWpfTextView textView, SyntaxNode node)
        {
            if (node != null)
            {
                // move the cursor to the previous member
                textView.Caret.MoveTo(new SnapshotPoint(textView.TextSnapshot, node.SpanStart));
            }
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
