using SmartCar.Map.Elem;
using System;
using System.Collections.Generic;

namespace SmartCar
{
    class Forward
    {
        private static CONFIG config;

        private struct CONFIG
        {
            public bool AchieveTarget;     //是否到到指定位置
            public bool AilseFounded;       //是否找到通道入口

            public int MaxForwardSpeed;         //最大前进速度
            public int MaxTranslateSpeed;       //最大平移速度，向左为正
            public int MaxRotateSpeed;          //最大旋转速度

            public PD_PARAMETER PD_F, PD_T, PD_R;

            public KeyPoint PreviousPos;

            public double ForwardDistance;

            //PD参数定义，PD的参数：Kp,Kd
            public struct PD_PARAMETER { public double Kp, Kd; public double Error2, Error1, Error0; }

            public struct URG_POINT { public double x, y, a, d; }       //当前位置点
        }

        public Forward()
        {
            config.AchieveTarget = false;    //未达到目的地
            config.AilseFounded = false;

            config.MaxForwardSpeed = InfoManager.moveIF.MaxFront;       //int.Parse(DataArea.infoModel.Data[(int)FileInfo.paramE.MaxFront]);
            config.MaxTranslateSpeed = 30;
            config.MaxRotateSpeed = 5;
            config.ForwardDistance = 1000 - 150;       //1000 - 150

            config.PD_F.Kp = 1;         //前进
            config.PD_F.Kd = 0;
            config.PD_F.Error0 = 0;
            config.PD_F.Error1 = 0;
            config.PD_F.Error2 = 0;

            config.PD_T.Kp = 0.3;       //转弯
            config.PD_T.Kd = 0;
            config.PD_T.Error0 = 0;
            config.PD_T.Error1 = 0;
            config.PD_T.Error2 = 0;

            config.PD_R.Kp = 0.6;      //旋转
            config.PD_R.Kd = 0;
            config.PD_R.Error0 = 0;
            config.PD_R.Error1 = 0;
            config.PD_R.Error2 = 0;

            config.AchieveTarget = false;
        }


        /****************************************** public method **************************************************/
        // 进入通道，U型弯使用
        public void EnterAilse(KeyPoint targetPoint, double keepLeft, IConPort conPort, IUrgPort urgPort, IDrPort drPort)
        {
            KeyPoint keyPoint = new KeyPoint();
            // 找通道入口
            FoundEntrance(conPort, urgPort, drPort);
            keyPoint.RecordTxt(drPort); keyPoint.RecordExcel(drPort);      // 第二个点和第六个点
            conPort.Control_Move_By_Speed(0, 0, 0);
            System.Threading.Thread.Sleep(1000);

            // 调整身位
            useRotateSpeed(conPort, urgPort);
            useTranslateSpeed(conPort, urgPort);
            DisplayPoint.displayPoint.VisibleMaxGap = false;

            // 记录该点
            keyPoint.RecordTxt(drPort); keyPoint.RecordExcel(drPort);       // 第三个点和第七个点

            // 通道内行走，到达目的地
            config.AchieveTarget = false;
            AilseRunning(conPort, urgPort, drPort);
            keyPoint.RecordTxt(drPort); keyPoint.RecordExcel(drPort);         // 第四个点和第八个点
            conPort.Control_Move_By_Speed(0, 0, 0);
            System.Threading.Thread.Sleep(1000);
        }

        // 离开通道
        public void LeaveAilse(IConPort conPort, IUrgPort urgPort, IDrPort drPort)
        {
            // 调整身位
            useRotateSpeed(conPort, urgPort);
            useTranslateSpeed(conPort, urgPort);

            // 记录该点
            KeyPoint keyPoint = new KeyPoint();
            keyPoint.RecordTxt(drPort); keyPoint.RecordExcel(drPort);           // 第十一个点

            // 出通道
            config.AchieveTarget = false;
            LeaveEntrance(conPort, urgPort, drPort);
            conPort.Control_Move_By_Speed(0, 0, 0);
            System.Threading.Thread.Sleep(1000);
        }


        // 测试使用
        public void EnterAilse(IConPort conPort, IUrgPort urgPort, IDrPort drPort)
        {
            KeyPoint keyPoint = new KeyPoint();

            // 找通道入口
            FoundEntrance(conPort, urgPort, drPort);
            conPort.Control_Move_By_Speed(0, 0, 0);
            System.Threading.Thread.Sleep(1000);

            // 调整身位
            useRotateSpeed(conPort, urgPort);
            useTranslateSpeed(conPort, urgPort);

            // 通道内行走，到达目的地
            config.AchieveTarget = false;
            AilseRunning(conPort, urgPort, drPort);
            keyPoint.RecordExcel(drPort);
            conPort.Control_Move_By_Speed(0, 0, 0);
            System.Threading.Thread.Sleep(1000);
        }

