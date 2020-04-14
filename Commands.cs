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
 *
 * CSpect:
 * Set/ClearBreakpoint: This increments/decrements a breakpoint counter.
 * One has to check with GetBreakpoint == 0 if the breakpoint is not active.
 * Normal breakpoints are "orange"m
 * Physical breakpoints are "red" in the CSpect UI.
 */


namespace DeZogPlugin
{
    /**
     * Handles the socket commands, responses and notifications.
     */
    public class Commands
    {
        /**
         * The break reason.
         */
        protected enum BreakReason
        {
            NO_REASON = 0,
            MANUAL_BREAK = 1,
            BREAKPOINT_HIT = 2,
            WATCHPOINT_READ = 3,
            WATCHPOINT_WRITE = 4,
        }


        // Index for setting a byte in the array.
        protected static int Index;

        // The data array for preparing data to send.
        protected static byte[] Data;

        // Temporary breakpoint (addresses) for Continue. -1 = unused.
        protected static int TmpBreakpoint1;
        protected static int TmpBreakpoint2;


        // The breakpoint map to keep the IDs and addresses.
        protected static Dictionary<ushort,ushort> BreakpointMap;

        // The last breakpoint ID used.
        protected static ushort LastBreakpointId;

        // Action queue.
        //protected static List<Action> ActionQueue = new List<Action>();

        // Stores the previous debugger state.
        protected static bool CpuRunning = false;

        /**
         * General initalization function.
         */
        public static void Init()
        {
            // ActionQueue = new List<Action>();
            BreakpointMap = new Dictionary<ushort, ushort>();
            LastBreakpointId = 0;
            TmpBreakpoint1 = -1;
            TmpBreakpoint2 = -1;
            CpuRunning = false;
            StartCpu(false);

            // Clear all breakpoints etc.
            var cspect = Main.CSpect;
            for (int addr = 0; addr < 0x10000; addr++)
            {
                while (cspect.Debugger(Plugin.eDebugCommand.GetBreakpoint, addr) != 0)
                    cspect.Debugger(Plugin.eDebugCommand.ClearBreakpoint, addr);
                while (cspect.Debugger(Plugin.eDebugCommand.GetReadBreakpoint, addr) != 0)
                    cspect.Debugger(Plugin.eDebugCommand.ClearReadBreakpoint, addr);
                while (cspect.Debugger(Plugin.eDebugCommand.GetBreakpoint, addr) != 0)
                    cspect.Debugger(Plugin.eDebugCommand.GetWriteBreakpoint, addr);
            }
            // Disable CSpect debugger screen
            //cspect.Debugger(Plugin.eDebugCommand.SetRemote, 1);
            //  cspect.Debugger(Plugin.eDebugCommand.Run);  // TODO
            /*
             * lock (ActionQueue)
            {
                ActionQueue.Add(() =>
                {
                    Main.CSpect.Debugger(Plugin.eDebugCommand.Run);
                });
            }
            */
        }


