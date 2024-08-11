namespace TinyHalflifeServer.UDP;
static class Encoder
{
    private static readonly byte[][] MungifyTable = [
        [0x7A, 0x64, 0x05, 0xF1, 0x1B, 0x9B, 0xA0, 0xB5, 0xCA, 0xED, 0x61, 0x0D, 0x4A, 0xDF, 0x8E, 0xC7],
        [0x05, 0x61, 0x7A, 0xED, 0x1B, 0xCA, 0x0D, 0x9B, 0x4A, 0xF1, 0x64, 0xC7, 0xB5, 0x8E, 0xDF, 0xA0],
        [0x20, 0x07, 0x13, 0x61, 0x03, 0x45, 0x17, 0x72, 0x0A, 0x2D, 0x48, 0x0C, 0x4A, 0x12, 0xA9, 0xB5]
    ];
    private static byte[] Internal(byte[] data, int seq, byte[] table, bool unpack = false)
    {
        int size = (data.Length & ~3) >> 2;
        byte[] result = new byte[data.Length];
        data.CopyTo(result, 0);
        for (int i = 0; i < size; i++)
        {
            byte[] bytes = unpack
                ? BitConverter.GetBytes(BitConverter.ToInt32(data.Skip(i << 2).Take(4).ToArray(), 0) ^ seq)
                : BitConverter.GetBytes(BitConverter.ToInt32(data.Skip(i << 2).Take(4).ToArray(), 0) ^ ~seq).Reverse().ToArray();

            for (int j = 0; j < 4; j++)
            {
                bytes[j] ^= (byte)(0xA5 | (j << j) | j | table[(i + j) & 0x0F]);
            }
            if (unpack)
                Buffer.BlockCopy(BitConverter.GetBytes(BitConverter.ToInt32(bytes.Reverse().ToArray(), 0) ^ ~seq), 0, result, i << 2, 4);
            else
                Buffer.BlockCopy(BitConverter.GetBytes(BitConverter.ToInt32(bytes, 0) ^ seq), 0, result, i << 2, 4);
        }
        return result;
    }
    public static byte[] Munge(byte[] data, int seq, int table = 0)
    {
        return Internal(data, seq, MungifyTable[table], false);
    }
    public static byte[] UnMunge(byte[] data, int seq, int table = 0)
    {
        return Internal(data, seq, MungifyTable[table], true);
    }
}