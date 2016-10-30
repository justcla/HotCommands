using Microsoft.CodeAnalysis;

namespace ExtractClass.Actions
{
    internal interface IModifierActionContext : IActionContext
    {
        SyntaxToken[] NewModifiers { get; set; }
    }
}
