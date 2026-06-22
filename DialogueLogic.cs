namespace HFFS3CustomLauncher
{
    public class DialogueLogic
    {
        internal readonly DataStore DS;
        public bool IsWindowsXP { get; private set; } = false;
        public bool IsYesNoDialogue { get; private set; }
        public string Title { get; private set; }
        public string Description { get; private set; }
        public string OK { get; private set; }
        public string Yes { get; private set; }
        public string No { get; private set; }

        private DialogueLogic(DataStore ds)
        {
            DS = ds;
            IsWindowsXP = DS.IsWindowsXP;
            OK = ds.GetDynamicResource("OK");
            Yes = ds.GetDynamicResource("Yes");
            No = ds.GetDynamicResource("No");
        }

        internal DialogueLogic(DataStore ds, string titleKey, string descriptionKey) : this(ds)
        {
            IsYesNoDialogue = false;
            Title = ds.GetDynamicResource(titleKey);
            Description = ds.GetDynamicResource(descriptionKey);
        }

        internal DialogueLogic(DataStore ds, string titleKey, string descriptionKey, string[] args) : this(ds)
        {
            IsYesNoDialogue = false;
            Title = ds.GetDynamicResource(titleKey);
            Description = string.Format(ds.GetDynamicResource(descriptionKey), args);
        }

        internal DialogueLogic(DataStore ds, bool isYesNoDialogue, string titleKey, string descriptionKey) : this(ds)
        {
            IsYesNoDialogue = isYesNoDialogue;
            Title = ds.GetDynamicResource(titleKey);
            Description = ds.GetDynamicResource(descriptionKey);
        }

        internal DialogueLogic(DataStore ds, int errorCode) : this(ds)
        {
            IsYesNoDialogue = false;
            Title = string.Format(ds.GetDynamicResource("Error_Code"), errorCode);
            Description = ds.GetErrorCodeDescription(errorCode);
        }

        internal DialogueLogic(DataStore ds, int errorCode, string[] args) : this(ds)
        {
            IsYesNoDialogue = false;
            Title = string.Format(ds.GetDynamicResource("Error_Code"), errorCode);
            Description = ds.GetErrorCodeDescription(errorCode, args);
        }
    }
}
