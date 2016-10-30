using Microsoft.CodeAnalysis.CodeRefactorings;

namespace HotCommands
{
    internal interface IActionContext
    {
        CodeRefactoringContext Context { get; set; }

        string Title { get; set; }
    }
}
