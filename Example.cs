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
    }
}
