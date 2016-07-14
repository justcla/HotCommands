using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace HotCommands.Adornment
{
    public static class TypeExports
    {
        [Export(typeof(ClassificationTypeDefinition))]
        [Name("todo-foreground")]
        public static ClassificationTypeDefinition OrdinaryClassificationType;
    }
}