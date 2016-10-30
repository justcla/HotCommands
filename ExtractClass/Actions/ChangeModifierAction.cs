using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ExtractClass.Actions
{
    internal sealed class ChangeModifierAction : CodeAction
    {
        IModifierActionContext _context;

        public override string Title
        {
            get
            {
                return _context.Title;
            }
        }

        public ChangeModifierAction (IModifierActionContext context)
        {
            _context = context;
        }

        protected override async Task<Document> GetChangedDocumentAsync (CancellationToken cancellationToken)
        {
            Document document = _context.Context.Document;

            SyntaxNode rootNode = await document.GetSyntaxRootAsync(_context.Context.CancellationToken).ConfigureAwait(false);
            BaseTypeDeclarationSyntax node = rootNode.FindNode(_context.Context.Span) as BaseTypeDeclarationSyntax;
            if (node == null) return document;

            // if the context is not in a class, return the Document
            ClassDeclarationSyntax nodeClassConceiler = node.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().Single();
            if (nodeClassConceiler == null) return document;

            // get the syntax root & replace the tokens
            rootNode = rootNode.ReplaceToken(node.Modifiers.First(), _context.NewModifiers);
            node = GetClassTypeNode(rootNode);

            // Cleanup additional modifiers (ie. "internal" left behind)
            while (GetClassTypeNode(rootNode).Modifiers.Count(m => !m.IsKind(SyntaxKind.None)) > _context.NewModifiers.Length)
            {
                rootNode = rootNode.ReplaceToken(node.Modifiers.Last(), SyntaxFactory.Token(SyntaxKind.None));
                node = GetClassTypeNode(rootNode);
            }

            return document.WithSyntaxRoot(rootNode);
        }

        private BaseTypeDeclarationSyntax GetClassTypeNode(SyntaxNode rootNode)
        {
            return rootNode.FindNode(_context.Context.Span) as BaseTypeDeclarationSyntax;
        }
    }
}
