using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Posnet API to connect with Posnet NEO.
/// </summary>
namespace Posnet
{
    /// <summary>
    /// 
    /// </summary>
    public class Connection : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of Connection class using specified serial port parameters.
        /// </summary>
        /// <param name="port">COM1 - COM99 for Windows, /dev/tty0AMA for Raspberry Pi</param>
        /// <param name="baudRate">Baud rate.</param>
        /// <param name="parity">Parity.</param>
        /// <param name="dataBits">Data bits.</param>
        /// <param name="stopBits">Stop bits.</param>
        public Connection(string port = "COM1", int baudRate = 9600, Parity parity = Parity.None, int dataBits = 8, StopBits stopBits = StopBits.One, int timeout = 250)
        {
            SerialPort = new SerialPort(port, baudRate, parity, dataBits, stopBits);
            SerialPort.WriteTimeout = timeout;
            SerialPort.WriteTimeout = timeout;
            SerialPort.DataReceived += SerialPort_DataReceived;
        }

        SerialPort SerialPort { get; set; }

        bool MessageStarted { get; set; } = false;

        bool MessageEnded { get; set; } = false;

        List<byte> MessageBuffer { get; set; } = new List<byte>();

        int byteBuf { get; set; }

        /// <summary>
        /// Opens connection to Posnet Neo.
        /// </summary>
        public void Open()
        {
            try
            {
                SerialPort.Open();
            }
            catch(Exception ex)
            {
                throw ex;
            }

        }

        /// <summary>
        /// Closes connection to Posnet Neo.
        /// </summary>
        public void Close()
        {
            SerialPort.Close();
        }

        /// <summary>
        /// Release all resources used by Connection.
        /// </summary>
        public void Dispose()
        {
            SerialPort.Dispose();
        }

        /// <summary>
        /// Sends a message to Posnet Neo using serial port.
        /// </summary>
        /// <param name="msg">Messge to send.</param>
        public void Send(Message msg)
        {
            if(!SerialPort.IsOpen)
            {
                Open();
            }


            try
            {
                SerialPort.Write(msg.Bytes, 0, msg.Bytes.Count());
                if (LogOutgoingMessage != null) LogOutgoingMessage(this, new MessageEventArgs(msg));
            }
            catch (Exception ex)
            {
                if (OnError != null)
                {
                    OnError(this, new System.IO.ErrorEventArgs(ex));
                }
                else
                {
                    throw ex;
                }
            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                while (byteBuf != -1)
                {

                    if ((byteBuf = (sender as SerialPort).ReadByte()) == 0x10)
                    {
                        byteBuf = (sender as SerialPort).ReadByte();

                        if (byteBuf == 0x02)
                        {
                            MessageStarted = true;
                        }
                        else if (byteBuf == 0x03)
                        {
                            MessageEnded = true;
                        }
                        else if (byteBuf == 0x08)
                        {
                            MessageStarted = false;
                            MessageEnded = true;
                        }
                    }

                    if (MessageStarted)
                    {
                        MessageBuffer.Add((byte)byteBuf);
                    }

                    if (MessageStarted && MessageEnded)
                    {
                        MessageStarted = false;
                        MessageEnded = false;
                        Message msg = new Message(MessageBuffer.ToArray());
                        if (LogRawIncomingCompleteMessage != null) LogRawIncomingCompleteMessage(this, new RawBytesEventArgs(MessageBuffer.ToArray()));
                        if (LogIncomingMessage != null) LogIncomingMessage(this, new MessageEventArgs(msg));
                        if (ReceivedMessage != null) ReceivedMessage(this, new MessageEventArgs(msg));
                        MessageBuffer = new List<byte>();
                    }
                    else if (!MessageStarted && MessageEnded)
                    {
                        if (LogRawIncomingIncompleteMessage != null) LogRawIncomingIncompleteMessage(this, new RawBytesEventArgs(MessageBuffer.ToArray()));
                        MessageEnded = false;
                        MessageBuffer = new List<byte>();
                    }
                }
            }
            catch(Exception ex)
            {
                if (OnError != null)
                {
                    OnError(this, new System.IO.ErrorEventArgs(ex));
                }
                else
                {
                    throw ex;
                }
            }
        }

        /// <summary>
        /// Received message event from Posnet Neo.
        /// </summary>
        public event MessageHandler ReceivedMessage;

        public event MessageHandler LogIncomingMessage;
        public event MessageHandler LogOutgoingMessage;
        public event RawBytesHandler LogRawIncomingCompleteMessage;
        public event RawBytesHandler LogRawIncomingIncompleteMessage;

        public event ErrorHandler OnError;

    }

    public delegate void MessageHandler(object sender, MessageEventArgs e);

    public delegate void RawBytesHandler(object sender, RawBytesEventArgs e);

    public delegate void ErrorHandler(object sender, System.IO.ErrorEventArgs e);

    /// <summary>
    /// Message event arguments.
    /// </summary>
    public class MessageEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the MessageEventArgs class.
        /// </summary>
        /// <param name="msg"></param>
        public MessageEventArgs(Message msg)
        {
            Message = msg;
        }

        /// <summary>
        /// Message received from Posnet Neo.
        /// </summary>
        public Message Message { get; private set; }
    }
    
    public class RawBytesEventArgs
    {
        public RawBytesEventArgs(byte[] bytes)
        {
            Bytes = bytes;
        }
        
        public byte[] Bytes { get; private set; }
    }
}
