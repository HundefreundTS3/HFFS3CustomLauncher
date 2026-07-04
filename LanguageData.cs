using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HFFS3CustomLauncher
{
    public class LanguageData : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private static DataStore ds = null;
        internal static DataStore DS
        {
            private get
            {
                return ds;
            }
            set
            {
                if (ds == null)
                {
                    ds = value;
                }
            }
        }

        internal LanguageData(LanguageCode code, string key)
        {
            Code = code;
            Key = key;
        }

        internal LanguageData(LanguageCode code, string key, byte completionState)
        {
            Code = code;
            Key = key;
            CompletionState = completionState;
        }

        internal void UpdateName()
        {
            OnPropertyChanged("Name");
        }

        public LanguageCode Code { get; private set; }
        public string Key { get; private set; }
        public string Name
        {
            get
            {
                return (CompletionState < 100 ? "(" + CompletionState.ToString() + "%) " : "") + DS.GetDynamicResource(Key);
            }
        }
        public byte CompletionState { get; private set; } = 100;
    }
}
