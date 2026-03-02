using System.Collections;

using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

/*
 *	Java 读取IO 
 */
public class JavaReader
{

    public BinaryReader br;
    public BinaryWriter bw;



    public void setBinaryReader(BinaryReader br)
    {
        this.br = br;
    }

    public void setBinaryWriter(BinaryWriter bw)
    {
        this.bw = bw;
    }

    public void initBinaryWrite()
    {
        setBinaryWriter(new BinaryWriter(new MemoryStream()));
    }

    public void initBinaryReader()
    {
        setBinaryReader(new BinaryReader(new MemoryStream()));
    }

    public void close()
    {
        release();
    }
    public void release()
    {

        if(br != null)
        {
            br.Close();
            br = null;
        }

        if(bw != null)
        {
            bw.Close();
            bw = null;
        }

    }

    public int available()
    {
        return (int)(br.BaseStream.Length - br.BaseStream.Position);
    }
    public int available2()
    {
        return (int)(dataSize- br.BaseStream.Position);
    }
    public int size()
    {
        return (int)(bw.BaseStream.Length);
    }


    public int readTimes = 0;
    public void resetReadTimes()
    {
        readTimes = 0;
    }


    public byte readByte()
    {
        readTimes+=1;
        return br.ReadByte();
    }
    public sbyte readSByte()
    {
        return br.ReadSByte();
    }

    public byte[] readBytes(int len)
    {
        readTimes+=1;
        return br.ReadBytes(len);
    }

    public byte[] read(byte[] value)
    {
        readTimes+=1;
        for (int i = 0; i < value.Length; i++)
        {
            value[i] = br.ReadByte();
        }
        return value;
    }

    public byte[] read(byte[] value, int start, int length)
    {
        readTimes+=1;
        for (int i = 0; i < length; i++)
        {
            value[start+i] = br.ReadByte();
        }
        return value;
    }

    public byte[] readFully(byte[] value)
    {
        readTimes+=1;
        for (int i = 0; i < value.Length; i++)
        {
            value[i] = br.ReadByte();
        }
        return value;
    }

    public bool readBoolean()
    {
        readTimes+=1;
        return br.ReadBoolean();
    }

    public short readShort()
    {
        readTimes+=1;
        short rSt = ((short)(br.ReadByte() << 8 | br.ReadByte()));
        return rSt;
    }

    public int readInt()
    {
        readTimes+=1;
        return br.ReadByte() << 24 | br.ReadByte() << 16 | br.ReadByte() << 8 | br.ReadByte();
    }
    
    public long readLong()
    {
        readTimes+=1;
        return ((long)br.ReadByte() << 56 | (long)br.ReadByte() << 48 | (long)br.ReadByte() << 40 | (long)br.ReadByte() << 32 | (long)br.ReadByte() << 24 | (long)br.ReadByte() << 16 | (long)br.ReadByte() << 8 | (long)br.ReadByte());
    }

    public float readFloat()
    {
        readTimes+=1;
        int i = (br.ReadByte() << 24 | br.ReadByte() << 16 | br.ReadByte() << 8 | br.ReadByte());
        byte[] bytes = BitConverter.GetBytes(i);
        return BitConverter.ToSingle(bytes, 0);
    }

    public double readDouble()
    {
        readTimes+=1;
        long i = (((long)br.ReadByte() << 56 | (long)br.ReadByte() << 48 | (long)br.ReadByte() << 40 | (long)br.ReadByte() << 32 | (long)br.ReadByte() << 24 | (long)br.ReadByte() << 16 | (long)br.ReadByte() << 8 | (long)br.ReadByte()));
        byte[] bytes = BitConverter.GetBytes(i);
        return BitConverter.ToDouble(bytes, 0);
    }

    public char readChar()
    {
        readTimes+=1;
        short i = ((short)(br.ReadByte() << 8 | br.ReadByte()));
        byte[] bytes = BitConverter.GetBytes(i);
        return BitConverter.ToChar(bytes, 0);
    }

    public string readUTF(bool jtofflag=true)
    {
        readTimes+=1;
        short len = ((short)(br.ReadByte() << 8 | br.ReadByte()));
        byte[] bytes = br.ReadBytes(len);
        string temp=Encoding.UTF8.GetString(bytes);
       // if(jtofflag)
       // temp = CTUTools.jiantofan(temp);
        return temp;
    }


    public void writeBoolean(bool value)
    {
        bw.Write(value);
    }

    public void writeByte(byte value)
    {
        bw.Write(value);
    }
    public void writeByte(sbyte value)
    {
        bw.Write(value);
    }
    public void writeChar(char c)
    {
        bw.Write(c);
    }

    public void writeShort(short value)
    {
        byte[] bytes = BitConverter.GetBytes(value).Reverse().ToArray();
      
        bw.Write(bytes);
    }

    public void writeInt(int value)
    {
        byte[] bytes = BitConverter.GetBytes(value).Reverse().ToArray();

        bw.Write(bytes);
    }

    public void writeLong(long value)
    {
        byte[] bytes = BitConverter.GetBytes(value).Reverse().ToArray();

        bw.Write(bytes);
    }

