using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace ExtractClass.Actions
{
    internal sealed class ChangeModifierContext : IModifierActionContext
    {
        public CodeRefactoringContext Context { get; set; }

        public SyntaxToken NewModifier { get; set; }

        public SyntaxToken OldModifier { get; set; }

        public string Title { get; set; }
    }
}