        /**
         * Start/stop debugger.
         */
        protected static void StartCpu(bool start)
        {
            CpuRunning = start;
            var cspect = Main.CSpect;
            if (start)
                cspect.Debugger(Plugin.eDebugCommand.Run);
            else
                cspect.Debugger(Plugin.eDebugCommand.Enter);
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
         * Called on every tick.
         */
        static int counter = 0;
        public static void Tick()
        {
            if (counter == 100)
            {
                Console.WriteLine("RUN");
                var csp = Main.CSpect;
                Main.CSpect.Debugger(Plugin.eDebugCommand.Run);
                Console.WriteLine("RUNdone " );
            }

            // Check if something to send
            /*
            int count = ActionQueue.Count;
            if (count > 0)
            {
                lock (ActionQueue)
                {
                    for (int i = 0; i < 0; i++)
                        ActionQueue[i].Invoke();
                    ActionQueue.Clear();
                }
            }
            */

            // Check if debugger state changed
            var cspect = Main.CSpect;
            var debugState = cspect.Debugger(Plugin.eDebugCommand.GetState);
            bool running = (debugState == 0);
            if(CpuRunning != running)
            {
                // State changed
                CpuRunning = running;
                if(CpuRunning==false)
                {
                    DebuggerStopped();
                }
            }
        }


        /**
         * Called when the debugger stopped.
         * E.g. because a breakpoint was hit.
         */
        protected static void DebuggerStopped()
        {
            // Disable temporary breakpoints
            var cspect = Main.CSpect;
            if (TmpBreakpoint1 >= 0)
                cspect.Debugger(Plugin.eDebugCommand.ClearBreakpoint, TmpBreakpoint1);
            if (TmpBreakpoint2 >= 0)
                cspect.Debugger(Plugin.eDebugCommand.ClearBreakpoint, TmpBreakpoint2);

            // Send break notification
            SendPauseNotification(BreakReason.MANUAL_BREAK, 0);
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
            ushort valueByte = (ushort)(value & 0xFF);
            // Get registers
            var cspect = Main.CSpect;
            var regs = cspect.GetRegs();
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

                case 14: regs.AF = (ushort)((regs.AF & 0xFF00) + valueByte); break;  // F
                case 15: regs.AF = (ushort)((regs.AF & 0xFF) + 256* valueByte); break;  // A
                case 16: regs.BC = (ushort)((regs.BC & 0xFF00) + valueByte); break;  // C
                case 17: regs.BC = (ushort)((regs.BC & 0xFF) + 256 * valueByte); break;  // B
                case 18: regs.DE = (ushort)((regs.DE & 0xFF00) + valueByte); break;  // E
                case 19: regs.DE = (ushort)((regs.DE & 0xFF) + 256 * valueByte); break;  // D
                case 20: regs.HL = (ushort)((regs.HL & 0xFF00) + valueByte); break;  // L
                case 21: regs.HL = (ushort)((regs.HL & 0xFF) + 256 * valueByte); break;  // H
                case 22: regs.IX = (ushort)((regs.IX & 0xFF00) + valueByte); break;  // IXL
                case 23: regs.IX = (ushort)((regs.IX & 0xFF) + 256 * valueByte); break;  // IXH
                case 24: regs.IY = (ushort)((regs.IY & 0xFF00) + valueByte); break;  // IYL
                case 25: regs.IY = (ushort)((regs.IY & 0xFF) + 256 * valueByte); break;  // IYH
                case 26: regs._AF = (ushort)((regs._AF & 0xFF00) + valueByte); break;  // F'
                case 27: regs._AF = (ushort)((regs._AF & 0xFF) + 256 * valueByte); break;  // A'
                case 28: regs._BC = (ushort)((regs._BC & 0xFF00) + valueByte); break;  // C'
                case 29: regs._BC = (ushort)((regs._BC & 0xFF) + 256 * valueByte); break;  // B'
                case 30: regs._DE = (ushort)((regs._DE & 0xFF00) + valueByte); break;  // E'
                case 31: regs._DE = (ushort)((regs._DE & 0xFF) + 256 * valueByte); break;  // D'
                case 32: regs._HL = (ushort)((regs._HL & 0xFF00) + valueByte); break;  // L'
                case 33: regs._HL = (ushort)((regs._HL & 0xFF) + 256 * valueByte); break;  // H'

                default:
                    // TODO: Error
                    break;
            }

            // Set register(s)
            cspect.SetRegs(regs);

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
            // TODO: ENABLE
            Console.WriteLine("WriteBank: bank={0}", bankNumber);
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
            // Set in CSpect (increment counter)
            Main.CSpect.Debugger(Plugin.eDebugCommand.SetBreakpoint, address);
            // Add to array (ID = element position + 1)
            BreakpointMap.Add(++LastBreakpointId, address);
            return LastBreakpointId;
        }