        // 不用
        public void Start(KeyPoint targetPoint, KeyPoint lastPoint, double keepLeft, IConPort conPort, IUrgPort urgPort, IDrPort drPort)
        {
            if (ControlMethod.curState == ControlMethod.ctrlItem.ExpMap)
            {
                int setLeft = InfoManager.wayIF.WayLeft * 10;//10 * int.Parse(DataArea.infoModel.Data[(int)FileInfo.paramE.WayLeft]);
                int setRigh = InfoManager.wayIF.WayRight * 10;//10 * int.Parse(DataArea.infoModel.Data[(int)FileInfo.paramE.WayRight]);
                keepLeft = (Form_Path.wayType == 0) ? 0 :
                           (Form_Path.wayType == 1) ? setLeft : -setRigh;
            }

            // 记录距离，自己决定开启
            AlignAisle align = new AlignAisle();
            double record = align.recordDistance();
            align.Start();

            Backward backward = new Backward();
            backward.clear();

            config.PreviousPos = drPort.getPosition();

            #region 找到通道入口

            while (true)
            {
                // 获取速度
                int ySpeed = getForwardSpeed(config.MaxForwardSpeed, urgPort, drPort);
                int xSpeed = 0;
                int wSpeed = 0;

                // 退出条件
                List<CONFIG.URG_POINT> pointsL = getUrgPoint(160, 180, urgPort);
                List<CONFIG.URG_POINT> pointsR = getUrgPoint(0, 20, urgPort);

                double minL = double.MaxValue, minR = double.MaxValue;
                for (int i = 0; i < pointsL.Count; i++)
                {
                    double x = Math.Abs(pointsL[i].x);
                    if (x < minL) { minL = x; }
                }
                for (int i = 0; i < pointsR.Count; i++)
                {
                    double x = Math.Abs(pointsR[i].x);
                    if (x < minR) { minR = x; }
                }

                if (minL < 1000 || minR < 1000) { break; }

                // 控制
                conPort.Control_Move_By_Speed(ySpeed, xSpeed, wSpeed);

                // 比较之前与现在的位置
                KeyPoint currentPos = drPort.getPosition();

                bool recored = currentPos.x != config.PreviousPos.x ||
                    currentPos.y != config.PreviousPos.y ||
                    currentPos.w != config.PreviousPos.w;

                config.PreviousPos = currentPos;

                if (!PortManager.conPort.IsStop && recored)
                {
                    Backward.COMMAND command = new Backward.COMMAND();
                    command.ForwardSpeed = ySpeed;
                    command.LeftSpeed = xSpeed;
                    command.RotateSpeed = wSpeed;
                    backward.set(command);
                }



                System.Threading.Thread.Sleep(100);
            }
            // 2019-10-9 改
            drPort.setPosition(lastPoint);
            #endregion

            //backward.startpoint = drPort.getPosition();

            #region 通道内行走

            if (keepLeft < 0) { keepLeft -= 225; }
            if (keepLeft > 0) { keepLeft += 225; }

            while (!config.AchieveTarget)
            {
                int ForwardSpeed = getForwardSpeed(config.MaxForwardSpeed, urgPort, drPort);
                int TranslateSpeed = getTranslateSpeed(conPort, urgPort, drPort);
                int RotateSpeed = getRotateSpeed(conPort, urgPort, drPort);

                // 距离限速
                List<CONFIG.URG_POINT> pointsH = getUrgPoint(85, 95, urgPort);
                double minH = double.MaxValue;
                for (int i = 0; i < pointsH.Count; i++)
                { if (minH > pointsH[i].y) { minH = pointsH[i].y; } }
                if (minH < 1200)
                {
                    TranslateSpeed = 0;
                    //RotateSpeed = 0;
                }

                double current = drPort.getPosition().y;
                while (Math.Abs(current - targetPoint.y) < 0.02) { break; }

                conPort.Control_Move_By_Speed(ForwardSpeed, TranslateSpeed, RotateSpeed);

                // 比较之前与现在的位置
                KeyPoint currentPos = drPort.getPosition();

                bool recored = currentPos.x != config.PreviousPos.x ||
                    currentPos.y != config.PreviousPos.y ||
                    currentPos.w != config.PreviousPos.w;

                config.PreviousPos = currentPos;

                if (!PortManager.conPort.IsStop && recored)
                {
                    Backward.COMMAND command = new Backward.COMMAND();
                    command.ForwardSpeed = ForwardSpeed;
                    command.LeftSpeed = TranslateSpeed;
                    command.RotateSpeed = RotateSpeed;
                    backward.set(command);
                }
                System.Threading.Thread.Sleep(100);
            }

            #endregion
            if (ControlMethod.curState == ControlMethod.ctrlItem.ExpMap)
            {
                ProcessNewMap.markKeyPoint(1, record);
            }

            // 校准方式1
            //CorrPos(targetPoint, conPort, drPort);

            // 校准方式2
            //CorrectPosition corrp = new CorrectPosition();
            //corrp.Start(PortManager.conPort, PortManager.urgPort, targetPoint);
            //PortManager.drPort.setPosition(targetPoint);

            // 单一路径返回
            if (ControlMethod.curState == ControlMethod.ctrlItem.ExpMap && !Form_Path.wayBack)
            {
                return;
            }
            if (ControlMethod.curState == ControlMethod.ctrlItem.GoMap && !targetPoint.moveBack)
            {
                return;
            }

            // 后退
            conPort.Control_Move_By_Speed(0, 0, 0);
            System.Threading.Thread.Sleep(1000);

            backward.Start();

            //2019-10-9 改 调整距离改 
            double showdistance = 0;
            showdistance = align.recordDistance2();
            //  Console.Clear();
            //  Console.WriteLine("距离长度 {0} ", showdistance);
            align.adjustDistance2(3350);//调整出口时y的位置 3350
            showdistance = align.recordDistance2();
            //Console.WriteLine("距离长度 {0} ", showdistance);
            drPort.setPosition(targetPoint);
            // 调整距离，自己决定开启
            /*
            if (ControlMethod.curState == ControlMethod.ctrlItem.GoMap)
            {
                // 大于10mm开启调整
                if (targetPoint.disWay > 10)
                {
                    align.adjustDistance(targetPoint.disWay);
                }
            }
            else if (ControlMethod.curState == ControlMethod.ctrlItem.ExpMap)
            {
                align.adjustDistance(record);
            }
                  */
            //drPort.setPosition(targetPoint);
        }




