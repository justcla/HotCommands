using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
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

            ClassDeclarationSyntax nodeClassConceiler = node.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().Single();

            // if the context is not in a class, return the Document
            if (nodeClassConceiler == null) return document;

            SyntaxToken oldToken = node.Modifiers.First(m => m.GetType().Equals(_context.OldModifier.GetType()));

            // if no 'Private' keyword return the same old Document
            if (oldToken == null) return document;

            // get the syntax root & replace the tokens
            SyntaxNode newRoot = rootNode.ReplaceToken(oldToken, new[] { _context.NewModifier });
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
