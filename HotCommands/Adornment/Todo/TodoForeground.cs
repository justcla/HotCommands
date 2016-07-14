using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace HotCommands.Adornment
{
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "todo-foreground")]
    [Name("todo-foreground")]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    public sealed class TodoForeground : ClassificationFormatDefinition
    {
        public TodoForeground ()
        {
            ForegroundColor = Colors.MidnightBlue;
            IsItalic = true;
            IsBold = true;
        }
    }
}