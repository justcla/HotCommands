using Microsoft.CodeAnalysis.CodeRefactorings;

namespace HotCommands
{
    class ExtractClassContext : IClassActionContext
    {
        public CodeRefactoringContext Context { get; set; }

        public bool CreateNamespaceFolders { get; set; }

        public string[] Folders { get; set; }

        public string Title { get; set; }
    }
}
