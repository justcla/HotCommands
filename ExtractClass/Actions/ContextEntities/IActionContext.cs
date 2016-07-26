using Microsoft.CodeAnalysis.CodeRefactorings;

namespace ExtractClass.Actions
{
    internal interface IActionContext
    {
        CodeRefactoringContext Context { get; set; }

        string Title { get; set; }
    }
}
