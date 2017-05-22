using HotCommands;
using Xunit;
using Microsoft.CodeAnalysis.CodeRefactorings;
using TestHelper;

namespace HotRefactorings.Tests
{
    public class ChangeModifierTests : CodeRefactoringVerifier
    {
        [Fact]
        public void PublicToProtectedTests()
        {
            var oldSource =
@"public class Class1
{
}";

            var newSource =
@"protected class Class1
{
}";
            VerifyRefactoring(oldSource, newSource, 0, "To Protected");
        }

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

        [Fact]
        public void PublicToPrivateTests()
        {
            var oldSource =
@"public class Class1
{
}";

            var newSource =
@"private class Class1
{
}";
            VerifyRefactoring(oldSource, newSource, 0, "To Private");
        }

        [Fact]
        public void PublicToProtectedInternalTests()
        {
            var oldSource =
@"public class Class1
{
}";

            var newSource =
@"protected internal class Class1
{
}";
            VerifyRefactoring(oldSource, newSource, 0, "To Protected Internal");
        }
        protected override CodeRefactoringProvider GetCodeRefactoringProvider()
        {
            return new ChangeModifier();
        }
    }
}