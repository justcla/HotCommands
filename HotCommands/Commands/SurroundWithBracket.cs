using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using HotCommands.Utility;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using IServiceProvider = System.IServiceProvider;

namespace HotCommands
{
    internal sealed class SurroundWithBracket
    {
        private readonly Package package;

        public static SurroundWithBracket Instance { get; private set; }

        private IServiceProvider ServiceProvider => package;

        public static void Initialize(Package package)
        {
            Instance = new SurroundWithBracket(package);
            TemplateHelper.Init();
        }

        private SurroundWithBracket(Package package)
        {
            if (package == null)
                throw new ArgumentNullException(nameof(package));
            this.package = package;
        }

        public int HandleCommand(IWpfTextView textView, IClassifier classifier, IOleCommandTarget commandTarget, IEditorOperations editorOperations)
        {
            Guid cmdGroup = VSConstants.VSStd2K;
            EncloseMember(textView, commandTarget, cmdGroup, IsMethodOrProperty(textView));
            editorOperations.Tabify();
            return VSConstants.S_OK;
        }

        private void EncloseMember(IWpfTextView textView, IOleCommandTarget commandTarget, Guid cmdGroup, bool isMethodOrProperty)
        {
            MemberTemplate t = isMethodOrProperty ? MemberTemplate.METHOD : MemberTemplate.CLASS;
            var constructs = t.GetType().GetField(t.ToString()).GetCustomAttribute<TemplateDetailAttribute>().Constructs;
            IList<Construct> _c = TemplateHelper.ConstructList(constructs);

#pragma warning disable 1587
            /// TODO: 
            /// 1. based on 't', show the constructs (by using the list of Construct GUID present in the TemplateDetail)
            /// 2. show list of constructs & get the user input
#pragma warning restore 1587
            commandTarget.Exec(ref cmdGroup, (uint)VSConstants.VSStd2KCmdID.CUT, (uint)OLECMDEXECOPT.OLECMDEXECOPT_PROMPTUSER, IntPtr.Zero, IntPtr.Zero);

            string userInput = "7C6B761B-FC42-4F42-93AB-2263A9894986";   /// TODO: remove this later. its just a mock data for now.

            IList<CommandStep> steps = _c.First(x => x.Id.Equals(userInput)).CommandSteps;
            ProcessCommandSteps(textView, commandTarget, cmdGroup, steps);
        }

        private void ProcessCommandSteps(IWpfTextView textView, IOleCommandTarget commandTarget, Guid cmdGroup, IList<CommandStep> steps)
        {
            foreach (var s in steps)
            {
                if (s.StepType == CommandStepType.ADDCONTENT)
                {
                    commandTarget.Exec(ref cmdGroup, (uint)VSConstants.VSStd2KCmdID.PASTE, (uint)OLECMDEXECOPT.OLECMDEXECOPT_DONTPROMPTUSER, IntPtr.Zero, IntPtr.Zero);
                    commandTarget.Exec(ref cmdGroup, (uint)VSConstants.VSStd2KCmdID.RETURN, (uint)OLECMDEXECOPT.OLECMDEXECOPT_DONTPROMPTUSER, IntPtr.Zero, IntPtr.Zero);
                }
                else
                {
                    if (s.Begin != null)
                    {
                        InsertSpan(textView, SelectionStart(textView), s.Begin);
                        commandTarget.Exec(ref cmdGroup, (uint)VSConstants.VSStd2KCmdID.RETURN, (uint)OLECMDEXECOPT.OLECMDEXECOPT_DONTPROMPTUSER, IntPtr.Zero, IntPtr.Zero);
                    }
                    if (s.End != null)
                    {
                        InsertSpan(textView, SelectionEnd(textView), s.End);
                        commandTarget.Exec(ref cmdGroup, (uint)VSConstants.VSStd2KCmdID.RETURN, (uint)OLECMDEXECOPT.OLECMDEXECOPT_DONTPROMPTUSER, IntPtr.Zero, IntPtr.Zero);
                    }
                }
            }
        }

        private void InsertSpan(IWpfTextView textView, int position, string text)
        {
            using (ITextEdit edit = textView.TextBuffer.CreateEdit())
            {
                edit.Insert(position, text);
                edit.Apply();
            }
        }

        private bool IsMethodOrProperty(IWpfTextView textView)
        {
            return true;
            /// TODO: implementation pending
            throw new NotImplementedException();
        }

        private int SelectionStart(IWpfTextView textView)
        {
            foreach (SnapshotSpan snapshotSpan in textView.Selection.SelectedSpans)
            {
                SnapshotSpan spanToCheck = snapshotSpan.Length == 0 ? new SnapshotSpan(textView.TextSnapshot, textView.Caret.ContainingTextViewLine.Extent.Span) : snapshotSpan;
                return spanToCheck.Start.Position;
            }
            return 0;
        }

        private int SelectionEnd(IWpfTextView textView)
        {
            int pos = default(int);
            foreach (SnapshotSpan snapshotSpan in textView.Selection.SelectedSpans)
            {
                SnapshotSpan spanToCheck = snapshotSpan.Length == 0 ? new SnapshotSpan(textView.TextSnapshot, textView.Caret.ContainingTextViewLine.Extent.Span) : snapshotSpan;
                pos = spanToCheck.End.Position;
            }
            return pos;
        }
    }

    internal enum MemberTemplate
    {
        [TemplateDetail("7956D06F-EFAC-4C4A-911E-0D26A5AC0CE9", "C571B1BF-EEBC-42CA-A4EA-68541390DC54", "21C8DBAF-B938-41DF-B003-1F8274E70705", "9DF728D6-818D-495B-BDC8-61BEC2346CEA"
                        , "7849B603-C476-457F-B3EF-11BA188A7141", "3ADAD126-0C7D-4DD7-B320-4CA10D5CABE4", "7C6B761B-FC42-4F42-93AB-2263A9894986")]
        METHOD = 0,

        [TemplateDetail("11445A9D-FE39-421B-9F72-C85B6C36C454", "DE0CD972-40E5-4F63-BB3A-FDDF48F56471")]
        CLASS = 1,
    }

    [AttributeUsage(AttributeTargets.Enum | AttributeTargets.Field)]
    internal sealed class TemplateDetailAttribute : Attribute
    {
        internal IList<string> Constructs { get; private set; }

        internal TemplateDetailAttribute(params string[] constructIdentifiers)
        {
            Constructs = constructIdentifiers.ToList();
        }
    }

    internal sealed class TemplateHelper
    {
        private static List<Construct> _c = null;

        private TemplateHelper()
        {
        }

        internal static void Init()
        {
            if (_c == null)
                _c = new List<Construct>();

            foreach (var e in GetTemplateStream().Root.Elements())
                _c.Add(new Construct(e));
        }

        private static XDocument GetTemplateStream()
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("HotCommands.Resources.SurroundWithCommandTemplate.xml"))
            {
                if (stream == null) return null;

                using (StreamReader sr = new StreamReader(stream))
                {
                    return XDocument.Load(sr.BaseStream);
                }
            }
        }

        internal static IList<Construct> ConstructList(IEnumerable<string> g)
        {
            List<Construct> list = new List<Construct>();
            foreach (var x in _c)
            {
                if (g.Contains(x.Id)) list.Add(x);
            }
            return list;
        }
    }
}
