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
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(ExtractClassToAFile)), Shared]
    public class ExtractClassToAFile : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync (CodeRefactoringContext context)
        {
            SyntaxNode rootNode = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (rootNode == null) return;

            // Check if the selected token is at the start of a class
            BaseTypeDeclarationSyntax node = rootNode.FindNode(context.Span) as BaseTypeDeclarationSyntax;
            if (node == null) return;
            // Don't apply the refactoring if the class is nested
            if (node.IsNested()) return;
            // Don't apply the refactoring if it's a Private class
            if (node.Modifiers.Any(SyntaxKind.PrivateKeyword)) return;

            int classCount = rootNode.DescendantNodes().OfType<BaseTypeDeclarationSyntax>().Count();

            Document document = context.Document;
            Project project = document.Project;

            string nodeNamespace = node.Namespace();

            // find any existing document within the folder with the name same as namespace name
            bool documentExistsInNamespaceDirectory = DocumentExistsInNamespaceDirectory(project, node, document);

            if (documentExistsInNamespaceDirectory)
            {
                if (classCount > 1)
                {
                    // Already existing file. (Probably this one!) Offer to extract to new file.
                    context.RegisterRefactoring(new ExtractClassAction(new ExtractClassContext
                    {
                        Title = $"Extract {node.Identifier.Text} to new file",
                        Folders = document.Folders.ToArray(),
                        Context = context
                    }));
                }
            }
            else //if (anyExistingDocumentWithinNamespaceDirectory == null)
            {
                if (classCount > 1)
                {
                    //extract class in the current document folder
                    context.RegisterRefactoring(new ExtractClassAction(new ExtractClassContext
                    {
                        Title = $"Extract {node.Identifier.Text} to {node.Identifier.Text}.cs",
                        Folders = document.Folders.ToArray(),
                        Context = context
                    }));
                }
                else if (classCount == 1)
                {
                    //rename file
                    context.RegisterRefactoring(new RenameFileAction(new ExtractClassContext
                    {
                        Title = $"Rename file to {node.Identifier.Text}.cs",
                        Context = context
                    }));
                }
            }

            string traditionalNamespaceDeclararion = document.Folders.Any() ? $"{document.Project.AssemblyName}.{document.Folders.Join(".")}" :
                $"{document.Project.AssemblyName}{document.Folders.Join(".")}";

            // make nested folders
            bool isNested = nodeNamespace.StartsWith($"{project.AssemblyName}", StringComparison.Ordinal)
                            && nodeNamespace.NotEquals(traditionalNamespaceDeclararion);
            if (isNested)
            {
                var folders = nodeNamespace.Substring(project.AssemblyName.Length).Split('.').Where(s => !string.IsNullOrEmpty(s)).ToArray();

                // find any existing document within the folder structures
                bool documentExistsInProject = DocumentExistsInProject(project, node, folders);

                if (documentExistsInProject)
                {
                    if (classCount > 1)
                    {
                        // already existing file
                        context.RegisterRefactoring(new ExtractClassAction(new ExtractClassContext
                        {
                            Title = $"Extract {node.Identifier.Text} to new file under namespace folder",
                            Folders = folders,
                            Context = context
                        }));
                    }
                }
                else // if (anyExistingDocument == null)
                {
                    if (classCount > 1)
                    {
                        //extract class in a namespace based folder
                        context.RegisterRefactoring(new ExtractClassAction(new ExtractClassContext
                        {
                            Context = context,
                            Folders = folders,
                            Title = $"Extract {node.Identifier.Text} to {node.Identifier.Text}.cs under namespace folder"
                        }));
                    }
                    else if (classCount == 1)
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

        private static bool DocumentExistsInNamespaceDirectory(Project project, BaseTypeDeclarationSyntax node, Document document)
        {
            return project.FindDocument($"{node.Identifier.Text}.cs", document.Folders.ToArray()) != null;
        }

        private static bool DocumentExistsInProject(Project project, BaseTypeDeclarationSyntax node, string[] folders)
        {
            return project.FindDocument($"{node.Identifier.Text}.cs", folders) != null;
        }
    }
}
