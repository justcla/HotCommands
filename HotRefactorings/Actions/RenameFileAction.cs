using Microsoft.CodeAnalysis.CodeActions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotCommands
{
    internal sealed class RenameFileAction : CodeAction
    {

        IActionContext _context;

        public override string Title
        {
            get
            {
                return _context.Title;
            }
        }

        public RenameFileAction (IActionContext context)
        {
            _context = context;
        }

        protected override async Task<Solution> GetChangedSolutionAsync (CancellationToken cancellationToken)
        {
            Document document = _context.Context.Document;
            SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            BaseTypeDeclarationSyntax node = root.FindNode(_context.Context.Span) as BaseTypeDeclarationSyntax;

            var project = document.Project.RemoveDocument(document.Id);
            var newDocument = project.AddDocument($"{node.Identifier.Text}.cs", root, document.Folders);

            return newDocument.Project.Solution;
        }
    }
}
