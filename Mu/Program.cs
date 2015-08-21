using System;

namespace Mu
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Globals.CommandLineArgs = args;
            if (System.Diagnostics.Debugger.IsAttached)
            {
                Game1 game = new Game1();
                game.Run();
            }
            else
            {
                try
                {
                    Game1 game = new Game1();
                    game.Run();
                }
                catch (Exception e)
                {
                    HandleException(e);
                }
            }
        }

        private static void HandleException(Exception e)
        {
            string err = e.ToString();
            ExceptionForm form = new ExceptionForm();
            form.ShowException(err);
            System.IO.File.WriteAllText("lasterror.txt", err);
        }
    }
}

