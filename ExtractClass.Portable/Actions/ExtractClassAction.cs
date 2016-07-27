using Microsoft.CodeAnalysis.CodeActions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ExtractClass.Actions
{
    internal sealed class ExtractClassAction : CodeAction
    {
        IClassActionContext _context;

        public override string Title
        {
            get
            {
                return _context.Title;
            }
        }

        public ExtractClassAction (IClassActionContext context)
        {
            _context = context;
        }

        protected override async Task<Solution> GetChangedSolutionAsync (CancellationToken cancellationToken)
        {
            Document document = _context.Context.Document;
            SyntaxNode rootNode = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            BaseTypeDeclarationSyntax node = rootNode.FindNode(_context.Context.Span) as BaseTypeDeclarationSyntax;

            // symbol representing the type
            SemanticModel semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            INamedTypeSymbol typeSymbol = semanticModel.GetDeclaredSymbol(node, cancellationToken);

            // remove type from current files            
            SyntaxNode currentRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            SyntaxNode replacedRoot = currentRoot.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);

            // check for empty namespaces. if empty namespace is present, delete it         
            var namespaces = replacedRoot.DescendantNodesAndSelf().OfType<NamespaceDeclarationSyntax>();
            if (namespaces.Any())
            {
                // do not consider the namespace definition     
                var emptyNamespaces = namespaces.Where(n =>
                {
                    return n.ChildNodes().Where(x => !x.IsKind(SyntaxKind.QualifiedName) || !x.IsKind(SyntaxKind.IdentifierName) || !x.IsKind(SyntaxKind.EmptyStatement)).Count() <= 1;
                });

                if (emptyNamespaces.Any())
                    replacedRoot = replacedRoot.RemoveNodes(emptyNamespaces, SyntaxRemoveOptions.KeepNoTrivia);
            }

            document = document.WithSyntaxRoot(replacedRoot);
            document = await document.RemoveUnusedUsings(cancellationToken);

            // create new tree for a new file
            // copy all the usings, as we don't know which are required
            List<SyntaxNode> currentUsings = currentRoot.DescendantNodesAndSelf().Where(s => s is UsingDirectiveSyntax).ToList();

            CompilationUnitSyntax newTreeNode = SyntaxFactory.CompilationUnit()
                .WithUsings(SyntaxFactory.List(currentUsings.Select(i => (UsingDirectiveSyntax)i)))
                .WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(SyntaxFactory.NamespaceDeclaration(SyntaxFactory.IdentifierName(typeSymbol.ContainingNamespace.ToString()))))
                .WithoutLeadingTrivia()
                .NormalizeWhitespace();

            var members = newTreeNode.Members.Select(m =>
            {
                if (m is NamespaceDeclarationSyntax)
                    return ((NamespaceDeclarationSyntax)m).WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(node));
                return m;
            });

            if (!members.Any()) return document.Project.Solution;

            newTreeNode = newTreeNode.WithMembers(SyntaxFactory.List(members.ToList()));

            //move to new File
            Document newDocument = document.Project.AddDocument($"{node.Identifier.Text}.cs", SourceText.From(newTreeNode.ToFullString()), _context.Folders);

            // remove the unnecessory usings from the new document
            newDocument = await newDocument.RemoveUnusedUsings(cancellationToken);

            return newDocument.Project.Solution;
        }
    }
}
