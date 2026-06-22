using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HFFS3CustomLauncher
{
    public class AnyFile : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        internal AnyFile(bool _, string _filename, DateTime _lastWriteTime)
        {
            IsErroneous = true;
            filename = _filename;
            lastWriteTime = _lastWriteTime;
        }

        internal AnyFile(string _filename, DateTime _lastWriteTime)
        {
            filename = _filename;
            lastWriteTime = _lastWriteTime;
        }

        private DateTime lastWriteTime;
        internal DateTime LastWriteTime
        {
            get { return lastWriteTime; }
            set { lastWriteTime = value; }
        }

        private string filename;
        public string Filename
        {
            get { return filename; }
            private set { filename = value; }
        }

        public virtual string PhysicalFilename
        {
            get { return Filename; }
        }

        public bool IsErroneous { get; protected set; } = false;
    }
}
