using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SmartCar.Map.Elem
{
    public class DisplayPoint
    {
        public static DisplayPoint displayPoint = new DisplayPoint();

        public string FileName = "UBending_tra1.5.xls";
        public string FileName1 = "OneSecondUBending_tra_1_6.xls";

        public string TxtFileName = "UBending.txt";

        // 位置信息（x,y,w）   编码器得到
        public double x { get; set; }
        public double y { get; set; }
        public double w { get; set; }

        // 激光雷达得到
        public double MaxGap { get; set; }

        public double FrontUrgK { get; set; }  //拟合出小车前方直线的斜率（单位：度）
        public double FrontUrgB { get; set; }  //拟合出小车前方直线的截距（单位：mm）

        public double LeftUrgK { get; set; }  //拟合出小车左方直线的斜率（单位：度）
        public double LeftUrgB { get; set; }  //拟合出小车左方直线的截距（单位：mm）


        public double RightUrgK { get; set; }  //拟合出小车右方直线的斜率（单位：度）
        public double RightUrgB { get; set; }  //拟合出小车左方直线的截距（单位：mm）


        // 四个轮子速度  编码器得到
        public double HLSpeed;
        public double TLSpeed;
        public double HRSpeed;
        public double TRSpeed;

        // 小车顶点、拐点和墙壁直线
        public struct Point { public double x; public double y; }
        public struct Line { public double A; public double B; public double C; }
        public Point point, pointA, pointB, pointC, pointD;
        public Line FrontLine, LeftLine, carLine;

        // UI通道口宽度文本框设置
        public bool VisibleMaxGap;

        public DisplayPoint() 
        {
            x = 0;
            y = 0;
            w = 0;

            MaxGap = 0;

            FrontUrgB = 0;
            LeftUrgB = 0;
            RightUrgB = 0;

            FrontUrgK = 0;
            LeftUrgK = 0;
            RightUrgK = 0;

            VisibleMaxGap = true;

            pointA.x = 0;
            pointA.y = 0;
            pointB.x = 0;
            pointB.y = 0;
            pointC.x = 0;
            pointC.y = 0;
            pointD.x = 0;
            pointD.y = 0;


        }

        public DisplayPoint(KeyPoint p)
        {
            this.x = p.x;
            this.y = p.y;
            this.w = p.w;
        }

    }
}
