# A dummy goldsrc game server implementation

1. Why

For fun.

2. Can it work like a real game server?

No.

3. This thing will destroy the game environment/ Polluted server list!

¯\\_(ツ)_/¯ I only need a shell script and a real game server to do the same thing, so this thing will not cause any changes to the current status of the GoldSrc server list

4. I want to redirect to another game server, but it's not working properly

Yes, because GoldSrc's update stufftext no longer works as before

how to redirect:

- use `reconnect`

- Fully forward UDP packets between the server and client

> Then the client will enjoy double the latency, yay

- Construct UDP packets containing fake IP addresses

> Note that on Windows Server 2008 and later versions of Windows or some Linux distributions, UDP packets with IP addresses that do not match the local machine will be intercepted. Similarly, some VPS providers may also block UDP packets with IP addresses that do not match the local machine. In some regions, sending fake UDP packets may pose legal risks of felony
