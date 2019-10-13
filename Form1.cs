using System;
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
            progressBar.Maximum = Image.GetLength(0) * Image.GetLength(1);
            progressBar.Value = 1;
            progressBar.Step = 1;

            // Copy input Bitmap to array            
            for (int x = 0; x < Image.GetLength(0); x++)
            {
                for (int y = 0; y < Image.GetLength(1); y++)
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
                case ("Hough visualization"):
                    HoughVisualization();
                    break;
                case ("Nothing"):
                default:
                    break;
            }
            //==========================================================================================

            // Copy array to output Bitmap
            for (int x = 0; x < Image.GetLength(0); x++)
            {
                for (int y = 0; y < Image.GetLength(1); y++)
                {
                    OutputImage.SetPixel(x, y, Image[x, y]);               // Set the pixel color at coordinate (x,y)
                }
            }

            pictureBox2.Image = (Image)OutputImage;                         // Display output image
            progressBar.Visible = false;                                    // Hide progress bar
        }

        double dAng;
        double dRad;
        Color[,] houghImage;

        private void HoughTransform()
        {
            int xCtr = Image.GetLength(0) / 2;
            int yCtr = Image.GetLength(1) / 2;
            int nAng = 360;
            int nRad = 360;         
            int cRad = nRad / 2;
            dAng = Math.PI / nAng;
            double rMax = Math.Sqrt(xCtr * xCtr + yCtr * yCtr);
            dRad = (2.0 * rMax) / nRad;
            float[,] houghArray = new float[nAng,nRad];

            double lowerBound = 0, upperBound = Math.PI;
            try
            {
                lowerBound = int.Parse(textBox6.Text);
                if (lowerBound < 0)
                    lowerBound = 0;
                lowerBound = lowerBound * Math.PI / 180;
                upperBound = int.Parse(textBox1.Text);
                if (upperBound > 180)
                    upperBound = 180;
                upperBound = upperBound * Math.PI / 180;
            }
            catch
            { }

            for (int v = 0; v < Image.GetLength(1); v++)
            {
                for (int u = 0; u < Image.GetLength(0); u++)
                {
                    if (Image[u,v].R > 0)           // Hier gaat het fout, voor zowel > 0 en >= 0
                    {                               // Ziet er nu wel raar uit met plaatjes van alleen zwarte lijnen
                        int x = u - xCtr;           // Volgens de opdracht krijgen we alleen edge images (zwart plaatje met witte lijnen), hiervoor gedraagt de code zich wel anders
                        int y = v - yCtr;           // Transform of line detection heeft nog moeite met schuine lijnen
                        float edgeStrength = EdgeDetection(u, v);       
                        for (int ia = 0; ia < nAng; ia++)
                        {
                            double theta = dAng * ia;
                            if (theta >= lowerBound && theta <= upperBound)
                            {
                                int ir = cRad + (int)Math.Ceiling((x * Math.Cos(theta) + y * Math.Sin(theta)) / dRad);
                                if (ir >= 0 && ir < nRad)
                                {
                                    houghArray[ia, ir] += edgeStrength;
                                }
                            }
                        }
                    }
                    progressBar.PerformStep();                              // Increment progress bar
                }
            }
            
            houghImage = new Color[houghArray.GetLength(0), houghArray.GetLength(1)];
            OutputImage = new Bitmap(houghArray.GetLength(0), houghArray.GetLength(1));

            double maxval = 0;

            for (int x = 0; x < houghImage.GetLength(0); x++)
            {
                for (int y = 0; y < houghImage.GetLength(1); y++)
                {
                    if (houghArray[x, y] > maxval)
                        maxval = houghArray[x, y];
                }                                       
            }

            Color[,] higherBrightnessCopy = houghImage;

            for (int x = 0; x < houghImage.GetLength(0); x++)
                for (int y = 0; y < houghImage.GetLength(1); y++)
                {
                    double value = 0;
                    if (maxval != 0)                    
                        value = ((houghArray[x, y] / maxval) * 255);      // Brightness is scaled to be a percentage of the largest value                    
                    houghImage[x, y] = Color.FromArgb((int)value, (int)value, (int)value);
                    value = Math.Min(value + 10, 255);
                    higherBrightnessCopy[x, y] = Color.FromArgb((int)value, (int)value, (int)value);    // After scaling, most values become nearly invisible, 
                    OutputImage.SetPixel(x, y, higherBrightnessCopy[x,y]);                              // so we add a flat amount to all of them to keep the diagram readable
                }            

            pictureBox3.Image = OutputImage;

            OutputImage = new Bitmap(Image.GetLength(0), Image.GetLength(1));
        }

        private void HoughPeakFinder()
        {
            float threshold;

            try
            {
                threshold = float.Parse(textBox1.Text);
            }
            catch
            {
                return;
            }

            Color[,] OriginalImage = new Color[Image.GetLength(0), Image.GetLength(1)];   // Duplicate the original image
            for (int x = 0; x < Image.GetLength(0); x++)
            {
                for (int y = 0; y < Image.GetLength(1); y++)
                {
                    OriginalImage[x, y] = Image[x, y];
                }
            }

            for (int x = 0; x < Image.GetLength(0); x++)
            {
                for (int y = 0; y < Image.GetLength(1); y++)
                {
                    if (x > 0)
                    {
                        if (Image[x, y].R < Image[x - 1, y].R)
                        {
                            OriginalImage[x, y] = Color.FromArgb(0, 0, 0);
                        }
                    }
                    if (x < Image.GetLength(0) - 1)
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
                    if (y < Image.GetLength(1) - 1)
                    {
                        if (Image[x, y].R < Image[x, y + 1].R)
                        {
                            OriginalImage[x, y] = Color.FromArgb(0, 0, 0);
                        }
                    }
                }
            }

            Image = OriginalImage;

            Thresholding(threshold);

            string message = "R/Theta-pairs: \n";
            bool second = false;
            List<Vector> rThetaPairs = new List<Vector>();
            Dictionary<Vector, int> checkIfDouble = new Dictionary<Vector, int>();

            for (int x = 0; x < Image.GetLength(0); x++)
            {
                for (int y = 0; y < Image.GetLength(1); y++)
                {
                    if (Image[x, y].R > 0)
                    {
                        int theta = (int)((x * dAng) * 180 / Math.PI);
                        int r = (int)((y - 180) * dRad);
                        if (!checkIfDouble.ContainsKey(new Vector(r, theta)))     // Make sure no duplicate values are displayed
                        {
                            rThetaPairs.Add(new Vector(r, theta));
                            checkIfDouble.Add(new Vector(r, theta), 1);
                        }
                    }
                    progressBar.PerformStep();                              // Increment progress bar
                }
            }

            foreach(Vector rTheta in rThetaPairs)
            {
                if (!second)
                {
                    message += "(" + rTheta.X + ", " + rTheta.Y + "), ";
                    second = true;
                }
                else
                {
                    message += "(" + rTheta.X + ", " + rTheta.Y + "),\n";
                    second = false;
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
                theta = theta / 180 * Math.PI;
                minIntensity = int.Parse(textBox3.Text);
                minLength = int.Parse(textBox4.Text);
                maxGap = int.Parse(textBox5.Text);
            }
            catch
            {
                return;
            }

            Vector v = new Vector(Math.Cos(theta), Math.Sin(theta));
            Vector intersectionPoint = new Vector(v.X * r + Image.GetLength(0) / 2, v.Y * r + Image.GetLength(1) / 2);      // The algorithm starts from the centre of the image
            Vector lineFormula = new Vector(v.Y, -v.X);
            List<Vector[]> linePairList = new List<Vector[]>();
            bool[,] inLine = new bool[Image.GetLength(0), Image.GetLength(1)];      // Any coordinate marked true is already part of the line and thus being counted double. When this is the case the pixel will be ignored
            Vector[] linePair = new Vector[2];
            bool makingLine = false, onGap = false;
            int gapCount = 0, lengthCount = 0;
            Vector lastOnLine = new Vector(0,0);

            for (int x = 0; x < Image.GetLength(0); x++)
            {
                for (int y = 0; y < Image.GetLength(1); y++)
                {
                    double factor = (x - intersectionPoint.X) / lineFormula.X;
                    Vector linePoint = intersectionPoint + factor * lineFormula; // Get the position of the line at the same x value
                    if (lineFormula.X == 0 && (int)intersectionPoint.X == x || Math.Abs(y - linePoint.Y) < 1)  // If linePoint's y is 'within' the pixels y
                    {
                        if (Image[x, y].R >= minIntensity)
                        {
                            if (x != 0 && x < Image.GetLength(0) - 1 && y != 0 && y < Image.GetLength(1) - 1)
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
                                lastOnLine = new Vector(x, y);
                            }
                        }
                        else if (gapCount < maxGap && makingLine)
                        {
                            gapCount++;
                            lengthCount++;
                            if (!onGap)
                                lastOnLine = linePair[1];
                            linePair[1] = new Vector(x, y);
                            inLine[x, y] = true;
                            onGap = true;
                        }
                        /*!!!*/
                        else if (gapCount >= maxGap)   // Add line pair to list if line ends or we've arrived at the opposite border of the image
                        {
                            linePair[1] = lastOnLine;
                            if (lengthCount >= minLength)
                            {
                                linePairList.Add(linePair);
                                linePair = new Vector[2];
                            }
                            makingLine = false;
                            gapCount = 0;
                            lengthCount = 0;
                        }
                    }
                    else if (x == Image.GetLength(0) - 1 && y == Image.GetLength(1) - 1 && makingLine)
                    {
                        linePair[1] = lastOnLine;
                        if (lengthCount >= minLength)
                            linePairList.Add(linePair);
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
        
        private void HoughVisualization()
        {
            List<Vector[]> linesegements = new List<Vector[]>();
            string[] all;

            try
            {
                all = textBox7.Text.Split('\n');
                
            }
            catch
            {
                return;
            }

            foreach (string x in all)
            {
                string[] coordinates = x.Split(' ');
                Vector[] points = new Vector[2];
                points[0] = new Vector(double.Parse(coordinates[0]), double.Parse(coordinates[1]));
                points[1] = new Vector(double.Parse(coordinates[2]), double.Parse(coordinates[3]));
                linesegements.Add(points);

            }

            Pen redPen = new Pen(Color.Red, 2);

            foreach (Vector[] vectors in linesegements)
            {
                using (var graphics = Graphics.FromImage(InputImage))
                {
                    graphics.DrawLine(redPen, (float) vectors[0].X, (float) vectors[0].Y, (float) vectors[1].X, (float) vectors[1].Y);
                }
            }

            for (int x = 0; x < Image.GetLength(0); x++)
            {
                for (int y = 0; y < Image.GetLength(1); y++)
                {
                    Image[x, y] = InputImage.GetPixel(x, y);
                }
            }
        }

        // Initialising arrays seems to take a lot of time. Since we never change these arrays, we might as well define them once instead of each time the method below is called upon.
        int[,] edgeFilterX = new int[,]
        {
                        { -1, 0, 1 },
                        { -2, 0, 2 },
                        { -1, 0, 1 }
        };
        int[,] edgeFilterY = new int[,]
                {
                        { -1, -2, -1 },
                        { 0, 0, 0 },
                        { 1, 2, 1 }
                };

        private float EdgeDetection(int x, int y)
        {
            double normalisationFactor = 1f / 8f;
            double totalX = 0, totalY = 0;
            int width = Image.GetLength(0), height = Image.GetLength(1);
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (x + i >= 0 && x + i < width && y + j >= 0 && y + j < height)
                    {
                        totalX += Image[x + i, y + j].R * edgeFilterX[i + 1, j + 1];
                        totalY += Image[x + i, y + j].R * edgeFilterY[i + 1, j + 1];
                    }
                    // If the selected pixel is out of bounds, count that pixel value as 0, which does nothing
                }
            }
            totalX *= normalisationFactor;
            totalY *= normalisationFactor;
            return (float)Math.Sqrt(totalX * totalX + totalY * totalY);
        }

        private void Thresholding(float percent)
        {
            percent = Math.Max(0, Math.Min(100, percent));              // Clamp threshold between 0 and 255              
            int totalPixels = 0;
            int[] histogram = new int[256];
            for (int x = 0; x < Image.GetLength(0); x++)
            {
                for (int y = 0; y < Image.GetLength(1); y++)
                {
                    if (houghImage[x, y].R > 0)
                    {
                        totalPixels++;
                        histogram[houghImage[x, y].R]++;
                    }
                    else
                    {

                    }
                }
            }
            int nrPixels = (int)((float)totalPixels / 100 * percent);
            int counter = 0;
            int threshold = 0;

            for (int i = histogram.Length - 1; i >= 0; i--)
            {
                counter += histogram[i];
                if (counter >= nrPixels)
                {
                    threshold = i;
                    break;
                }
            }

            for (int x = 0; x < Image.GetLength(0); x++)
            {
                for (int y = 0; y < Image.GetLength(1); y++)
                {
                    if (Image[x, y].R < threshold)                          // Set color to black if grayscale (thus either R, G or B) is above threshold, else make the color white
                        Image[x, y] = Color.Black;
                }
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