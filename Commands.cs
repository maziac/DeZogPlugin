using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;


/*
 * The used socket protocol is simple. It consists of header and payload.
 *
 * Message:
 * int Length: The length of the following bytes containing Command. Little endian. Size=4.
 * byte SeqNo:  Sequence number (must be returned in response)
 * byte Command: UART_DATA.
 * Payload bytes: The data
 *
 * A client may connect at anytime.
 * A connection is terminated only by the client.
 * If a connection has been terminated a new connection can be established.
 */


namespace DeZogPlugin
{

    public class Commands
    {
        // Index for setting a byte in the array.
        protected static int Index;

        // The data array for preparing data to send.
        protected static byte[] Data;

        // Temporary breakpoint (IDs) for Continue. 0 = unused.
        protected static ushort TmpBreakpoint1;
        protected static ushort TmpBreakpoint2;


        // The breakpoint map to keep the IDs and addresses.
        protected static Dictionary<ushort,ushort> BreakPointMap;

        // The last breakpoint ID used.
        protected static ushort LastBreakPointId;



        /**
         * General initalization function.
         */
        public static void Init()
        {
            BreakPointMap = new Dictionary<ushort, ushort>();
            LastBreakPointId = 0;
            TmpBreakpoint1 = 0;
            TmpBreakpoint2 = 0;
            // Clear all breakpoints etc.
            var cspect = Main.CSpect;
            for (int addr = 0; addr < 0x10000; addr++)
            {
                cspect.Debugger(Plugin.eDebugCommand.ClearBreakpoint, addr);
                cspect.Debugger(Plugin.eDebugCommand.ClearReadBreakpoint, addr);
                cspect.Debugger(Plugin.eDebugCommand.ClearWriteBreakpoint, addr);
            }
        }


        /**
         * Initializes the data buffer.
         */
        protected static void InitData(int size)
        {
            Index = 0;
            Data = new byte[size];
        }


        /**
         * Sets a byte in the Data array.
         */
        protected static void SetByte(int value)
        {
            Data[Index++] = (byte) value;
        }


        /**
         * Sets a dword in the Data array.
         */
        protected static void SetDword(int value)
        {
            Data[Index++] = (byte)(value & 0xFF);
            Data[Index++] = (byte)(value>>8);
        }


        /**
         * Returns the configuration.
         */
        public static void GetConfig()
        {
            // Return configuration
            CSpectSocket.SendResponse(new byte[] { 0x01 });

        }

        /**
         * Returns the registers.
         */
        public static void GetRegisters()
        {
            // Get registers
            var regs = Main.CSpect.GetRegs();
            InitData(28);
            // Return registers
            SetDword(regs.PC);
            SetDword(regs.SP);
            SetDword(regs.AF);
            SetDword(regs.BC);
            SetDword(regs.DE);
            SetDword(regs.HL);
            SetDword(regs.IX);
            SetDword(regs.IY);
            SetDword(regs._AF);
            SetDword(regs._BC);
            SetDword(regs._DE);
            SetByte(regs.I);
            SetByte(regs.R);
            SetByte(regs.IM);
            SetByte(0);
            CSpectSocket.SendResponse(Data);
        }


        /**
         * Sets one double or single register.
         */
        public static void SetRegister()
        {
            // Get register number
            byte regNumber = CSpectSocket.GetDataByte();
            // Get new value
            ushort value = CSpectSocket.GetDataWord();
            // Get registers
            var regs = Main.CSpect.GetRegs();
            // Set a specific register
            switch (regNumber)
            {
                case 0: regs.PC = value; break;
                case 1: regs.SP = value; break;
                case 2: regs.AF = value; break;
                case 3: regs.BC = value; break;
                case 4: regs.DE = value; break;
                case 5: regs.HL = value; break;
                case 6: regs.IX = value; break;
                case 7: regs.IY = value; break;
                case 8: regs._AF = value; break;
                case 9: regs._BC = value; break;
                case 10: regs._DE = value; break;
                case 11: regs._HL = value; break;

                case 13: regs.IM = (byte)value; break;

                case 15: regs.AF = (ushort)((regs.AF & 0xFF00) + value); break;  // F
                case 16: regs.AF = (ushort)((regs.AF & 0xFF) + 256*value); break;  // A
                case 17: regs.BC = (ushort)((regs.AF & 0xFF00) + value); break;  // C
                case 18: regs.BC = (ushort)((regs.AF & 0xFF) + 256 * value); break;  // B
                case 19: regs.DE = (ushort)((regs.AF & 0xFF00) + value); break;  // E
                case 20: regs.DE = (ushort)((regs.AF & 0xFF) + 256 * value); break;  // D
                case 21: regs.HL = (ushort)((regs.AF & 0xFF00) + value); break;  // L
                case 22: regs.HL = (ushort)((regs.AF & 0xFF) + 256 * value); break;  // H
                case 23: regs.IX = (ushort)((regs.AF & 0xFF00) + value); break;  // IXL
                case 24: regs.IX = (ushort)((regs.AF & 0xFF) + 256 * value); break;  // IXH
                case 25: regs.IY = (ushort)((regs.AF & 0xFF00) + value); break;  // IYL
                case 26: regs.IY = (ushort)((regs.AF & 0xFF) + 256 * value); break;  // IYH
                case 27: regs._AF = (ushort)((regs.AF & 0xFF00) + value); break;  // F'
                case 28: regs._AF = (ushort)((regs.AF & 0xFF) + 256 * value); break;  // A'
                case 29: regs._BC = (ushort)((regs.AF & 0xFF00) + value); break;  // C'
                case 30: regs._BC = (ushort)((regs.AF & 0xFF) + 256 * value); break;  // B'
                case 31: regs._DE = (ushort)((regs.AF & 0xFF00) + value); break;  // E'
                case 32: regs._DE = (ushort)((regs.AF & 0xFF) + 256 * value); break;  // D'
                case 33: regs._HL = (ushort)((regs.AF & 0xFF00) + value); break;  // L'
                case 34: regs._HL = (ushort)((regs.AF & 0xFF) + 256 * value); break;  // H'

                default:
                    // TODO: Error
                    break;
            }
            // Respond
            CSpectSocket.SendResponse();

        }


