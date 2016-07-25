using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.Text.Editor;
using System.Linq;

namespace HotCommands
{
    static class EditorExtensions
    {
        internal static void SwapMembers(this IWpfTextView textView, MemberDeclarationSyntax member1, MemberDeclarationSyntax member2)
        {
            var content = member2.GetText().ToString();
            var editor = textView.TextSnapshot.TextBuffer.CreateEdit();
            editor.Delete(member2.FullSpan.Start, member2.FullSpan.Length);

            if(member1.SpanStart> member2.SpanStart)
            {
                editor.Insert(member1.FullSpan.End, content);
            }
            else
            {
                editor.Insert(member1.FullSpan.Start, content);
            }
            editor.Apply();
        }

        internal static MemberDeclarationSyntax FindMemberDeclarationAt(this SyntaxNode root, int position)
        {
            if(position > root.FullSpan.End || position< root.FullSpan.Start) return null;
            var token = root.FindToken(position, false);
            var member = token.Parent.AncestorsAndSelf().OfType<MemberDeclarationSyntax>().FirstOrDefault();
            return member;
        }
    }
}
