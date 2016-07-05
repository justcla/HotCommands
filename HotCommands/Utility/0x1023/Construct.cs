using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace HotCommands.Utility
{
    internal sealed class Construct
    {
        internal string DisplayText { get; private set; }

        internal string Id { get; private set; }

        internal IList<CommandStep> CommandSteps { get; private set; }

        private XElement _e = null;

        internal Construct(XElement e)
        {
            _e = e;
            CommandSteps = new List<CommandStep>();
            Init();
            LoadSteps();
        }

        private void Init()
        {
            DisplayText = _e.Attributes().First(x => x.Name.LocalName.Equals("display")).Value;
            Id = _e.Attributes().First(x => x.Name.LocalName.Equals("id")).Value;
        }

        private void LoadSteps()
        {
            foreach (XElement s in _e.Descendants(XName.Get("Step")))
            {
                string v = s.Attribute(XName.Get("add")).Value;
                if (v.Equals("[CONTENT]"))
                    CommandSteps.Add(new CommandStep { StepType = CommandStepType.ADDCONTENT });
                else if (v.StartsWith("::"))
                {
                    var _x = _e.Descendants(XName.Get("Item")).First(x =>
                    {
                        string _a = x.Attribute(XName.Get("index")).Value;
                        return ("::" + _a).Equals(v);
                    });

                    CommandSteps.Add(new CommandStep
                    {
                        StepType = CommandStepType.ADDCONSTRUCTEXT,
                        Begin = _x.Attribute(XName.Get("begin")).Value,
                        End = _x.Attribute(XName.Get("end")).Value
                    });
                }
                else if (v.Equals(":begin"))
                    CommandSteps.Add(new CommandStep { Begin = _e.Attribute(XName.Get("begin")).Value, StepType = CommandStepType.ADDCONSTRUCT });
                else
                    CommandSteps.Add(new CommandStep { End = _e.Attribute(XName.Get("end")).Value, StepType = CommandStepType.ADDCONSTRUCT });
            }
        }
    }


    internal sealed class CommandStep
    {
        internal string Begin { get; set; }

        internal string End { get; set; }

        internal CommandStepType StepType { get; set; }
    }

    internal enum CommandStepType
    {
        ADDCONSTRUCT,
        ADDCONSTRUCTEXT,
        ADDCONTENT
    }
}
