using System.Text;

namespace GoldsrcNetPackage;

public class GameFullPackage
{
    public enum OpCode
    {
        svc_nop = 0x01,
        svc_disconnect = 0x02,
        svc_event = 0x03,
        svc_version = 0x05,
        svc_setview = 0x08,
        svc_sound = 0x09,
        svc_time = 0x0A,
        svc_stufftext = 0x12,
        svc_usermessage = 0x18,
        svc_packetentities = 0x1E,
        svc_deltapacketentities = 0x20,
        svc_choke = 0xFF
    }

    public static byte[] EncryptPayload(byte[] packet, uint xorMask)
    {
        ArgumentNullException.ThrowIfNull(packet);
        if (packet.Length < 8)
            throw new ArgumentException("Packet too short for GoldSrc header.");
        byte[] encrypted = new byte[packet.Length];
        Array.Copy(packet, encrypted, packet.Length);
        for (int i = 8; i < encrypted.Length; i++)
        {
            byte maskByte = (byte)((xorMask >> ((i - 8) % 4) * 8) & 0xFF);
            encrypted[i] ^= maskByte;
        }

        return encrypted;
    }

    public static byte[] DecryptPayload(byte[] encryptedPacket, uint xorMask)
    {
        return EncryptPayload(encryptedPacket, xorMask);
    }

    public static byte[] GetConnectedBytes(uint challenge, int seq, int ack, OpCode code, GameNetworkMessage? payload = null, uint? encrypt_mask = null)
    {
        using MemoryStream buffer = new();
        using BinaryWriter bw = new(buffer);
        bw.Write((uint)challenge);
        if (encrypt_mask != null)
            bw.Write((uint)encrypt_mask);

        //Payload
        bw.Write((int)seq);
        bw.Write((int)ack);
        bw.Write((byte)code);
        if (payload != null)
            bw.Write(payload.ToArray());
        bw.Write((byte)0x00);

        byte[] fullPacket = buffer.ToArray();
        if (encrypt_mask != null)
            return EncryptPayload(fullPacket, (uint)encrypt_mask);
        return fullPacket;
    }


    public static byte[] GetConnectionlessBytes(string payload)
    {
        using MemoryStream ms = new();
        using BinaryWriter bw = new(ms);
        //none sense prefix
        bw.Write(0xFFFFFFFF);
        bw.Write(Encoding.UTF8.GetBytes(payload));
        return ms.ToArray();
    }
}
