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
        
        // Moves the cursor to either the previous or next member declaration, or does nothing if it is at the top/bottom already
        internal static int MoveToAdjacentMember1(IWpfTextView textView, bool up)
        {
            // TODO there are some odd cases that crop up. For example, it counts a class declaration which is only in a namespace (not an inner class) and also seems to count the namespace.
            // It also doesn't behave well when above or below all of them. (if above, it should go to the first, and vice versa)
            // If it is at the last and you tell it to go next, it should not move. (and vice versa)
            // These do not work at the moment
            // Additionally, It does not always go to what I feel should be the beginning of a declaration. It goes to the beginning of the [attribbutes], not the access modifier.


            // Get the Syntax Root
            var syntaxRoot = textView.TextSnapshot.GetOpenDocumentInCurrentContextWithChanges().GetSyntaxRootAsync().Result;

            // Find the Current Declaration Member from caret Position
            // This will be the most nested of Method, field, class, or namespace
            var currMember = FindDeclarationAt(syntaxRoot, textView.Caret.Position.BufferPosition.Position);
            // At this point, we have the member declaration that the cursor is inside now.
            // we need to decide the behavior based on whether it is in a method/field or if it is in a class/namespace (or nothing)
            // First, if the cursor is not inside any kind of declaration (currMember is null)
            if (currMember == null)
            {
                // TODO find the closest member up or down and go to that
                return VSConstants.S_OK;
            }
            // handle the cases of types that can have members inside them
            else if (currMember.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.ClassDeclaration)
                || currMember.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.NamespaceDeclaration))
            {
                // The cursor was on a class or namespace declaration. 
                // First, determine if we are at the beginning or the end
                int posInContainer = getPosInContainer(currMember, textView.Caret.Position.BufferPosition.Position);
                SyntaxNode adjacentMember = null;
                if (posInContainer == 0)
                {
                    // This namespace or class has no members, we need to look above or below it, but there is no reason to look into it
                    // TODO
                }
                // There are four basic possibilities here:
                // First, we are at the top of the container and going to the next member
                    // go to the first member
                // second, we are at the bottom of the container and going to the previous member
                    // go to the last member
                // Third, we are at the top and moving up
                    // We need to look at whatever member is above this
                // Fourth, we are at the end of a container and moving down
                    // we need to look after this contained for the next member
                // Five (ish) is that there are no member, in which case we still need to look above or below.
                else if (posInContainer == -1 && !up)
                {
                    adjacentMember = currMember.DescendantNodes().OfType<MemberDeclarationSyntax>().FirstOrDefault();
                }
                else if (posInContainer == 1 && up)
                {
                    adjacentMember = currMember.DescendantNodes().OfType<MemberDeclarationSyntax>().LastOrDefault();
                }
                else if (posInContainer == -1 && up)
                {
                    // we are at the top and moving up, move to the last member of hte container above or the thing above itself. 
                    // Special case: the thing immediately above is the parent of the current member, in which case just move to it
                    // find the member declaration immediately before this class or namespace
                    adjacentMember = FindDeclarationAt(syntaxRoot, currMember.FullSpan.Start - 1);
                    if (adjacentMember != currMember.Parent
                        && (adjacentMember.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.ClassDeclaration)
                        || adjacentMember.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.NamespaceDeclaration)))
                    {
                        // The thing right above is also a container. We need to go to its last member or it if there are none
                        adjacentMember = adjacentMember.DescendantNodesAndSelf().OfType<MemberDeclarationSyntax>().Last();
                    }
                    // other cases are that it is the parent or it is just another non container declaration, in which case we just need to leave adjacent member set as it is.
                }
                else
                {
                    // The only case left is that we are at the end and moving down, in which case we just need to find the first thing below us.
                    adjacentMember = FindDeclarationAt(syntaxRoot, currMember.FullSpan.End + 1);
                }
                if (adjacentMember != null)
                {
                    // move the cursor to the previous member
                    textView.Caret.MoveTo(new SnapshotPoint(textView.TextSnapshot, adjacentMember.SpanStart));
                }
                return VSConstants.S_OK;
            }
            // Handle all the members that can't contain other members
            else
            {
                SyntaxNode adjacentMember;
                if (up)
                {
                    adjacentMember = FindDeclarationAt(syntaxRoot, currMember.FullSpan.Start - 1);
                }
                else
                {
                    adjacentMember = FindDeclarationAt(syntaxRoot, currMember.FullSpan.End + 1);
                    if (adjacentMember == currMember.Parent)
                    {
                        // if we are on the last member of a container and moving down, we need to move to the next member below the parent container
                        adjacentMember = null;
                    }
                }
                // At this point, we have an adjacent member, but it may not be totally what we want
                if (adjacentMember != null)
                {
                    // move the cursor to the previous member
                    textView.Caret.MoveTo(new SnapshotPoint(textView.TextSnapshot, adjacentMember.SpanStart));
                }
            }
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        public static int MoveToAdjacentMember2(IWpfTextView textView, bool up)
        {
            var syntaxRoot = textView.TextSnapshot.GetOpenDocumentInCurrentContextWithChanges().GetSyntaxRootAsync().Result; 
            // get the position
            var position = textView.Caret.Position.BufferPosition.Position;
            SyntaxNode adjacentMember = null;
            for (int i = 0; i < 2; i ++)
            {
                var currMember = FindDeclarationAt(syntaxRoot, position);
                if (currMember == null)
                {
                    return VSConstants.S_OK;
                }
                if (isContainer(currMember))
                {
                    int posInContainer = getPosInContainer(currMember, position);
                    // The current member is a container. We need to deal with one of four (ish) possibilities:
                    // 1. We are at the top (or it is empty) and moving up
                    if (posInContainer <= 0 && up)
                    {
                        // set the position to before the start of this container and loop
                        position = currMember.FullSpan.Start - 1;
                        adjacentMember = currMember;
                        continue;
                    }
                    // 2. We are at the bottom (or it is empty) and moving down
                    if (posInContainer >= 0 && !up)
                    {
                        // set the position to right after the end of this container and loop 
                        position = currMember.FullSpan.End + 1;
                        adjacentMember = currMember;
                        continue;
                    }
                    // 1. We are at the top and moving down
                    if (posInContainer == -1 && !up)
                    {
                        // go to first child member declaration
                        adjacentMember = currMember.DescendantNodes().OfType<MemberDeclarationSyntax>().FirstOrDefault();
                        break;
                    }
                    // 2. WE are at the bottom and moving up
                    else if (posInContainer == 1 && up)
                    {
                        // go to the last member
                        adjacentMember = currMember.DescendantNodes().OfType<MemberDeclarationSyntax>().LastOrDefault();
                        break;
                    }
                }
                else
                {
                    if (up)
                    {
                        adjacentMember = FindDeclarationAt(syntaxRoot, currMember.FullSpan.Start - 1);
                        break;
                    }
                    else
                    {
                        adjacentMember = FindDeclarationAt(syntaxRoot, currMember.FullSpan.End + 1);
                        if (adjacentMember != currMember.Parent)
                        {
                            break;
                        }
                        // the current member is the last member of its parent container. We need to set the position to after the end of the container and loop
                        position = adjacentMember.FullSpan.End + 1;
                        continue;
                    }
                }
            }
            if (adjacentMember != null)
            {
                // move the cursor to the previous member
                textView.Caret.MoveTo(new SnapshotPoint(textView.TextSnapshot, adjacentMember.SpanStart));
            }
            return VSConstants.S_OK;
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

        private static int moveToImmediateNeighbor()
        {
            return 0;
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
