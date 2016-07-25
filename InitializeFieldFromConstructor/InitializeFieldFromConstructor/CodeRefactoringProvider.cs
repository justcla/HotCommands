using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace InitializeFieldFromConstructor
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(InitializeFieldFromConstructorCodeRefactoringProvider)), Shared]
    internal class InitializeFieldFromConstructorCodeRefactoringProvider : CodeRefactoringProvider
    {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // find the node at the selection.
            var node = root.FindNode(context.Span);

            // only offer a refactoring if the selected node is a field declaration.
            var variableDeclarator = node as VariableDeclaratorSyntax;
            var fieldDeclaration = variableDeclarator?.Ancestors().OfType<FieldDeclarationSyntax>().SingleOrDefault();
            var classDeclaration = fieldDeclaration?.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            if (classDeclaration == null)
            {
                // either not a field declaration, or not part of a class (invalid syntax)
                return;
            }

            if (GetConstructors(variableDeclarator).Count() < 2)
            {
                // create the code action
                var action = CodeAction.Create("Initialize field from constructor", 
                    c => InitializeFromConstructor(context.Document, fieldDeclaration, variableDeclarator, false, c));
                // register this code action.
                context.RegisterRefactoring(action);
            }
            else
            {
                // create the code action
                var action = CodeAction.Create("Initialize field from existing constructors",
                    c => InitializeFromConstructor(context.Document, fieldDeclaration, variableDeclarator, false, c));
                // register this code action.
                context.RegisterRefactoring(action);

                // create the code action
                action = CodeAction.Create("Initialize field from new constructor",
                    c => InitializeFromConstructor(context.Document, fieldDeclaration, variableDeclarator, true, c));
                // register this code action.
                context.RegisterRefactoring(action);
            }
        }

        private static IEnumerable<ConstructorDeclarationSyntax> GetConstructors(VariableDeclaratorSyntax fieldVariable)
        {
            var classDeclaration = fieldVariable.Ancestors().OfType<ClassDeclarationSyntax>().SingleOrDefault();

            if (classDeclaration == null)
            {
                return new List<ConstructorDeclarationSyntax>();
            }

            var constructors = classDeclaration.DescendantNodes().OfType<ConstructorDeclarationSyntax>();

            var relevantConstructors = constructors.Where(x =>
                x.Body
                .DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .All(identifier => identifier.Identifier.Text != fieldVariable.Identifier.Text));

            return relevantConstructors;
        }

        private static async Task<Document> InitializeFromConstructor(Document document, FieldDeclarationSyntax fieldDeclaration, VariableDeclaratorSyntax fieldVariable, bool alwaysCreateNew, CancellationToken cancellationToken)
        {
            var classDeclaration = fieldDeclaration.Ancestors().OfType<ClassDeclarationSyntax>().Single();

            var parameterType = fieldDeclaration.Declaration.Type.ToString();
            var identifierToken = fieldVariable.Identifier;
            var parameterName = identifierToken.Text.TrimStart('_');

            // create the new parameter
            var parameter = Parameter(
                Identifier(parameterName))
                .WithType(IdentifierName(parameterType)
            );

            // create the new assignment to add to the constructor body
            var assignment = ExpressionStatement(
                AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, 
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ThisExpression(), IdentifierName(identifierToken)), 
                    IdentifierName(parameterName)));

            // get the syntax root
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // get existing constructor if any
            var existingConstructors = GetConstructors(fieldVariable);

            // update the class by either updating the constructor, or adding one
            SyntaxNode updatedClassDecl = classDeclaration;
            if (!existingConstructors.Any() || alwaysCreateNew)
            {
                var constructor = ConstructorDeclaration(classDeclaration.Identifier.Text)
                    .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                    .WithParameterList(ParameterList(
                        SingletonSeparatedList(parameter)
                    ))
                    .WithBody(Block(assignment))
                    .WithLeadingTrivia(fieldVariable.GetLeadingTrivia().Insert(0, CarriageReturnLineFeed));

                var insertionPoint = (SyntaxNode)existingConstructors.LastOrDefault() ?? fieldDeclaration;

                updatedClassDecl = classDeclaration.InsertNodesAfter(insertionPoint, new[] { constructor });

            }
            else
            {
                updatedClassDecl = classDeclaration.ReplaceNodes(existingConstructors, 
                    (constructor, updatedConstructor) => constructor
                        .AddParameterListParameters(parameter)
                        .AddBodyStatements(assignment));
            }

            // replace the root node with the updated class
            var newRoot = root.ReplaceNode(classDeclaration, updatedClassDecl);

            return document.WithSyntaxRoot(newRoot);
        }

        /*
            // Get the symbol representing the type to be renamed.
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var typeSymbol = semanticModel.GetDeclaredSymbol(typeDecl, cancellationToken);

            var originalSolution = document.Project.Solution;
            var optionSet = originalSolution.Workspace.Options;
            //var newSolution = await Renamer.RenameSymbolAsync(document.Project.Solution, typeSymbol, newName, optionSet, cancellationToken).ConfigureAwait(false);
         */

    }
}