﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Numerics;
using System.Windows;

namespace INFOIBV
{
    public partial class INFOIBV : Form
    {
        private Bitmap InputImage;
        private Bitmap OutputImage;
        Color[,] Image;

        public INFOIBV()
        {
            InitializeComponent();
        }

        private void LoadImageButton_Click(object sender, EventArgs e)
        {
            if (openImageDialog.ShowDialog() == DialogResult.OK)             // Open File Dialog
            {
                string file = openImageDialog.FileName;                     // Get the file name
                imageFileName.Text = file;                                  // Show file name
                if (InputImage != null) InputImage.Dispose();               // Reset image
                InputImage = new Bitmap(file);                              // Create new Bitmap from file
                if (InputImage.Size.Height <= 0 || InputImage.Size.Width <= 0 ||
                    InputImage.Size.Height > 512 || InputImage.Size.Width > 512) // Dimension check
                    MessageBox.Show("Error in image dimensions (have to be > 0 and <= 512)");
                else
                    pictureBox1.Image = (Image)InputImage;                 // Display input image
            }
        }

        //This project was made by:
        //Steven van Blijderveen	5553083
        //Jeroen Hijzelendoorn		6262279
        //As an assignment to be delivered by at most sunday 22 september 2019

        private void applyButton_Click(object sender, EventArgs e)
        {
            if (InputImage == null) return;                                 // Get out if no input image
            if (OutputImage != null) OutputImage.Dispose();                 // Reset output image
            OutputImage = new Bitmap(InputImage.Size.Width, InputImage.Size.Height); // Create new output image
            Image = new Color[InputImage.Size.Width, InputImage.Size.Height];       // Create array to speed-up operations (Bitmap functions are very slow)

            // Setup progress bar
            progressBar.Visible = true;
            progressBar.Minimum = 1;
            progressBar.Maximum = InputImage.Size.Width * InputImage.Size.Height;
            progressBar.Value = 1;
            progressBar.Step = 1;

            // Copy input Bitmap to array            
            for (int x = 0; x < InputImage.Size.Width; x++)
            {
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    Image[x, y] = InputImage.GetPixel(x, y);                // Set pixel color in array at (x,y)
                }
            }

            //==========================================================================================
            // TODO: include here your own code
            // example: create a negative image

            string filter = (string)comboBox1.SelectedItem;

            switch (filter)
            {
                case ("Hough transform"):
                    HoughTransform();
                    break;
                case ("Hough peak finder"):
                    HoughPeakFinder();
                    break;
                case ("Hough line detection"):
                    HoughLineDectection();
                    break;
                case ("Edge detection"):
                    EdgeDetection();
                    break;
                case ("Thresholding"):
                    Thresholding();
                    break;
                case ("Nothing"):
                default:
                    break;
            }
            //==========================================================================================

            // Copy array to output Bitmap
            for (int x = 0; x < InputImage.Size.Width; x++)
            {
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    OutputImage.SetPixel(x, y, Image[x, y]);               // Set the pixel color at coordinate (x,y)
                }
            }

