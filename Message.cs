using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Posnet
{
    /// <summary>
    /// Message for Posnet Neo.
    /// </summary>
    public partial class Message
    {
        /// <summary>
        /// Initializes a new instance of the Message class using specified parameters.
        /// </summary>
        /// <param name="command">Command to send.</param>
        /// <param name="flag">Use 0 for now.</param>
        /// <param name="token">Token to identify message.</param>
        /// <param name="fields">Fields to send if required by command.</param>
        public Message(Connection connection, Commands command, Flags flag, uint token, List<Field> fields = null)
        {
            Connection = connection;
            Command = command;
            Flag = flag;
            Token = token;
            Fields = fields;
            Length = 16;

            if (fields == null)
            {
                Fields = new List<Field>();
            }
            else
            {
                Fields = fields;
            }
            
            foreach (Field f in Fields)
            {
                Length += (ushort)f.Bytes.Length;
            }

            Value = new List<byte>();

            Value.Add(0x02);

            foreach (byte b in BitConverter.GetBytes((ushort)Flag))
            {
                Value.Add(b);
            }

            foreach (byte b in BitConverter.GetBytes((uint)Token))
            {
                Value.Add(b);
            }

            foreach (byte b in BitConverter.GetBytes((ushort)Length))
            {
                Value.Add(b);
            }

            if (fields != null)
            {
                foreach (byte b in BitConverter.GetBytes((ushort)Fields.Count))
                {
                    Value.Add(b);
                }
            }
            else
            {
                Value.Add(0x00);
                Value.Add(0x00);
            }

            foreach (byte b in BitConverter.GetBytes((ushort)Command))
            {
                Value.Add(b);
            }

            foreach (Field f in Fields)
            {
                foreach (byte b in f.Bytes)
                {
                    Value.Add(b);
                }
            }

            Crc16 = Crc16Ccitt.ComputeChecksum(Value.GetRange(1, Value.Count - 1).ToArray());

            foreach (byte b in BitConverter.GetBytes((ushort)Crc16))
            {
                Value.Add(b);
            }

            Value.Add(0x03);

        }

        /// <summary>
        /// Initializes a new instance of the Message class using bytes from serial port.
        /// </summary>
        /// <param name="bytes">Bytes received from serial port.</param>
        internal Message(byte[] bytes)
        {
            //if(bytes[0] != 0x10 || bytes[1] != 0x02 || bytes[bytes.Length - 2] != 0x10 || bytes[bytes.Length - 1] != 0x03)
            if (bytes[0] != 0x02 || bytes[bytes.Length - 1] != 0x03)
            {
                throw new Exception("Possibly wrong data passed for Message(byte[] bytes) constructor.");
            }

            Value = bytes.ToList();
            //Value.RemoveAt(0);
            //Value.RemoveAt(Value.Count - 2);

            //for (int i = Value.Count - 1; i > 0; i--)
            //{
            //    if (Value[i] == 0x10 && Value[i - 1] == 0x10)
            //    {
            //        Value.RemoveAt(i--);
            //    }
            //}

            bytes = Value.ToArray();

            Flag = (Flags)BitConverter.ToUInt16(bytes, 1);

            Token = BitConverter.ToUInt32(bytes, 3);

            Length = BitConverter.ToUInt16(bytes, 7);

            Command = (Commands)BitConverter.ToUInt16(bytes, 11);

            Crc16 = BitConverter.ToUInt16(bytes, Value.Count - 3);

            List<byte> fields = Value.GetRange(13, Value.Count - 16);

            Fields = new List<Field>(BitConverter.ToUInt16(bytes, 9));

            for (int i = 0; i < fields.Count; i++)
            {
                byte[] buf = null;
                switch ((DataTypes)fields[i])
                {
                    case DataTypes.S:
                        for (int j = i + 1; j < fields.Count; j++)
                        {
                            if (fields[j] == 0x00)
                            {
                                buf = new byte[j - i + 1];
                                fields.CopyTo(i, buf, 0, j - i + 1);
                                i = j++;
                                break;
                            }
                        }
                        break;
                    case DataTypes.B:
                        buf = new byte[] { fields[i], fields[i + 1] };
                        i = i + 1;
                        break;
                    case DataTypes.V:
                        buf = new byte[] { fields[i], fields[i + 1], fields[i + 2] };
                        i = i + 2;
                        break;
                    case DataTypes.L:
                        buf = new byte[] { fields[i], fields[i + 1], fields[i + 3], fields[i + 4], fields[i + 5] };
                        i = i + 5;
                        break;
                    case DataTypes.N:
                        buf = new byte[] { fields[i], fields[i + 1], fields[i + 3], fields[i + 4], fields[i + 5], fields[i + 6], fields[i + 7] };
                        i = i + 7;
                        break;
                    default:
                        throw new Exception("Something gone wrong on Message(byte[] bytes) constructor while parsing fields.");
                }
                Fields.Add(new Field(buf));
            }

        }

        public Connection Connection { get; set; }

        /// <summary>
        /// Manufacturer was drunk while making documentation to this parameter and I'm too lazy to check it myself so it's always 0.
        /// </summary>
        public Flags Flag { get; private set; }

        /// <summary>
        /// Used to identify message. Serial port will reply with same token if message sent.
        /// </summary>
        public uint Token { get; private set; }

        /// <summary>
        /// Length of whole message without special characters SYN.
        /// </summary>
        public ushort Length { get; internal set; }

        /// <summary>
        /// Sent/received command.
        /// </summary>
        public Commands Command { get; private set; }

        /// <summary>
        /// Fields sent/received within the message.
        /// </summary>
        public List<Field> Fields { get; private set; }

        /// <summary>
        /// Checksum to verify the message.
        /// </summary>
        public ushort Crc16 { get; private set; }

        /// <summary>
        /// Raw message in bytes. No special characters SYN here.
        /// </summary>
        List<byte> Value { get; set; }

        /// <summary>
        /// Returns message in bytes including special characters SYN.
        /// </summary>
        internal byte[] Bytes
        {
            get
            {
                int len = 2;
                for (int i = 0; i < Value.Count; i++)
                {
                    len++;
                    if (Value[i] == 0x10) { len++; }
                }

                byte[] result = new byte[len];

                int j = 0;
                for (int i = 0; i < len; i++)
                {
                    if ((j == 0 && Value[0] == 0x02) || (j == Value.Count - 1 && Value[Value.Count - 1] == 0x03 )|| Value[j] == 0x10)
                    {
                        result[i++] = 0x10;
                    }
                    result[i] = Value[j];
                    j++;
                }

                return result;
            }
        }

        public string Debug
        {
            get
            {
                string tmp = "Command: " + Enum.GetName(typeof(Commands), Command) + " Token: " + Token + " Flags: " + Flag + " Length: " + Length + " Crc16: " + Crc16.ToString("X4") + Environment.NewLine;

                for(int i=0; i<Fields.Count; i++)
                {
                    tmp += (char)Fields[i].DataType + " : " + Fields[i].Value;
                    if(i+1<Fields.Count)
                    {
                        tmp += Environment.NewLine;
                    }
                }

                return tmp;
            }
        }

        public void SendIt()
        {
            Connection.Send(this);
        }
    }
}
