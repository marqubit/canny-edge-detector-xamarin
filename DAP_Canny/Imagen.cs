using System;


using Android.Graphics;


namespace DAP_Canny
{
    class Imagen
    {
        public int[] getGrayScaleMatrixFromBitmap(Bitmap bitmap)
        {
            int[] matrixData = new int[bitmap.Height * bitmap.Width];
            bitmap.GetPixels(matrixData, 0, bitmap.Width, 0, 0, bitmap.Width, bitmap.Height);

            for (int i = 0; i < bitmap.Height; i++)
                for (int j = 0; j < bitmap.Width; j++)
                {
                    Color color = new Color(matrixData[i * bitmap.Width + j]);
                    matrixData[i * bitmap.Width + j] = (color.R + color.G + color.B) / 3;
                }
            return matrixData;
        }

        public Bitmap getBitmapFromMatrix(int[] matrixData, Bitmap bitmap)
        {
            Bitmap CopyBitmap = Android.Graphics.Bitmap.CreateBitmap(bitmap.Width, bitmap.Height, bitmap.GetConfig());
            CopyBitmap.SetPixels(getPixelsFromMatrix(matrixData), 0, bitmap.Width, 0, 0, bitmap.Width, bitmap.Height);
            //ivGrayScale.SetImageBitmap(CopyBitmap);
            return CopyBitmap;
        }

        public int[] getPixelsFromMatrix(int[] matrixData)
        {
            int[] pixels = new int[matrixData.Length];
            int i = 0;
            //int j = 0;
            unsafe
            {
                fixed (int* p = &(pixels[0]))
                {
                    foreach (int grayScalePixel in matrixData)
                    {
                        //if (i == 101243)
                        //    j++;
                        p[i++] = Color.Rgb(grayScalePixel, grayScalePixel, grayScalePixel); //i++;
                    }
                }
            }
            return pixels;
        }

        public int[] convolucion3x3(int[] matrixData, int width, int height, double[] convolitionMatrix, double factor)
        {
            int[] newMatrix = new int[matrixData.Length];
            double suma = 0;
            unsafe
            {
                fixed (int* p = &(matrixData[0]), pN = &(newMatrix[0]))
                {
                    for (int i = 1; i < height - 1; i++)
                        for (int j = 1; j < width - 1; j++)
                        {
                            suma = 0;
                            for (int y = 0; y < convolitionMatrix.Length / 3; y++)
                                for (int x = 0; x < convolitionMatrix.Length / 3; x++)
                                {
                                    suma += p[((i - 1 + y) * width) + (j - 1 + x)] * convolitionMatrix[y * 3 + x];
                                }
                            suma = suma / factor;
                            if (suma > 255)
                                suma = 255;
                            else if (suma < 0)
                                suma = 0;
                            pN[(i * width) + j] = Convert.ToInt32(suma);
                        }
                }
                return newMatrix;
            }
        }

        public int[] convolucion5x5(int[] matrixData, int width, int height, double[] convolitionMatrix, double factor)
        {
            int[] newMatrix = new int[matrixData.Length];
            double suma = 0;
            unsafe
            {
                fixed (int* p = &(matrixData[0]), pN = &(newMatrix[0]))
                {
                    for (int i = 2; i < height - 2; i++)
                        for (int j = 2; j < width - 2; j++)
                        {
                            suma = 0;
                            for (int y = 0; y < convolitionMatrix.Length / 5; y++)
                                for (int x = 0; x < convolitionMatrix.Length / 5; x++)
                                {
                                    suma += p[((i - 2 + y) * width) + (j - 2 + x)] * convolitionMatrix[y * 5 + x];
                                }
                            suma = suma / factor;
                            if (suma > 255)
                                suma = 255;
                            else if (suma < 0)
                                suma = 0;
                            pN[(i * width) + j] = Convert.ToInt32(suma);
                        }
                }
                return newMatrix;
            }
        }
    }
}