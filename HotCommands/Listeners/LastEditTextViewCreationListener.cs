using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using System;
using System.ComponentModel.Composition;
#pragma warning disable 0649

namespace HotCommands
{
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    internal sealed class LastEditTextViewCreationListener : IVsTextViewCreationListener
    {
        [Import]
        private IVsEditorAdaptersFactoryService _editorAdaptersFactoryService { get; set; }

        [Import]
        private ITextDocumentFactoryService _textDocumentFactoryService;

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            var textView = _editorAdaptersFactoryService.GetWpfTextView(textViewAdapter);
            LastEditHandler lastEditHandler = new LastEditHandler(textView, _textDocumentFactoryService);
            textView.TextDataModel.DocumentBuffer.PostChanged += lastEditHandler.RecordLastEdit;
        }
    }

    public class LastEditHandler {

        private IWpfTextView _textView;
        private ITextDocumentFactoryService _textDocumentFactoryService;

        public LastEditHandler(IWpfTextView textView, ITextDocumentFactoryService textDocumentFactoryService)
        {
            _textView = textView;
            _textDocumentFactoryService = textDocumentFactoryService;
        }

        public void RecordLastEdit(object sender, EventArgs e)
        {
            // An edit has been made. Record the caret file and position.
            string filepath = GetCurrentFilePath();
            int position = GetCurrentCaretPosition();
            LastEdit.SetLastEdit(filepath, position);
        }

        private string GetCurrentFilePath()
        {
            ITextBuffer textBuffer = _textView.TextDataModel.DocumentBuffer;
            _textDocumentFactoryService.TryGetTextDocument(textBuffer, out ITextDocument textDocument);
            return textDocument.FilePath;
        }

        private int GetCurrentCaretPosition()
        {
            return _textView.Caret.Position.BufferPosition;
        }

    }

    public class LastEdit
    {
        public static string LastEditFile { get; private set; }
        public static int LastEditPosn { get; private set; }

        internal static void SetLastEdit(string filepath, int position)
        {
            LastEditFile = filepath;
            LastEditPosn = position;
        }
    }
}
