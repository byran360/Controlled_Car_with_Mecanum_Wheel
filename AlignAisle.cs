using SmartCar.Map.Elem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SmartCar
{
    class AlignAisle
    {

        
        public struct CONFIG
        {
            public struct URG_POINT { public double x, y, a, d; }
        }

        /********************************************* public method *******************************************/
        /// <summary>
        /// 对准通道口
        /// </summary>
        public void Start()
        {
            // 找通道入口
            bool Finished = false;
            while (!Finished)
            {
                int leftSpeed = getTranslateSpeed(30, ref Finished);

                PortManager.conPort.Control_Move_By_Speed(0, leftSpeed, 0);
                System.Threading.Thread.Sleep(100);
            }

        }

        /// <summary>
        /// 记录距离，找最短距离
        /// </summary>
        /// <returns></returns>
        public double recordDistance()
        {
            // 取点
            List<CONFIG.URG_POINT> pointsH = getUrgPoint(40, 140); //45 135
            if (pointsH.Count == 0) { return 0; }

            // 寻找最小距离
            double minDis = double.MaxValue;
            for (int i = 0; i < pointsH.Count - 1; i++)
            {
                double dis = pointsH[i].y;
                if (dis < minDis) { minDis = dis; }
            }
            return minDis;
        }

        /// <summary>
        /// 调整距离
        /// </summary>
        /// <param name="dis">当前距离</param>
        public void adjustDistance(double dis)
        {
            while (true)
            {
                // 获取控制
                double currnt = recordDistance();
                double target = dis;
                double Kp = 1;

                double adjust = Kp * (currnt - target);
                if (Math.Abs(currnt - target) < 20) { break; }

                //限速
                if (adjust > 200) { adjust = 200; }
                if (adjust < -200) { adjust = -200; }

                PortManager.conPort.Control_Move_By_Speed((int)adjust, 0, 0);
                System.Threading.Thread.Sleep(100);
            }
        }

        // 2019- 10 -9 改
        public double recordDistance2()
        {
            // 取点
            List<CONFIG.URG_POINT> pointsH = getUrgPoint(84, 96); 

            // 寻找最小距离
            double minDis = double.MaxValue;
            for (int i = 0; i < pointsH.Count - 1; i++)
            {
                double dis = pointsH[i].y;
                if (dis < minDis) { minDis = dis; }
            }
            if (pointsH.Count == 0) { return 0; }
            return minDis;
        }


        public void adjustDistance2(double dis)
        {
            while (true)
            {
                // 获取控制
                double currnt = recordDistance2();
                double target = dis;
                double Kp = 1;

                double adjust = Kp * (currnt - target);
                if (Math.Abs(currnt - target) < 20) { break; }

                //限速
                if (adjust > 200) { adjust = 200; }
                if (adjust < -200) { adjust = -200; }

                PortManager.conPort.Control_Move_By_Speed((int)adjust, 0, 0);
                System.Threading.Thread.Sleep(100);
            }
        }


        /********************************************* private method *******************************************/
        /// <summary>
        /// 获取平移速度
        /// </summary>
        /// <param name="keepSpeed">维持该速度去寻找通道口</param>
        /// <param name="Finished">寻找完成</param>
        /// <returns></returns>
        private int getTranslateSpeed(int keepSpeed, ref bool Finished)
        {
            // 取点
            List<CONFIG.URG_POINT> pointsH = getUrgPoint(45, 135);

            // 去掉 Y 方向跨度过大的点
            double acceptDis = 1500;
            for (int i = pointsH.Count - 1; i >= 0; i--)
            {
                if (pointsH[i].d > acceptDis) { pointsH.RemoveAt(i); }
            }

            // 点数量不够
            if (pointsH.Count <= 3) { return 0; }

            // 对 X 坐标从小到大排序
            for (int i = 1; i < pointsH.Count; i++)
            {
                for (int j = 0; j < pointsH.Count - i; j++)
                {
                    if (pointsH[j].x <= pointsH[j + 1].x) { continue; }
                    CONFIG.URG_POINT point = pointsH[j];
                    pointsH[j] = pointsH[j + 1];
                    pointsH[j + 1] = point;
                }
            }

            // 获取最大间隙
            double MaxGap = double.MinValue;
            int indexL = 0, indexR = 0;
            for (int i = 0; i < pointsH.Count - 1; i++)
            {
                double gap = Math.Abs(pointsH[i].x - pointsH[i + 1].x);
                if (gap > MaxGap) { MaxGap = gap; indexL = i; indexR = i + 1; }
            }
            double disL = Math.Abs(pointsH[indexL].x);
            double disR = Math.Abs(pointsH[indexR].x);
            DisplayPoint.displayPoint.MaxGap = MaxGap;
            
            // 判断间隙是否足够大
            if (MaxGap < 500) { return keepSpeed; }

            // 是否已经满足退出条件
            double current = disL;
            double target = (disL + disR) / 2;
            if (Math.Abs(current - target) < 50) { Finished = true; }

            // 获取控制
            double Kp = 0.5;
            int xSpeed = (int)(Kp * (current - target));

            // 限速
            if (xSpeed > 100) { xSpeed = 100; }
            if (xSpeed < -100) { xSpeed = -100; }
            return xSpeed;
        }


        private List<CONFIG.URG_POINT> getUrgPoint(double angleBG, double angleED)
        {
            List<CONFIG.URG_POINT> points = new List<CONFIG.URG_POINT>();
            if (!PortManager.urgPort.IsOpen) { return points; }

            UrgModel urgModel = PortManager.urgPort.getUrgData();
            while (urgModel.Distance == null || urgModel.Distance.Count == 0) 
            { 
                urgModel = PortManager.urgPort.getUrgData(); 
            }

            int BG = (int)((angleBG - -30) / (360.0 / 1024.0));
            int ED = (int)((angleED - -30) / (360.0 / 1024.0));
            if (urgModel.Distance.Count < ED) { return points; }

            for (int i = BG; i < ED; i++)
            {
                if (urgModel.Distance[i] == 0) { continue; }

                CONFIG.URG_POINT point = new CONFIG.URG_POINT();
                point.d = urgModel.Distance[i];

                double angle = -30.0 + i * 360.0 / 1024.0;

                point.a = angle;
                point.x = point.d * Math.Cos(angle * Math.PI / 180);
                point.y = point.d * Math.Sin(angle * Math.PI / 180);

                points.Add(point);
            }

            return points;
        }
    }
}
 