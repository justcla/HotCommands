using Microsoft.CodeAnalysis;

namespace ExtractClass.Actions
{
    internal interface IModifierActionContext : IActionContext
    {
        SyntaxToken OldModifier { get; set; }

        SyntaxToken NewModifier { get; set; }
    }
}
