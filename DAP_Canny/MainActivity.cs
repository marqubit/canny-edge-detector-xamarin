using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Provider;
using Android.Widget;
using Java.IO;
using Environment = Android.OS.Environment;
using Uri = Android.Net.Uri;
namespace DAP_Canny
{
    [Activity(Label = "DAP_Canny", MainLauncher = true)]
    public class MainActivity : Activity
    {
        private ImageView imvPicture;
        private Button btnTakePicture, btnCanny;
        private Bitmap sourceBitmap;
        private int[] grayScaleMatrix, gaussianMatrix, derivateXMatrix, derivateYMatrix, gradientMatrix, nonMaxMatrix, postHysteresisMatrix, edgePointsMatrix, edgeMapMatrix, edgesMatrix, visitedMapMatrix;
        private double[] GNH,GNL,gaussMask;
        double MaxHysteresisThresh, MinHysteresisThresh;
        private Imagen imagen;
        private int size;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Main);
            if (IsThereAnAppToTakePictures())
            {
                CreateDirectoryForPictures();

                btnTakePicture = FindViewById<Button>(Resource.Id.btnTakePicture);
                imvPicture = FindViewById<ImageView>(Resource.Id.imvPicture);
                btnTakePicture.Click += TakeAPicture;
            }
            imagen = new Imagen();
            gaussMask = new double[] { 2, 4, 5, 4, 2,
             4, 9, 12, 9, 4,
              5, 12, 15, 12, 5,
               4, 9, 12, 9, 4,
                2, 4, 5, 4, 2 };
            MaxHysteresisThresh = 20;
            MinHysteresisThresh = 10;
            btnCanny = FindViewById<Button>(Resource.Id.btnCanny);
            btnCanny.Click += Canny;
        }

        private void ApplyCanny(object sender, EventArgs e)
        {
            //APPLY GRAYSCALE AND GET SIZE
            grayScaleMatrix = imagen.getGrayScaleMatrixFromBitmap(sourceBitmap);
            size = grayScaleMatrix.Length;
            //APPLY GAUSSIAN FILTER
            gaussianMatrix = imagen.convolucion5x5(grayScaleMatrix, sourceBitmap.Width, sourceBitmap.Height, gaussMask, 100);
            
            //DERIVATE X & Y FROM SOBEL
            derivateXMatrix = new int[size];
            derivateYMatrix = new int[size];
            double[] sobelRowGradient = new double[]    { 1, 0, -1, 1, 0, -1,  1,  0, -1 };
            double[] sobelColumnGradient = new double[] { 1, 1,  1, 0, 0,  0, -1, -1, -1 };
            derivateXMatrix = imagen.convolucion3x3(gaussianMatrix, sourceBitmap.Width, sourceBitmap.Height, sobelRowGradient, 4);
            derivateYMatrix = imagen.convolucion3x3(gaussianMatrix, sourceBitmap.Width, sourceBitmap.Height, sobelColumnGradient, 4);
            // GET GRADIENT
            gradientMatrix = new int[size];
            for (int i = 0; i < size; i++)
            {
                gradientMatrix[i] = Convert.ToInt32(Math.Sqrt(derivateXMatrix[i] * derivateXMatrix[i] + derivateYMatrix[i] * derivateYMatrix[i]));
            }
            
            //NON MAX SUPpRESION EQUAL TO GRADIENT AT THE BEGINNING
            nonMaxMatrix = new int[size];
            for (int i = 0; i < size; i++)
            {
                nonMaxMatrix[i] = gradientMatrix[i]; 
            }
            int limit = 5 / 2;
            double tangent;

            for (int i = limit; i <= (sourceBitmap.Width - limit) - 1; i++)
            {
                for (int j = limit; j <= (sourceBitmap.Height - limit) - 1; j++)
                {

                    if (derivateXMatrix[i * sourceBitmap.Width + j] == 0)
                        tangent = 90F;
                    else
                        tangent = (float)(Math.Atan(derivateYMatrix[i * sourceBitmap.Width + j] / derivateXMatrix[i * sourceBitmap.Width + j]) * 180 / Math.PI); //rad to degree



                    //Horizontal Edge
                    if (((-22.5 < tangent) && (tangent <= 22.5)) || ((157.5 < tangent) && (tangent <= -157.5)))
                    {
                        if ((gradientMatrix[i * sourceBitmap.Width + j] < gradientMatrix[i * sourceBitmap.Width + (j + 1)]) || (gradientMatrix[i * sourceBitmap.Width + j] < gradientMatrix[i * sourceBitmap.Width + (j - 1)]))
                            nonMaxMatrix[i * sourceBitmap.Width + j] = 0;
                    }


                    //Vertical Edge
                    if (((-112.5 < tangent) && (tangent <= -67.5)) || ((67.5 < tangent) && (tangent <= 112.5)))
                    {
                        if ((gradientMatrix[i * sourceBitmap.Width + j] < gradientMatrix[(i+1) * sourceBitmap.Width + j]) || (gradientMatrix[i * sourceBitmap.Width + j] < gradientMatrix[(i-1) * sourceBitmap.Width + j]))
                            nonMaxMatrix[i * sourceBitmap.Width + j] = 0;
                    }

                    //+45 Degree Edge
                    if (((-67.5 < tangent) && (tangent <= -22.5)) || ((112.5 < tangent) && (tangent <= 157.5)))
                    {
                        if ((gradientMatrix[i * sourceBitmap.Width + j] < gradientMatrix[(i+1) * sourceBitmap.Width + (j-1)]) || (gradientMatrix[i * sourceBitmap.Width + j] < gradientMatrix[(i-1) * sourceBitmap.Width + (j+1)]))
                            nonMaxMatrix[i * sourceBitmap.Width + j] = 0;
                    }

                    //-45 Degree Edge
                    if (((-157.5 < tangent) && (tangent <= -112.5)) || ((67.5 < tangent) && (tangent <= 22.5)))
                    {
                        if ((gradientMatrix[i * sourceBitmap.Width + j] < gradientMatrix[(i+1) * sourceBitmap.Width + (j+1) ]) || (gradientMatrix[i * sourceBitmap.Width + j] < gradientMatrix[(i-1) * sourceBitmap.Width + (j-1)]))
                            nonMaxMatrix[i * sourceBitmap.Width + j] = 0;
                    }

                }
            }
            /*
            //POSTHYSTERESIS EQUAL TO NONMAX AT THE BEGINNING
            postHysteresisMatrix = new int[size];
            for (int i = 0; i < size; i++)
            {
                postHysteresisMatrix[i] = nonMaxMatrix[i];
            }

            //PostHysteresis = NonMax;
            for (int r = limit; r <= (sourceBitmap.Width - limit) - 1; r++)
            {
                for (int c = limit; c <= (sourceBitmap.Height - limit) - 1; c++)
                {

                    postHysteresisMatrix[r * sourceBitmap.Width + c] = nonMaxMatrix[r * sourceBitmap.Width + c];
                }

            }
            
            //FIND MAX AND MIN FROM POSTHYSTERESIS
            float min, max;
            min = 100;
            max = 0;
            for (int r = limit; r <= (sourceBitmap.Width - limit) - 1; r++)
                for (int c = limit; c <= (sourceBitmap.Height - limit) - 1; c++)
                {
                    if (postHysteresisMatrix[r * sourceBitmap.Width + c] > max)
                    {
                        max = postHysteresisMatrix[r * sourceBitmap.Width + c];
                    }

                    if ((postHysteresisMatrix[r * sourceBitmap.Width + c] < min) && (postHysteresisMatrix[r * sourceBitmap.Width + c] > 0))
                    {
                        min = postHysteresisMatrix[r * sourceBitmap.Width + c];
                    }
                }

            GNH = new double[size];
            GNL = new double[size]; 
            edgePointsMatrix = new int[size];
            edgeMapMatrix = new int[size];
            edgesMatrix = new int[size];
            visitedMapMatrix = new int[size];

            for (int r = limit; r <= (sourceBitmap.Width - limit) - 1; r++)
            {
                for (int c = limit; c <= (sourceBitmap.Height - limit) - 1; c++)
                {
                    if (postHysteresisMatrix[r * sourceBitmap.Width + c] >= MaxHysteresisThresh)
                    {

                        edgePointsMatrix[r * sourceBitmap.Width + c] = 1;
                        GNH[r * sourceBitmap.Width + c] = 255;
                    }
                    if ((postHysteresisMatrix[r * sourceBitmap.Width + c] < MaxHysteresisThresh) && (postHysteresisMatrix[r * sourceBitmap.Width + c] >= MinHysteresisThresh))
                    {

                        edgePointsMatrix[r * sourceBitmap.Width + c] = 2;
                        GNL[r * sourceBitmap.Width + c] = 255;

                    }

                }

            }
            
            HysterisisThresholding(edgePointsMatrix);

            for (int i = 0; i <= (sourceBitmap.Width - 1); i++)
                for (int j = 0; j <= (sourceBitmap.Height - 1); j++)
                {
                    edgeMapMatrix[i * sourceBitmap.Width + j] = edgeMapMatrix[i * sourceBitmap.Width + j] * 255;
                }
                */
            imvPicture.SetImageBitmap(imagen.getBitmapFromMatrix(nonMaxMatrix, sourceBitmap));

        }

        private void Canny(object sender, EventArgs e)
        {
            Canny objCanny = new Canny(sourceBitmap,20,10,5,1);
            int[] values = new int[sourceBitmap.Width *sourceBitmap.Height];
            //int[,] edges = objCanny.EdgeMap;
            //Bitmap bmp = sourceBitmap;
            for (int i = 0; i < sourceBitmap.Height; i++)
            {
                
                for (int j = 0; j < sourceBitmap.Width; j++)
                {
                    values[i * sourceBitmap.Width + j] = objCanny.EdgeMap[j,i];
                }
            }
            int[] aux = imagen.getPixelsFromMatrix(values);
            imvPicture.SetImageBitmap(imagen.getBitmapFromMatrix(aux, sourceBitmap));
            //imvPicture.SetImageBitmap(bmp);
        }

        private void HysterisisThresholding(int[] Edges)
        {

            int i, j;
            int Limit = 5 / 2;


            for (i = Limit; i <= (sourceBitmap.Width - 1) - Limit; i++)
                for (j = Limit; j <= (sourceBitmap.Height - 1) - Limit; j++)
                {
                    if (Edges[i * sourceBitmap.Width + j] == 1)
                    {
                        edgeMapMatrix[i * sourceBitmap.Width + j] = 1;

                    }

                }

            for (i = Limit; i <= (sourceBitmap.Width - 1) - Limit; i++)
            {
                for (j = Limit; j <= (sourceBitmap.Height - 1) - Limit; j++)
                {
                    if (Edges[i * sourceBitmap.Width + j] == 1)
                    {
                        edgeMapMatrix[i * sourceBitmap.Width + j] = 1;
                        Travers(i, j);
                        visitedMapMatrix[i * sourceBitmap.Width + j] = 1;
                    }
                }
            }
            return;
        }

        private void Travers(int X, int Y)
        {


            if (visitedMapMatrix[X * sourceBitmap.Width + Y] == 1)
            {
                return;
            }

            //1
            if (edgePointsMatrix[(X+1) * sourceBitmap.Width + Y ] == 2)
            {
                edgeMapMatrix[(X + 1) * sourceBitmap.Width + Y] = 1;
                visitedMapMatrix[(X + 1) * sourceBitmap.Width + Y] = 1;
                Travers(X + 1 , Y);
                return;
            }
            //2
            if (edgePointsMatrix[(X + 1) * sourceBitmap.Width + (Y- 1)] == 2)
            {
                edgeMapMatrix[(X + 1) * sourceBitmap.Width + (Y - 1)] = 1;
                visitedMapMatrix[(X + 1) * sourceBitmap.Width + (Y - 1)] = 1;
                Travers(X + 1, Y - 1);
                return;
            }

            //3

            if (edgePointsMatrix[X * sourceBitmap.Width + (Y - 1)] == 2)
            {
                edgeMapMatrix[X * sourceBitmap.Width + (Y - 1)] = 1;
                visitedMapMatrix[X * sourceBitmap.Width + (Y - 1)] = 1;
                Travers(X, Y - 1);
                return;
            }

            //4

            if (edgePointsMatrix[(X-1) * sourceBitmap.Width + (Y - 1)] == 2)
            {
                edgeMapMatrix[(X - 1) * sourceBitmap.Width + (Y - 1)] = 1;
                visitedMapMatrix[(X - 1) * sourceBitmap.Width + (Y - 1)] = 1;
                Travers(X - 1, Y - 1);
                return;
            }
            //5
            if (edgePointsMatrix[(X - 1) * sourceBitmap.Width + Y] == 2)
            {
                edgeMapMatrix[(X - 1) * sourceBitmap.Width + Y] = 1;
                visitedMapMatrix[(X - 1) * sourceBitmap.Width + Y] = 1;
                Travers(X - 1, Y);
                return;
            }
            //6
            if (edgePointsMatrix[(X - 1) * sourceBitmap.Width + (Y+1)] == 2)
            {
                edgeMapMatrix[(X - 1) * sourceBitmap.Width + (Y + 1)] = 1;
                visitedMapMatrix[(X - 1) * sourceBitmap.Width + (Y + 1)] = 1;
                Travers(X - 1, Y + 1);
                return;
            }
            //7
            if (edgePointsMatrix[X  * sourceBitmap.Width + (Y + 1)] == 2)
            {
                edgeMapMatrix[X * sourceBitmap.Width + (Y + 1)] = 1;
                visitedMapMatrix[X * sourceBitmap.Width + (Y + 1)] = 1;
                Travers(X, Y + 1);
                return;
            }
            //8

            if (edgePointsMatrix[(X+1) * sourceBitmap.Width + (Y + 1)] == 2)
            {
                edgeMapMatrix[(X + 1) * sourceBitmap.Width + (Y + 1)] = 1;
                visitedMapMatrix[(X + 1) * sourceBitmap.Width + (Y + 1)] = 1;
                Travers(X + 1, Y + 1);
                return;
            }


            //VisitedMap[X, Y] = 1;
            return;
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            // Make it available in the gallery

            Intent mediaScanIntent = new Intent(Intent.ActionMediaScannerScanFile);
            Uri contentUri = Uri.FromFile(App._file);
            mediaScanIntent.SetData(contentUri);
            SendBroadcast(mediaScanIntent);

            // Display in ImageView. We will resize the bitmap to fit the display
            // Loading the full sized image will consume to much memory 
            // and cause the application to crash.

            int height = Resources.DisplayMetrics.HeightPixels;
            int width = imvPicture.Width;
            App.bitmap = App._file.Path.LoadAndResizeBitmap(width, height);
            if (App.bitmap != null)
            {
                imvPicture.SetImageBitmap(App.bitmap);
                sourceBitmap = App.bitmap;
                App.bitmap = null;
            }

            // Dispose of the Java side bitmap.
            GC.Collect();
        }

        private void CreateDirectoryForPictures()
        {
            App._dir = new File(
                Environment.GetExternalStoragePublicDirectory(
                    Environment.DirectoryPictures), "DAP_Canny");
            if (!App._dir.Exists())
            {
                App._dir.Mkdirs();
            }
        }

        private bool IsThereAnAppToTakePictures()
        {
            Intent intent = new Intent(MediaStore.ActionImageCapture);
            IList<ResolveInfo> availableActivities =
                PackageManager.QueryIntentActivities(intent, PackageInfoFlags.MatchDefaultOnly);
            return availableActivities != null && availableActivities.Count > 0;
        }

        private void TakeAPicture(object sender, EventArgs eventArgs)
        {
            Intent intent = new Intent(MediaStore.ActionImageCapture);

            App._file = new File(App._dir, String.Format("myPhoto_{0}.jpg", Guid.NewGuid()));

            intent.PutExtra(MediaStore.ExtraOutput, Uri.FromFile(App._file));

            StartActivityForResult(intent, 0);
        }
    }

    public static class App
    {
        public static File _file;
        public static File _dir;
        public static Bitmap bitmap;
        public static Bitmap getBitmap()
        {
            return bitmap;
        }
    }
}

