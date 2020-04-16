using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;




namespace DeZogPlugin
{

    // The command enums.    
    public enum DZRP {
        CMD_GET_CONFIG=1,
	    CMD_GET_REGISTERS=2,
	    CMD_SET_REGISTER=3,
	    CMD_WRITE_BANK=4,
	    CMD_CONTINUE=5,
	    CMD_PAUSE=6,

	    CMD_ADD_BREAKPOINT=7,
	    CMD_REMOVE_BREAKPOINT=8,

	    CMD_ADD_WATCHPOINT=9,
	    CMD_REMOVE_WATCHPOINT=0xA,

	    CMD_READ_MEM=0xB,
	    CMD_WRITE_MEM=0xC,

        CMD_GET_SLOTS = 0xD,

        CMD_READ_STATE = 0xE,
        CMD_WRITE_STATE = 0xF,

        CMD_GET_TBBLUE_REG = 0x10,

        CMD_GET_SPRITES_PALETTE = 0x11,
        CMD_GET_SPRITES = 0x12,
        CMD_GET_SPRITE_PATTERNS = 0x13,
        CMD_GET_SPRITE_CLIP_WINDOW = 0x14,
    }


    // The notifications
    public enum DZRP_NTF
    {
        NTF_PAUSE = 1
    }


    // State object for reading client data asynchronously  
    public class StateObject
    {
        // Client  socket.  
        public Socket workSocket = null;
        // Size of receive buffer.  
        public const int BufferSize = 1024;
        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];
        // Received data string.  
        public StringBuilder sb = new StringBuilder();
        // A message is collected into this list until it is complete.
        public List<byte> Data = new List<byte>();
        // The length of the currently received message.
        public int MsgLength = 0;
        // Set if some communication error occurred.
        public bool error = false;
    }


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
    public class CSpectSocket
    {
        // The used port
        public static int Port;

        // The received data.
        protected static List<byte> DzrpData = new List<byte>();

        // The connected client.
        protected static StateObject socket = null;

        // Constants for the header parameters.
        protected const int HEADER_LEN_LENGTH = 4;
        protected const int HEADER_CMD_SEQNO_LENGTH = 2;

        // Stores the received sequence number.
        protected static byte receveivedSeqno = 0;

        // Set to true to enable logging.
        public static bool LogEnabled;


        /**
         * Call this to start listiening on 'Port'.
         * Is asynchronous, i.e. not blocking.
         */
        public static void StartListening()
        {
            socket = null;

            // Establish the local endpoint for the socket.  
            IPAddress ipAddress = IPAddress.Loopback;   // localhost
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, Port);

            // Create a TCP/IP socket.  
            Socket listener = new Socket(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and listen for incoming connections.  
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(1);

                Console.WriteLine("DeZog plugin: Waiting for a connection on port {0} (localhost)...", Port);

                // Start an asynchronous socket to listen for connections.  
                listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }


        /**
         * A client has connected.
         */
        protected static void AcceptCallback(IAsyncResult ar)
        {
            // Init
            DzrpData = new List<byte>();
            receveivedSeqno = 0;
            Commands.Init();

            // Get the socket that handles the client request.  
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);
            listener.Close();

            // Create the state object.  
            StateObject state = new StateObject();
            state.workSocket = handler;
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
            CSpectSocket.socket = state;

            Console.WriteLine("Connected.");
        }


        /**
         * Data from the client has been received
         * (or the connection was closed).
         */
        public static void ReadCallback(IAsyncResult ar)
        {
            // from the asynchronous state object.  
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

            try
            {
                // Retrieve the state object and the handler socket  
               if (receveivedSeqno != 0)
                {
                    // If this happens a response has not been sent for the previous message.
                    throw new Exception("Message received before command last response was sent.");
                }

                // Read data from the client socket.   
                int bytesRead = handler.EndReceive(ar);
                Console.WriteLine("bytesRead={0}, MsgLength={1}", bytesRead, state.MsgLength);
                if (bytesRead <= 0)
                {
                    // Disconnected
                    Console.WriteLine("Disconnected.");
                    // Restart listener
                    StartListening();
                    return;
                }

                if (state.error)
                {
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
                    return; // Don't receive anything until connection close
                }

                // Add data
                List<byte> readData = new List<byte>(state.buffer);
                Console.WriteLine("Data before: " + GetStringFromData(state.Data.ToArray()));
                Console.WriteLine("Added data:  " + GetStringFromData(readData.ToArray(), 0, bytesRead));
                state.Data.AddRange(readData.GetRange(0, bytesRead));

                // Check if header was already previously received.
                int len = state.Data.Count;
                Console.WriteLine("Len={0}", len);
                while (len > 0)
                {
                    if (state.MsgLength == 0)
                    {
                        // Check if header is complete
                        if (len >= HEADER_LEN_LENGTH)
                        {
                            // Header received -> Decode length
                            int length = state.Data[0];
                            length += state.Data[1] << 8;
                            length += state.Data[2] << 16;
                            length += state.Data[3] << 24;
                            if (length < HEADER_CMD_SEQNO_LENGTH)
                            {
                                // Wrong length detected
                                state.error = true;
                                Console.WriteLine("Length too short ({0}). Stopping communication. Please reconnect.", length);
                                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
                                return;
                            }
                            Console.WriteLine("Received Length={0}", length);
                            state.MsgLength = length;
                        }
                    }

                    int totalLength = HEADER_LEN_LENGTH + state.MsgLength;
                    //Console.WriteLine("state.MsgLength={0}, totalLength={1}", state.MsgLength, totalLength);
                    if (len < totalLength)
                        break;

                    // Message completely received.
                    ParseMessage(handler, state.Data);
                    // Next
                    state.MsgLength = 0;
                    Console.WriteLine("Count={0}, totallength={1}", state.Data.Count, totalLength);
                    //for (int i = 0; i < state.Data.Count; i++)
                    //    Console.WriteLine("  Data[{0}]={1}", i, state.Data[i]);
                    state.Data.RemoveRange(0, totalLength);
                    Console.WriteLine("End of message, Data.Count={0}", state.Data.Count);

                    // Next
                    len -= totalLength;
                }

                // Receive the next data.
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
            }
            catch (Exception e)
            {
                Console.WriteLine("{0}", e);
                HandleError(e.Message, handler);
            }
        }


        /**
         * One complete message from the client has been received.
         * The message is interpreted.
         */
        protected static void ParseMessage(Socket socket, List<byte> data)
        {
            Console.WriteLine("ParseMessage");
            if (LogEnabled)
                WriteCmd(data.ToArray());
            Console.WriteLine("data.Count={0}", data.Count);

            DzrpData = new List<byte>();
            int startIndex = HEADER_LEN_LENGTH + HEADER_CMD_SEQNO_LENGTH;
            DzrpData.AddRange(data.GetRange(startIndex, data.Count - startIndex));
            receveivedSeqno = data[HEADER_LEN_LENGTH];

            // Interprete
            DZRP command = (DZRP)data[HEADER_LEN_LENGTH + 1];
            switch (command)
            {
                case DZRP.CMD_GET_CONFIG:
                    Commands.GetConfig();
                    break;

                case DZRP.CMD_GET_REGISTERS:
                    Commands.GetRegisters();
                    break;

                case DZRP.CMD_SET_REGISTER:
                    Commands.SetRegister();
                    break;

                case DZRP.CMD_WRITE_BANK:
                    Commands.WriteBank();
                    break;

                case DZRP.CMD_CONTINUE:
                    Commands.Continue();
                    break;

                case DZRP.CMD_PAUSE:
                    Commands.Pause();
                    break;

                case DZRP.CMD_ADD_BREAKPOINT:
                    Commands.AddBreakpoint();
                    break;

                case DZRP.CMD_REMOVE_BREAKPOINT:
                    Commands.RemoveBreakpoint();
                    break;

                case DZRP.CMD_ADD_WATCHPOINT:
                    Commands.AddWatchpoint();
                    break;

                case DZRP.CMD_REMOVE_WATCHPOINT:
                    Commands.RemoveWatchpoint();
                    break;

                case DZRP.CMD_READ_MEM:
                    Commands.ReadMem();
                    break;

                case DZRP.CMD_WRITE_MEM:
                    Commands.WriteMem();
                    break;

                case DZRP.CMD_GET_SLOTS:
                    Commands.GetSlots();
                    break;

                case DZRP.CMD_READ_STATE:
                    Commands.ReadState();
                    break;

                case DZRP.CMD_WRITE_STATE:
                    Commands.WriteState();
                    break;


                case DZRP.CMD_GET_TBBLUE_REG:
                    Commands.GetTbblueReg();
                    break;


                case DZRP.CMD_GET_SPRITES_PALETTE:
                    Commands.GetSpritesPalette();
                    break;

                case DZRP.CMD_GET_SPRITES:
                    Commands.GetSprites();
                    break;

                case DZRP.CMD_GET_SPRITE_PATTERNS:
                    Commands.GetSpritePatterns();
                    break;

                case DZRP.CMD_GET_SPRITE_CLIP_WINDOW:
                    Commands.GetSpritesClipWindow();
                    break;

                default:
                    throw new Exception("Unexpected command: " + command.ToString());
            }

        }


        /**
         * Prints an error text and disconnects.
         */
        protected static void HandleError(string text, Socket socket=null)
        {

            Console.WriteLine("Error: {0}", text);
            if (socket != null)
            {
                Console.WriteLine("Disconnecting...");
                socket.Shutdown(SocketShutdown.Both);
                // Restart listener
                StartListening();
            }
        }


        /**
         * Used to retrieve one element from the buffer.
         * Returns the data or -1 if no data available.
         */
        public static byte GetDataByte()
        {
            // Check if daa available
            int count = DzrpData.Count;
            if (count == 0)
                throw new Exception("GetDataByte: no data");
            // Get value
            byte value = CSpectSocket.DzrpData[0];
            // Remove it from fifo
            CSpectSocket.DzrpData.RemoveAt(0);
            //Console.WriteLine("GetDataByte: Data.Count={0}", DzrpData.Count);
            // Return
            return value;
        }


        /**
         * Used to retrieve 2 elements (a word) from the buffer.
         * Returns the data or -1 if no data available.
         */
        public static ushort GetDataWord()
        {
            // Check if data available
            int count = DzrpData.Count;
            if (count < 2)
                throw new Exception("GetDataWord: no data");
            // Get value
            ushort value = (ushort)(CSpectSocket.DzrpData[0] + 256 * CSpectSocket.DzrpData[1]);
            // Remove it from fifo
            DzrpData.RemoveAt(0);
            DzrpData.RemoveAt(0);
            Console.WriteLine("GetDataWord: Data.Count={0}", DzrpData.Count);
            // Return
            return value;
        }


        /**
          * Returns the data buffer.
          */
        public static List<byte> GetRemainingData()
        {
            // Return
            return DzrpData;
        }


        /**
         * Sends the response.
         */
        public static void SendResponse(byte[] byteData=null)
        {
            // Length
            int length = (byteData != null) ? byteData.Length : 0;
            var wrapBuffer = new byte[length + HEADER_LEN_LENGTH + 1];
            wrapBuffer[0] = (byte)(length & 0xFF);
            wrapBuffer[1] = (byte)((length >> 8) & 0xFF);
            wrapBuffer[2] = (byte)((length >> 16) & 0xFF);
            wrapBuffer[3] = (byte)(length >> 24);
            wrapBuffer[4] = receveivedSeqno;
            if(byteData!=null)
                byteData.CopyTo(wrapBuffer, HEADER_LEN_LENGTH + 1);
            receveivedSeqno = 0;    // Ready for next message.
            // Begin sending the data to the remote device.
            Send(wrapBuffer);
        }


        /**
         * Used to send bytes to the socket.
         */
        public static void Send(byte[] byteData)
        {
            if (CSpectSocket.socket == null)
                return;

            // Log
            if (LogEnabled)
                WriteResp(byteData);
            // Begin sending the data to the remote device.
            Socket handler = CSpectSocket.socket.workSocket;
            handler.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), handler);
        }


        /**
         * The async callback for sending.
         */
        protected static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = handler.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to client.", bytesSent);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }


        /**
         * Creates a string from dat bytes.
         */
        protected static string GetStringFromData(byte[] data, int start = 0, int count = -1)
        {
            if (count == -1)
                count = data.Length;
            if (start + count > data.Length)
                count = data.Length - start;
            if (count <= 0)
                return "";

            string result = "";
            int printCount = count;
            if (printCount > 20)
                printCount = 20;
            for (int i = 0; i < printCount; i++)
                result += " " + data[i + start];
            if (printCount != count)
                result += " ...";
            return result;
        }


        /**
         * Writes a received command message to the console.
         */
        protected static void WriteCmd(byte[] data)
        {
            int count = data.Length;
            int index = 0;
            if (count >= 6)
            {
                string length = "" + data[0] + " " + data[1] + " " + data[2] + " " + data[3];
                int seqno = data[4];
                int cmd = data[5];
                string cmdString = ((DZRP)cmd).ToString();
                Console.WriteLine();
                Console.WriteLine("<-- Command {0}:", cmdString);
                Console.WriteLine("  Length: {0} ", length);
                Console.WriteLine("  SeqNo:  {0}", seqno);
                Console.WriteLine("  Cmd:    {0}", cmd);
                index = 6;
            }
            // Rest of data
            string dataString = GetStringFromData(data, index);
            Console.Write("  Data:"+dataString);
            Console.WriteLine();
        }


        /**
         * Writes a sent response message to the console.
         */
        protected static void WriteResp(byte[] data)
        {
            int count = data.Length;
            int index = 0;
            if (count >= 5)
            {
                string length = "" + data[0] + " " + data[1] + " " + data[2] + " " + data[3];
                int seqno = data[4];
                string text;
                if (seqno == 0)
                    text = "Notification:";
                else
                    text = "Response:";
                Console.WriteLine();
                Console.WriteLine("--> "+text);
                Console.WriteLine("  Length: {0} ", length);
                Console.WriteLine("  SeqNo:  {0}", seqno);
                index = 5;
            }
            // Rest of data
            string dataString = GetStringFromData(data, index);
            Console.Write("  Data:" + dataString);
            Console.WriteLine();
        }

    }

}
