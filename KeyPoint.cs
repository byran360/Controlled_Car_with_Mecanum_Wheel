using SmartCar.Map.Elem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SmartCar {
    public class KeyPoint
    {
        // 关键点ID
        public int id { get; set; }
        // 位置信息（x,y,w）
        public double x { get; set; }
        public double y { get; set; }
        public double w { get; set; }
        // 关键点类型（）
        public int type { get; set; }
        // 校准信息
        public double Can_Adj { get; set; }
        public double UrgK { get; set; }  //拟合出小车前方直线的斜率（单位：度）
        public double UrgB { get; set; }  //拟合出小车前方直线的截距（单位：mm）

        // 通道行进模式
        public double wayType { get; set; }

        // 距通道口距离
        public double disWay { get; set; }
        // 是否返回
        public bool moveBack { get; set; }

        public KeyPoint() 
        {
            x = 0;
            y = 0;
            w = 0;
            id = 0;
            type = 0;
            Can_Adj = 0;
            UrgB = 0;
            UrgK = 0;
            wayType = 0;
            disWay = 0;
            moveBack = false;
        }


        public KeyPoint(KeyPoint p)
        {
            this.x = p.x;
            this.y = p.y;
            this.w = p.w;
        }


        /// <summary>
        /// Get the distance between two points
        /// </summary>
        public double getDis(KeyPoint point)
        {
            return this.getDis(point.x, point.y);
        }


        /// <summary>
        /// Get the distance between two points
        /// </summary>
        public double getDis(double x, double y)
        {
            double dx = this.x - x;
            double dy = this.y - y;
            return Math.Sqrt(dx * dx + dy * dy);
        }


        public double dd = 0.0003;
        public bool comparePos(KeyPoint p)
        {
            double px = this.x - p.x;
            double py = this.y - p.y;
            return (Math.Sqrt(px * px + py * py) < dd);
            //return this.x == p.x && this.y == p.y && this.w == p.w;
        }

       
        // 打印输出在txt文件或者那个
        private static int num = 0;
        public void RecordTxt(IDrPort drPort)
        {
            using (FileStream rswrite = new FileStream(@"E:\DadaStorage\ExcelFile_20210520\" + $"{DisplayPoint.displayPoint.TxtFileName}", FileMode.Append, FileAccess.Write))
            {
                num++;
                KeyPoint keys = new KeyPoint(drPort.getPosition());
                string str1 = "这是第" + num.ToString() + "次记录的点信息:" + "\r\n";
                byte[] buffer1 = Encoding.Default.GetBytes(str1);
                rswrite.Write(buffer1, 0, buffer1.Length);
                PropertyInfo[] property = keys.GetType().GetProperties();
                foreach (PropertyInfo ppty in property)
                {
                    string str2 = ppty.Name.ToString() + ":" + ppty.GetValue(keys, null).ToString() + "\r\n";
                    byte[] buffer2 = Encoding.Default.GetBytes(str2);
                    rswrite.Write(buffer2, 0, buffer2.Length);
                }

            }
        }

        private static int num2 = -1;
        public void RecordExcel(IDrPort drPort,string Filename)
        {
            using (FileStream rswrite = new FileStream(@"E:\DadaStorage\ExcelFile_20210520\"+$"{Filename}", FileMode.Append, FileAccess.Write))
            {
                string str;
                byte[] buffer;
                if (num2 == -1)
                {
                    str = " " + "\t" + "x" + "\t" + "y" + "\t" +"w" + "\t" + "HL" + "\t" +"HR" + "\t" +"TL" + "\t" +"TR" + "\t" 
                          + "FrontDistance" + "\t" + "LeftDistance" + "\t" + "RightDistacne" + "\t"
                          + "A.x" + "\t" + "A.y" + "\t" + "前方拟合直线" + "\t" + "D.x" + "\t" + "D.y" + "\t" + "左方直线" + "\t"
                          + "拐点.x " + "\t" + "拐点.y" + "\t" + "小车BC直线"
                          + "\r\n";
                    buffer = Encoding.Default.GetBytes(str);
                    rswrite.Write(buffer, 0, buffer.Length);
                }
                num2++;
                KeyPoint keys = new KeyPoint(drPort.getPosition());
                keys.w = keys.w  * 180 / Math.PI;
                str = num2.ToString() + "\t" + keys.x.ToString("F3") + "\t" + keys.y.ToString("F3") + "\t" + keys.w.ToString("F3")
                             + "\t" + DisplayPoint.displayPoint.HLSpeed.ToString() + "\t" + DisplayPoint.displayPoint.HRSpeed.ToString() + "\t"
                             + DisplayPoint.displayPoint.TLSpeed.ToString() + "\t" + DisplayPoint.displayPoint.TRSpeed.ToString() + "\t"
                             + DisplayPoint.displayPoint.FrontUrgB.ToString("F3") + "\t"  + DisplayPoint.displayPoint.LeftUrgB.ToString("F3") + "\t" 
                             + DisplayPoint.displayPoint.RightUrgB.ToString("F3") + "\t"

                             + DisplayPoint.displayPoint.pointA.x.ToString("F3") + "\t" + DisplayPoint.displayPoint.pointA.y.ToString("F3") + "\t"
                             + DisplayPoint.displayPoint.FrontLine.A.ToString("F3") + "x" + "+" + DisplayPoint.displayPoint.FrontLine.B.ToString("F3") + "y" + "+" + DisplayPoint.displayPoint.FrontLine.C.ToString("F1") + "\t"
                             + DisplayPoint.displayPoint.pointD.x.ToString("F3") + "\t" + DisplayPoint.displayPoint.pointD.y.ToString("F3") + "\t"
                             + DisplayPoint.displayPoint.LeftLine.A.ToString("F3") + "x" + "+" + DisplayPoint.displayPoint.LeftLine.B.ToString("F3") + "y" + "+" + DisplayPoint.displayPoint.LeftLine.C.ToString("F1") + "\t"
                             + DisplayPoint.displayPoint.point.x.ToString("F3") + "\t" + DisplayPoint.displayPoint.point.y.ToString("F3") + "\t"
                             + DisplayPoint.displayPoint.pointB.x.ToString("F3") + "\t" + DisplayPoint.displayPoint.pointB.y.ToString("F3") + "\t"
                             + DisplayPoint.displayPoint.pointC.x.ToString("F3") + "\t" + DisplayPoint.displayPoint.pointC.y.ToString("F3") + "\t"
                             + DisplayPoint.displayPoint.carLine.A.ToString("F3") + "x" + "+" + DisplayPoint.displayPoint.carLine.B.ToString("F3") + "y" + "+" + DisplayPoint.displayPoint.carLine.C.ToString("F1") + "\t"
                             + "\r\n";
                buffer = Encoding.Default.GetBytes(str);
                rswrite.Write(buffer, 0, buffer.Length);
            }
        
        }

        private static int num3 = -1;
        public void RecordExcel(IDrPort drPort)
        {
            using (FileStream rswrite = new FileStream(@"E:\DadaStorage\ExcelFile_20210520"+$"{DisplayPoint.displayPoint.FileName}", FileMode.Append, FileAccess.Write))
            {
                string str;
                byte[] buffer;
                if (num3 == -1)
                {
                    str = " " + "\t" + "x" + "\t" + "y" + "\t" + "w" + "\t" + "HL" + "\t" + "HR" + "\t" + "TL" + "\t" + "TR" + "\t"
                          + "FrontDistance" + "\t" + "LeftDistance" + "\t" + "RightDistacne" + "\t" 
                          + "A.x" + "\t" + "A.y" + "\t" + "前方拟合直线" + "\t" + "D.x"  + "\t" + "D.y" + "\t" + "左方直线" + "\t"
                          + "拐点.x " + "\t" + "拐点.y" + "\t" + "小车BC直线"
                          + "\r\n";
                    buffer = Encoding.Default.GetBytes(str);
                    rswrite.Write(buffer, 0, buffer.Length);
                }
                num3++;
                KeyPoint keys = new KeyPoint(drPort.getPosition());
                keys.w = keys.w * 180 / Math.PI;
                str = num3.ToString() + "\t" + keys.x.ToString("F3") + "\t" + keys.y.ToString("F3") + "\t" + keys.w.ToString("F3")
                             + "\t" + DisplayPoint.displayPoint.HLSpeed.ToString() + "\t" + DisplayPoint.displayPoint.HRSpeed.ToString() + "\t"
                             + DisplayPoint.displayPoint.TLSpeed.ToString() + "\t" + DisplayPoint.displayPoint.TRSpeed.ToString() + "\t"
                             + DisplayPoint.displayPoint.FrontUrgB.ToString() + "\t" + DisplayPoint.displayPoint.LeftUrgB.ToString() + "\t"
                             + DisplayPoint.displayPoint.RightUrgB.ToString() + "\t"

                             + DisplayPoint.displayPoint.pointA.x.ToString("F3") + "\t" + DisplayPoint.displayPoint.pointA.y.ToString("F3") + "\t"
                             + DisplayPoint.displayPoint.FrontLine.A.ToString() + "x" + "+" + DisplayPoint.displayPoint.FrontLine.B.ToString() + "y" + "+" + DisplayPoint.displayPoint.FrontLine.C.ToString("F1")+"\t"
                             + DisplayPoint.displayPoint.pointD.x.ToString("F3") + "\t" + DisplayPoint.displayPoint.pointD.y.ToString("F3") + "\t"
                             + DisplayPoint.displayPoint.LeftLine.A.ToString() + "x" + "+" + DisplayPoint.displayPoint.LeftLine.B.ToString() + "y" + "+" + DisplayPoint.displayPoint.LeftLine.C.ToString("F1") + "\t"
                             + DisplayPoint.displayPoint.point.x.ToString("F3") + "\t" + DisplayPoint.displayPoint.point.y.ToString("F3") + "\t"   
                             + DisplayPoint.displayPoint.pointB.x.ToString("F3") + "\t" + DisplayPoint.displayPoint.pointB.y.ToString("F3") + "\t"   
                             + DisplayPoint.displayPoint.pointC.x.ToString("F3") + "\t" + DisplayPoint.displayPoint.pointC.y.ToString("F3") + "\t"
                             + DisplayPoint.displayPoint.carLine.A.ToString() + "x" + "+" + DisplayPoint.displayPoint.carLine.B.ToString() + "y" + "+" + DisplayPoint.displayPoint.carLine.C.ToString("F1") + "\t"
                             + "\r\n";
                buffer = Encoding.Default.GetBytes(str);
                rswrite.Write(buffer, 0, buffer.Length);
            }

        }

        public void PrintInfo()
        {
            using (FileStream rswrite = new FileStream(@"E:\DadaStorage\ExcelFile_20210520\" + $"{DisplayPoint.displayPoint.FileName}", FileMode.Append, FileAccess.Write))
            {

                string str1 =  "校正成功" + "\r\n";
                byte[] buffer1 = Encoding.Default.GetBytes(str1);
                rswrite.Write(buffer1, 0, buffer1.Length); 

            }
        }
    }
}
