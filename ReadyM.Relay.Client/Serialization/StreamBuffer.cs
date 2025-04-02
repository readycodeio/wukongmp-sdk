// using System;
// using System.IO;
//
// namespace ReadyM.Relay.Client.Serialization
// {
//     public class StreamBuffer
//     {
//         private const int DefaultInitialSize = 0;
//         private int pos;
//         private int len;
//         private byte[] buf;
//
//         public StreamBuffer(int size = 0) => this.buf = new byte[size];
//
//         public StreamBuffer(byte[] buf)
//         {
//             this.buf = buf;
//             this.len = buf.Length;
//         }
//
//         /// <summary>
//         /// Allocates a new byte[] that is the exact used length. Use GetBuffer for nonalloc operations.
//         /// </summary>
//         public byte[] ToArray()
//         {
//             byte[] dst = new byte[this.len];
//             Buffer.BlockCopy((Array)this.buf, 0, (Array)dst, 0, this.len);
//             return dst;
//         }
//
//         /// <summary>
//         /// Allocates a new byte[] that is the exact used length. Use GetBuffer for nonalloc operations.
//         /// </summary>
//         public byte[] ToArrayFromPos()
//         {
//             int count = this.len - this.pos;
//             if (count <= 0)
//                 return new byte[0];
//             byte[] dst = new byte[count];
//             Buffer.BlockCopy((Array)this.buf, this.pos, (Array)dst, 0, count);
//             return dst;
//         }
//
//         /// <summary>
//         /// Returns a new ArraySegment for the StreamBuffer starting at Position.
//         /// </summary>
//         public ArraySegment<byte> ToArraySegmentFromPos()
//         {
//             return new ArraySegment<byte>(this.buf, this.pos, this.len - this.pos);
//         }
//
//         /// <summary>
//         /// The bytes between Position and Length are copied to the beginning of the buffer. Length decreased by Position. Position set to 0.
//         /// </summary>
//         public void Compact()
//         {
//             long count = (long)(this.Length - this.Position);
//             if (count > 0L)
//                 Buffer.BlockCopy((Array)this.buf, this.Position, (Array)this.buf, 0, (int)count);
//             this.Position = 0;
//             this.SetLength(count);
//         }
//
//         public byte[] GetBuffer() => this.buf;
//
//         /// <summary>
//         /// Brings StreamBuffer to the state as after writing of 'length' bytes. Returned buffer and offset can be used to actually fill "written" segment with data.
//         /// </summary>
//         public byte[] GetBufferAndAdvance(int length, out int offset)
//         {
//             offset = this.Position;
//             this.Position += length;
//             return this.buf;
//         }
//
//         public bool CanRead => true;
//
//         public bool CanSeek => true;
//
//         public bool CanWrite => true;
//
//         public int Length => this.len;
//
//         public int Position
//         {
//             get => this.pos;
//             set
//             {
//                 this.pos = value;
//                 if (this.len >= this.pos)
//                     return;
//                 this.len = this.pos;
//                 this.CheckSize(this.len);
//             }
//         }
//
//         /// <summary>
//         /// Remaining bytes in this StreamBuffer. Returns 0 if len - pos is less than 0.
//         /// </summary>
//         public int Available
//         {
//             get
//             {
//                 int num = this.len - this.pos;
//                 return num < 0 ? 0 : num;
//             }
//         }
//
//         public void Flush() { }
//
//         public void Reset()
//         {
//             this.pos = 0;
//             this.len = 0;
//         }
//
//         public long Seek(long offset, SeekOrigin origin)
//         {
//             int num;
//             switch (origin)
//             {
//                 case SeekOrigin.Begin:
//                     num = (int)offset;
//                     break;
//                 case SeekOrigin.Current:
//                     num = this.pos + (int)offset;
//                     break;
//                 case SeekOrigin.End:
//                     num = this.len + (int)offset;
//                     break;
//                 default:
//                     throw new ArgumentException("Invalid seek origin");
//             }
//
//             if (num < 0)
//                 throw new ArgumentException("Seek before begin");
//             this.pos = num <= this.len ? num : throw new ArgumentException("Seek after end");
//             return (long)this.pos;
//         }
//
//         /// <summary>
//         /// Sets stream length. If current position is greater than specified value, it's set to the value.
//         /// </summary>
//         /// <remarks>
//         /// SetLength(0) resets the stream to initial state but preserves underlying byte[] buffer.
//         /// </remarks>
//         public void SetLength(long value)
//         {
//             this.len = (int)value;
//             this.CheckSize(this.len);
//             if (this.pos <= this.len)
//                 return;
//             this.pos = this.len;
//         }
//
//         /// <summary>
//         /// Guarantees that the buffer is at least neededSize bytes.
//         /// </summary>
//         public void SetCapacityMinimum(int neededSize) => this.CheckSize(neededSize);
//
//         public int Read(byte[] buffer, int dstOffset, int count)
//         {
//             int num = this.len - this.pos;
//             if (num <= 0)
//                 return 0;
//             if (count > num)
//                 count = num;
//             Buffer.BlockCopy((Array)this.buf, this.pos, (Array)buffer, dstOffset, count);
//             this.pos += count;
//             return count;
//         }
//
//         public void Write(byte[] buffer, int srcOffset, int count)
//         {
//             int size = this.pos + count;
//             this.CheckSize(size);
//             if (size > this.len)
//                 this.len = size;
//             Buffer.BlockCopy((Array)buffer, srcOffset, (Array)this.buf, this.pos, count);
//             this.pos = size;
//         }
//
//         public byte ReadByte()
//         {
//             return this.pos < this.len ? this.buf[this.pos++] : throw new EndOfStreamException("SteamBuffer.ReadByte() failed. pos:" + this.pos.ToString() + " len:" + this.len.ToString());
//         }
//
//         public void WriteByte(byte value)
//         {
//             if (this.pos >= this.len)
//             {
//                 this.len = this.pos + 1;
//                 this.CheckSize(this.len);
//             }
//
//             this.buf[this.pos++] = value;
//         }
//
//         public void WriteBytes(byte v0, byte v1)
//         {
//             int num = this.pos + 2;
//             if (this.len < num)
//             {
//                 this.len = num;
//                 this.CheckSize(this.len);
//             }
//
//             this.buf[this.pos++] = v0;
//             this.buf[this.pos++] = v1;
//         }
//
//         public void WriteBytes(byte v0, byte v1, byte v2)
//         {
//             int num = this.pos + 3;
//             if (this.len < num)
//             {
//                 this.len = num;
//                 this.CheckSize(this.len);
//             }
//
//             this.buf[this.pos++] = v0;
//             this.buf[this.pos++] = v1;
//             this.buf[this.pos++] = v2;
//         }
//
//         public void WriteBytes(byte v0, byte v1, byte v2, byte v3)
//         {
//             int num = this.pos + 4;
//             if (this.len < num)
//             {
//                 this.len = num;
//                 this.CheckSize(this.len);
//             }
//
//             this.buf[this.pos++] = v0;
//             this.buf[this.pos++] = v1;
//             this.buf[this.pos++] = v2;
//             this.buf[this.pos++] = v3;
//         }
//
//         public void WriteBytes(
//             byte v0,
//             byte v1,
//             byte v2,
//             byte v3,
//             byte v4,
//             byte v5,
//             byte v6,
//             byte v7)
//         {
//             int num = this.pos + 8;
//             if (this.len < num)
//             {
//                 this.len = num;
//                 this.CheckSize(this.len);
//             }
//
//             this.buf[this.pos++] = v0;
//             this.buf[this.pos++] = v1;
//             this.buf[this.pos++] = v2;
//             this.buf[this.pos++] = v3;
//             this.buf[this.pos++] = v4;
//             this.buf[this.pos++] = v5;
//             this.buf[this.pos++] = v6;
//             this.buf[this.pos++] = v7;
//         }
//
//         private bool CheckSize(int size)
//         {
//             if (size <= this.buf.Length)
//                 return false;
//             int length = this.buf.Length;
//             if (length == 0)
//                 length = 1;
//             while (size > length)
//                 length *= 2;
//             byte[] dst = new byte[length];
//             Buffer.BlockCopy((Array)this.buf, 0, (Array)dst, 0, this.buf.Length);
//             this.buf = dst;
//             return true;
//         }
//     }
// }