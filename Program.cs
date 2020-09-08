using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Linq;
using System.Collections.Generic;

namespace id3g_v2
{
    public struct tagVersionInfo
    {
       public bool presence; // 0 means not present
       public int major;
    };


    public class myFile
    {
        private FileStream fs;
        private int fsLength;



        public bool openFile(string fileName, FileMode mode)
        {
            try
            {
                fs = new FileStream(fileName, mode, FileAccess.ReadWrite, FileShare.None);
            }
            catch(IOException ex)
            {
                MessageBox.Show("Could not open file: "+ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            fsLength = (int)fs.Length;
            return true;
        }

        public tagVersionInfo[] detectID3()
        {
            byte[] probe = new byte[2];
            tagVersionInfo[] foundTags = new tagVersionInfo[3];
            fs.Seek(-128, SeekOrigin.End);
            fs.Read(probe, 0, 3);
            if (probe.SequenceEqual(Globals.ID3v1signature)) { foundTags[0].presence = true; }//ID3v1
            fs.Seek(0, SeekOrigin.Begin);
            fs.Read(probe, 0, 3);
            fs.Seek(3, SeekOrigin.Begin);
            if (probe.SequenceEqual(Globals.ID3v2signature)) { foundTags[1].presence = true; foundTags[1].major = Convert.ToInt32(fs.ReadByte()); }//ID3v2 front
            //fs.Seek(-3, SeekOrigin.End); !TODO
            //fs.Read(probe, 0, 3);
            //if (probe.SequenceEqual(Globals.ID3v2signature.Reverse())) { foundTags[2].presence = true; foundTags[2].major = 4; }//ID3v2 back 
            fs.Seek(0,SeekOrigin.Begin);
            return foundTags;
        }

        public byte[] getAllBytes()
        {
            byte[] tmp = new byte[fs.Length];
            fs.Read(tmp, 0, (int)fs.Length);
            fs.Close();
            return tmp;
        }

        public void writeAllBytes(byte[] data)
        {
            fs.Seek(0, SeekOrigin.Begin);
            fs.Write(data, 0, data.Length);
            fs.Flush();
            fs.Close();
        }
    }

    public class ID3v2tag
    {
        private Form1 myForm;
        private struct ID3v2frame
        {
            public byte[] type;
            public byte[] size;
            public byte[] flags;
            public byte[] contents;
        }
        private bool alreadyHasID3v2 = false;
        private int version;
        private int tagLength;
        private int frameHeaderLength;
        private int frameTypeAndSizeLength;
        private List<ID3v2frame> frames;
        //private List<ID3v2frame> customFrames;

        private byte[] sourceData;
        private byte[] targetData;

        public ID3v2tag(Form1 f, myFile file, int major, bool isPresent)
        {
            version = major;
            frameHeaderLength = 6;
            frameTypeAndSizeLength = 3;
            if (version > 2)
            {
                frameHeaderLength = 10;
                frameTypeAndSizeLength = 4;
            }
            myForm = f;
            sourceData = file.getAllBytes();
            alreadyHasID3v2 = isPresent;
        }

        public void readID3v2()
        {
            Program.setActiveElements(2, 256);
            using (var reader = new MemoryStream(sourceData))
            {
                int pos = 10;
                tagLength = 0;
                int shift = 0;
                byte[] size = new byte[4];
                if (alreadyHasID3v2)
                {
                    reader.Seek(6L, SeekOrigin.Begin);
                    reader.Read(size, 0, 4); // 4 байта - размер ID3v2
                    tagLength = ((int)size[0] << 21 | (int)size[1] << 14 | (int)size[2] << 7 | (int)size[3]) + 10; // размер ID3v2
                    while (pos < tagLength)
                    {
                        shift = readFrame(reader);
                        if (shift == 0) break;
                        else pos += shift;
                    }
                    //TODO set genres
                }
                else
                {
                    MessageBox.Show("ID3v2 tag not found, will create new", "Message", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
        }

        ID3v2frame newFrameStruct()
        {
            ID3v2frame myFrame = new ID3v2frame() ;
            myFrame.type = new byte[frameTypeAndSizeLength];
            myFrame.size = new byte[frameTypeAndSizeLength];
            if (version > 2) myFrame.flags = new byte[2];
            else myFrame.flags = null;
            myFrame.contents = new byte[2048];
            return myFrame;
        }

        public int readFrame(MemoryStream ms) //rewritten
        {
            int frameContentsLength = 0;
            ID3v2frame currentFrame = newFrameStruct();
            if (Convert.ToByte(ms.ReadByte()) == 0x0) return 0;
            else ms.Seek(-1, SeekOrigin.Current);
            ms.Read(currentFrame.type, 0, frameTypeAndSizeLength);
            ms.Read(currentFrame.size, 0, frameTypeAndSizeLength);
            for (int i = frameTypeAndSizeLength - 1, shift = 0; i >= 0; i--, shift += 8)
            {
                frameContentsLength = frameContentsLength | ((int)currentFrame.size[i] << shift);
            }
            currentFrame.flags = null;
            if (version > 2) { ms.Read(currentFrame.flags, 0, 2); }
            Console.WriteLine(System.Text.Encoding.ASCII.GetString(currentFrame.type));
            Console.WriteLine(frameContentsLength);
            Array.Resize<byte>(ref currentFrame.contents, frameContentsLength);
            ms.Read(currentFrame.contents, 0, frameContentsLength);
            frames.Add(currentFrame);
            return frameHeaderLength + frameContentsLength;
        }
    }

    public class ID3v1tag
    {
        private Form1 myForm;

        private byte[] title;
        private byte[] artist;
        private byte[] album;
        private byte[] year;
        private byte[] comment;
        private byte track;
        private byte genre;

        private byte[] sourceData;
        private byte[] targetData;

        private bool alreadyHasID3v1 = false;

        public ID3v1tag(Form1 f, myFile file, bool isPresent)
        {  
            myForm = f;
            sourceData = file.getAllBytes();
            alreadyHasID3v1 = isPresent;
        }

        private string byteArrayToString(ref byte[] arr, int size)
        {
            string res = "";
            int charCount = 0;
            for (int i = 0; i < size; i++)
            {
                if (arr[i] == 0x0) break;
                charCount++;
            }
            res = new string(Program.cp1251.GetChars(arr, 0, charCount));
            return res;
        }

        private void initializeArrays()
        {
            title = new byte[30];
            artist = new byte[30];
            album = new byte[30];
            year = new byte[4];
            comment = new byte[30];
        }

        public void writeRawData(int offset)
        {
            using (var writer = new MemoryStream())
            {
                writer.Seek(0, SeekOrigin.Begin);
                writer.Write(sourceData, 0, sourceData.Length - offset);
                targetData = writer.ToArray();
            }
        }

        public void readID3v1() // rewritten
        {
            Program.setActiveElements(1, 30);
            //ID3v1tag myTag;
            using (var reader = new MemoryStream(sourceData))
            {
                //reader = new BinaryReader(file.getFileStream());
                if (alreadyHasID3v1) // если уже есть тег ID3v1
                {
                    initializeArrays();
                    reader.Seek(reader.Length - 128L + 3L, SeekOrigin.Begin);
                    reader.Read(title, 0, 30);
                    reader.Read(artist, 0, 30);
                    reader.Read(album, 0, 30);
                    reader.Read(year, 0, 4);
                    reader.Read(comment, 0, 30);
                    track = comment[29];
                    genre = Convert.ToByte(reader.ReadByte());
                    myForm.setTextBoxValue(0, byteArrayToString(ref title, 30).TrimEnd(' '));
                    myForm.setTextBoxValue(1, byteArrayToString(ref artist, 30).TrimEnd(' '));
                    myForm.setTextBoxValue(2, byteArrayToString(ref album, 30).TrimEnd(' '));
                    myForm.setTextBoxValue(3, byteArrayToString(ref year, 4).TrimEnd(' '));
                    if (comment[28] == 0x0 && Convert.ToInt32(track) > 0)
                    {
                        Array.Resize(ref comment, 28);
                        myForm.setTextBoxValue(4, byteArrayToString(ref comment, 28).TrimEnd(' '));
                        myForm.setTextBoxValue(7, Convert.ToInt32(track).ToString());
                    }
                    else
                    {
                        myForm.setTextBoxValue(4, byteArrayToString(ref comment, 30).TrimEnd(' '));
                        myForm.setTextBoxValue(7, "");
                    }
                    Form1.genreBox.SelectedIndex = 192;
                    if (Convert.ToInt32(genre) <= 191)
                    {
                        Form1.genreBox.SelectedIndex = Convert.ToInt32(genre);
                    }
                }
                else
                {
                    MessageBox.Show("ID3v1 tag not found, will create new", "Message", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
        }

        public void writeID3v1() // Rewritten?
        {
           // ID3v1tag myTag;
            int trkNum = 0;
            int commLen = 30;
            initializeArrays();
            Array.Copy(Encoding.GetEncoding(1251).GetBytes(Form1.values[0].Text), title, Encoding.GetEncoding(1251).GetByteCount(Form1.values[0].Text));
            Array.Copy(Encoding.GetEncoding(1251).GetBytes(Form1.values[1].Text), artist, Encoding.GetEncoding(1251).GetByteCount(Form1.values[1].Text));
            Array.Copy(Encoding.GetEncoding(1251).GetBytes(Form1.values[2].Text), album, Encoding.GetEncoding(1251).GetByteCount(Form1.values[2].Text));
            Array.Copy(Encoding.GetEncoding(1251).GetBytes(Form1.values[3].Text), year, Encoding.GetEncoding(1251).GetByteCount(Form1.values[3].Text));
            Array.Copy(Encoding.GetEncoding(1251).GetBytes(Form1.values[4].Text), comment, Encoding.GetEncoding(1251).GetByteCount(Form1.values[4].Text));
            track = 0x0;
            if (Int32.TryParse(Form1.values[7].Text, out trkNum))
            {
                if (trkNum > 0 && trkNum < 256) { commLen = 28; track = Convert.ToByte(trkNum); }
            }
            genre = Convert.ToByte(Form1.genreBox.SelectedIndex);
            if (alreadyHasID3v1)
            {
                writeRawData(128);
            }
            else writeRawData(0);
            using (var writer = new MemoryStream())
            {
                writer.Seek(0, SeekOrigin.End);
                writer.Write(Globals.ID3v1signature, 0, 3);
                writer.Write(title, 0, 30);
                writer.Write(artist, 0, 30);
                writer.Write(album, 0, 30);
                writer.Write(year, 0, 4);
                writer.Write(comment, 0, commLen);
                if (commLen == 28) { writer.WriteByte(0x0); writer.WriteByte(track); }
                writer.WriteByte(genre);
                targetData = targetData.Concat(writer.ToArray()).ToArray();
            }
        }

        public byte[] getOutput()
        {
            return targetData;
        }
    };

    public static class Globals
    {
        public static byte[] ID3v1signature = { (byte)'T', (byte)'A', (byte)'G' };
        public static byte[] ID3v2signature = { (byte)'I', (byte)'D', (byte)'3' };
    }

    // Token: 0x02000002 RID: 2
    internal static class Program
    {
        
        //private static ID3v1tag myID3v1Tag;
        //private static ID3v2tag myID3v2Tag;

        //public static void initID3v1()
       // {
        //    myID3v1Tag = new ID3v1tag();
        //}

        //public static void initID3v2()
        //{
        //    myID3v2Tag = new ID3v2tag();
        //}

        // Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00000250
        public static string typeDescription(string a)
        {
            switch (a)
            {
                case "TALB":
                    return "Альбом";
                case "TYER":
                    return "Год";
                case "TRCK":
                    return "№ дорожки";
                case "TCON":
                    return "Жанр";
                case "TCOM":
                    return "Композитор";
                case "TPUB":
                    return "Издатель";
                case "TPE1":
                    return "Исполнитель";
                case "COMM":
                    return "Комментарий";
                case "TIT2":
                    return "Имя дорожки";
                case "TPE2":
                    return "Участники";
                case "TCOP":
                    return "Авт. права";
                case "TIT1":
                    return "Раздел";
                case "TIT3":
                    return "Пояснение";
                case "TPE3":
                    return "Дирижёр";
                case "TBPM":
                    return "Уд. в минуту";
                case "TEXT":
                    return "Автор текста";
                case "TRDA":
                    return "Дата записи";
            }
            return a;
        }

        // Token: 0x06000002 RID: 2 RVA: 0x00002254 File Offset: 0x00000454
        public static void size2hex(int sz, int len, ref byte[] header)
        {
            string hex;
            if (len == 3)
            {
                hex = sz.ToString("X6");
            }
            else
            {
                hex = sz.ToString("X8");
            }
            for (int i = 0; i < len; i++)
            {
                header[i + len] = byte.Parse(hex.Substring(i * 2, 2), NumberStyles.HexNumber);
            }
        }

        

        // Token: 0x06000004 RID: 4 RVA: 0x0000248C File Offset: 0x0000068C
        /*public static void writeID3v2()
        {
            if (!(Program.save.FileName == ""))
            {
                if (Form1.genreBox.Items.Contains(Form1.genreBox.Text))
                {
                    Form1.genreBox.SelectedItem = Form1.genreBox.Text;
                    if (Form1.genreBox.SelectedIndex <= 191)
                    {
                        Form1.values[5].Text = "(" + Form1.genreBox.SelectedIndex + ")";
                    }
                    else if (Form1.genreBox.SelectedIndex == 193)
                    {
                        Form1.values[5].Text = "(RX)";
                    }
                    else if (Form1.genreBox.SelectedIndex == 194)
                    {
                        Form1.values[5].Text = "(CR)";
                    }
                    else
                    {
                        Form1.values[5].Text = "(255)";
                    }
                }
                else if (Form1.genreBox.Text.Trim() != "")
                {
                    Form1.values[5].Text = Form1.genreBox.Text;
                }
                else
                {
                    Form1.values[5].Text = "(255)";
                }
                Program.reader = new BinaryReader(new FileStream(Program.open.FileName, FileMode.Open));
                byte[] tmp = new byte[Program.reader.BaseStream.Length - (long)Program.length];
                Program.reader.BaseStream.Seek((long)Program.length, SeekOrigin.Begin);
                Program.reader.Read(tmp, 0, (int)(Program.reader.BaseStream.Length - (long)Program.length));
                Program.reader.Close();
                FileStream fs = null;
                if (Program.checkDestination(ref fs) != 0)
                {
                    byte[] newtag = new byte[]
					{
						73,
						68,
						51,
						0,
						0,
						0,
						0,
						0,
						0,
						0
					};
                    if (Program.confValues[3] == 0)
                    {
                        newtag[3] = 2;
                    }
                    else if (Program.confValues[3] == 2)
                    {
                        newtag[3] = 4;
                    }
                    else
                    {
                        newtag[3] = 3;
                    }
                    fs.Seek(0L, SeekOrigin.Begin);
                    fs.Write(newtag, 0, newtag.Length);
                    fs.Seek((long)(newtag.Length + 1), SeekOrigin.Begin);
                    byte[] codeFlag;
                    if (Program.confValues[4] == 0)
                    {
                        byte[] array = new byte[1];
                        codeFlag = array;
                    }
                    else if (Program.confValues[4] == 2)
                    {
                        codeFlag = new byte[]
						{
							2,
							254,
							byte.MaxValue
						};
                    }
                    else if (Program.confValues[4] == 3)
                    {
                        codeFlag = new byte[]
						{
							3
						};
                    }
                    else
                    {
                        codeFlag = new byte[]
						{
							1,
							byte.MaxValue,
							254
						};
                    }
                    for (int i = 0; i < Program.kol; i++)
                    {
                        if (Form1.values[i].Text.Trim() != "")
                        {
                            byte[] contents = null;
                            byte[] header;
                            if (Program.confValues[3] == 0)
                            {
                                header = new byte[6];
                            }
                            else
                            {
                                header = new byte[10];
                            }
                            Program.encodeText(ref contents, Form1.values[i].Text);
                            int newsz = codeFlag.Length + contents.Length;
                            for (int j = 0; j <= 3; j++)
                            {
                                if (Program.confValues[3] == 0 && j != 3)
                                {
                                    header[j] = (byte)Program.tagsCompat[i][j];
                                }
                                else
                                {
                                    header[j] = (byte)Form1.labels[i].Name[j];
                                }
                            }
                            if (Form1.labels[i].Name == "COMM")
                            {
                                newsz += 3 + codeFlag.Length - 1 + 1;
                                if (Program.confValues[4] == 1 || Program.confValues[4] == 2)
                                {
                                    newsz++;
                                }
                            }
                            if (Program.confValues[3] == 0)
                            {
                                Program.size2hex(newsz, 3, ref header);
                            }
                            else
                            {
                                Program.size2hex(newsz, 4, ref header);
                            }
                            MemoryStream newframe = new MemoryStream();
                            newframe.Write(header, 0, header.Length);
                            if (Form1.labels[i].Name != "COMM")
                            {
                                newframe.Write(codeFlag, 0, codeFlag.Length);
                            }
                            else
                            {
                                newframe.Write(codeFlag, 0, 1);
                                if (Program.confValues[2] == 0)
                                {
                                    newframe.Write(new byte[]
									{
										101,
										110,
										103
									}, 0, 3);
                                }
                                else
                                {
                                    newframe.Write(new byte[]
									{
										114,
										117,
										115
									}, 0, 3);
                                }
                                if (Program.confValues[4] == 1 || Program.confValues[4] == 2)
                                {
                                    newframe.Write(codeFlag, 1, 2);
                                    newframe.WriteByte(0);
                                }
                                newframe.WriteByte(0);
                                if (Program.confValues[4] == 1 || Program.confValues[4] == 2)
                                {
                                    newframe.Write(codeFlag, 1, 2);
                                }
                            }
                            newframe.Write(contents, 0, contents.Length);
                            fs.Seek(0L, SeekOrigin.End);
                            fs.Write(newframe.ToArray(), 0, newframe.ToArray().Length);
                        }
                    }
                    for (int i = 0; i < Program.pics.Length; i++)
                    {
                        if (Program.pics[i].img != null)
                        {
                            byte[] contents = null;
                            byte[] header;
                            if (Program.confValues[3] == 0)
                            {
                                header = new byte[6];
                            }
                            else
                            {
                                header = new byte[10];
                            }
                            for (int j = 0; j <= 3; j++)
                            {
                                if (Program.confValues[3] == 0 && j != 3)
                                {
                                    header[j] = (byte)"PIC"[j];
                                }
                                else
                                {
                                    header[j] = (byte)"APIC"[j];
                                }
                            }
                            MemoryStream ms = new MemoryStream();
                            if (i == 1)
                            {
                                Program.pics[i].img = new Bitmap(Program.pics[i].img, new Size(32, 32));
                            }
                            else
                            {
                                if (Program.confValues[5] == 0)
                                {
                                    Program.pics[i].img = new Bitmap(Program.pics[i].img, new Size(200, 200));
                                }
                                if (Program.confValues[5] == 2)
                                {
                                    Program.pics[i].img = new Bitmap(Program.pics[i].img, new Size(500, 500));
                                }
                                else
                                {
                                    Program.pics[i].img = new Bitmap(Program.pics[i].img, new Size(300, 300));
                                }
                            }
                            if (Program.pics[i].mime.EndsWith("jpeg") || Program.pics[i].mime.EndsWith("jpg"))
                            {
                                Program.pics[i].img.Save(ms, ImageFormat.Jpeg);
                            }
                            else
                            {
                                Program.pics[i].img.Save(ms, ImageFormat.Png);
                            }
                            Program.encodeText(ref contents, Program.pics[i].text);
                            int newsz = Program.pics[i].mime.Length + codeFlag.Length + ms.ToArray().Length + contents.Length + 3;
                            if (Program.confValues[4] == 1 || Program.confValues[4] == 2)
                            {
                                newsz++;
                            }
                            if (Program.confValues[3] == 0)
                            {
                                Program.size2hex(newsz, 3, ref header);
                            }
                            else
                            {
                                Program.size2hex(newsz, 4, ref header);
                            }
                            byte[] raw = ms.ToArray();
                            MemoryStream newframe = new MemoryStream();
                            newframe.Write(header, 0, header.Length);
                            newframe.WriteByte(codeFlag[0]);
                            newframe.Write(Encoding.Default.GetBytes(Program.pics[i].mime), 0, Program.pics[i].mime.Length);
                            newframe.WriteByte(0);
                            newframe.WriteByte(Convert.ToByte(i));
                            if (Program.confValues[4] == 1 || Program.confValues[4] == 2)
                            {
                                newframe.Write(codeFlag, 1, 2);
                            }
                            newframe.Write(contents, 0, contents.Length);
                            if (Program.confValues[4] == 1 || Program.confValues[4] == 2)
                            {
                                newframe.WriteByte(0);
                            }
                            newframe.WriteByte(0);
                            newframe.Write(raw, 0, raw.Length);
                            fs.Seek(0L, SeekOrigin.End);
                            fs.Write(newframe.ToArray(), 0, newframe.ToArray().Length);
                        }
                    }
                    long newlen = fs.Length - 10L;
                    fs.Seek(0L, SeekOrigin.Begin);
                    for (int i = 0; i < 4; i++)
                    {
                        newtag[6 + i] = (byte)(newlen >> 21 - i * 7);
                    }
                    fs.Write(newtag, 0, newtag.Length);
                    fs.Seek(newlen + 10L, SeekOrigin.Begin);
                    int offset = 0;
                    Program.resolveConflict(ref offset, 1);
                    fs.Write(tmp, 0, tmp.Length - offset);
                    fs.Seek(0L, SeekOrigin.End);
                    fs.Close();
                    Program.rescan();
                }
            }
        }*/

        // Token: 0x06000005 RID: 5 RVA: 0x00002EE0 File Offset: 0x000010E0
        public static void encodeText(ref byte[] cont, string src)
        {
            if (Program.confValues[4] == 0)
            {
                cont = Program.cp1251.GetBytes(src);
            }
            else if (Program.confValues[4] == 2)
            {
                cont = Encoding.BigEndianUnicode.GetBytes(src);
            }
            else if (Program.confValues[4] == 3)
            {
                cont = Encoding.UTF8.GetBytes(src);
            }
            else
            {
                cont = Encoding.Unicode.GetBytes(src);
            }
        }

        // Token: 0x06000006 RID: 6 RVA: 0x00002F64 File Offset: 0x00001164
        

        // Token: 0x06000007 RID: 7 RVA: 0x000030F8 File Offset: 0x000012F8
        public static void clearInput()
        {
            for (int i = 0; i < 3; i++)
            {
                Form1.buttons[i + 1].Enabled = true;
            }
            for (int i = 0; i < Program.kol; i++)
            {
                Form1.values[i].Text = "";
            }
            Form1.genreBox.Enabled = true;
            Form1.genreBox.SelectedIndex = 192;
        }



        // Token: 0x0600000C RID: 12 RVA: 0x000032F4 File Offset: 0x000014F4
        public static void setActiveElements(int ID3Type, int textLengthLimit)
        {
            Program.clearInput();
            if (Form1.buttons[2].Text == "<<")
            {
                Form1.buttons[2].PerformClick();
            }
            if (ID3Type == 1)
            {
                Form1.genreBox.DropDownStyle = ComboBoxStyle.DropDownList;
                for (int i = 0; i <=4; i++)
                {
                    Form1.values[i].Enabled = true;
                }
                Form1.values[7].Enabled = true;
            }
            else
            {
                Form1.genreBox.DropDownStyle = ComboBoxStyle.DropDown;
            }
            /*for (int i = 0; i < 2; i++)
            {
                Form1.buttons[2 + i].Enabled = flag;
            }
            for (int i = 0; i < Program.kol; i++)
            {
                if (i >= 6 || v == 2)
                {
                    Form1.values[i].Enabled = flag;
                }
                else
                {
                    Form1.values[i].MaxLength = textLengthLimit;
                }
            }
            bool c = Form1.genreBox.Items.Contains("Remix");
            if (c && v == 1)
            {
                Form1.genreBox.Items.Remove("Remix");
                Form1.genreBox.Items.Remove("Cover");
            }
            else if (!c && v == 2)
            {
                Form1.genreBox.Items.Add("Remix");
                Form1.genreBox.Items.Add("Cover");
            }
            if (v == 1)
            {
                Form1.genreBox.DropDownStyle = ComboBoxStyle.DropDownList;
            }
            else
            {
                Form1.genreBox.DropDownStyle = ComboBoxStyle.DropDown;
            }
            Form1.buttons[2].Enabled = flag;
            Program.switchImageControls(flag, 0);*/
        }

        

        // Token: 0x0600000D RID: 13 RVA: 0x0000346C File Offset: 0x0000166C
        

        // Token: 0x0600000E RID: 14 RVA: 0x000036CC File Offset: 0x000018CC

        // Token: 0x06000013 RID: 19 RVA: 0x000041C0 File Offset: 0x000023C0
        [STAThread]
        private static void Main()
        {
            if (File.Exists("config.ini"))
            {
                string config = File.ReadAllText("config.ini", Encoding.Default);
                Debug.Write(config);
                StringReader sr = new StringReader(config);
                for (; ; )
                {
                    string line = sr.ReadLine();
                    if (line == null)
                    {
                        break;
                    }
                    for (int i = 0; i < 6; i++)
                    {
                        if (line.StartsWith(Program.confStrings[i]))
                        {
                            Program.confValues[i] = int.Parse(line[line.Length - 1].ToString());
                        }
                    }
                }
                if (Program.confValues[3] < 2 && Program.confValues[4] > 1)
                {
                    Program.confValues[3] = 2;
                }
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        // Token: 0x06000014 RID: 20 RVA: 0x000042B8 File Offset: 0x000024B8
        

        // Token: 0x04000001 RID: 1
        public static int pos = 0;

        // Token: 0x04000002 RID: 2
        public static int length;

        // Token: 0x04000003 RID: 3
        public static OpenFileDialog open = new OpenFileDialog();

        // Token: 0x04000004 RID: 4
        public static OpenFileDialog openimg = new OpenFileDialog();

        // Token: 0x04000005 RID: 5
        public static SaveFileDialog save = new SaveFileDialog();

        // Token: 0x04000006 RID: 6
        public static SaveFileDialog saveimg = new SaveFileDialog();

        // Token: 0x04000007 RID: 7
        public static BinaryReader reader;

        // Token: 0x04000008 RID: 8
        public static bool align;

        // Token: 0x04000009 RID: 9
        public static int kol = 16;

        // Token: 0x0400000A RID: 10
        public static int mode = 1;

        // Token: 0x0400000B RID: 11
        public static string[][] t = new string[][]
		{
			new string[]
			{
				"Assume v1",
				"Assume v2",
				"Delete",
				"Ask User"
			},
			new string[]
			{
				"Keep both",
				"Resolve",
				"Ask User"
			},
			new string[]
			{
				"English",
				"Russian"
			},
			new string[]
			{
				"2.2",
				"2.3",
				"2.4"
			},
			new string[]
			{
				"ISO-8859-1",
				"UTF-16LE",
				"UTF-16BE",
				"UTF-8"
			},
			new string[]
			{
				"200x200",
				"300x300",
				"500x500"
			}
		};

        // Token: 0x0400000C RID: 12
        public static string[] tl = new string[]
		{
			"Editor Operation",
			"Tag Conflicts",
			"Target Language",
			"Target Version",
			"Target Encoding",
			"Image Size"
		};

        // Token: 0x0400000D RID: 13
        public static string[] bt = new string[]
		{
			"Open",
			"Save",
			">>",
			"Lyrics",
			"Options",
			"About"
		};

        // Token: 0x0400000E RID: 14
        public static string[] ob = new string[]
		{
			"Save",
			"Quit"
		};

        // Token: 0x0400000F RID: 15
        public static string[] imgOptLbl = new string[]
		{
			"+",
			"X",
			"S"
		};

        // Token: 0x04000010 RID: 16
        public static string[] tags = new string[]
		{
			"TIT2",
			"TPE1",
			"TALB",
			"TYER",
			"COMM",
			"TCON",
			"TPUB",
			"TRCK",
			"TPE2",
			"TIT1",
			"TIT3",
			"TCOM",
			"TPE3",
			"TBPM",
			"TEXT",
			"TRDA"
		};

        // Token: 0x04000011 RID: 17
        public static string[] tagsCompat = new string[]
		{
			"TT2",
			"TP1",
			"TAL",
			"TYE",
			"COM",
			"TCO",
			"TPB",
			"TRK",
			"TP2",
			"TT1",
			"TT3",
			"TCM",
			"TP3",
			"TBP",
			"TXT",
			"TRD"
		};

        // Token: 0x04000012 RID: 18
        public static string[] genres = new string[]
		{
			"Blues",
			"Classic Rock",
			"Country",
			"Dance",
			"Disco",
			"Funk",
			"Grunge",
			"Hip-Hop",
			"Jazz",
			"Metal",
			"New Age",
			"Oldies",
			"Other",
			"Pop",
			"R&B",
			"Rap",
			"Reggae",
			"Rock",
			"Techno",
			"Industrial",
			"Alternative",
			"Ska",
			"Death Metal",
			"Pranks",
			"Soundtrack",
			"Euro-Techno",
			"Ambient",
			"Trip-Hop",
			"Vocal",
			"Jazz+Funk",
			"Fusion",
			"Trance",
			"Classical",
			"Instrumental",
			"Acid",
			"House",
			"Game",
			"Sound Clip",
			"Gospel",
			"Noise",
			"Alt. Rock",
			"Bass",
			"Soul",
			"Punk",
			"Space",
			"Meditative",
			"Instrumental Pop",
			"Instrumental Rock",
			"Ethnic",
			"Gothic",
			"Darkwave",
			"Techno-Industrial",
			"Electronic",
			"Pop-Folk",
			"Eurodance",
			"Dream",
			"Southern Rock",
			"Comedy",
			"Cult",
			"Gangsta Rap",
			"Top 40",
			"Christian Rap",
			"Pop/Funk",
			"Jungle",
			"Native American",
			"Cabaret",
			"New Wave",
			"Psychedelic",
			"Rave",
			"Showtunes",
			"Trailer",
			"Lo-Fi",
			"Tribal",
			"Acid Punk",
			"Acid Jazz",
			"Polka",
			"Retro",
			"Musical",
			"Rock & Roll",
			"Hard Rock",
			"Folk",
			"Folk-Rock",
			"National Folk",
			"Swing",
			"Fast-Fusion",
			"Bebop",
			"Latin",
			"Revival",
			"Celtic",
			"Bluegrass",
			"Avantgarde",
			"Gothic Rock",
			"Progressive Rock",
			"Psychedelic Rock",
			"Symphonic Rock",
			"Slow Rock",
			"Big Band",
			"Chorus",
			"Easy Listening",
			"Acoustic",
			"Humour",
			"Speech",
			"Chanson",
			"Opera",
			"Chamber Music",
			"Sonata",
			"Symphony",
			"Booty Bass",
			"Primus",
			"Porn Groove",
			"Satire",
			"Slow Jam",
			"Club",
			"Tango",
			"Samba",
			"Folklore",
			"Ballad",
			"Power Ballad",
			"Rhythmic Soul",
			"Freestyle",
			"Duet",
			"Punk Rock",
			"Drum Solo",
			"A Cappella",
			"Euro-House",
			"Dance Hall",
			"Goa",
			"Drum & Bass",
			"Club-House",
			"Hardcore",
			"Terror",
			"Indie",
			"BritPop",
			"Afro-Punk",
			"Polsk Punk",
			"Beat",
			"Christian Gangsta Rap",
			"Heavy Metal",
			"Black Metal",
			"Crossover",
			"ConfoundID3Versionsorary Christian",
			"Christian Rock",
			"Merengue",
			"Salsa",
			"Thrash Metal",
			"Anime",
			"JPop",
			"Synthpop",
			"Abstract",
			"Art Rock",
			"Baroque",
			"Bhangra",
			"Big Beat",
			"Breakbeat",
			"Chillout",
			"DownfoundID3Versionso",
			"Dub",
			"EBM",
			"Eclectic",
			"Electro",
			"Electroclash",
			"Emo",
			"Experimental",
			"Garage",
			"Global",
			"IDM",
			"Illbient",
			"Industro-Goth",
			"Jam Band",
			"Krautrock",
			"Leftfield",
			"Lounge",
			"Math Rock",
			"New Romantic",
			"Nu-Breakz",
			"Post-Punk",
			"Post-Rock",
			"Psytrance",
			"Shoegaze",
			"Space Rock",
			"Trop Rock",
			"World Music",
			"Neoclassical",
			"Audiobook",
			"Audio Theatre",
			"Neue Deutsche Welle",
			"Podcast",
			"Indie Rock",
			"G-Funk",
			"Dubstep",
			"Garage Rock",
			"Psybient",
			"None",
			"Remix",
			"Cover"
		};

        // Token: 0x04000013 RID: 19
        public static string[] imageTypes = new string[]
		{
			"Другое",
			"Значок 32x32",
			"Другой значок",
			"Обложка (ст.1)",
			"Обложка (ст.2)",
			"Брошюра",
			"Компакт-диск",
			"Ведущий исп.",
			"Исполнитель",
			"Дирижёр",
			"Группа",
			"Композитор",
			"Автор текста",
			"Место записи",
			"Процесс записи",
			"Выступление",
			"Скриншот",
			"Colourful Fish",
			"Иллюстрация",
			"Логотип группы",
			"Логотип студии"
		};

        // Token: 0x04000014 RID: 20
        public static int[] confValues = new int[]
		{
			3,
			2,
			1,
			1,
			1,
			1
		};

        // Token: 0x04000015 RID: 21
        public static string[] confStrings = new string[]
		{
			"OPERATION",
			"CONFLICT",
			"LANGUAGE",
			"VERSION",
			"ENCODING",
			"IMAGESIZE"
		};
        

        // Token: 0x04000016 RID: 22
        public static byte[] proc;

        // Token: 0x04000017 RID: 23
        public static string pf = "";

        // Token: 0x04000018 RID: 24
        public static Encoding cp1251 = Encoding.GetEncoding(1251);

        // Token: 0x04000019 RID: 25
        public static Encoding latin1 = Encoding.GetEncoding("iso-8859-1");

        // Token: 0x0400001A RID: 26
        public static Program.Picture[] pics = new Program.Picture[21];

        // Token: 0x02000003 RID: 3
        public struct Frame
        {
            // Token: 0x06000016 RID: 22 RVA: 0x0000514C File Offset: 0x0000334C
            public static Program.Frame Create()
            {
                return new Program.Frame
                {
                    type = "",
                    text = "",
                    lang = ""
                };
            }

            // Token: 0x0400001B RID: 27
            public string type;

            // Token: 0x0400001C RID: 28
            public string text;

            // Token: 0x0400001D RID: 29
            public string lang;
        }

        // Token: 0x02000004 RID: 4
        public struct Picture
        {
            // Token: 0x06000017 RID: 23 RVA: 0x0000518C File Offset: 0x0000338C
            public static Program.Picture Create()
            {
                return new Program.Picture
                {
                    mime = "",
                    text = ""
                };
            }

            // Token: 0x0400001E RID: 30
            public Image img;

            // Token: 0x0400001F RID: 31
            public string mime;

            // Token: 0x04000020 RID: 32
            public string text;
        }
    }
}
