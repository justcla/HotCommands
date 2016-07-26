using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtractClass.RefactoringProviders
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp)]
    public sealed class NamespaceRefactoring : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync (CodeRefactoringContext context)
        {
            return;
        }
    }
}
