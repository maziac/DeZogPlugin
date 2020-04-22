using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;



namespace DeZogPlugin
{
    /**
     * This static class implements a simple logging functionality.
     * Use e.g.
     * ~~~
     * if(Log.Enabled)
     *     Log.Write("bytesRead={0}, MsgLength={1}", bytesRead, state.MsgLength);
     * ~~~
     *
     * Usage:
     * - if(Log.Enabled)  Log.Write(....
     *   For conditional logs. Logs will not appear if logging is not enabled.
     * - Log.Write(....
     *   For Logs that will always appear. E.g. for errors or exceptions.
     * - Log.ConsoleWrite(...
     *   For 'normal' user information. These logs will always be printed also on the console.
     *   This is for future, if I decide to log also into a file. Then the normal Log.Write
     *   would not be visible to the user.
     */
    public class Log
    {
        /// <summary>
        ///  Use to enable logging.
        /// </summary>
        static public bool Enabled = false;


        /**
         * Writes a formatted string.
         * E.g. use Log.Write("bytesRead={0}, MsgLength={1}", bytesRead, state.MsgLength);
         */
        static public void Write(string format, params object[] args)
        {
            string text = string.Format(format, args);
            Console.Write(text);
        }

        /**
         * Writes a formatted string and adds a newline.
         * E.g. use Log.Write("bytesRead={0}, MsgLength={1}", bytesRead, state.MsgLength);
         */
        static public void WriteLine(string format, params object[] args)
        {
            string text = string.Format(format, args);
            Console.WriteLine(text);
        }

        /**
         * Writes an empty line.
         */
        static public void WriteLine()
        {
            Console.WriteLine();
        }


        /// Same as above but make sure that output goes also to console.
        static public void ConsoleWrite(string format, params object[] args)
        {
            Log.Write(format, args);
        }
        static public void ConsoleWriteLine(string format, params object[] args)
        {
            Log.WriteLine(format, args);
        }
        static public void ConsoleWriteLine()
        {
            Log.WriteLine();
        }
    }

}
