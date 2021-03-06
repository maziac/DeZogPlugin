﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plugin;


namespace DeZogPlugin
{
    /**
     * The plugin implements a socket to communicate with [DeZog](https://github.com/maziac/DeZog).
     * The received commands are executed and control the CSpect debugger.
     */
    public class Main : iPlugin
    {

        public static string ProgramName;
        public static iCSpect CSpect;
        public static Settings Settings;


        /**
         * Initialization. Called by CSpect.
         * Returns a list with the ports to be registered.
         */
        public List<sIO> Init(iCSpect _CSpect)
        {
            string version = typeof(Main).Assembly.GetName().Version.ToString();
            ProgramName = typeof(Main).Assembly.GetName().Name;
            ProgramName += " v" + version;
            string dzrpVersion = Commands.GetDzrpVersion();
            Log.WriteLine("v{0} started. DZRP v{1}.", version, dzrpVersion);

            CSpect = _CSpect;

            // Read settings file (port)
            Settings = Settings.Load();
            Log.Enabled = Settings.LogEnabled;

 
            //Server.Listen(Settings.Port);
            CSpectSocket.Port = Settings.Port;
            CSpectSocket.StartListening();

            // No ports
            List<sIO> ports = new List<sIO>();
            return ports;
        }


        /**
         * Called by CSpect to quit the plugin.
         */
        public void Quit()
        {
            // If the program is stopped the socket is closed anyway.
            Log.WriteLine("Terminated.");
        }


        /**
         * Called every frame. I.e. interrupt.
         */
        public void Tick()
        {
            Commands.Tick();
        }
        

        /**
         * Writes a TX byte (_value).
         * The bytes are collected until enough (length) data has been received.
         * If no client is connected nothing happens, the byte is not cached.
         */
        public bool Write(eAccess _type, int _port, byte _value)
        {
            return true;
        }


        /**
         * Reads the state or reads a byte from the receive fifo.
         * _isvalid is set to true if the returned value could be provided.
         */
        public byte Read(eAccess _type, int _port, out bool _isvalid)
        {
            _isvalid = false;
            return 0;
        }
    }
}