        /**
         * Writes one memory bank.
         */
        public static void WriteBank()
        {
            // Get bank number
            byte bankNumber = CSpectSocket.GetDataByte();
            // Calculate physical address
            // Example: phys. address $1f021 = ($1f021&$1fff) for offset and ($1f021>>13) for bank.
            Int32 physAddress = bankNumber * 0x2000;
            // Write memory
            var cspect = Main.CSpect;
            for (int i=0x2000; i>0; i--)
            {
                byte value = CSpectSocket.GetDataByte();
                cspect.PokePhysical(physAddress++, value);
            }
            // Respond
            CSpectSocket.SendResponse();

        }



        /**
         * Sets a breakpoint and returns an ID (!=0).
         */
        protected static ushort SetBreakpoint(ushort address)
        {
            // Set in CSpect (if not already existing)
            if (!BreakPointMap.ContainsValue(address))
                Main.CSpect.Debugger(Plugin.eDebugCommand.SetBreakpoint, address);
            // Add to array (ID = element position + 1)
            BreakPointMap.Add(++LastBreakPointId, address);
            return LastBreakPointId;
        }


        /**
         * Removes a breakpoint.
         */
        protected static void RemoveBreakpoint(ushort bpId)
        {
            // Remove
            ushort address;
            if (BreakPointMap.TryGetValue(bpId, out address))
            {
                BreakPointMap.Remove(bpId);

                // Clear in CSpect (if no other breakpoint exists)
                if (!BreakPointMap.ContainsValue(address))
                    Main.CSpect.Debugger(Plugin.eDebugCommand.ClearBreakpoint, address);
            }
        }


        /**
         * Continues execution.
         */
        public static void Continue()
        {
            // Breakpoint 1
            bool bp1Enable = (CSpectSocket.GetDataByte() != 0);
            ushort bp1Address = CSpectSocket.GetDataWord();
            // Breakpoint 2
            bool bp2Enable = (CSpectSocket.GetDataByte() != 0);
            ushort bp2Address = CSpectSocket.GetDataWord();

            // Set temporary breakpoints
            TmpBreakpoint1 = (bp1Enable) ? SetBreakpoint(bp1Address) : (ushort)0;
            TmpBreakpoint2 = (bp1Enable) ? SetBreakpoint(bp2Address) : (ushort)0;

            // Run
            Main.CSpect.Debugger(Plugin.eDebugCommand.Run);

            // Respond
            CSpectSocket.SendResponse(new byte[] { 0x01 });
        }

        /**
         * Pauses execution.
         */
        public static void Pause()
        {
            // Respond
            CSpectSocket.SendResponse(new byte[] { 0x01 });

        }

        /**
         * Adds a breakpoint.
         */
        public static void AddBreakpoint()
        {
            // Respond
            CSpectSocket.SendResponse(new byte[] { 0x01 });

        }

        /**
         * Removes a breakpoint.
         */
        public static void RemoveBreakpoint()
        {
            // Respond
            CSpectSocket.SendResponse(new byte[] { 0x01 });

        }

        /**
         * Adds a watchpoint.
         */
        public static void AddWatchpoint()
        {
            // Respond
            CSpectSocket.SendResponse(new byte[] { 0x01 });

        }

        /**
         * Removes a watchpoint.
         */
        public static void RemoveWatchpoint()
        {
            // Respond
            CSpectSocket.SendResponse(new byte[] { 0x01 });

        }

        /**
         * Reads a memory area.
         */
        public static void ReadMem()
        {
            // Respond
            CSpectSocket.SendResponse(new byte[] { 0x01 });

        }

        /**
         * Writes a memory area.
         */
        public static void WriteMem()
        {
            // Respond
            CSpectSocket.SendResponse(new byte[] { 0x01 });

        }

        /**
         * Returns the 8 slots.
         */
        public static void GetSlots()
        {
            // Respond
            CSpectSocket.SendResponse(new byte[] { 0x01 });

        }


    }
}
