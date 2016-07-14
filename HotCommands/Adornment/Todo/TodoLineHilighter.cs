using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace HotCommands.Adornment
{
    [Export(typeof(IViewTaggerProvider))]
    [ContentType("any")]
    [TagType(typeof(ClassificationTag))]
    public sealed class TodoLineHilighter : IViewTaggerProvider
    {
        [Import]
        public IClassificationTypeRegistryService Registry;

        [Import]
        internal ITextSearchService TextSearchService { get; set; }

        public ITagger<T> CreateTagger<T> (ITextView textView, ITextBuffer buffer) where T : ITag
        {
            if (buffer != textView.TextBuffer)
                return null;

            var classType = Registry.GetClassificationType("todo-foreground");
            return new TodoClassificationTagger(textView, TextSearchService, classType) as ITagger<T>;
        }
    }
}