        /*************************************** private method *******************************************/

        private void FoundEntrance(IConPort conPort, IUrgPort urgPort, IDrPort drPort)
        {
            while (!config.AilseFounded)
            {
                // 获取速度
                int ySpeed = getForwardSpeed(config.MaxForwardSpeed, urgPort, drPort);
                int xSpeed = 0;
                int wSpeed = 0;

                // 退出条件
                List<CONFIG.URG_POINT> pointsL = getUrgPoint(160, 180, urgPort);
                List<CONFIG.URG_POINT> pointsR = getUrgPoint(0, 20, urgPort);

                // 通道太窄就退出,即已经达到通道入口
                double minL = double.MaxValue, minR = double.MaxValue;
                for (int i = 0; i < pointsL.Count; i++)
                {
                    double x = Math.Abs(pointsL[i].x);
                    if (x < minL) { minL = x; }
                }
                for (int i = 0; i < pointsR.Count; i++)
                {
                    double x = Math.Abs(pointsR[i].x);
                    if (x < minR) { minR = x; }
                }
                DisplayPoint.displayPoint.LeftUrgB = minL;
                DisplayPoint.displayPoint.RightUrgB = minR;
                if (minL < 1000 && minR < 1000) { config.AilseFounded = true; break; }

                // 控制行进
                conPort.Control_Move_By_Speed(ySpeed, xSpeed, wSpeed);
                System.Threading.Thread.Sleep(100);
            }
        }

        private void AilseRunning(IConPort conPort, IUrgPort urgPort, IDrPort drPort)
        {
            while (!config.AchieveTarget)
            {
                int TranslateSpeed, ForwardSpeed, RotateSpeed;

                /*
                // 得到前方距离
                List<CONFIG.URG_POINT> pointsH = getUrgPoint(85, 95, urgPort);
                double minH = double.MaxValue;
                for (int i = 0; i < pointsH.Count; i++)
                {
                    if (minH > pointsH[i].y) { minH = pointsH[i].y; }
                }
                DisplayPoint.displayPoint.FrontUrgB = minH;

                //速度控制
                if (minH < 1200) TranslateSpeed = 0;
                */
                TranslateSpeed = getTranslateSpeed(conPort, urgPort, drPort);
                ForwardSpeed = getForwardSpeed(config.MaxForwardSpeed, urgPort, drPort);  //退出语句在里面
                RotateSpeed = getRotateSpeed(conPort, urgPort, drPort);

                conPort.Control_Move_By_Speed(ForwardSpeed, TranslateSpeed, RotateSpeed);
                System.Threading.Thread.Sleep(100);
                /*
                conPort.Control_Move_By_Speed(ForwardSpeed, 0, 0);
                System.Threading.Thread.Sleep(500);
                conPort.Control_Move_By_Speed(0, TranslateSpeed, 0);
                System.Threading.Thread.Sleep(500);
                conPort.Control_Move_By_Speed(0, 0, RotateSpeed);
                System.Threading.Thread.Sleep(500);
                */
            }
        }

