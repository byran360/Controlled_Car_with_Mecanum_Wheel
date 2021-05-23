using SmartCar.Map.Elem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SmartCar.Nav
{
    class Turn
    {
        /*************************** private field *****************************/
        private const double carWidth = 440;        // mm
        private const double carHeight = 1020;
        private double criticalAngle1 = Math.Atan(carHeight / 2 / (carWidth / 2));  // 前方的临界角
        private double criticalAngle2 = Math.Atan(carWidth / 2 / (carHeight / 2));  // 左方的临界角

        /**************************** public field *****************************/
        public static Config config;
        public struct Config
        {
            public bool Turned;

            public Vertexs carA, carB, carC, carD;
            public struct Vertexs { public double x, y; }

            public double DistanceA_L2;
            public double DistanceBC_F;
            public double DistanceD_L3;
            public double LimitD;           //距离阈值
            public double StopD;            //停止时
       

            public double wayWidth1;
            public double wayWidth2;
            public double wayWidth3;

            public int controlTime;

            public double disDiviation;
            public double angDiviation;
            public double disGain, angGain;

            public struct URG_POINT { public double x, y, a, d; }       //当前位置点

            public int State;

            // 墙壁信息
            public Wall wallL2, wallL3, carBC;
            public struct Wall { public double A, B, C; }

            // 转弯转折点
            public CornerPoint cornerPoint;
            public struct CornerPoint { public double x; public double y; }

        }

        public Turn()
        {
            config.Turned = false;

            config.LimitD = 150;         // 受限距离150mm
            config.StopD = 150;

            config.wayWidth1 = 1000;      // 通道均为1000mm
            config.wayWidth2 = 1000;
            config.wayWidth3 = 1000;

            config.controlTime = 100*5;    // 100ms

            config.angGain = 10;
            config.disGain = 0.3;
        }

        // 转第一个弯
        public void TurnFirstRight(IConPort conPort, UrgPort urgPort, IDrPort drPort)
        {
            //先摆正
            CorrectPosition correctPosition = new CorrectPosition();
            KeyPoint targetPoint = new KeyPoint() { UrgB = config.wayWidth2 - config.StopD, UrgK = 0 };
            correctPosition.Start(conPort, urgPort, targetPoint);
            KeyPoint currentPoint = new KeyPoint(drPort.getPosition());
            drPort.setPosition(currentPoint.x, currentPoint.y, Math.PI / 2);

            // 记录该点
            currentPoint.RecordTxt(drPort); currentPoint.RecordExcel(drPort);                 // 第五个点

            //获得此状态的环境信息
            while (!getEnviromentPoint(drPort, urgPort, 0)) ;

            //转弯
            while (!config.Turned)
            {
                getCarDistance(drPort);
                int ForwardSpeed = 0, LeftSpeed = 0, RotateSpeed = 0;
                config.State = judgeState();

                // 退出条件
                currentPoint = new KeyPoint(drPort.getPosition());
                if (currentPoint.w < 2 * Math.PI / 180 && currentPoint.w > -2 * Math.PI / 180) { config.Turned = true; break; }

                config.disDiviation = config.DistanceA_L2 - config.LimitD;
                config.angDiviation = currentPoint.w;

                useTurnSpeed(conPort, currentPoint, config.State, ForwardSpeed, RotateSpeed, LeftSpeed);

            }
        }

        // 转第二个弯
        public void TurnSecondRight(IConPort conPort, UrgPort urgPort, IDrPort drPort)
        {
            config.Turned = false;

            //先摆正
            CorrectPosition correctPosition = new CorrectPosition();
            KeyPoint targetPoint = new KeyPoint() { UrgB = config.wayWidth3 - config.StopD, UrgK = 0 };
            correctPosition.Start(conPort, urgPort, targetPoint);

            KeyPoint currentPoint = new KeyPoint(drPort.getPosition());
            drPort.setPosition(currentPoint.x, currentPoint.y, 0);

            // 记录该点
            currentPoint.RecordTxt(drPort); currentPoint.RecordExcel(drPort);             // 第九个点

            //获得此状态的环境信息
            while (!getEnviromentPoint(drPort, urgPort, 1)) ;

            //转弯
            while (!config.Turned)
            {
                getCarDistance(drPort);
                int ForwardSpeed = 0, LeftSpeed = 0, RotateSpeed = 0;
                config.State = judgeState();
                RecordExcel("State", config.State);

                // 退出条件
                currentPoint = new KeyPoint(drPort.getPosition());
                if (currentPoint.w < -88 * Math.PI / 180 && currentPoint.w > -92 * Math.PI / 180) { config.Turned = true; break; }
                config.disDiviation = config.DistanceA_L2 - config.LimitD;               // 0-650
                config.angDiviation = currentPoint.w + Math.PI / 2;                      // 0-1.57   

                useTurnSpeed(conPort, currentPoint, config.State, ForwardSpeed, RotateSpeed, LeftSpeed);
               
            }
        }


        /******************************************** private method *************************************************/
        private void useTurnSpeed(IConPort conPort, KeyPoint currentPoint, int mode, int ForwardSpeed, int RotateSpeed, int LeftSpeed)
        {
            switch (config.State)
            {
                case 0:
                    ForwardSpeed = (int)(config.disGain * config.disDiviation + 20);   // 20-50 mm/s
                    RotateSpeed = -(int)(config.angGain * config.angDiviation + 4);    // 5-10 rad/s
                    if (ForwardSpeed > 50) ForwardSpeed = 50;
                    if (RotateSpeed < -7) RotateSpeed = -7;
                                       
                     
                     conPort.Control_Move_By_Speed(ForwardSpeed, 0, 0);
                     System.Threading.Thread.Sleep(config.controlTime);
                     conPort.Control_Move_By_Speed(0, 0, RotateSpeed);
                     System.Threading.Thread.Sleep(config.controlTime); 
                    
                    break;

                case 1:
                    LeftSpeed = 30;
                    RotateSpeed = -(int)(config.angGain * config.angDiviation + 5);
                    if (RotateSpeed < -7) RotateSpeed = -7;           // 11 °/s
                    
                    ForwardSpeed = 60;
                    conPort.Control_Move_By_Speed(0, LeftSpeed, 0);
                    System.Threading.Thread.Sleep(config.controlTime);
                    conPort.Control_Move_By_Speed(ForwardSpeed, 0, 0);
                    System.Threading.Thread.Sleep(config.controlTime);
                    break;

                case 2:
                    ForwardSpeed = (int)(config.disGain * config.disDiviation + 30);   // 20-50 mm/s
                    if (ForwardSpeed > 60) ForwardSpeed = 60;
                    conPort.Control_Move_By_Speed(ForwardSpeed, 0, 0);
                    System.Threading.Thread.Sleep(config.controlTime);
                    if (currentPoint.w < criticalAngle2)
                    {
                        RotateSpeed = -(int)(config.angGain * config.angDiviation + 5);     // 5-10 rad/s
                        if (RotateSpeed < -7) RotateSpeed = -7;
                        conPort.Control_Move_By_Speed(0, 0, RotateSpeed);
                        System.Threading.Thread.Sleep(config.controlTime);
                    }
                   break;

                case 3:
                    ForwardSpeed = (int)(config.disGain * config.disDiviation + 30);   // 20-50 mm/s
                    if (ForwardSpeed > 60) ForwardSpeed = 60;
                    conPort.Control_Move_By_Speed(ForwardSpeed, 0, 0);
                    System.Threading.Thread.Sleep(config.controlTime); break;

                case 4:
                    if (currentPoint.w < criticalAngle1)
                    {
                        RotateSpeed = -(int)(config.angGain * config.angDiviation + 5);    // -5~10 rad/s
                        if (RotateSpeed < -7) RotateSpeed = -7;
                        conPort.Control_Move_By_Speed(0, 0, RotateSpeed);
                        System.Threading.Thread.Sleep(config.controlTime); break;
                    }
                    else
                    {
                        LeftSpeed = -(int)(config.disGain * (config.DistanceBC_F - config.LimitD) + 10);
                        if (LeftSpeed < -30) ForwardSpeed = -30;
                        conPort.Control_Move_By_Speed(0, LeftSpeed, 0);
                        System.Threading.Thread.Sleep(config.controlTime);
                        break;
                    }

                case 5:
                    if (currentPoint.w < criticalAngle1)
                    {
                        RotateSpeed = -(int)(config.angGain * config.angDiviation + 5);    // 5~10 rad/s
                        if (RotateSpeed < -7) RotateSpeed = -7;
                        conPort.Control_Move_By_Speed(0, 0, RotateSpeed);
                        System.Threading.Thread.Sleep(config.controlTime); break;
                    }
                    break;

                case 6:
                    LeftSpeed = -(int)(config.disGain * (config.DistanceBC_F - config.LimitD));
                    if (LeftSpeed < -30) ForwardSpeed = -30;
                    conPort.Control_Move_By_Speed(0, LeftSpeed, 0);
                    System.Threading.Thread.Sleep(config.controlTime);
                    break;

                case 7:
                    break;
                default: break;
            }
        }

        private int judgeState()
        {
            // 0 0 0
            if (config.DistanceA_L2 > config.LimitD && config.DistanceD_L3 > config.LimitD && config.DistanceBC_F > config.LimitD) { config.State = 0; }
            // 0 0 1
            else if (config.DistanceA_L2 > config.LimitD && config.DistanceD_L3 > config.LimitD && config.DistanceBC_F < 170) { config.State = 1; }
           // 0 1 0
            else if (config.DistanceA_L2 > config.LimitD && config.DistanceD_L3 < config.LimitD && config.DistanceBC_F > config.LimitD) { config.State = 2; }
            // 0 1 1
            else if (config.DistanceA_L2 > config.LimitD && config.DistanceD_L3 < config.LimitD && config.DistanceBC_F < config.LimitD) { config.State = 3; }
            // 1 0 0
            else if (config.DistanceA_L2 < config.LimitD && config.DistanceD_L3 > config.LimitD && config.DistanceBC_F > config.LimitD) { config.State = 4; }
            // 1 0 1
            else if (config.DistanceA_L2 < config.LimitD && config.DistanceD_L3 > config.LimitD && config.DistanceBC_F < config.LimitD) { config.State = 5; }
            // 1 1 0
            else if (config.DistanceA_L2 < config.LimitD && config.DistanceD_L3 < config.LimitD && config.DistanceBC_F > config.LimitD) { config.State = 6; }
            // 1 1 1
            else if (config.DistanceA_L2 < config.LimitD && config.DistanceD_L3 < config.LimitD && config.DistanceBC_F < config.LimitD) { config.State = 7; }
            return config.State;
        }

        // 得到小车顶点位置
        private void getCurrentVertex(KeyPoint keyPoint)    // 单位m
        {
            double L1 = carHeight / 1000 / 2;
            double L2 = carWidth / 1000 / 2;
            config.carA.x = keyPoint.x + L1 * Math.Cos(keyPoint.w) - L2 * Math.Sin(keyPoint.w);  //A点
            config.carA.y = keyPoint.y + L1 * Math.Sin(keyPoint.w) + L2 * Math.Cos(keyPoint.w);
            config.carB.x = keyPoint.x + L1 * Math.Cos(keyPoint.w) + L2 * Math.Sin(keyPoint.w);  //B点
            config.carB.y = keyPoint.y + L1 * Math.Sin(keyPoint.w) - L2 * Math.Cos(keyPoint.w);
            config.carC.x = keyPoint.x - L1 * Math.Cos(keyPoint.w) + L2 * Math.Sin(keyPoint.w);  //C点
            config.carC.y = keyPoint.y - L1 * Math.Sin(keyPoint.w) - L2 * Math.Cos(keyPoint.w);
            config.carD.x = keyPoint.x - L1 * Math.Cos(keyPoint.w) - L2 * Math.Sin(keyPoint.w);  //D点
            config.carD.y = keyPoint.y - L1 * Math.Sin(keyPoint.w) + L2 * Math.Cos(keyPoint.w);
            DisplayPoint.displayPoint.pointA.x = config.carA.x;
            DisplayPoint.displayPoint.pointA.y = config.carA.y;
            DisplayPoint.displayPoint.pointB.x = config.carB.x;
            DisplayPoint.displayPoint.pointB.y = config.carB.y;
            DisplayPoint.displayPoint.pointC.x = config.carC.x;
            DisplayPoint.displayPoint.pointC.y = config.carC.y;
            DisplayPoint.displayPoint.pointD.x = config.carD.x;
            DisplayPoint.displayPoint.pointD.y = config.carD.y;

        }

        // 环境信息
        private bool getEnviromentPoint(IDrPort drPort, UrgPort urgPort, int mode)
        {
            KeyPoint currentPoint = new KeyPoint(drPort.getPosition());

            //得到两个墙壁直线和一个拐点的位置
            if (mode == 0)       // 第一个弯环境信息   坐标单位：m
            {
                config.cornerPoint.x = currentPoint.x + config.wayWidth1 / 1000 / 2;
                config.cornerPoint.y = currentPoint.y + (carHeight / 2 - config.StopD) / 1000;
                config.wallL2.A = 0;
                config.wallL2.B = 1;
                config.wallL2.C = -(currentPoint.y + (carHeight / 2 + config.wayWidth2 - config.StopD) / 1000);
                config.wallL3.A = 1;
                config.wallL3.B = 0;
                config.wallL3.C = -(currentPoint.x - config.wayWidth1 / 1000 / 2);
            }
            else  // 第二个弯环境信息
            {
                config.cornerPoint.x = currentPoint.x + (carHeight / 2 - config.StopD) / 1000;
                config.cornerPoint.y = currentPoint.y - config.wayWidth2 / 2 / 1000;
                config.wallL2.A = 1;
                config.wallL2.B = 0;
                config.wallL2.C = -(currentPoint.x + (carHeight / 2 + config.wayWidth3 - config.StopD) / 1000);
                config.wallL3.A = 0;
                config.wallL3.B = 1;
                config.wallL3.C = -(currentPoint.y + config.wayWidth2 / 2 / 1000);
            }
            DisplayPoint.displayPoint.point.x = config.cornerPoint.x;
            DisplayPoint.displayPoint.point.y = config.cornerPoint.y;
            DisplayPoint.displayPoint.FrontLine.A = config.wallL2.A;
            DisplayPoint.displayPoint.FrontLine.B = config.wallL2.B;
            DisplayPoint.displayPoint.FrontLine.C = config.wallL2.C;
            DisplayPoint.displayPoint.LeftLine.A = config.wallL3.A;
            DisplayPoint.displayPoint.LeftLine.B = config.wallL3.B;
            DisplayPoint.displayPoint.LeftLine.C = config.wallL3.C;
            return true;
        }

        // 小车三边距离
        private void getCarDistance(IDrPort drPort)
        {
            KeyPoint currentPoint = new KeyPoint(drPort.getPosition());
            getCurrentVertex(currentPoint);
            getBCLine(currentPoint);
            config.DistanceA_L2 = getPointLineDistance(config.carA, config.wallL2);
            config.DistanceD_L3 = getPointLineDistance(config.carD, config.wallL3);
            config.DistanceBC_F = getPointLineDistance(config.cornerPoint, config.carBC);
            DisplayPoint.displayPoint.FrontUrgB = config.DistanceA_L2;
            DisplayPoint.displayPoint.LeftUrgB = config.DistanceD_L3;
            DisplayPoint.displayPoint.RightUrgB = config.DistanceBC_F;
        }

        // 小车BC边
        private void getBCLine(KeyPoint keyPoint)
        {
            if (keyPoint.w < 92 * Math.PI / 180 && keyPoint.w > 88 * Math.PI / 180)   // BC垂线
            {
                config.carBC.A = 1;
                config.carBC.B = 0;
                config.carBC.C = -(config.carB.x);
            }
            else if (keyPoint.w < 2 * Math.PI / 180 && keyPoint.w > -2 * Math.PI / 180)     // BC水平线
            {
                config.carBC.A = 0;
                config.carBC.B = 1;
                config.carBC.C = -(config.carB.y);
            }
            else  //两点式
            {
                config.carBC.A = config.carB.y - config.carC.y;
                config.carBC.B = -(config.carB.x - config.carC.x);
                config.carBC.C = -config.carC.y * config.carBC.B - config.carC.x * config.carBC.A;
            }
            DisplayPoint.displayPoint.carLine.A = config.carBC.A;
            DisplayPoint.displayPoint.carLine.B = config.carBC.B;
            DisplayPoint.displayPoint.carLine.C = config.carBC.C;
        }

        private double getPointLineDistance(Config.Vertexs vertexs, Config.Wall wall)
        {
            return (Math.Abs(wall.A * vertexs.x + wall.B * vertexs.y + wall.C) / Math.Sqrt(wall.A * wall.A + wall.B * wall.B)) * 1000;  // mm 
        }
        private double getPointLineDistance(Config.CornerPoint cornerPoint, Config.Wall wall)
        {
            return (Math.Abs(wall.A * cornerPoint.x + wall.B * cornerPoint.y + wall.C) / Math.Sqrt(wall.A * wall.A + wall.B * wall.B) * 1000);  //mm
        }


        private double getPointPiontDistance(Config.Vertexs vertexs, Config.CornerPoint cornerPoint)
        {
            return Math.Sqrt((vertexs.x - cornerPoint.x) * (vertexs.x - cornerPoint.x) + (vertexs.y - cornerPoint.y) * (vertexs.y - cornerPoint.y));
        }

        private static int num2 = -1;
        public void RecordExcel(string Filename, int state)
        {
            using (FileStream rswrite = new FileStream(@"E:\DadaStorage\ExcelFile_20210516\" + $"{Filename}", FileMode.Append, FileAccess.Write))
            {
                string str;
                byte[] buffer;
                if (num2 == -1)
                {
                    str = " " + "\t" + "State"
                          + "\r\n";
                    buffer = Encoding.Default.GetBytes(str);
                    rswrite.Write(buffer, 0, buffer.Length);
                }
                num2++;
                str = num2.ToString() + "\t" + state.ToString()
                      + "\r\n";
                buffer = Encoding.Default.GetBytes(str);
                rswrite.Write(buffer, 0, buffer.Length);
            }
        }




    }
}
