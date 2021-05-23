using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SmartCar.Data
{
    public class MatrixData
    {
        public static MatrixData matrixData = new MatrixData();
        public static int flag = 0;

        Matrix matrix1 = new Matrix();

        public const double controlTime = 0.1;           // T周期

        /******* 编码器数据 ********/
        // 距离信息
        public double[,] DrArrX = new double[1, 10];
        public double[,] DrArrY = new double[1, 10];
        public double[,] DrArrTheta = new double[1, 10];
        // 速度信息
        public double[,] DrArrVx = new double[1, 10];
        public double[,] DrArrVy = new double[1, 10];
        public double[,] DrArrW = new double[1, 10];
        public double currentDrVx, previousDrVx;
        public double currentDrVy, previousDrVy;
        public double currentDrW, previousDrW;

        // 加速度信息
        public double[,] DrArrAx = new double[1, 10];
        public double[,] DrArrAy = new double[1, 10];
        public double[,] DrArrAw = new double[1, 10];
        public double currentDrAx, previousDrAx;
        public double currentDrAy, previousDrAy;
        public double currentDrAw, previousDrAw;



        /********* 待更新矩阵 *********/
        // 更新X
        public double[,] currentX = new double[6, 1];     // 预测值
        public double[,] X = new double[6, 1];            // 预测值

        public double[,] A = new double[6, 6] { { 1,controlTime,0,0,0,0},
                                                { 0,1,0,0,0,0},
                                                { 0,0,1,controlTime,0,0},
                                                { 0,0,0,1,0,0},
                                                { 0,0,0,0,1,controlTime},
                                                { 0,0,0,0,0,1} };

        public double[,] B = new double[6, 6] { { controlTime * controlTime / 2,0,0,0,0,0},
                                                { controlTime,0,0,0,0,0},
                                                { 0,0,controlTime * controlTime / 2,0,0,0},
                                                { 0,0,controlTime,0,0,0},
                                                { 0,0,0,0,controlTime * controlTime / 2,0},
                                                { 0,0,0,0,controlTime,0} };

        public double[,] U = new double[6, 1];
        public double[,] Kg = new double[6, 6];



        // 更新P
        public double[,] Q = new double[6, 6]{ { 0,0,0,0,0,0},
                                              { 0,0,0,0,0,0},
                                              { 0,0,0,0,0,0},
                                              { 0,0,0,0,0,0},
                                              { 0,0,0,0,0,0},
                                              { 0,0,0,0,0,0} };
        public double[,] P = new double[6, 6]{ { 1,0,0,0,0,0},
                                              { 0,1,0,0,0,0},
                                              { 0,0,1,0,0,0},
                                              { 0,0,0,1,0,0},
                                              { 0,0,0,0,1,0},
                                              { 0,0,0,0,0,1} };


        public double[,] W = new double[6, 1];


        // 更新Z
        public double[,] Z = new double[6, 1];
        public double[,] _X = new double[6, 1];     // 实际值
        public double[,] H = new double[6, 6] { { 1,0,0,0,0,0},
                                                { 0,1,0,0,0,0},
                                                { 0,0,1,0,0,0},
                                                { 0,0,0,1,0,0},
                                                { 0,0,0,0,1,0},
                                                { 0,0,0,0,0,1} };
        public double[,] V = new double[6, 1];


        public double[,] R = new double[6, 6];      // 噪声误差矩阵，计算协方差
        public double[,] E = new double[6, 6]{ { 1,0,0,0,0,0},
                                                { 0,1,0,0,0,0},
                                                { 0,0,1,0,0,0},
                                                { 0,0,0,1,0,0},
                                                { 0,0,0,0,1,0},
                                                { 0,0,0,0,0,1} };


        // 初始化
        public MatrixData()
        {

            DrArrX[0, 0] = 0;
            DrArrY[0, 0] = 0;

            DrArrVx[0, 0] = 0;
            DrArrVy[0, 0] = 0;
            DrArrW[0, 0] = 0;

            _X[4, 0] = Math.PI / 2;
            X[4, 0] = Math.PI / 2;

            for (int i = 0; i < 10; i++)
            {
                DrArrTheta[0, i] = Math.PI / 2;
            }

            for (int i = 0; i < 6; i++)
            {
                Kg[i, 0] = 0.95;
            }

        }

        // 卡尔曼滤波
        public void refreshMatrix()
        {
            Matrix matrix = new Matrix();

            // 卡尔曼滤波
            KeyPoint keyPoint = new KeyPoint(PortManager.drPort.getPosition());
       
            _X[1, 0] = (keyPoint.x - _X[0, 0]) / controlTime;
            _X[3, 0] = (keyPoint.y - _X[2, 0]) / controlTime;
            _X[5, 0] = (keyPoint.w - _X[4, 0]) / controlTime;
            _X[0, 0] = keyPoint.x;      // m
            _X[2, 0] = keyPoint.y;
            _X[4, 0] = keyPoint.w;      // rad

            //  P = matrix.AddMatrix(matrix.MultiplyMatrix(matrix.MultiplyMatrix(A, P), matrix.Transpose(A)), Q);
            Z = matrix.MultiplyMatrix(H, _X);
            X = matrix.AddMatrix(X, matrix.MultiplyMatrix(Kg, matrix.SubMatrix(Z, X)));
           // PortManager.drPort.setPosition(X[0, 0], X[2, 0], X[4, 0]);      // 更新（x,y,w）
            RecordExcel(PortManager.drPort, "KalmanFilter.xls");            // 记录
            

            refreshDrData();
            //X = matrix.AddMatrix(matrix.MultiplyMatrix(A, X), matrix.MultiplyMatrix(B, U));
            X = matrix.MultiplyMatrix(A, X);
            if (X[4, 0] - _X[4, 0] > 0.2)
            {
                X[4, 0] = _X[4, 0];
            }
            if (X[2, 0] < 0)
            {
                X[2, 0] = _X[2, 0];

            }

        }



        // 更新编码器获得得到数据
        public void refreshDrData()
        {
            Matrix matrix = new Matrix();
            //double sumX = 0;
            //double sumY = 0;
            //double sumTheta = 0;
            //double sumVx = 0;
            //double sumVy = 0;
            //double sumW = 0;

            for (int i = 1; i < 10; i++)
            {
                DrArrX[0, i - 1] = DrArrX[0, i];
                DrArrY[0, i - 1] = DrArrY[0, i];
                DrArrTheta[0, i - 1] = DrArrTheta[0, i];  // rad
            }
            DrArrX[0, 9] = X[0,0];      // m
            DrArrY[0, 9] = X[2,0];
            DrArrTheta[0, 9] = X[4,0];  // rad

            for (int i = 1; i < 10; i++)
            {
                DrArrVx[0, i - 1] = DrArrVx[0, i];
                DrArrVy[0, i - 1] = DrArrVy[0, i];
                DrArrW[0, i - 1] = DrArrW[0, i];
            }
            DrArrVx[0, 9] = (DrArrX[0, 9] - DrArrX[0, 8]) / controlTime;
            DrArrVy[0, 9] = (DrArrY[0, 9] - DrArrY[0, 8]) / controlTime;
            DrArrW[0, 9] = (DrArrTheta[0, 9] - DrArrTheta[0, 8]) / controlTime;
            currentDrVx = (DrArrX[0, 9] - DrArrX[0, 0]) / 1;
            currentDrVy = (DrArrY[0, 9] - DrArrY[0, 0]) / 1;
            currentDrW = (DrArrTheta[0, 9] - DrArrTheta[0, 0]) / 1;

            // 得到加速度
            currentDrAx = (DrArrVx[0, 9] - DrArrVx[0, 0]) / 1;
            currentDrAy = (DrArrVy[0, 9] - DrArrVy[0, 0]) / 1;
            currentDrAw = (DrArrW[0, 9] - DrArrW[0, 0]) / 1;


            //// 得到距离
            //for (int i = 1; i < 10; i++)
            //{
            //    DrArrX[0, i - 1] = DrArrX[0, i];
            //    DrArrY[0, i - 1] = DrArrY[0, i];
            //    DrArrTheta[0, i - 1] = DrArrTheta[0, i];  // rad
            //}
            //DrArrX[0, 9] = keyPoint.x;      // m
            //DrArrY[0, 9] = keyPoint.y;
            //DrArrTheta[0, 9] = keyPoint.w;  // rad

            //// 得到速度
            //for (int i = 1; i < 9; i++)
            //{
            //    DrArrVx[0, i - 1] = DrArrVx[0, i];
            //    DrArrVy[0, i - 1] = DrArrVy[0, i];
            //    DrArrW[0, i - 1] = DrArrW[0, i];
            //}
            //DrArrVx[0, 8] = (DrArrX[0, 9] - DrArrX[0, 8]) / controlTime;
            //DrArrVy[0, 8] = (DrArrY[0, 9] - DrArrY[0, 8]) / controlTime;
            //DrArrW[0, 8] = (DrArrTheta[0, 9] - DrArrTheta[0, 8]) / controlTime;
            //for (int i = 0; i < 9; i++)
            //{
            //    sumX += DrArrVx[0, i];
            //    sumY += DrArrVy[0, i];
            //    sumTheta += DrArrW[0, i];
            //}
            //currentDrVx = sumX / 9;
            //currentDrVy = sumY / 9;
            //currentDrW = sumTheta / 9;

            //// 得到加速度
            //for (int i = 1; i < 8; i++)
            //{
            //    DrArrAx[0, i - 1] = DrArrAx[0, i];
            //    DrArrAy[0, i - 1] = DrArrAy[0, i];
            //    DrArrAw[0, i - 1] = DrArrAw[0, i];
            //}
            //DrArrAx[0, 7] = (DrArrVx[0, 8] - DrArrVx[0, 7]) / controlTime;
            //DrArrAy[0, 7] = (DrArrVy[0, 8] - DrArrVy[0, 7]) / controlTime;
            //DrArrAw[0, 7] = (DrArrW[0, 8] - DrArrW[0, 7]) / controlTime;

            //for (int i = 0; i < 8; i++)
            //{
            //    sumVx += DrArrAx[0, i];
            //    sumVy += DrArrAy[0, i];
            //    sumW += DrArrAw[0, i];
            //}
            //currentDrAx = sumVx / 8;
            //currentDrAy = sumVy / 8;
            //currentDrAw = sumW / 8;

            // 更新矩阵
            X[0, 0] = DrArrX[0, 9];
            X[1, 0] = currentDrVx;
            X[2, 0] = DrArrY[0, 9];
            X[3, 0] = currentDrVy;
            X[4, 0] = DrArrTheta[0, 9];
            X[5, 0] = currentDrW;

            U[0, 0] = currentDrAx;
            U[2, 0] = currentDrAy;
            U[4, 0] = currentDrAw;

        }



        /******************************* private method **************************************/
        private double[,] getAvarage(double[,] array)
        {
            int row = array.GetLength(0);
            int colomn = array.GetLength(1);
            double[,] temp = new double[row, 1];
            for (int i = 0; i < row; i++)
            {
                for (int j = 0; j < colomn; j++)
                {
                    temp[i, 0] += array[i, j];
                }
                temp[i, 0] /= colomn;
            }
            return temp;
        }


        private static int num2 = -1;
        public void RecordExcel(IDrPort drPort, string Filename)
        {
            using (FileStream rswrite = new FileStream(@"E:\DadaStorage\ExcelFile_20210520\" + $"{Filename}", FileMode.Append, FileAccess.Write))
            {
                string str;
                byte[] buffer;
                if (num2 == -1)
                {
                    str = " " + "\t" + "x" + "\t" + "y" + "\t" + "w"
                          + "\r\n";
                    buffer = Encoding.Default.GetBytes(str);
                    rswrite.Write(buffer, 0, buffer.Length);
                }
                num2++;
                KeyPoint keys = new KeyPoint(drPort.getPosition());
                keys.w = keys.w * 180 / Math.PI;
                str = num2.ToString() + "\t" + keys.x.ToString("F3") + "\t" + keys.y.ToString("F3") + "\t" + keys.w.ToString("F3") + "\t"
                      + X[0,0].ToString() + "\t"+ X[1, 0].ToString() + "\t" + X[2, 0].ToString() + "\t" + X[3, 0].ToString() + "\t" + X[4, 0].ToString() + "\t" + X[5, 0].ToString() + "\t"
                      + _X[0,0].ToString() + "\t"+_X[1, 0].ToString() + "\t" + _X[2, 0].ToString() + "\t" + _X[3, 0].ToString() + "\t" + _X[4, 0].ToString() + "\t" + _X[5, 0].ToString() + "\t"
                      + U[0,0].ToString() + "\t" + U[1, 0].ToString() + "\t" + U[2, 0].ToString() + "\t" + U[3, 0].ToString() + "\t" + U[4, 0].ToString() + "\t" + U[5, 0].ToString() + "\t"
                        + "\r\n";
                buffer = Encoding.Default.GetBytes(str);
                rswrite.Write(buffer, 0, buffer.Length);
            }
        }




    }
}
