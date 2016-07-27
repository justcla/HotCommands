using ExtractClass.Actions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Composition;

namespace ExtractClass.RefactoringProviders
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp), Shared]
    public sealed class ExtractClassToAFile : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync (CodeRefactoringContext context)
        {
            SyntaxNode rootNode = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (rootNode == null) return;

            BaseTypeDeclarationSyntax node = rootNode.FindNode(context.Span) as BaseTypeDeclarationSyntax;
            if (node == null || node.Modifiers.Any(SyntaxKind.PrivateKeyword) || node.IsNested()) return;

            int classCount = rootNode.DescendantNodes().OfType<BaseTypeDeclarationSyntax>().Count();

            Document document = context.Document;
            Project project = document.Project;


            string traditionalNamespaceDeclararion = document.Folders.Any() ? $"{document.Project.AssemblyName}.{document.Folders.Join(".")}" :
                                                                 $"{document.Project.AssemblyName}{document.Folders.Join(".")}";

            string nodeNamespace = node.Namespace();

            string[] folders = document.Folders.ToArray();

            // make nested folders
            var nested = nodeNamespace.StartsWith($"{project.AssemblyName}", StringComparison.Ordinal) && nodeNamespace.NotEquals(traditionalNamespaceDeclararion);
            if (nested)
            {
                folders = nodeNamespace
                    .Substring(project.AssemblyName.Length)
                    .Split('.')
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToArray();
            }

            // find any existing document within the folder with the name same as namespace name
            Document anyExistingDocumentWithinNamespaceDirectory = project.FindDocument($"{node.Identifier.Text}.cs", document.Folders.ToArray());

            if (anyExistingDocumentWithinNamespaceDirectory == null && classCount > 1)
            {
                //extract class in the current document folder
                context.RegisterRefactoring(new ExtractClassAction(new ExtractClassContext
                {
                    Title = $"Extract {node.Identifier.Text} to {node.Identifier.Text}.cs",
                    Folders = document.Folders.ToArray(),
                    Context = context
                }));
            }

            if (anyExistingDocumentWithinNamespaceDirectory == null && classCount == 1)
            {
                //rename file
                context.RegisterRefactoring(new RenameFileAction(new ExtractClassContext
                {
                    Title = $"Rename file to {node.Identifier.Text}.cs",
                    Context = context
                }));
            }

            // find any existing document within the folder structures
            Document anyExistingDocument = project.FindDocument($"{node.Identifier.Text}.cs", folders);
            if (nested && anyExistingDocument == null && classCount > 1)
            {
                //extract class in a namespace based folder
                context.RegisterRefactoring(new ExtractClassAction(new ExtractClassContext
                {
                    Context = context,
                    Folders = folders,
                    Title = $"Extract {node.Identifier.Text} to {node.Identifier.Text}.cs with directory structure"
                }));
            }

            if (nested && anyExistingDocument == null && classCount == 1)
            {
                //move file under namespace folder & rename if necessary
                context.RegisterRefactoring(new MoveFileToFolderAction(new ExtractClassContext
                {
                    Context = context,
                    Folders = folders,
                    Title = $"Move {node.Identifier.Text}.cs under namespace folder"
                }));
            }
        }
    }
}
