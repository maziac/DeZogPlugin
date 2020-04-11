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
            // Return registers
            CSpectSocket.SendResponse(new byte[] { 0x01 });

        }

        /**
         * Sets one double or single register.
         */
        public static void SetRegister()
        {
            // Return configuration
            CSpectSocket.SendResponse(new byte[] { 0x01 });

        }

        /**
         * Writes one memory bank.
         */
        public static void WriteBank()
        {
            // Return configuration
            CSpectSocket.SendResponse(new byte[] { 0x01 });

        }

        /**
         * Continues execution.
         */
        public static void Continue()
        {
            // Return configuration
            CSpectSocket.SendResponse(new byte[] { 0x01 });

        }

        /**
         * Pauses execution.
         */
        public static void Pause()
        {
            // Return configuration
            CSpectSocket.SendResponse(new byte[] { 0x01 });

        }

        /**
         * Adds a breakpoint.
         */
        public static void AddBreakpoint()
        {
            // Return configuration
            CSpectSocket.SendResponse(new byte[] { 0x01 });

        }

        /**
         * Removes a breakpoint.
         */
        public static void RemoveBreakpoint()
        {
            // Return configuration
            CSpectSocket.SendResponse(new byte[] { 0x01 });

        }

        /**
         * Adds a watchpoint.
         */
        public static void AddWatchpoint()
        {
            // Return configuration
            CSpectSocket.SendResponse(new byte[] { 0x01 });

        }

        /**
         * Removes a watchpoint.
         */
        public static void RemoveWatchpoint()
        {
            // Return configuration
            CSpectSocket.SendResponse(new byte[] { 0x01 });

        }

        /**
         * Reads a memory area.
         */
        public static void ReadMem()
        {
            // Return configuration
            CSpectSocket.SendResponse(new byte[] { 0x01 });

        }

        /**
         * Writes a memory area.
         */
        public static void WriteMem()
        {
            // Return configuration
            CSpectSocket.SendResponse(new byte[] { 0x01 });

        }

        /**
         * Returns the 8 slots.
         */
        public static void GetSlots()
        {
            // Return configuration
            CSpectSocket.SendResponse(new byte[] { 0x01 });

        }


    }
}
