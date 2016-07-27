using Microsoft.CodeAnalysis.CodeActions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ExtractClass.Actions
{
    internal sealed class MoveFileToFolderAction : CodeAction
    {
        IClassActionContext _context;

        public override string Title
        {
            get
            {
                return _context.Title;
            }
        }

        public MoveFileToFolderAction (IClassActionContext context)
        {
            _context = context;
        }


        protected override async Task<Solution> GetChangedSolutionAsync (CancellationToken cancellationToken)
        {
            Document document = _context.Context.Document;
            Project project = document.Project;
            Solution solution = project.Solution;

            SyntaxNode rootNode = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            BaseTypeDeclarationSyntax node = rootNode.FindNode(_context.Context.Span) as BaseTypeDeclarationSyntax;

            solution = solution.RemoveDocument(document.Id);
            solution = solution.AddDocument(DocumentId.CreateNewId(project.Id), $"{node.Identifier.Text}.cs", await document.GetTextAsync(cancellationToken), _context.Folders);

            return solution;
        }
    }
}
