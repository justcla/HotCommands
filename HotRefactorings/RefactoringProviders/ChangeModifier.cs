using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using System.Threading.Tasks;
using System.Composition;

namespace HotCommands
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(ChangeModifier)), Shared]
    public class ChangeModifier : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync (CodeRefactoringContext context)
        {
            var rootNode = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var node = rootNode.FindNode(context.Span) as BaseTypeDeclarationSyntax;

            // Check if it's a Class/Type
            if (node == null) return;
            // Skip if this is the main class (ie. has the same filename)
            //if (context.Document.Name.ToLowerInvariant() == $"{node.Identifier.ToString().ToLowerInvariant()}.cs") return;
            // Skip if nested class (Nested implementation needs work. Disabled for now.)
            if (node.IsNested()) return;

            // Activate if modifier is Public, Private or Internal
            if (!node.Modifiers.Any())
                return;

            var modifierCount = node.Modifiers.Count();

            if (modifierCount > 1 || !node.Modifiers.Any(SyntaxKind.PublicKeyword))
            {
                context.RegisterRefactoring(new ChangeModifierAction(new ChangeModifierContext
                {
                    Context = context,
                    Title = "To Public",
                    NewModifiers = new[] {SyntaxFactory.Token(SyntaxKind.PublicKeyword)}
                }));
            }

            if (modifierCount > 1 || !node.Modifiers.Any(SyntaxKind.ProtectedKeyword))
            {
                context.RegisterRefactoring(new ChangeModifierAction(new ChangeModifierContext
                {
                    Context = context,
                    Title = "To Protected",
                    NewModifiers = new[] { SyntaxFactory.Token(SyntaxKind.ProtectedKeyword) }
                }));
            }

            if (modifierCount > 1 || !node.Modifiers.Any(SyntaxKind.InternalKeyword))       // Consider: modifierCount != 0
            {
                context.RegisterRefactoring(new ChangeModifierAction(new ChangeModifierContext
                {
                    Context = context,
                    Title = "To Internal",
                    NewModifiers = new[] {SyntaxFactory.Token(SyntaxKind.InternalKeyword)}
                }));
            }

            if (modifierCount > 1 || !node.Modifiers.Any(SyntaxKind.PrivateKeyword))
            {
                context.RegisterRefactoring(new ChangeModifierAction(new ChangeModifierContext
                {
                    Context = context,
                    Title = "To Private",
                    NewModifiers = new[] {SyntaxFactory.Token(SyntaxKind.PrivateKeyword)}
                }));
            }

            if (modifierCount > 2 || !(node.Modifiers.Any(SyntaxKind.ProtectedKeyword) && node.Modifiers.Any(SyntaxKind.InternalKeyword)))
            {
                context.RegisterRefactoring(new ChangeModifierAction(new ChangeModifierContext
                {
                    Context = context,
                    Title = "To Protected Internal",
                    NewModifiers = new[] {SyntaxFactory.Token(SyntaxKind.ProtectedKeyword), SyntaxFactory.Token(SyntaxKind.InternalKeyword)}
                }));
            }
        }
    }
}
