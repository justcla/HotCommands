using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HotCommands;
using Xunit;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace HotRefactorings.Tests
{
    public class ChangeModifierTests
    {
        [Fact]
        public async Task PublicToInternalTestsAsync()
        {
            var context = new CodeRefactoringContext();
            var sut = new ChangeModifier();

            await sut.ComputeRefactoringsAsync(context);
        }
    }
}