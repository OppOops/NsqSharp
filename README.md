NsqSharp.Async
========

[![License](http://img.shields.io/:license-mit-blue.svg)](http://doge.mit-license.org)&nbsp;&nbsp;[![NuGet version](https://badge.fury.io/nu/nsqsharp.svg)](https://www.nuget.org/packages/nsqsharp)&nbsp;&nbsp;![Nuget](https://img.shields.io/nuget/dt/NsqSharp?label=nuget%20downloads)

A .NET client library for [NSQ](https://github.com/nsqio/nsq), a realtime distributed messaging platform.

Check out this [slide deck](https://speakerdeck.com/snakes/nsq-nyc-golang-meetup?slide=19) for a quick intro to NSQ.

Watch [Spray Some NSQ On It](https://www.youtube.com/watch?v=CL_SUzXIUuI) by co-author [Matt Reiferson](https://github.com/mreiferson) for an under 30-minute intro to NSQ as a messaging platform.

## Project Status

- Rewrite with async version.
- Support .NET 8.0+.

## Quick Install

NsqSharp is a client library that talks to the `nsqd` (message queue) and `nsqlookupd` (topic discovery service). See the slides above for more information about their roles.


You can also build these files from source: https://github.com/nsqio/nsq (official). Or you can use official docker image: https://nsq.io/deployment/docker.html .


## C# Examples

`PM> Install-Package NsqSharp.Async`

#### Simple Producer

```cs
using System;
using NsqSharp;

class Program
{
    static void Main()  
    {
        var producer = new Producer("127.0.0.1:4150");
        producer.Publish("test-topic-name", "Hello!");

        Console.WriteLine("Enter your message (blank line to quit):");
        string line = Console.ReadLine();
        while (!string.IsNullOrEmpty(line))
        {
            producer.Publish("test-topic-name", line);
            line = Console.ReadLine();
        }

        producer.Stop();
    }
}
```

#### Simple Consumer

```cs
using System;
using System.Text;
using NsqSharp;

class Program
{
    static void Main()  
    {
        // Create a new Consumer for each topic/channel
        var consumer = new Consumer("test-topic-name", "channel-name");
        consumer.AddHandler(new MessageHandler());
        consumer.ConnectToNsqLookupd("127.0.0.1:4161");

        Console.WriteLine("Listening for messages. If this is the first execution, it " +
                          "could take up to 60s for topic producers to be discovered.");
        Console.WriteLine("Press enter to stop...");
        Console.ReadLine();

        consumer.Stop();
    }
}

public class MessageHandler : IHandler
{
    /// <summary>Handles a message.</summary>
    public void HandleMessage(IMessage message)
    {
        string msg = Encoding.UTF8.GetString(message.Body);
        Console.WriteLine(msg);
    }

    /// <summary>
    /// Called when a message has exceeded the specified <see cref="Config.MaxAttempts"/>.
    /// </summary>
    /// <param name="message">The failed message.</param>
    public void LogFailedMessage(IMessage message)
    {
        // Log failed messages
    }
}
```

## NsqSharp Project Goals
- Structurally similar to the official [go-nsq](https://github.com/nsqio/go-nsq) client.
- Provide similar behavior and semantics as the official package.

## Pull Requests

Pull requests and issues are very welcome and appreciated.

When submitting a pull request please keep in mind we're trying to stay as close to [go-nsq](https://github.com/nsqio/go-nsq) as possible. This sometimes means writing C# which looks more like Go and follows their file layout. Code in the `NsqSharp.Bus` namespace should follow C# conventions and more or less look like other code in this namespace.

## License

This project is open source and released under the [MIT license.](LICENSE)