        private void LeaveEntrance(IConPort conPort, IUrgPort urgPort, IDrPort drPort)
        {
            int TranslateSpeed, ForwardSpeed, RotateSpeed;
            // 出通道
            while (!config.AchieveTarget)
            {
                // 退出条件
                List<CONFIG.URG_POINT> pointsL = getUrgPoint(160, 180, urgPort);
                List<CONFIG.URG_POINT> pointsR = getUrgPoint(0, 20, urgPort);

                // 通道太宽就退出,即已经达到通道入口
                double minL = double.MaxValue, minR = double.MaxValue;
                for (int i = 0; i < pointsL.Count; i++)
                {
                    double x = Math.Abs(pointsL[i].x);
                    if (x < minL) { minL = x; }
                }
                for (int i = 0; i < pointsR.Count; i++)
                {
                    double x = Math.Abs(pointsR[i].x);
                    if (x < minR) { minR = x; }
                }
                DisplayPoint.displayPoint.LeftUrgB = minL;
                DisplayPoint.displayPoint.RightUrgB = minR;

                if (minL > 1000 && minR > 1000) { config.AchieveTarget = true; break; }
                if (minL == 0 || minR == 0) { config.AchieveTarget = true; break; }

                List<CONFIG.URG_POINT> pointsH = getUrgPoint(85, 95, urgPort);
                // 换成绝对距离
                for (int i = 0; i < pointsH.Count; i++)
                { CONFIG.URG_POINT point = pointsH[i]; point.y = Math.Abs(point.y); pointsH[i] = point; }
                double minH = double.MaxValue;
                for (int i = 0; i < pointsH.Count; i++)
                { if (minH > pointsH[i].y) { minH = pointsH[i].y; } }
                DisplayPoint.displayPoint.FrontUrgB = minH;

                //速度控制
                TranslateSpeed = getTranslateSpeed(conPort, urgPort, drPort); //getTranslateSpeed(conPort, urgPort, drPort);
                ForwardSpeed = 60;   //退出语句在里面
                RotateSpeed = getRotateSpeed(conPort, urgPort, drPort);

                conPort.Control_Move_By_Speed(ForwardSpeed, TranslateSpeed, RotateSpeed);
                System.Threading.Thread.Sleep(100);


            }
        }

        // 得到前进速度
        private int getForwardSpeed(int keepSpeed, IUrgPort urgPort, IDrPort drPort)
        {
            // 取点
            List<CONFIG.URG_POINT> pointsH = getUrgPoint(85, 95, urgPort);

            // 数量不够
            if (pointsH.Count == 0) { return keepSpeed; }

            // 换成绝对距离
            for (int i = 0; i < pointsH.Count; i++)
            { CONFIG.URG_POINT point = pointsH[i]; point.y = Math.Abs(point.y); pointsH[i] = point; }

            // 是否达到目的地
            double minH = double.MaxValue;
            for (int i = 0; i < pointsH.Count; i++)
            { if (minH > pointsH[i].y) { minH = pointsH[i].y; } }
            DisplayPoint.displayPoint.FrontUrgB = minH;
            if (minH < config.ForwardDistance) { config.AchieveTarget = true; return 0; }   //到达目的地

            //PD控制前进速度
            double current = minH;
            double target = config.ForwardDistance;
            if (Math.Abs(current - config.ForwardDistance) < 20) { config.AchieveTarget = true; return 0; } //到达目的地
            int ForwardSpeed = (int)PDcontroller(current, target, ref config.PD_F);

            // 限速
            if (ForwardSpeed > config.MaxForwardSpeed) { return config.MaxForwardSpeed; }
            if (ForwardSpeed < -config.MaxForwardSpeed) { return -config.MaxForwardSpeed; }
            return ForwardSpeed;
        }

