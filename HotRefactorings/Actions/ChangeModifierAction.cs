using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotCommands
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
            BaseTypeDeclarationSyntax node = GetClassTypeNode(rootNode);
            if (node == null) return document;

            // if the context is not in a class, return the Document
            ClassDeclarationSyntax nodeClassConceiler = node.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().SingleOrDefault();
            if (nodeClassConceiler == null) return document;

            // First, remove all but the first MainModifier
            while (HasMoreThanOneMainModifier(node))
            {
                // Remove the last MainModifier
                rootNode = rootNode.ReplaceToken(GetLastMainModifier(node), SyntaxFactory.Token(SyntaxKind.None));
                node = GetClassTypeNode(rootNode);
            }

            // Second, replace the MainModifier with the NewModifiers
            rootNode = rootNode.ReplaceToken(node.Modifiers.First(), _context.NewModifiers);

            // Cleanup additional modifiers (ie. "internal" left behind)
            return document.WithSyntaxRoot(rootNode);
        }

        private BaseTypeDeclarationSyntax GetClassTypeNode(SyntaxNode rootNode)
        {
            return rootNode.FindNode(_context.Context.Span) as BaseTypeDeclarationSyntax;
        }

        private bool HasMoreThanOneMainModifier(BaseTypeDeclarationSyntax node)
        {
            var mainModifierCount = node.Modifiers.Count(m => m.IsKind(SyntaxKind.PublicKeyword) ||
                                                  m.IsKind(SyntaxKind.ProtectedKeyword) ||
                                                  m.IsKind(SyntaxKind.InternalKeyword) ||
                                                  m.IsKind(SyntaxKind.PrivateKeyword));
            return mainModifierCount > 1;
        }

        private static SyntaxToken GetLastMainModifier(BaseTypeDeclarationSyntax node)
        {
            return node.Modifiers.Last(m => m.IsKind(SyntaxKind.PublicKeyword) ||
                                            m.IsKind(SyntaxKind.ProtectedKeyword) ||
                                            m.IsKind(SyntaxKind.InternalKeyword) ||
                                            m.IsKind(SyntaxKind.PrivateKeyword));
        }
    }
}
