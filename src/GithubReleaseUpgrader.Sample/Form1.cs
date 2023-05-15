using System.Threading;

namespace GithubReleaseUpgrader.Sample
{
    public partial class Form1 : Form
    {
        private SynchronizationContext _synchronizationContext;
        public Form1()
        {
            InitializeComponent();
            _synchronizationContext = SynchronizationContext.Current;
        }

        public void SetLog(string log)
        {
            _synchronizationContext.Send((c) =>
            {
                textBox1.Text = log;
            }, this);
        }
    }
}