using HotCommands;
using Xunit;
using Microsoft.CodeAnalysis.CodeRefactorings;
using TestHelper;

namespace HotRefactorings.Tests
{
    public class ChangeModifierTests : CodeRefactoringVerifier
    {
        [Theory]
        [InlineData("public", "protected", "To Protected")]
        [InlineData("public", "internal", "To Internal")]
        [InlineData("public", "private", "To Private")]
        [InlineData("public", "protected internal", "To Protected Internal")]
        public void FromToModifierTheory(string fromModifier, string toModifier, string refactoringTitle)
        {
            var oldSource = CreateClassWithModifier(fromModifier);
            var newSource = CreateClassWithModifier(toModifier);

            VerifyRefactoring(oldSource, newSource, 0, refactoringTitle);
        }

        private static string CreateClassWithModifier(string modifier)
        {
            return $@"{modifier} class Class1
{{
}}";
        }

        protected override CodeRefactoringProvider GetCodeRefactoringProvider()
        {
            return new ChangeModifier();
        }
    }
}