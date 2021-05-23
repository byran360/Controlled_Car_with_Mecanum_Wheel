using SmartCar.Map.Elem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SmartCar.Data
{
    class Matrix
    {
        public static Matrix matrix = new Matrix();

        public const double controlTime = 0.1;
        public int seekSeed = unchecked((int)DateTime.Now.Ticks);

        /*************************** public method *********************************/
        // 这里计算一个数组的方差
        public double[,] getArrayCorariance(double[,] arr)
        {
            int row = arr.GetLength(0);
            int colomn = arr.GetLength(1);
            double[,] ava = new double[row, 1];
            double[,] temp = new double[row, 1];
            ava = getAvarage(arr);                   // [x x x x x x]
            for (int i = 0; i < row; i++)
            {
                for (int j = 0; j < colomn; j++)
                {
                    temp[i, 0] += (arr[i, j] - ava[i, 0]) * (arr[i, j] - ava[i, 0]);
                }
                temp[i, 0] = 1 / Math.Sqrt(colomn - 1) * Math.Sqrt(temp[i, 0]);
            }
            return temp;
        }

        // 对X求Q矩阵
        public double[,] getMatrixCorariance(double[,] arr)
        {
            int row = arr.GetLength(0);
            double[,] temp = new double[row, 1];
            for (int i = 0; i < row; i++)
            {
                temp = getArrayCorariance(arr);
            }
            return temp;
        }

        /// <summary>
        /// 得到高斯分布值
        /// </summary>
        /// <param name="u"></param>
        /// <param name="sigma"></param>
        /// <returns></returns>
        public double[,] getGaussiDistri(double[,] arr)
        {
            Random r = new Random(seekSeed);
            int i = r.Next(0, 1000);
            double x = i / 1000;
            int row = arr.GetLength(0);
            double[,] fx = new double[row, 1];
            for (i = 0; i < row; i++)
            {
                if (arr[i, i] > 0) { fx[i, 0] = 1 / (Math.Sqrt(Math.PI * 2) * arr[i, i]) * Math.Exp(-x * x / (arr[i, i] * arr[i, i] * 2)); }
                else fx[i, 0] = 0;

                if (fx[i, 0] > 0.01)
                {
                    fx[i, 0] = 0.01;
                }

            }
            return fx;
        }

        ///   <summary> 
        ///   矩阵的转置 
        ///   </summary> 
        ///   <param   name= "iMatrix "> </param> 
        public double[,] Transpose(double[,] iMatrix)
        {
            int row = iMatrix.GetLength(0);
            int column = iMatrix.GetLength(1);
            //double[,] iMatrix = new double[column, row];
            double[,] TempMatrix = new double[row, column];
            double[,] iMatrixT = new double[column, row];
            for (int i = 0; i < row; i++)
            {
                for (int j = 0; j < column; j++)
                {
                    TempMatrix[i, j] = iMatrix[i, j];
                }
            }
            for (int i = 0; i < column; i++)
            {
                for (int j = 0; j < row; j++)
                {
                    iMatrixT[i, j] = TempMatrix[j, i];
                }
            }
            return iMatrixT;

        }

        ///   <summary> 
        ///   矩阵的逆矩阵 
        ///   </summary> 
        ///   <param   name= "iMatrix "> </param> 
        public double[,] Athwart(double[,] iMatrix)
        {
            int i = 0;
            int row = iMatrix.GetLength(0);
            double[,] MatrixZwei = new double[row, row * 2];
            double[,] iMatrixInv = new double[row, row];
            for (i = 0; i < row; i++)
            {
                for (int j = 0; j < row; j++)
                {
                    MatrixZwei[i, j] = iMatrix[i, j];
                }
            }
            for (i = 0; i < row; i++)
            {
                for (int j = row; j < row * 2; j++)
                {
                    MatrixZwei[i, j] = 0;
                    if (i + row == j)
                        MatrixZwei[i, j] = 1;
                }
            }

            for (i = 0; i < row; i++)
            {
                if (MatrixZwei[i, i] != 0)
                {
                    double intTemp = MatrixZwei[i, i];
                    for (int j = 0; j < row * 2; j++)
                    {
                        MatrixZwei[i, j] = MatrixZwei[i, j] / intTemp;
                    }
                }
                for (int j = 0; j < row; j++)
                {
                    if (j == i)
                        continue;
                    double intTemp = MatrixZwei[j, i];
                    for (int k = 0; k < row * 2; k++)
                    {
                        MatrixZwei[j, k] = MatrixZwei[j, k] - MatrixZwei[i, k] * intTemp;
                    }
                }
            }

            for (i = 0; i < row; i++)
            {
                for (int j = 0; j < row; j++)
                {
                    iMatrixInv[i, j] = MatrixZwei[i, j + row];
                }
            }
            return iMatrixInv;
        }

        ///   <summary> 
        ///   矩阵加法 
        ///   </summary> 
        ///   <param   name= "MatrixEin "> </param> 
        ///   <param   name= "MatrixZwei "> </param> 
        public double[,] AddMatrix(double[,] MatrixEin, double[,] MatrixZwei)
        {
            double[,] MatrixResult = new double[MatrixEin.GetLength(0), MatrixZwei.GetLength(1)];
            for (int i = 0; i < MatrixEin.GetLength(0); i++)
                for (int j = 0; j < MatrixZwei.GetLength(1); j++)
                    MatrixResult[i, j] = MatrixEin[i, j] + MatrixZwei[i, j];
            return MatrixResult;
        }

        ///   <summary> 
        ///   矩阵减法 
        ///   </summary> 
        ///   <param   name= "MatrixEin "> </param> 
        ///   <param   name= "MatrixZwei "> </param> 
        public double[,] SubMatrix(double[,] MatrixEin, double[,] MatrixZwei)
        {
            double[,] MatrixResult = new double[MatrixEin.GetLength(0), MatrixZwei.GetLength(1)];
            for (int i = 0; i < MatrixEin.GetLength(0); i++)
                for (int j = 0; j < MatrixZwei.GetLength(1); j++)
                    MatrixResult[i, j] = MatrixEin[i, j] - MatrixZwei[i, j];
            return MatrixResult;
        }

        ///   <summary> 
        ///   矩阵乘法 
        ///   </summary> 
        ///   <param   name= "MatrixEin "> </param> 
        ///   <param   name= "MatrixZwei "> </param> 
        public double[,] MultiplyMatrix(double[,] MatrixEin, double[,] MatrixZwei)
        {
            double[,] MatrixResult = new double[MatrixEin.GetLength(0), MatrixZwei.GetLength(1)];
            for (int i = 0; i < MatrixEin.GetLength(0); i++)
            {
                for (int j = 0; j < MatrixZwei.GetLength(1); j++)
                {
                    for (int k = 0; k < MatrixEin.GetLength(1); k++)
                    {
                        MatrixResult[i, j] += MatrixEin[i, k] * MatrixZwei[k, j];
                    }
                }
            }
            return MatrixResult;
        }

        ///   <summary> 
        ///   矩阵对应行列式的值 
        ///   </summary> 
        ///   <param   name= "MatrixEin "> </param> 
        ///   <returns> </returns> 
        public double ResultDeterminant(double[,] MatrixEin)
        {
            return MatrixEin[0, 0] * MatrixEin[1, 1] * MatrixEin[2, 2] + MatrixEin[0, 1] * MatrixEin[1, 2] * MatrixEin[2, 0] + MatrixEin[0, 2] * MatrixEin[1, 0] * MatrixEin[2, 1]
            - MatrixEin[0, 2] * MatrixEin[1, 1] * MatrixEin[2, 0] - MatrixEin[0, 1] * MatrixEin[1, 0] * MatrixEin[2, 2] - MatrixEin[0, 0] * MatrixEin[1, 2] * MatrixEin[2, 1];

        }

        /*******************************  private method *********************************/
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












    }


}