        private int getForwardSpeed(IUrgPort urgPort, IDrPort drPort)
        {
            while (true)
            {
                // 取点
                List<CONFIG.URG_POINT> pointsH = getUrgPoint(85, 95, urgPort);

                // 数量不够
                if (pointsH.Count == 0) { continue; }

                // 换成绝对距离
                for (int i = 0; i < pointsH.Count; i++)
                { CONFIG.URG_POINT point = pointsH[i]; point.y = Math.Abs(point.y); pointsH[i] = point; }

                // 是否达到目的地
                double minH = double.MaxValue;
                for (int i = 0; i < pointsH.Count; i++)
                { if (minH > pointsH[i].y) { minH = pointsH[i].y; } }
                DisplayPoint.displayPoint.FrontUrgB = minH;

                //PD控制前进速度
                double current = minH;
                double target = config.ForwardDistance;
                int ForwardSpeed = (int)PDcontroller(current, target, ref config.PD_F);

                // 限速
                if (ForwardSpeed > config.MaxForwardSpeed) { return config.MaxForwardSpeed; }
                if (ForwardSpeed < -config.MaxForwardSpeed) { return -config.MaxForwardSpeed; }
                return ForwardSpeed;
            }

        }

        // 得到平移速度
        private int getTranslateSpeed(IConPort conPort, IUrgPort urgPort, IDrPort drPort)
        {
            double distanceL = double.MaxValue;
            double distanceR = double.MaxValue;
            double AisleWidth = 1000;   // 通道宽度(mm)
            int TranslateSpeed = 0;     // 平移速度
            bool acceptL, acceptR;      // 判断左右距离行吗

            // 激光雷达扫两边
            List<CONFIG.URG_POINT> pointsL = getUrgPoint(160, 180, urgPort);
            List<CONFIG.URG_POINT> pointsR = getUrgPoint(0, 20, urgPort);

            // 两边最短距离
            double minL = double.MaxValue, minR = double.MaxValue;
            for (int i = 0; i < pointsL.Count; i++)
            {
                double x = Math.Abs(pointsL[i].x);
                if (x < minL) { minL = x; }
            }
            for (int i = 0; i < pointsR.Count; i++)
            {
                double x = Math.Abs(pointsR[i].x);
                if (x < minR) { minR = x; }
            }
            distanceL = Math.Min(distanceL, minL);
            distanceR = Math.Min(distanceR, minR) + 40;
            DisplayPoint.displayPoint.LeftUrgB = distanceL;
            DisplayPoint.displayPoint.RightUrgB = distanceR;

            // 数据能否使用
            if (distanceL > 0 && distanceL < AisleWidth && distanceR > 0 && distanceR < AisleWidth) { acceptL = true; acceptR = true; }
            else { acceptL = false; acceptR = false; }

            // 数据无效模式
            if (!acceptL && !acceptR) { return 0; }
            if (distanceL == 0 && distanceR == 0) { return 0; }

            // 速度控制
            double current = distanceL;
            double target = (distanceL + distanceR) / 2;
            TranslateSpeed = (int)PDcontroller(current, target, ref config.PD_T);
            if (TranslateSpeed > config.MaxTranslateSpeed) { return config.MaxTranslateSpeed; }
            if (TranslateSpeed < -config.MaxTranslateSpeed) { return -config.MaxTranslateSpeed; }
            return TranslateSpeed;
        }

