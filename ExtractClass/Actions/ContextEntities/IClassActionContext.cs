namespace ExtractClass.Actions
{
    interface IClassActionContext : IActionContext
    {
        bool CreateNamespaceFolders { get; set; }

        string[] Folders { get; set; }
    }
}
