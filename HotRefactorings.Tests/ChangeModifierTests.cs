using HotCommands;
using Xunit;
using Microsoft.CodeAnalysis.CodeRefactorings;
using TestHelper;

namespace HotRefactorings.Tests
{
    public class ChangeModifierTests : CodeRefactoringVerifier
    {
        [Fact]
        public void PublicToInternalTests()
        {
            var oldSource =
@"public class Class1
{
}";

            var newSource =
@"internal class Class1
{
}";
            VerifyRefactoring(oldSource, newSource, 0, "To Internal");
        }

        protected override CodeRefactoringProvider GetCodeRefactoringProvider()
        {
            return new ChangeModifier();
        }
    }
}