        // 得到旋转速度
        private int getRotateSpeed(IConPort conPort, IUrgPort urgPort, IDrPort drPort)
        {
            List<CONFIG.URG_POINT> pointsH = getUrgPoint(85, 95, urgPort);
            int RotateSpeed = 0;     //旋转速度

            // 取点
            List<CONFIG.URG_POINT> pointsL = getUrgPoint(120, 180, urgPort);
            List<CONFIG.URG_POINT> pointsR = getUrgPoint(0, 60, urgPort);

            // 交换并取绝对坐标
            for (int i = 0; i < pointsL.Count; i++)
            {
                CONFIG.URG_POINT point = pointsL[i];
                double tempx = Math.Abs(point.x);
                double tempy = Math.Abs(point.y);
                point.x = tempy; point.y = tempx; pointsL[i] = point;
            }
            for (int i = 0; i < pointsR.Count; i++)
            {
                CONFIG.URG_POINT point = pointsR[i];

                double tempx = Math.Abs(point.x);
                double tempy = Math.Abs(point.y);
                point.x = tempy; point.y = tempx; pointsR[i] = point;
            }

            // 滤波，太远的不要
            for (int i = pointsL.Count - 1; i >= 0; i--)
            {
                if (pointsL[i].y > 700) { pointsL.RemoveAt(i); }
            }
            for (int i = pointsR.Count - 1; i >= 0; i--)
            {
                if (pointsR[i].y > 700) { pointsR.RemoveAt(i); }
            }

            // 拟合左右两边障碍物信息
            pointsL = SortPoints(pointsL);
            pointsR = SortPoints(pointsR);
            //   pointsL = getFitPoints(pointsL);
            //  pointsR = getFitPoints(pointsR);
            double[] KAB_L = getFitLine(pointsL);
            double[] KAB_R = getFitLine(pointsR);
            DisplayPoint.displayPoint.LeftUrgK = KAB_L[0];
            DisplayPoint.displayPoint.RightUrgK = KAB_R[0];

            // 点数量不够
            bool acceptL = pointsL.Count > 10;
            bool acceptR = pointsR.Count > 10;
            if (!acceptL && !acceptR) { return 0; }

            // 控制策略
            if (acceptL && acceptR)
            {
                double current = 0;
                double target = (KAB_L[1] - KAB_R[1]) / 2;
                RotateSpeed = -(int)PDcontroller(current, target, ref config.PD_R);
            }
            if (acceptL && !acceptR)
            {
                double current = 0;
                double target = KAB_L[1];
                RotateSpeed = -(int)PDcontroller(current, target, ref config.PD_R);
            }
            if (!acceptL && acceptR)
            {
                double current = 0;
                double target = KAB_R[1];
                RotateSpeed = (int)PDcontroller(current, target, ref config.PD_R);
            }

            // 限速
            if (RotateSpeed > 10) { RotateSpeed = 10; }
            if (RotateSpeed < -10) { RotateSpeed = -10; }
            return RotateSpeed;
        }


        // 对y排序所有点，并滤波（本质是对x排序）
        private static List<CONFIG.URG_POINT> SortPoints(List<CONFIG.URG_POINT> points)
        {
            // 点的数量不够，直接返回
            if (points.Count <= 3) { return points; }

            // 距离排序
            for (int i = 1; i < points.Count; i++)
            {
                for (int j = 0; j < points.Count - i; j++)
                {
                    if (points[j].y <= points[j + 1].y) { continue; }
                    CONFIG.URG_POINT temp = new CONFIG.URG_POINT();
                    temp = points[j];
                    points[j] = points[j + 1];
                    points[j + 1] = temp;
                }
            }

            // 选取距离，滤波
            int indexofcut = points.Count;
            for (int i = 0; i < points.Count - 1; i++)
            {
                if (points[i + 1].y - points[i].y > 200) { indexofcut = i + 1; break; }
            }
            points.RemoveRange(indexofcut, points.Count - indexofcut);

            // 距离跨度要求
            double xMax = double.MinValue, xMin = double.MaxValue;
            for (int i = 0; i < points.Count; i++)
            {
                double x = points[i].x;
                if (x > xMax) { xMax = x; }
                if (x < xMin) { xMin = x; }
            }
            if (xMax - xMin < 100) { return new List<CONFIG.URG_POINT>(); }

            return points;
        }


