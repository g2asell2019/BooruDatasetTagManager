using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using Newtonsoft.Json;
using System.Threading;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;

namespace BooruDatasetTagManager
{
    static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            //tools = new TextTool(Application.StartupPath);
            if (!File.Exists("settings.json"))
            {
                Settings = new AppSettings();
                File.WriteAllText("settings.json", JsonConvert.SerializeObject(Settings));
            }
            else
            {
                Settings = JsonConvert.DeserializeObject<AppSettings>(File.ReadAllText("settings.json"));
            }

            TagsList = new TagsDB();
            
            string tagsDir = Path.Combine(Application.StartupPath, "tags");
            string tagFile = Path.Combine(tagsDir, "list.tag");
            if (File.Exists(tagFile))
            {
                TagsList.LoadFromTagFile(tagFile, false);
            }
            else
            {
                if (Directory.Exists(tagsDir))
                {
                    string[] csvFiles = Directory.GetFiles(tagsDir, "*.csv");
                    foreach (var item in csvFiles)
                    {
                        TagsList.LoadFromCSVFile(item, true);
                    }
                    List<string> temp = new List<string>(TagsList.Tags.Cast<string>());
                    temp.Sort();
                    TagsList.Tags.Clear();
                    TagsList.Tags.AddRange(temp.ToArray());
                    TagsList.SaveTags(tagFile);
                }
            }
            Application.ThreadException += new ThreadExceptionEventHandler(CustomExceptionHandler.OnThreadException);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            AppDomain.CurrentDomain.UnhandledException +=
        new UnhandledExceptionEventHandler(CustomExceptionHandler.CurrentDomain_UnhandledException);
            
            Application.Run(new Form1());
        }

        internal class CustomExceptionHandler
        {
            // Handle the exception event
            public static void OnThreadException(object sender, ThreadExceptionEventArgs t)
            {
                DialogResult result = ShowThreadExceptionDialog(t.Exception);

                // Exit the program when the user clicks Abort.
                if (result == DialogResult.Abort)
                    Application.Exit();
            }

            // Create and display the error message.
            private static DialogResult ShowThreadExceptionDialog(Exception e)
            {
                string errorMsg = "An error occurred.  Please contact the adminstrator " +
                     "with the following information:\n\n";
                errorMsg += string.Format("Exception Type: {0}\n\n", e.GetType().Name);
                errorMsg += "\n\nStack Trace:\n" + e.StackTrace;
                return MessageBox.Show(errorMsg, "Application Error",
                     MessageBoxButtons.AbortRetryIgnore, MessageBoxIcon.Stop);
            }

            public static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
            {
                try
                {
                    Exception ex = (Exception)e.ExceptionObject;
                    string errorMsg = "An application error occurred. Please contact the adminstrator " +
                        "with the following information:\n\n";

                    // Since we can't prevent the app from terminating, log this to the event log.
                    if (!EventLog.SourceExists("ThreadException"))
                    {
                        EventLog.CreateEventSource("ThreadException", "Application");
                    }

                    // Create an EventLog instance and assign its source.
                    EventLog myLog = new EventLog();
                    myLog.Source = "ThreadException";
                    myLog.WriteEntry(errorMsg + ex.Message + "\n\nStack Trace:\n" + ex.StackTrace);
                }
                catch (Exception exc)
                {
                    try
                    {
                        MessageBox.Show("Fatal Non-UI Error",
                            "Fatal Non-UI Error. Could not write the error to the event log. Reason: "
                            + exc.Message, MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    }
                    finally
                    {
                        Application.Exit();
                    }
                }
            }

            // Creates the error message and displays it.
            private static DialogResult ShowThreadExceptionDialog(string title, Exception e)
            {
                string errorMsg = "An application error occurred. Please contact the adminstrator " +
                    "with the following information:\n\n";
                errorMsg = errorMsg + e.Message + "\n\nStack Trace:\n" + e.StackTrace;
                return MessageBox.Show(errorMsg, title, MessageBoxButtons.AbortRetryIgnore,
                    MessageBoxIcon.Stop);
            }
        }


        public static DatasetManager DataManager;

        //public static TextTool tools;

        public static AppSettings Settings;

        public static TagsDB TagsList;
    }
}
