using ExtractClass.Actions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using System.Threading.Tasks;
using System.Composition;

namespace ExtractClass.RefactoringProviders
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(ExtractClassToAFile)), Shared]
    public class ChangeModifier : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync (CodeRefactoringContext context)
        {
            var rootNode = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var node = rootNode.FindNode(context.Span) as BaseTypeDeclarationSyntax;

            if (node == null || context.Document.Name.ToLowerInvariant() == $"{node.Identifier.ToString().ToLowerInvariant()}.cs") return;

            if (node.Modifiers.Any(SyntaxKind.PrivateKeyword) && !node.IsNested())
            {
                context.RegisterRefactoring(new ChangeModifierAction(new ChangeModifierContext
                {
                    Context = context,
                    Title = "To Public",
                    NewModifier = SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                    OldModifier = SyntaxFactory.Token(SyntaxKind.PrivateKeyword)
                }));

                context.RegisterRefactoring(new ChangeModifierAction(new ChangeModifierContext
                {
                    Context = context,
                    Title = "To Internal",
                    NewModifier = SyntaxFactory.Token(SyntaxKind.InternalKeyword),
                    OldModifier = SyntaxFactory.Token(SyntaxKind.PrivateKeyword)
                }));
            }
        }
    }
}