        // 得到适合的点 已改
        private static List<CONFIG.URG_POINT> getFitPoints(List<CONFIG.URG_POINT> points)
        {
            // 点的数量不够，直接返回
            if (points.Count <= 3) { return points; }

            // 找最近的点
            double x0 = 0.0, y0 = double.MaxValue;
            int closest = 0;
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i].y > y0) { continue; }
                x0 = points[i].x;
                y0 = points[i].y;
                closest = i;
            }

            // 其余点相对角度
            List<double> angles = new List<double>();
            double MaxAngle = 0;
            double MinAngle = double.MaxValue;
            int indexOfMax = 0, indexOfMin = 0;
            double targetAngle = 0;

            for (int i = 0; i < points.Count; i++)
            {
                if (i == closest) { angles.Add(90.0); continue; }
                // 与基准点的相对角度
                double angle = Math.Atan(Math.Abs(points[i].y - y0) / Math.Abs(points[i].x - x0));
                if (points[i].x < x0) { angle += 90; }
                angles.Add(angle);
                if (angle > MaxAngle) { MaxAngle = angle; indexOfMax = i; }
                if (angle < MinAngle) { MinAngle = angle; indexOfMin = i; }
            }

            //去一定阈值，此时为5
            if (MaxAngle > 95)       //通道缩小
            {
                targetAngle = MaxAngle;
            }
            else if (MaxAngle < 85)
            {
                targetAngle = MinAngle;
            }
            else targetAngle = angles[closest];

            // 取出斜率符合的点
            List<CONFIG.URG_POINT> fitpoints = new List<CONFIG.URG_POINT>();
            for (int i = 1; i < angles.Count; i++)
            {
                if (Math.Abs(angles[i] - targetAngle) < 10) { fitpoints.Add(points[i]); continue; }
            }
            return fitpoints;
        }


        // 最小二乘法求拟合直线
        private static double[] getFitLine(List<CONFIG.URG_POINT> points)
        {
            // 点数量不够
            if (points.Count <= 3) { return new double[3] { 0, 0, 0 }; }

            // 拟合直线
            double sumX = 0, sumY = 0, sumXX = 0, sumYY = 0, sumXY = 0;
            int N = points.Count;
            for (int i = 0; i < N; i++)
            {
                sumX += points[i].x;
                sumY += points[i].y;
                sumXX += points[i].x * points[i].x;
                sumXY += points[i].x * points[i].y;
                sumYY += points[i].y * points[i].y;
            }

            double denominator = N * sumXX - sumX * sumX;
            if (denominator == 0) { denominator = 0.000000001; }

            // 计算斜率和截距
            double UrgK = (N * sumXY - sumX * sumY) / denominator;
            double UrgB = (sumXX * sumY - sumX * sumXY) / denominator;

            double UrgA = Math.Atan(UrgK) * 180 / Math.PI;
            if (Math.Abs(UrgA) > 80) { return new double[3] { 0, 0, 0 }; }  // 直线与x轴的夹角

            return new double[3] { UrgK, UrgA, UrgB };
        }


        // 获得[BG,ED]间的距离值等等
        private List<CONFIG.URG_POINT> getUrgPoint(double angleBG, double angleED, IUrgPort urgPort)
        {
            List<CONFIG.URG_POINT> points = new List<CONFIG.URG_POINT>();
            double anglePace = 360.0 / 1024.0;

            UrgModel urgModel = urgPort.getUrgData();
            while (urgModel.Distance == null || urgModel.Distance.Count == 0) { urgModel = urgPort.getUrgData(); }

            int BG = (int)((angleBG - -30) / anglePace);
            int ED = (int)((angleED - -30) / anglePace);
            if (urgModel.Distance.Count < ED) { return points; }

            for (int i = BG; i < ED; i++)
            {
                if (urgModel.Distance[i] == 0) { continue; }   // 去除无用点

                CONFIG.URG_POINT point = new CONFIG.URG_POINT();
                point.d = urgModel.Distance[i];

                double angle = -30.0 + i * anglePace;
                point.a = angle;
                point.x = point.d * Math.Cos(angle * Math.PI / 180);  // 这里可能为负值
                point.y = point.d * Math.Sin(angle * Math.PI / 180);

                points.Add(point);
            }
            return points;
        }


        // PD控制速度
        private double PDcontroller(double current, double target, ref CONFIG.PD_PARAMETER PD)
        {
            PD.Error2 = PD.Error1;
            PD.Error1 = PD.Error0;
            PD.Error0 = current - target;

            double pControl = PD.Kp * PD.Error0;
            double dControl = PD.Kd * (PD.Error0 - PD.Error1);

            return pControl + dControl;
        }


        private void useRotateSpeed(IConPort conPort, IUrgPort urgPort)
        {
            int RotateSpeed = 0;     //旋转速度
            bool Ailgnment = false;
            while (!Ailgnment)
            {
                // 取点
                List<CONFIG.URG_POINT> pointsL = getUrgPoint(120, 180, urgPort);
                List<CONFIG.URG_POINT> pointsR = getUrgPoint(0, 60, urgPort);

                // 交换并取绝对坐标
                for (int i = 0; i < pointsL.Count; i++)
                {
                    CONFIG.URG_POINT point = pointsL[i];
                    double tempx = Math.Abs(point.x);
                    double tempy = Math.Abs(point.y);
                    point.x = tempy; point.y = tempx; pointsL[i] = point;
                }
                for (int i = 0; i < pointsR.Count; i++)
                {
                    CONFIG.URG_POINT point = pointsR[i];

                    double tempx = Math.Abs(point.x);
                    double tempy = Math.Abs(point.y);
                    point.x = tempy; point.y = tempx; pointsR[i] = point;
                }

                // 滤波，太远的不要
                for (int i = pointsL.Count - 1; i >= 0; i--)
                {
                    if (pointsL[i].y > 800) { pointsL.RemoveAt(i); }
                }
                for (int i = pointsR.Count - 1; i >= 0; i--)
                {
                    if (pointsR[i].y > 800) { pointsR.RemoveAt(i); }
                }

                // 拟合左右两边障碍物信息
                pointsL = SortPoints(pointsL);
                pointsR = SortPoints(pointsR);
                //    pointsL = getFitPoints(pointsL);
                //  pointsR = getFitPoints(pointsR);
                double[] KAB_L = getFitLine(pointsL);
                double[] KAB_R = getFitLine(pointsR);
                DisplayPoint.displayPoint.LeftUrgK = KAB_L[0];
                DisplayPoint.displayPoint.RightUrgK = KAB_R[0];

                // 点数量不够
                bool acceptL = pointsL.Count > 5;
                bool acceptR = pointsR.Count > 5;
                double target, current;

                // 控制策略
                if (acceptL && acceptR)
                {
                    current = 0;
                    target = (KAB_L[1] - KAB_R[1]) / 2;
                    RotateSpeed = (int)PDcontroller(current, target, ref config.PD_R);
                }
                if (acceptL && !acceptR)
                {
                    current = 0;
                    target = KAB_L[1];
                    RotateSpeed = (int)PDcontroller(current, target, ref config.PD_R);
                }
                if (!acceptL && acceptR)
                {
                    current = 0;
                    target = KAB_R[1];
                    RotateSpeed = -(int)PDcontroller(current, target, ref config.PD_R);
                }
                else { break; }

                // 限速
                if (RotateSpeed > 10) { RotateSpeed = 10; }
                if (RotateSpeed < -10) { RotateSpeed = -10; }

                if (target < 0.2) { Ailgnment = true; break; }

                conPort.Control_Move_By_Speed(0, 0, RotateSpeed);
                System.Threading.Thread.Sleep(500);
            }
        }

        private void useTranslateSpeed(IConPort conPort, IUrgPort urgPort)
        {
            double distanceL = double.MaxValue;
            double distanceR = double.MaxValue;
            double AisleWidth = 900;    // 通道宽度(mm)
            int TranslateSpeed = 0;     // 平移速度
            bool acceptL, acceptR;      // 判断左右距离行吗
            bool Ailgnment = false;

            while (!Ailgnment)
            {
                // 激光雷达扫两边
                List<CONFIG.URG_POINT> pointsL = getUrgPoint(160, 180, urgPort);
                List<CONFIG.URG_POINT> pointsR = getUrgPoint(0, 20, urgPort);

                // 两边最短距离
                double minL = double.MaxValue, minR = double.MaxValue;
                for (int i = 0; i < pointsL.Count; i++)
                {
                    double x = Math.Abs(pointsL[i].x);
                    if (x < minL) { minL = x; }
                }
                for (int i = 0; i < pointsR.Count; i++)
                {
                    double x = Math.Abs(pointsR[i].x);
                    if (x < minR) { minR = x; }
                }
                distanceL = Math.Min(distanceL, minL);
                distanceR = Math.Min(distanceR, minR) + 30;
                DisplayPoint.displayPoint.LeftUrgB = distanceL;
                DisplayPoint.displayPoint.RightUrgB = distanceR;



                // 数据能否使用
                if (distanceL == 0 && distanceR == 0) { break; }
                if (distanceL > 0 && distanceL < AisleWidth) { acceptL = true; }
                else acceptL = false;

                if (distanceR > 0 && distanceR < AisleWidth) { acceptR = true; }
                else acceptR = false;

                // 两边最短距离
                double acceptDistance = 300;
                acceptL = distanceR < acceptDistance;
                acceptR = distanceL < acceptDistance;

                double current = 0, target = 0;
                // 速度控制
                if (!acceptL && !acceptR)
                {
                    current = distanceL;
                    target = (distanceL + distanceR) / 2;
                }
                else //(acceptL && !acceptR || acceptL && !acceptR)
                {
                    current = distanceL;
                    target = AisleWidth / 2;
                }

                TranslateSpeed = (int)PDcontroller(current, target, ref config.PD_T);
                if (TranslateSpeed > config.MaxTranslateSpeed) { TranslateSpeed = config.MaxTranslateSpeed; }
                if (TranslateSpeed < -config.MaxTranslateSpeed) { TranslateSpeed = -config.MaxTranslateSpeed; }

                if (current - target < 10)
                {
                    Ailgnment = true; break;
                }

                conPort.Control_Move_By_Speed(0, TranslateSpeed, 0);
                System.Threading.Thread.Sleep(100);
            }

        }





    }
}
