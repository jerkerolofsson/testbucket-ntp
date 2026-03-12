namespace TestBucket.Ntp.Core.UnitTests.Helpers
{
    internal class TempFolder : IDisposable
    {
        private string _path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp", Guid.NewGuid().ToString());

        public TempFolder()
        { 
            Directory.CreateDirectory(_path);
        }

        public string Path => _path;

        public void Dispose()
        {
            try
            {
                Directory.Delete(_path, true);
            }
            catch { }
        }
    }
}