            pictureBox2.Image = (Image)OutputImage;                         // Display output image
            progressBar.Visible = false;                                    // Hide progress bar
        }

        double dAng;                // Deze gaan we in andere methodes nodig hebben om de goede theta uit te rekenen die horen bij een [ia, ir] paar

        private void HoughTransform()
        {
            int xCtr = InputImage.Size.Width / 2;
            int yCtr = InputImage.Size.Height / 2;
            int nAng = 360;
            int nRad = 360;         
            int cRad = nRad / 2;
            dAng = Math.PI / nAng;
            double rMax = Math.Sqrt(xCtr * xCtr + yCtr * yCtr);
            double dRad = (2.0 * rMax) / nRad;
            int[,] houghArray = new int[nAng,nRad];

            int h = InputImage.Size.Height;
            int w = InputImage.Size.Width;
            for (int v = 0; v < h; v++)
            {
                for (int u = 0; u < w; u++)
                {
                    if (Image[u,v].R < 255)
                    {
                        int x = u - xCtr;
                        int y = v - yCtr;

                        for(int ia = 0; ia < nAng; ia++)
                        {
                            double theta = dAng * ia;
                            int ir = cRad + (int) Math.Floor(((x * Math.Cos(theta) + y * Math.Sin(theta)) / dRad));
                            if (ir >= 0 && ir < nRad)
                                houghArray[ia, ir]++;
                        }
                    }
                }
            }
            
            Color[,] houghImage = new Color[houghArray.GetLength(0), houghArray.GetLength(1)];
            OutputImage = new Bitmap(houghArray.GetLength(0), houghArray.GetLength(1));

            for (int x = 0; x < houghImage.GetLength(0); x++)
            {
                for (int y = 0; y < houghImage.GetLength(1); y++)
                {
                    int value = (houghArray[x, y] * 10);
                    if (value > 255)
                        value = 255;
                    houghImage[x, y] = Color.FromArgb(value, value, value);
                    OutputImage.SetPixel(x, y, houghImage[x, y]);
                }
            }

            pictureBox3.Image = OutputImage;

            OutputImage = new Bitmap(InputImage.Size.Width, InputImage.Size.Height);
        }

        private void HoughPeakFinder()
        {
            int threshold;

            try
            {
                threshold = int.Parse(textBox1.Text);
            }
            catch
            {
                return;
            }

            Color[,] OriginalImage = new Color[InputImage.Size.Width, InputImage.Size.Height];   // Duplicate the original image
            for (int x = 0; x < InputImage.Size.Width; x++)
            {
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    OriginalImage[x, y] = Image[x, y];
                }
            }

            for (int x = 0; x < InputImage.Size.Width; x++)
            {
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    if (x > 0)
                    {
                        if (Image[x, y].R < Image[x - 1, y].R)
                        {
                            OriginalImage[x, y] = Color.FromArgb(0, 0, 0);
                        }
                    }
                    if (x < InputImage.Size.Width - 1)
                    {
                        if (Image[x, y].R < Image[x + 1, y].R)
                        {
                            OriginalImage[x, y] = Color.FromArgb(0, 0, 0);
                        }
                    }
                    if (y > 0)
                    {
                        if (Image[x, y].R < Image[x, y - 1].R)
                        {
                            OriginalImage[x, y] = Color.FromArgb(0, 0, 0);
                        }
                    }
                    if (y < InputImage.Size.Height - 1)
                    {
                        if (Image[x, y].R < Image[x, y + 1].R)
                        {
                            OriginalImage[x, y] = Color.FromArgb(0, 0, 0);
                        }
                    }
                }
            }

            Image = OriginalImage;

            Thresholding();

            string message = "R/Theta-pairs: \n";

            for (int x = 0; x < InputImage.Size.Width; x++)
            {
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    if (Image[x, y].R > 0)
                    {
                        message += "(" + y + ", " + x + ")\n";  // X is theta here, so for a R/Theta pair we use y, then x
                    }
                }
            }

            MessageBox.Show(message, "R/Theta-pairs", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void HoughLineDectection()
        {
            int minLength, maxGap;
            double r, theta, minIntensity;

            try
            {
                r = double.Parse(textBox2.Text.Split(' ')[0]);
                theta = double.Parse(textBox2.Text.Split(' ')[1]);
                minIntensity = int.Parse(textBox3.Text);
                minLength = int.Parse(textBox4.Text);
                maxGap = int.Parse(textBox5.Text);
            }
            catch
            {
                return;
            }

            Vector v = new Vector(Math.Cos(theta * dAng), Math.Sin(theta * dAng));
            v.Normalize();
            Vector intersectionPoint = new Vector(v.X * r, v.Y * r);
            Vector lineFormula = new Vector(v.Y, v.X);
            List<Vector[]> linePairList = new List<Vector[]>();
            bool[,] inLine = new bool[Image.GetLength(0), Image.GetLength(1)];      // Any coordinate marked true is already part of the line and thus being counted double. When this is the case the pixel will be ignored
            Vector[] linePair = new Vector[2];
            bool makingLine = false, onGap = false;
            int gapCount = 0, lengthCount = 0;
            Vector startGap = new Vector(0,0);

            for (int x = 0; x < InputImage.Size.Width; x++)
            {
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    double factor = (x - intersectionPoint.X) / lineFormula.X;
                    Vector linePoint = intersectionPoint + factor * lineFormula; // Get the position of the line at the same x value
                    if (lineFormula.X == 0 && (int)intersectionPoint.X == x || Math.Abs(y - linePoint.Y) < 1)  // If linePoint's y is 'within' the pixels y
                    {
                        if (Image[x, y].R <= minIntensity)
                        {
                            if (makingLine)
                            {
                                linePair[1] = new Vector(x, y);
                            }
                            else if (!inLine[x, y])
                            {
                                linePair[0] = new Vector(x, y);
                                makingLine = true;
                            }
                            gapCount = 0;
                            onGap = false;
                            lengthCount++;
                            inLine[x, y] = true;
                        }
                        else if (gapCount < maxGap && makingLine)
                        {
                            gapCount++;
                            lengthCount++;
                            if (!onGap)
                                startGap = linePair[1];
                            linePair[1] = new Vector(x, y);
                            inLine[x, y] = true;
                            onGap = true;
                        }
                        /*!!!*/
                        else if (gapCount >= maxGap || (x == InputImage.Size.Width - 1 || y == InputImage.Size.Height - 1 && makingLine))   // Add line pair to list if line ends or we've arrived at the opposite border of the image
                        {
                            if (gapCount >= maxGap)
                                linePair[1] = startGap;
                            if (lengthCount >= minLength)
                                linePairList.Add(linePair);
                            makingLine = false;
                            gapCount = 0;
                            lengthCount = 0;
                        }
                    }
                }
            }

            string message = "";

            for (int i = 0; i < linePairList.Count; i++)
            {
                Vector v1 = linePairList.ElementAt(i).ElementAt(0);
                Vector v2 = linePairList.ElementAt(i).ElementAt(1);

                message += "Line segment " + (i + 1) + ": (" + v1.X + ", " + v1.Y + "), (" + v2.X + ", " + v2.Y + ")\n";
            }

            MessageBox.Show(message, "List of line pairs", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void EdgeDetection()
        {
            double normalisationFactor;
            double[,] edgeFilterX = GetEDFilter(comboBox2.Text + "x");
            double[,] edgeFilterY = GetEDFilter(comboBox2.Text + "y");

            switch (comboBox2.Text)
            {
                case ("Prewitt"):
                    normalisationFactor = 1f / 6f;
                    break;
                case ("Sobel"):
                    normalisationFactor = 1f / 8f;
                    break;
                default:
                    normalisationFactor = 0f;
                    break;
            }

            Color[,] OriginalImage = new Color[InputImage.Size.Width, InputImage.Size.Height];   // Duplicate the original image
            for (int x = 0; x < InputImage.Size.Width; x++)
            {
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    OriginalImage[x, y] = Image[x, y];
                }
            }

            for (int x = 0; x < InputImage.Size.Width; x++)
            {
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    double totalX = 0, totalY = 0;
                    for (int i = -1; i <= 1; i++)
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            if (x + i >= 0 && x + i < InputImage.Size.Width && y + j >= 0 && y + j < InputImage.Size.Height)
                            {
                                totalX += OriginalImage[x + i, y + j].R * edgeFilterX[i + 1, j + 1];
                                totalY += OriginalImage[x + i, y + j].R * edgeFilterY[i + 1, j + 1];
                            }
                            // If the selected pixel is out of bounds, count that pixel value as 0, which does nothing
                        }
                    }
                    totalX *= normalisationFactor;
                    totalY *= normalisationFactor;
                    double EdgeStrength = Math.Sqrt(totalX * totalX + totalY * totalY);
                    Image[x, y] = Color.FromArgb((int)EdgeStrength, (int)EdgeStrength, (int)EdgeStrength);
                    progressBar.PerformStep();                              // Increment progress bar
                }
            }
        }

        private void Thresholding()
        {
            int threshold;
            try
            {
                threshold = int.Parse(textBox1.Text);                       // Try to get the threshold by parsing
            }
            catch
            {
                return;
            }

            threshold = Math.Max(0, Math.Min(255, threshold));              // Clamp threshold between 0 and 255              

            for (int x = 0; x < InputImage.Size.Width; x++)
            {
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    Color pixelColor = Image[x, y];                         // Get the pixel color at coordinate (x,y)
                    if (pixelColor.R > threshold)                           // Set color to black if grayscale (thus either R, G or B) is above threshold, else make the color white
                        Image[x, y] = Color.White;
                    else
                        Image[x, y] = Color.Black;
                    progressBar.PerformStep();                              // Increment progress bar
                }
            }
        }

        private double[,] GetEDFilter(string filterName)
        {
            switch (filterName)
            {
                case ("Prewittx"):
                    return new double[,]
                    {
                        { -1, 0, 1 },
                        { -1, 0, 1 },
                        { -1, 0, 1 }
                    };
                case ("Sobelx"):
                    return new double[,]
                    {
                        { -1, 0, 1 },
                        { -2, 0, 2 },
                        { -1, 0, 1 }
                    };
                case ("Prewitty"):
                    return new double[,]
                    {
                        { -1, -1, -1 },
                        { 0, 0, 0 },
                        { 1, 1, 1 }
                    };
                case ("Sobely"):
                    return new double[,]
                    {
                        { -1, -2, -1 },
                        { 0, 0, 0 },
                        { 1, 2, 1 }
                    };
                default:
                    return new double[,]
                    {
                        { 0, 0, 0 },
                        { 0, 0, 0 },
                        { 0, 0, 0 }
                    };
            }
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            if (OutputImage == null) return;                                // Get out if no output image
            if (saveImageDialog.ShowDialog() == DialogResult.OK)
                OutputImage.Save(saveImageDialog.FileName);                 // Save the output image
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (pictureBox3.Visible)
            {
                pictureBox1.Image = pictureBox3.Image;
                InputImage = new Bitmap(pictureBox3.Image);
                return;
            }
            if (OutputImage == null) return;                                // Get out if no output image
            pictureBox1.Image = pictureBox2.Image;
            InputImage = new Bitmap(pictureBox2.Image);
        }
    }
}