    public void writeFloat(float value)
    {
        byte[] bytes = BitConverter.GetBytes(value).Reverse().ToArray();
        write(bytes);
    }

    public void writeDouble(double value)
    {
        byte[] bytes = BitConverter.GetBytes(value).Reverse().ToArray();
        write(bytes);
    }

    public void writeUTF(string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        // byte[] bytes = getByteArrFromUTF(value);
        writeShort((short)bytes.Length);
        write(bytes);
    }

    public static byte[] getByteArrFromUTF(string str)
    {
        int strlen = str.Length;
        int utflen = 0;
        int c, count = 0;

        /* use charAt instead of copying String to char array */
        for (int i = 0; i < strlen; i++)
        {
            c = str[i];
            if ((c >= 0x0001) && (c <= 0x007F))
            {
                utflen++;
            }
            else if (c > 0x07FF)
            {
                utflen += 3;
            }
            else
            {
                utflen += 2;
            }
        }

        byte[] bytearr = new byte[utflen + 2];
        bytearr[count++] = (byte)((utflen >> 8) & 0xFF);
        bytearr[count++] = (byte)((utflen >> 0) & 0xFF);

        int j = 0;
        for (; j < strlen; j++)
        {
            c = str[j];
            if (!((c >= 0x0001) && (c <= 0x007F)))
            {
                break;
            }
            bytearr[count++] = (byte)c;
        }

        for (; j < strlen; j++)
        {
            c = str[j];
            if ((c >= 0x0001) && (c <= 0x007F))
            {
                bytearr[count++] = (byte)c;
            }
            else if (c > 0x07FF)
            {
                bytearr[count++] = (byte)(0xE0 | ((c >> 12) & 0x0F));
                bytearr[count++] = (byte)(0x80 | ((c >> 6) & 0x3F));
                bytearr[count++] = (byte)(0x80 | ((c >> 0) & 0x3F));
            }
            else
            {
                bytearr[count++] = (byte)(0xC0 | ((c >> 6) & 0x1F));
                bytearr[count++] = (byte)(0x80 | ((c >> 0) & 0x3F));
            }
        }
        return bytearr;
    }

    public void write(byte b)
    {
        bw.Write(b);
    }

    public void write(byte[] b, int start, int length)
    {
        for (int i = start; i < start + length; i++)
        {
            bw.Write(b[i]);
        }

    }

    public void write(byte[] b)
    {
        for (int i = 0; i < b.Length; i++)
        {
            bw.Write(b[i]);
        }
        
    }

    public void flush()
    {
        bw.Flush();
    }

    public string toString()
    {
        return Encoding.UTF8.GetString(toByteArray());
    }

    public byte[] toByteArray()
    {
        byte[] b = new byte[bw.BaseStream.Length];
        bw.BaseStream.Seek(0, SeekOrigin.Begin);
        bw.BaseStream.Read(b, 0, b.Length);

        return b;
    }

    public byte[] toByteArrayBR()
    {
        byte[] b = new byte[br.BaseStream.Length];
        br.BaseStream.Seek(0, SeekOrigin.Begin);
        br.BaseStream.Read(b, 0, b.Length);

        return b;
    }

    public byte[] getRemainByteArray()
    {        
        byte[] b = new byte[bw.BaseStream.Length - bw.BaseStream.Position];
        bw.BaseStream.Seek(bw.BaseStream.Position, SeekOrigin.Begin);
        bw.BaseStream.Read(b, 0, b.Length);

        return b;
    }
    public string toOutString()
    {
        return Encoding.UTF8.GetString(toByteArrayBW());
    }

    public string toInString()
    {
        return Encoding.UTF8.GetString(toByteArrayBR());
    }

    public byte[] toByteArrayBW()
    {
        byte[] b = new byte[bw.BaseStream.Length];
        bw.BaseStream.Seek(0, SeekOrigin.Begin);
        bw.BaseStream.Read(b, 0, b.Length);

        return b;
    }

    public void replaceInt(int v1, int v2)
    {
        bw.BaseStream.Seek(v2, SeekOrigin.Begin);
        writeInt(v1);
        bw.BaseStream.Seek(0, SeekOrigin.End);
    }

    public byte[] getRemainBWByteArray()
    {
        byte[] b = new byte[bw.BaseStream.Length - bw.BaseStream.Position];
        bw.BaseStream.Seek(bw.BaseStream.Position, SeekOrigin.Begin);
        bw.BaseStream.Read(b, 0, b.Length);

        return b;
    }

    public byte[] getRemainBRByteArray()
    {
        byte[] b = new byte[br.BaseStream.Length - br.BaseStream.Position];
        br.BaseStream.Seek(br.BaseStream.Position, SeekOrigin.Begin);
        br.BaseStream.Read(b, 0, b.Length);

        return b;
    }
    public void skip(int len)
    {
        br.ReadBytes(len);
    }
    int dataSize;
    public void setdataSize(int dataSize)
    {
        this.dataSize = dataSize;
    }
}