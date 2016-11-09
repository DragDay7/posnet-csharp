using System;
using Posnet;

class ExampleClass
{

    void Example()
    {
        Connection conn = new Connection("COM1");

        conn.ReceivedMessage += Conn_ReceivedMessage;

        Message msg = new Message(conn, Commands.DATEGET, Flags.NONE, 0);

        conn.Send(msg);
    }

    private static void Conn_ReceivedMessage(object sender, MessageEventArgs e)
    {
        Console.WriteLine(e.Message.Debug);
        
        // Output
        //
        // Command: DATEGET Token: 0 Flags: NONE Length: 7 Crc16: 1884
        // B: 9
        // B: 11
        // B: 2016
    }
}
