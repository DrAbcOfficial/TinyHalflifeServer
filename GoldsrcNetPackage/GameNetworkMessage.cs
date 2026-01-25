using System.Text;

namespace GoldsrcNetPackage;

public class GameNetworkMessage : IDisposable
{
    private readonly MemoryStream _buffer = new();
    private readonly BinaryWriter _writer;
    private static readonly Encoding StringEncoding = Encoding.UTF8;

    public GameNetworkMessage(byte messageId)
    {
        _writer = new BinaryWriter(_buffer, StringEncoding, leaveOpen: true);
        _writer.Write(messageId);
    }
    public void WriteByte(byte value) => _writer.Write(value);
    public void WriteShort(short value) => _writer.Write(value);
    public void WriteLong(int value) => _writer.Write(value);
    public void WriteFloat(float value) => _writer.Write(value);
    public void WriteCoord(float value) => WriteFloat(value);
    public void WriteString(string? text)
    {
        if (text == null)
            text = "";
        var bytes = StringEncoding.GetBytes(text);
        _writer.Write(bytes);
        _writer.Write((byte)0); // null terminator
    }
    public byte[] ToArray()
    {
        return _buffer.ToArray();
    }
    public void Clear()
    {
        _buffer.SetLength(0);
        _buffer.Position = 0;
    }
    public void Dispose()
    {
        _writer?.Dispose();
        _buffer?.Dispose();
    }
}
