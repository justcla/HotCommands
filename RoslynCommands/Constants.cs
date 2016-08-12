using System;

namespace HotCommands
{
    public class Constants
    {
        public static readonly Guid HotCommandsGuid = new Guid("1023dc3d-550c-46b8-a3ec-c6b03431642c");
        public const uint ExpandSelectionCmdId = 0x1022;
        public const uint ShrinkSelectionCmdId = 0x1023;
        public const uint MoveMemberUpCmdId = 0x1031;
        public const uint MoveMemberDownCmdId = 0x1032;
        public const uint MoveCursorPrevMember = 0x1033;
        public const uint MoveCursorNextMember = 0x1034;
    }
}
