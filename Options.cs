using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace id3g_v2
{
    // Token: 0x02000007 RID: 7
    public partial class Options : Form
    {
        // Token: 0x06000024 RID: 36 RVA: 0x000055DC File Offset: 0x000037DC
       /* protected override void Dispose(bool disposing)
        {
            if (disposing && this.components != null)
            {
                this.components.Dispose();
            }
            base.Dispose(disposing);
        }*/

        // Token: 0x06000025 RID: 37 RVA: 0x00005614 File Offset: 0x00003814
        /*private void InitializeComponent()
        {
            this.components = new Container();
            base.AutoScaleMode = AutoScaleMode.Font;
            this.Text = "Options";
            base.FormBorderStyle = FormBorderStyle.FixedDialog;
            base.MaximizeBox = false;
            base.ClientSize = new Size(200, 220);
            base.Shown += this.options_Shown;
            base.StartPosition = FormStartPosition.CenterParent;
        }*/

        // Token: 0x06000026 RID: 38 RVA: 0x00005684 File Offset: 0x00003884
        public Options()
        {
            Options.target = new ComboBox[6];
            Options.targetLbl = new Label[6];
            Options.func = new Button[2];
            int y = 10;
            for (int i = 0; i < 6; i++)
            {
                Options.target[i] = new ComboBox();
                Options.targetLbl[i] = new Label();
                Options.targetLbl[i].Size = new Size(90, 20);
                Options.target[i].Size = new Size(80, 20);
                Options.targetLbl[i].Location = new Point(10, y + 2);
                Options.target[i].Location = new Point(100, y);
                Options.targetLbl[i].Text = Program.tl[i];
                Options.target[i].DropDownStyle = ComboBoxStyle.DropDownList;
                base.Controls.Add(Options.target[i]);
                base.Controls.Add(Options.targetLbl[i]);
                y += 30;
                for (int j = 0; j < 6; j++)
                {
                    if (j < Program.t[i].Length)
                    {
                        Options.target[i].Items.Add(Program.t[i][j]);
                    }
                }
                Options.target[i].SelectedIndex = 1;
                Options.target[i].SelectedIndexChanged += this.options_SelectedIndexChanged;
            }
            int x = 10;
            for (int i = 0; i < 2; i++)
            {
                Options.func[i] = new Button();
                Options.func[i].Size = new Size(80, 20);
                Options.func[i].Location = new Point(x, y);
                Options.func[i].Text = Program.ob[i];
                base.Controls.Add(Options.func[i]);
                x += 90;
            }
            Options.func[0].Enabled = false;
            Options.func[0].Click += this.optsave_Click;
            Options.func[1].Click += this.optquit_Click;

            ConfigRead();

            this.InitializeComponent();
        }

        // Token: 0x06000027 RID: 39 RVA: 0x000058C8 File Offset: 0x00003AC8
        private void options_SelectedIndexChanged(object sender, EventArgs e)
        {
            Options.func[0].Enabled = true;
            if (Options.target[3].SelectedIndex < 2 && Options.target[4].SelectedIndex > 1)
            {
                if (sender == Options.target[3])
                {
                    Options.target[4].SelectedIndex = 1;
                }
                else
                {
                    Options.target[3].SelectedIndex = 2;
                }
            }
        }

        // Token: 0x06000028 RID: 40 RVA: 0x00005940 File Offset: 0x00003B40
        private void options_Shown(object sender, EventArgs e)
        {
            for (int i = 0; i < 6; i++)
            {
                Options.target[i].SelectedIndex = Program.confValues[i];
            }
        }

        // Token: 0x06000029 RID: 41 RVA: 0x00005974 File Offset: 0x00003B74
        private void optsave_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < 6; i++)
            {
                Program.confValues[i] = Options.target[i].SelectedIndex;
            }

            ConfigWrite();

            Options.func[0].Enabled = false;
        }

        // Token: 0x0600002A RID: 42 RVA: 0x000059B5 File Offset: 0x00003BB5
        private void optquit_Click(object sender, EventArgs e)
        {
            base.Close();
        }

        // Token: 0x04000028 RID: 40
       // private IContainer components = null;

        // Token: 0x04000029 RID: 41
        public static ComboBox[] target;

        // Token: 0x0400002A RID: 42
        public static Label[] targetLbl;

        // Token: 0x0400002B RID: 43
        public static Button[] func;
        
        // читает значения настроек из файла
        private static void ConfigRead()
        {
            string _fileName = @"D:\note.txt";
            using (FileStream fstream = File.OpenRead(_fileName))
            {
                byte[] array = new byte[fstream.Length];
                fstream.Read(array, 0, array.Length);

                // декодируем байты в строку
                string textFromFile = System.Text.Encoding.UTF8.GetString(array);
                
                if (textFromFile != null) 
                { 
                    for (int i = 0; i < Program.confValues.Length; i++) 
                    {
                        string nameOfValue = Program.confStrings[i];
                        int indexOfValue = textFromFile.IndexOf(nameOfValue) + nameOfValue.Length+1;
                        
                        Program.confValues[i] = Convert.ToInt32(textFromFile[indexOfValue])-48; // вычитаем 48, чтобы получить нужный код символа
                    }
                }
            }
        }

        // сохраняет настройки
        private static void ConfigWrite()
        {
            string _fileName = @"D:\note.txt"; // временный адрес для дебага
            
            string configValues = "";
            using (FileStream fstream = new FileStream(_fileName, FileMode.OpenOrCreate))
            {
                for (int i=0; i<Program.confValues.Length; i++)
                {
                    configValues += Program.confStrings[i] + ":" + Program.confValues[i] + "\n";
                }
                
                byte[] array = System.Text.Encoding.UTF8.GetBytes(configValues);
                fstream.Write(array, 0, array.Length);
            }
        }
    }
}
