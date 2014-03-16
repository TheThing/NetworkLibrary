NetworkLibrary
==============

Network library is a simple C# Network Library that takes the
complication of managing a TCP/IP connections and includes many
handy features.

Features
--------

Network comes with the follow features:

* Simple and easy to use.
* [Events](https://github.com/TheThing/NetworkLibrary/wiki/Registering-events).
* Native serializing support.
* [Object registration](https://github.com/TheThing/NetworkLibrary/wiki/Registering-objects).
* [Support bind-able objects](https://github.com/TheThing/NetworkLibrary/wiki/Binding-objects).
* And lots more...



Quick Start
-----------

When using Network Library, both the client and server share the
same interface `INetwork`. As such, you can keep the core code
almost the same, whether you're dealing with the server or the
client.

Creating a server is very simple. Create an instance of
`ConnectionHost` and run `StartBroadcasting`:

```CSharp
using System;
using NetworkLibrary.Core;

public class MainProgram
{
    static INetwork theConnection;

    public static void Main(params string[] args)
    {
        //Create an instance of our host.
        theConnection = new ConnectionHost(33050);
        
        //Run the listener.
        (theConnection as ConnectionHost).StartBroadcasting();
        Console.WriteLine("\nHost up and running. Press any key to quit.");
        
        //Prevent the program from closing
        Console.ReadKey();
    }
}
```

Creating a client is also simple:

```CSharp
using System;
using NetworkLibrary.Core;

public class MainProgram
{
    static INetwork theConnection;

    public static void Main(params string[] args)
    {
        //Create an instance of our host.
        theConnection = new ConnectionClient();
        
        //Connect to server.
        (theConnection as ConnectionClient).Connect("127.0.0.1", 33050);
        Console.WriteLine("\nSucessfully connected. Press any key to quit.");
        
        //Prevent the program from closing
        Console.ReadKey();
    }
}
```

Sending events is also very easy. Just register a function with specific integer code:

```CSharp
int code = 0; //Code for this event
ourNetworkConnection.RegisterEvent(code, eventThatShouldBeRun);

/* ... */

private void eventThatShouldBeRun(object source, NetworkEventArgs args)
{
    Console.WriteLine(source as string);
}
```

After that, you can easily send this type of message by simply doing:

```CSharp
int code = 0; //Code for this event
connection.SendEvent(code, "Hello World");
```

For more information, check out [Establish Connection](https://github.com/TheThing/NetworkLibrary/wiki/Establish-Connection) and [Registering events](https://github.com/TheThing/NetworkLibrary/wiki/Registering-events).

Documentation
-------------

Full documentation on Network Library can be found on the [wiki](https://github.com/TheThing/NetworkLibrary/wiki).

License
-------

Network Library is licensed under the terms of the WTFPL, see the included LICENSE file.
