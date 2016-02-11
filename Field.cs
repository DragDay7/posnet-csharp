using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Posnet
{
    /// <summary>
    /// Field creator.
    /// </summary>
    public partial class Field
    {
        /// <summary>
        /// Initializes a new instance of the Field class using specified parameters.
        /// </summary>
        /// <param name="dataType">Type of data field.</param>
        /// <param name="value">Content of a field.</param>
        public Field(DataTypes dataType, object value)
        {
            DataType = dataType;
            Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the Field class using bytes from serial port.
        /// </summary>
        /// <param name="bytes">Bytes of a field received within a message from serial port.</param>
        internal Field(byte[] bytes)
        {
            switch((DataTypes)bytes[0])
            {
                case DataTypes.S:
                    Value = Encoding.Default.GetString(bytes, 1, bytes.Length - 2);
                    break;
                case DataTypes.B:
                    Value = bytes[1];
                    break;
                case DataTypes.V:
                    Value = BitConverter.ToUInt16(bytes, 1);
                    break;
                case DataTypes.L:
                    Value = BitConverter.ToUInt32(bytes, 1);
                    break;
                case DataTypes.N:
                    byte[] buf = new byte[bytes.Length - 1];
                    for (int i = 0; i < buf.Length; i++)
                    {
                        buf[i] = bytes[i + 1];
                    }
                    Value = FromBCD(buf);
                    break;
                default:
                    throw new Exception("Unknown DataType in Field constructor.");
            }
            DataType = (DataTypes)bytes[0];
        }

        /// <summary>
        /// Value of a field.
        /// </summary>
        public object Value { get; private set; }

        /// <summary>
        /// Type of data field.
        /// </summary>
        public DataTypes DataType { get; private set; }

        /// <summary>
        /// Returns field in bytes. No special characters SYN here.
        /// </summary>
        internal byte[] Bytes
        {
            get
            {
                byte[] tmp = new byte[0];

                switch (DataType)
                {
                    case DataTypes.S:
                        tmp = Encoding.ASCII.GetBytes((string)Value + '\0');
                        break;
                    case DataTypes.B:
                        tmp = new byte[] { (byte)Value };
                        break;
                    case DataTypes.V:
                        tmp = BitConverter.GetBytes((ushort)Value);
                        break;
                    case DataTypes.L:
                        tmp = BitConverter.GetBytes((uint)Value);
                        break;
                    case DataTypes.N:
                        tmp = ToBCD((long)Value);
                        break;
                    default:
                        throw new Exception("Field.Bytes; unknown DataType!");
                }

                int len = tmp.Length + 1;
                byte[] result = new byte[len];
                result[0] = (byte)DataType;
                for (int i = 1; i < len; i++)
                {
                    result[i] = tmp[i - 1];
                }
                return result;
            }
        }
    }
}
