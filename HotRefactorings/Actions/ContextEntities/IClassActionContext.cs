namespace HotCommands
{
    interface IClassActionContext : IActionContext
    {
        bool CreateNamespaceFolders { get; set; }

        string[] Folders { get; set; }
    }
}
