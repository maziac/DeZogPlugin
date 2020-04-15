using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plugin;


namespace DeZogPlugin
{
    /**
     * The plugin implements a socket to communitate with [DeZog](https://github.com/maziac/DeZog).
     * The received commands are executed and control the CSpect debugger.
     */
    public class Main : iPlugin
    {

        public static iCSpect CSpect;
        public static Settings Settings;


        /**
         * Initialization. Called by CSpect.
         * Returns a list with the ports to be registered.
         */
        public List<sIO> Init(iCSpect _CSpect)
        {
            Console.WriteLine("DeZog plugin started.");
            
            // Read settings file (port)
            Settings = Settings.Load();

            CSpectSocket.LogEnabled = Settings.LogEnabled;

            //Server.Listen(Settings.Port);
            CSpectSocket.Port = Settings.Port;
            CSpectSocket.StartListening();

            CSpect = _CSpect;

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
            Console.WriteLine("DeZog plugin terminated.");
        }


        /**
         * Called every frame. I.e. interrupt.
         */
        public void Tick()
        {
            /*
            InstructionCounter++;
            if(InstructionCounter % 50 == 0)
            {
                Console.WriteLine("Tick called, instruction {0}.", InstructionCounter);
                Z80Regs regs = CSpect.GetRegs();
                Console.WriteLine("  PC={0:X4}, SP={1:X4}, AF={2:X4}, BC={3:X4}, DE={4:X4}, HL={5:X4}, IX={6:X4}, IY={7:X4}", regs.PC, regs.SP, regs.AF, regs.BC, regs.DE, regs.HL, regs.IX, regs.IY);
            }
            */
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