        /**
         * Removes a breakpoint.
         */
        protected static void DeleteBreakpoint(ushort bpId)
        {
            // Remove
            ushort address;
            if (BreakpointMap.TryGetValue(bpId, out address))
            {
                BreakpointMap.Remove(bpId);
                // Clear in CSpect (decrement counter)
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
            var cspect = Main.CSpect;
            TmpBreakpoint1 = -1;
            if (bp1Enable)
            {
                TmpBreakpoint1 = bp1Address;
                cspect.Debugger(Plugin.eDebugCommand.SetBreakpoint, TmpBreakpoint1);
            }
            TmpBreakpoint2 = -1;
            if (bp2Enable)
            {
                TmpBreakpoint2 = bp2Address;
                cspect.Debugger(Plugin.eDebugCommand.SetBreakpoint, TmpBreakpoint2);
            }

            // Run
            var regs = cspect.GetRegs();
            Console.WriteLine("Continue: Run debugger. pc=0x{0:X4}/{0}, bp1=0x{1:X4}/{1}, bp2=0x{2:X4}/{2}", regs.PC, TmpBreakpoint1, TmpBreakpoint2);
            Main.CSpect.Debugger(Plugin.eDebugCommand.Run);

            /*
            lock (ActionQueue)
            {
                ActionQueue.Add(() =>
                {
                    Main.CSpect.Debugger(Plugin.eDebugCommand.Run);
                });
            }
            */

            // Respond
            CSpectSocket.SendResponse();
        }


        /**
         * Pauses execution.
         */
        public static void Pause()
        {
            // Pause
            Console.WriteLine("Pause: Stop debugger.");
            Main.CSpect.Debugger(Plugin.eDebugCommand.Enter);
            /*
            lock (ActionQueue)
            {
               ActionQueue.Add(() =>
                {
                    Main.CSpect.Debugger(Plugin.eDebugCommand.Enter);
                });
            }
            */
            // Respond
            CSpectSocket.SendResponse();

        }


        /**
         * Adds a breakpoint.
         */
        public static void AddBreakpoint()
        {
            // Get breakpoint address
            ushort bpAddr = CSpectSocket.GetDataWord();

            // Set CSpect breakpoint
            ushort bpId = SetBreakpoint(bpAddr);

            // Respond
            InitData(2);
            SetDword(bpId);
            CSpectSocket.SendResponse(Data);

        }


        /**
         * Removes a breakpoint.
         */
        public static void RemoveBreakpoint()
        {
            // Get breakpoint ID
            ushort bpId = CSpectSocket.GetDataWord();
            // Remove breakpoint
            DeleteBreakpoint(bpId);
            // Respond
            CSpectSocket.SendResponse();

        }


        /**
         * Adds a watchpoint.
         */
        public static void AddWatchpoint()
        {
            // TODO: Need to check if it is correct that watchpoints have no ID in DZRP.
            // Respond
            CSpectSocket.SendResponse(new byte[] { 1 });
        }


        /**
         * Removes a watchpoint.
         */
        public static void RemoveWatchpoint()
        {
            // Respond
            CSpectSocket.SendResponse();
        }


        /**
         * Reads a memory area.
         */
        public static void ReadMem()
        {
            // Skip reserved
            CSpectSocket.GetDataByte();
            // Start of memory
            ushort address = CSpectSocket.GetDataWord();
            // Get size
            ushort size = CSpectSocket.GetDataWord();
            Console.WriteLine("address={0}, size={1}", address, size);

            // Respond
            InitData(size);
            var cspect = Main.CSpect;
            for (; size > 0; size--)
            {
                byte value = cspect.Peek(address++);
                SetByte(value);
            }
            CSpectSocket.SendResponse(Data);
        }


        /**
         * Writes a memory area.
         */
        public static void WriteMem()
        {
            // Skip reserved
            CSpectSocket.GetDataByte();
            // Start of memory
            ushort address = CSpectSocket.GetDataWord();
            // Get size
            var data = CSpectSocket.GetRemainingData();
            ushort size = (ushort)data.Count;

            // Write memory
            var cspect = Main.CSpect;
            for (int i=0; i<size; i++)
            {
                byte value = data[i];
                cspect.Poke(address++, value);
            }

            // Respond
            CSpectSocket.SendResponse();
        }


        /**
         * Returns the 8 slots.
         */
        public static void GetSlots()
        {
            // Read slots
            var cspect = Main.CSpect;
            for (int i = 0; i < 8; i++)
            {
                byte bank = cspect.GetNextRegister((byte)(0x50 + i));
                SetByte(bank);
            }
            // Respond
            CSpectSocket.SendResponse();
        }


        /**
         * Returns the state.
         */
        public static void ReadState()
        {
            // TODO: No CSpect interface yet.

            // Respond
            CSpectSocket.SendResponse();
        }


        /**
         * Writes the state.
         */
        public static void WriteState()
        {
            // TODO: No CSpect interface yet.

            // Respond
            CSpectSocket.SendResponse();
        }


        /**
         * Sends the pause notification.
         */
        protected static void SendPauseNotification(BreakReason reason, int bpId)
        {
            // Prepare data
            int length = 7;
            byte[] data =
            {
                // Length
                (byte)(length & 0xFF),
                (byte)((length >> 8) & 0xFF),
                (byte)((length >> 16) & 0xFF),
                (byte)(length >> 24),
                // SeqNo = 0
                0,
                // PAUSE
                (byte)DZRP_NTF.NTF_PAUSE,
                // Reason
                (byte)reason,
                // Breakpoint ID
                (byte)(bpId & 0xFF),
                (byte)((bpId >> 8) & 0xFF),
                // No string
                0
            };

            // Respond
            CSpectSocket.Send(data);
        }
    }
}
