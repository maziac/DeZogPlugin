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
        protected static Dictionary<ushort,ushort> BreakpointMap = null;

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
            var cspect = Main.CSpect;
            bool dbgVisible = Main.Settings.CSpectDebuggerVisible;
            Console.WriteLine("CSpectDebuggerVisible={0}", dbgVisible);
            cspect.Debugger(Plugin.eDebugCommand.SetRemote, (dbgVisible) ? 0 : 1);
            BreakpointMap = new Dictionary<ushort, ushort>();
            LastBreakpointId = 0;
            TmpBreakpoint1 = -1;
            TmpBreakpoint2 = -1;
            CpuRunning = false;
            StartCpu(false);

            // Clear all breakpoints etc.
            for (int addr = 0; addr < 0x10000; addr++)
            {
                cspect.Debugger(Plugin.eDebugCommand.ClearBreakpoint, addr);
                cspect.Debugger(Plugin.eDebugCommand.ClearReadBreakpoint, addr);
                cspect.Debugger(Plugin.eDebugCommand.GetWriteBreakpoint, addr);
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
         * Start/stop debugger.
         */
        protected static void StartCpu(bool start)
        {
            // Is required. Otherwise a stop could be missed because the tick is called only
            // every 20ms. If start/stop happens within this timeframe it would not be recognized.
            CpuRunning = start;
            // Start/stop
            var cspect = Main.CSpect;
            if (start)
            {
                // Run
                cspect.Debugger(Plugin.eDebugCommand.Run);
            }
            else
            {
                // Stop
                cspect.Debugger(Plugin.eDebugCommand.Enter);
            }
        }


        /**
         * Called on every tick.
         */
        public static void Tick()
        {
            // Return if not initialized
            if (BreakpointMap == null)
                return;

            // Check if debugger state changed
            var cspect = Main.CSpect;
            var debugState = cspect.Debugger(Plugin.eDebugCommand.GetState);
            bool running = (debugState == 0);
            if (CpuRunning != running)
            {
                // State changed
                CpuRunning = running;
                if (running == false)
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
            ////if(Main.Settings.LogEnabled)
          ////      Console.WriteLine("Debugger stopped");

            // Disable temporary breakpoints
            var cspect = Main.CSpect;
            if (TmpBreakpoint1 >= 0)
            {
                if(!BreakpointMap.ContainsValue((ushort)TmpBreakpoint1))
                   cspect.Debugger(Plugin.eDebugCommand.ClearBreakpoint, TmpBreakpoint1);
            }
            if (TmpBreakpoint2 >= 0)
            {
                if (!BreakpointMap.ContainsValue((ushort)TmpBreakpoint2))
                    cspect.Debugger(Plugin.eDebugCommand.ClearBreakpoint, TmpBreakpoint2);
            }

            // Guess break reason
            BreakReason reason = BreakReason.MANUAL_BREAK;
            var regs = cspect.GetRegs();
            var pc = regs.PC;
            if (BreakpointMap.ContainsValue(pc))
                reason = BreakReason.BREAKPOINT_HIT;

            // Note: Watchpoint reasons cannot be recognized.
            
            // Send break notification
            SendPauseNotification(reason, 0);

            // "Undefine" temporary breakpoints
            TmpBreakpoint1 = -1;
            TmpBreakpoint2 = -1;
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
            // Set in CSpect
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
                // Clear in CSpect (only if last breakpoint with that address)
                if(!BreakpointMap.ContainsValue(address))
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
            StartCpu(true);

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
            InitData(8);
            var cspect = Main.CSpect;
            for (int i = 0; i < 8; i++)
            {
                byte bank = cspect.GetNextRegister((byte)(0x50 + i));
                SetByte(bank);
            }
            // Respond
            CSpectSocket.SendResponse(Data);
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
            //CSpectSocket.SendResponse();
        }


        /**
         * Returns the value of one TBBlue register.
         */
        public static void GetTbblueReg()
        {
            // Get register
            byte reg = CSpectSocket.GetDataByte();
            // Get register value
            var cspect = Main.CSpect;
            cspect.GetNextRegister(reg);
            // Write register value
            InitData(1);
            byte value = cspect.GetNextRegister(reg);
            SetByte(value);
            // Respond
            CSpectSocket.SendResponse(Data);
        }


        /**
         * Returns the first or second sprites palette.
         */
        public static void GetSpritesPalette()
        {
            // Which palette
            int paletteIndex = CSpectSocket.GetDataByte() & 0x01;;

            // Prepare data
            InitData(2 * 256);

            // Store current values
            var cspect = Main.CSpect;
            byte eUlaCtrlReg = cspect.GetNextRegister(0x43);
            byte indexReg = cspect.GetNextRegister(0x40);
            // Bit 7: 0=first (8bit color), 1=second (9th bit color)
            byte machineReg = cspect.GetNextRegister(0x03);
            // Select sprites
            byte selSprites = (byte)((eUlaCtrlReg & 0x0F) | 0b1010_0000 | (paletteIndex << 6));
            cspect.SetNextRegister(0x43, selSprites); // Resets also 0x44
            // Set index to 0
            cspect.SetNextRegister(0x40, 0);
            // Read palette
            for (int i = 0; i < 256; i++)
            {
                byte colorMain = cspect.GetNextRegister(0x41);
                SetByte(colorMain);
                byte color9th = cspect.GetNextRegister(0x44);
                SetByte(color9th);
            }
            // Restore values
            cspect.SetNextRegister(0x40, indexReg);
            cspect.SetNextRegister(0x43, eUlaCtrlReg);
            if ((machineReg & 0x80) != 0)
            {
                // Bit 7 set, increase 0x44 index.
                // Get 8bit color
                byte col = cspect.GetNextRegister(0x41);
                // Write it to increase the index
                cspect.SetNextRegister(0x44, col);
            }

            // Respond
            CSpectSocket.SendResponse(Data);
        }


        /**
         * Returns the attributes of some sprites.
         */
        public static void GetSprites()
        {
            // Get index
            int index = CSpectSocket.GetDataByte();
            // Get count
            int count = CSpectSocket.GetDataByte();
            // Get sprite data
            InitData(5*count);
            var cspect = Main.CSpect;
            for (int i = 0; i < count; i++)
            {
                var sprite = cspect.GetSprite(index + i);
                SetByte(sprite.x);
                SetByte(sprite.y);
                SetByte(sprite.paloff_mirror_flip_rotate_xmsb);
                SetByte(sprite.visible_name);
                SetByte(sprite.H_N6_0_XX_YY_Y8);
            }
            // Respond
            CSpectSocket.SendResponse(Data);
        }


        /**
         * Returns some sprite patterns.
         */
        public static void GetSpritePatterns()
        {
            // Start of memory
            ushort index = CSpectSocket.GetDataByte();
            // Get size
            ushort count = CSpectSocket.GetDataByte();
            Console.WriteLine("Sprite pattern index={0}, count={1}", index, count);

            // Respond
            int address = index * 256;
            int size = count * 256;
            InitData(size);
            var cspect = Main.CSpect;
            for (; size > 0; size--)
            {
                byte value = cspect.PeekSprite(address++);
                SetByte(value);
            }
            CSpectSocket.SendResponse(Data);
        }


        /**
         * Returns the sprite clipping window.
         */
        public static void GetSpritesClipWindow()
        {
            // Get index (for restoration)
            var cspect = Main.CSpect;
            int prevIndex = cspect.GetNextRegister(0x1C);
            prevIndex = (prevIndex >> 2) & 0x03;
            // Get clip window
            cspect.SetNextRegister(0x1C, 0x02);
            byte[] clip = new byte[4];
            clip[0] = cspect.GetNextRegister(0x19); // xl
            clip[1] = cspect.GetNextRegister(0x19); // xr
            clip[2] = cspect.GetNextRegister(0x19); // yt
            clip[3] = cspect.GetNextRegister(0x19); // yb
            // Restore
            cspect.SetNextRegister(0x1C, 0x02); // reset
            for(int i=0; i<prevIndex;i++)
                cspect.SetNextRegister(0x19, clip[i]);   // Increase index
            // Respond
            CSpectSocket.SendResponse(clip);
        }


        /**
         * Sends the pause notification.
         */
        protected static void SendPauseNotification(BreakReason reason, int bpId)
        {
            // Prepare data
            int length = 6;
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
