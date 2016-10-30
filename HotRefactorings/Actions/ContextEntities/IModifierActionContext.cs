using Microsoft.CodeAnalysis;

namespace HotCommands
{
    internal interface IModifierActionContext : IActionContext
    {
        SyntaxToken[] NewModifiers { get; set; }
    }
}
