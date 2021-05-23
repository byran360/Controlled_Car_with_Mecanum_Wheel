using SmartCar.Data;
using SmartCar.Map.Elem;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SmartCar
{
    public partial class Form_Data : Form
    { 
        public Form_Data()
        {
            InitializeComponent();
        }

        public void InvsibleMaxGapTextBox()
        {
            this.MaxGaptextBox.Visible = false;
        }
        public void VsibleMaxGapTextBox()
        {
            this.MaxGaptextBox.Visible = true;
        }


        public void ChangeTextBox(DisplayPoint keyPoint)
        {
            this.xtextBox.Text = keyPoint.x.ToString("F3");
            this.ytextBox.Text = keyPoint.y.ToString("F3");
            keyPoint.w = keyPoint.w / Math.PI * 180;
            this.wtextBox.Text = keyPoint.w.ToString("F3");

            this.frontUrgKtextBox.Text = keyPoint.FrontUrgK.ToString("F3");
            this.leftUrgKtextBox.Text = keyPoint.LeftUrgK.ToString("F3");
            this.rightUrgKtextBox.Text = keyPoint.RightUrgK.ToString("F3");

            this.frontDistextBox.Text = keyPoint.FrontUrgB.ToString("F3");
            this.leftDistextBox.Text = keyPoint.LeftUrgB.ToString("F3");
            this.rightDistextBox.Text = keyPoint.RightUrgB.ToString("F3");

            this.turnCornerX.Text = keyPoint.point.x.ToString("F3");
            this.turnCornerY.Text = keyPoint.point.y.ToString("F3");
            this.frontWall.Text = keyPoint.FrontLine.A.ToString() + "x" + "+" + keyPoint.FrontLine.B.ToString() + "y"+"+"+ keyPoint.FrontLine.C.ToString("F1");
            this.leftWall.Text = keyPoint.LeftLine.A.ToString() + "x" + "+" + keyPoint.LeftLine.B.ToString() + "y" + "+"+ keyPoint.LeftLine.C.ToString("F1");

            this.textBoxAX.Text = keyPoint.pointA.x.ToString("F3");
            this.textBoxAY.Text = keyPoint.pointA.y.ToString("F3");
            this.textBoxBX.Text = keyPoint.pointB.x.ToString("F3");
            this.textBoxBY.Text = keyPoint.pointB.y.ToString("F3");
            this.textBoxCX.Text = keyPoint.pointC.x.ToString("F3");
            this.textBoxCY.Text = keyPoint.pointC.y.ToString("F3");
            this.textBoxDX.Text = keyPoint.pointD.x.ToString("F3");
            this.textBoxDY.Text = keyPoint.pointD.y.ToString("F3");

            if (keyPoint.VisibleMaxGap == false) { this.MaxGaptextBox.Visible = false; }
            this.MaxGaptextBox.Text = keyPoint.MaxGap.ToString("F3");
        }

        private void Form_Data_Load(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void label8_Click(object sender, EventArgs e)
        {

        }

        private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void leftDistextBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            KeyPoint keyPoint = new KeyPoint(PortManager.drPort.getPosition());
            DisplayPoint.displayPoint.x = keyPoint.x;
            DisplayPoint.displayPoint.y = keyPoint.y;
            DisplayPoint.displayPoint.w = keyPoint.w;
            ChangeTextBox(DisplayPoint.displayPoint);
            keyPoint.RecordExcel(PortManager.drPort,DisplayPoint.displayPoint.FileName1);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;

            xtextBox.Visible = false;
            ytextBox.Visible = false;
            wtextBox.Visible = false;

            leftUrgKtextBox.Visible = false;
            rightUrgKtextBox.Visible = false;
            frontUrgKtextBox.Visible = false;

            frontDistextBox.Visible = false;
            leftDistextBox.Visible = false;
            rightDistextBox.Visible = false;

            MaxGaptextBox.Visible = false;
        }


        private void button2_Click(object sender, EventArgs e)
        {
            timer1.Enabled = true;
           // timer2.Enabled = true;

            xtextBox.Visible = true;
            ytextBox.Visible = true;
            wtextBox.Visible = true;

            leftUrgKtextBox.Visible = true;
            rightUrgKtextBox.Visible = true;
            frontUrgKtextBox.Visible = true;

            frontDistextBox.Visible = true;
            leftDistextBox.Visible = true;
            rightDistextBox.Visible = true;

            MaxGaptextBox.Visible = true;
        }

        private void MaxGaptextBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void frontDistextBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void label34_Click(object sender, EventArgs e)
        {

        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            MatrixData.matrixData.refreshMatrix();
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
