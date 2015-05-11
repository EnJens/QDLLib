using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using log4net;
using log4net.Repository.Hierarchy;
using log4net.Appender;
using log4net.Layout;
using log4net.Core;
using System.IO;

namespace QDLNet
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ConfigureLogging();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        private static void ConfigureLogging()
        {
            Logger rootLog = ((Hierarchy)LogManager.GetRepository()).Root;
            var logName = String.Format("migration-{0}.log", DateTime.Now.ToString("yyyy-MM-dd-hh-mm"));
            var fileName = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), logName);
            var layout = new PatternLayout("%d %-5p %c [%x] - %m%n");
            var maxLevel = Level.Info;
#if DEBUG
            maxLevel = Level.All;
#endif
            var mainlogAppender = new FileAppender
            {
                AppendToFile = false,
                Layout = layout,
                File = fileName,
                ImmediateFlush = true,
                Threshold = maxLevel,
            };
            mainlogAppender.ActivateOptions();
            rootLog.AddAppender(mainlogAppender);
            rootLog.Repository.Configured = true;
        }
    }
}
