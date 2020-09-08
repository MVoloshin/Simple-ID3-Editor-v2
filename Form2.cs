using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace id3g_v2
{
    // Token: 0x02000005 RID: 5
    public partial class Form2 : Form
    {
        // Token: 0x06000018 RID: 24 RVA: 0x000051C0 File Offset: 0x000033C0
       /* protected override void Dispose(bool disposing)
        {
            if (disposing && this.components != null)
            {
                this.components.Dispose();
            }
            base.Dispose(disposing);
        }*/

        // Token: 0x06000019 RID: 25 RVA: 0x000051F8 File Offset: 0x000033F8
        

        // Token: 0x0600001A RID: 26 RVA: 0x00005250 File Offset: 0x00003450
        public Form2(tagVersionInfo[] foundTags)
        {
            this.InitializeComponent();
            base.Load += this.Form2_Load;
            Form2.tasks = new Button[3];
            int x = 10;
            for (int i = 0; i < 3; i++)
            {
                Form2.tasks[i] = new Button();
                if (i != 2)
                {
                    Form2.tasks[i].Text = "ID3v" + (i + 1);
                }
                Form2.tasks[i].Location = new Point(x, 50);
                Form2.tasks[i].Size = new Size(60, 20);
                base.Controls.Add(Form2.tasks[i]);
                x += 70;
            }
            Form2.tasks[2].Text = "Remove";
           // .Click += this.tagv0_Click;
            Form2.tasks[0].Click += (sender, EventArgs) => { tagv0_CustomClick(sender, EventArgs, foundTags[0]); };
            Form2.tasks[1].Click += (sender, EventArgs) => { tagv1_CustomClick(sender, EventArgs, foundTags[1]); };
            Form2.tasks[2].Click += this.erase_Click;
            Form2.foundID3Description = new Label();
            Form2.foundID3Description.Location = new Point(10, 10);
            Form2.foundID3Description.Size = new Size(160, 20);
            base.Controls.Add(Form2.foundID3Description);
            //Form2.foundID3Versions = Program.searchID3(ref Form2.subversion);
            Form2.foundID3Versions = 1;
            Form2.foundID3Description.Text = "Found: ";
            for(int i=0; i<2; i++)
            {
                if(foundTags[i].presence)
                {
                    if (i != 0) Form2.foundID3Description.Text += ", ";
                    Form2.foundID3Description.Text += "ID3v" + (i+1) +"."+foundTags[i].major;
                } 
            }
            base.ShowDialog();
        }

        // Token: 0x0600001B RID: 27 RVA: 0x000054BC File Offset: 0x000036BC
        private void tagv0_CustomClick(object sender, EventArgs e, tagVersionInfo id3v1)
        {
            Form1 f = Application.OpenForms[0] as Form1;
            Form1.myID3v1tag= new ID3v1tag(f, Form1.file, id3v1.presence);
            Form1.myID3v1tag.readID3v1();
            base.Close();
        }

        // Token: 0x0600001C RID: 28 RVA: 0x000054CC File Offset: 0x000036CC
        private void tagv1_CustomClick(object sender, EventArgs e, tagVersionInfo id3v2)
        {
            Form1 f = Application.OpenForms[0] as Form1;
            Form1.myID3v2tag = new ID3v2tag(f, Form1.file, id3v2.major, id3v2.presence);
            Form1.myID3v2tag.readID3v2();
            base.Close();
        }

        // Token: 0x0600001D RID: 29 RVA: 0x000054DC File Offset: 0x000036DC
        private void erase_Click(object sender, EventArgs e)
        {
            //Program.removeID3();
            base.Close();
        }

        // Token: 0x0600001E RID: 30 RVA: 0x000054EC File Offset: 0x000036EC
        private void Form2_Load(object sender, EventArgs e)
        {
            if (Program.confValues[0] == 0)
            {
                Form2.tasks[0].PerformClick();
            }
            else if (Program.confValues[0] == 1)
            {
                Form2.tasks[1].PerformClick();
            }
            else if (Program.confValues[0] == 2)
            {
                Form2.tasks[2].PerformClick();
            }
        }

        // Token: 0x04000021 RID: 33
        //private IContainer components = null;

        // Token: 0x04000022 RID: 34
        public static Button[] tasks;

        // Token: 0x04000023 RID: 35
        public static Label foundID3Description;

        // Token: 0x04000024 RID: 36
        public static int foundID3Versions = 0;

        // Token: 0x04000025 RID: 37
        public static int subversion = 3;
    }
}