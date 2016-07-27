using System;

namespace HotCommands
{
    public class Constants
    {
        public static readonly Guid HotCommandsGuid = new Guid("1023dc3d-550c-46b8-a3ec-c6b03431642c");
        public const uint DuplicateSelectionCmdId = 0x1019;
        public const uint DuplicateSelectionReverseCmdId = 0x1020;
        public const uint ToggleCommentCmdId = 0x1021;
        public const uint ExpandSelectionCmdId = 0x1022;
        public const uint ShrinkSelectionCmdId = 0x1023;
        public const uint FormatCodeCmdId = 0x1027;
        public const uint MoveMemberUpCmdId = 0x1031;
        public const uint MoveMemberDownCmdId = 0x1032;
        public const uint cmdidMoveCursorPrevMember = 0x1033;
        public const uint cmdidMoveCursorNextMember = 0x1034;
    }
}
