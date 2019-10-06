using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

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

        private void HoughTransform()
        {
            double thetaSize = 0.02;
            float rMax = 1;
            int diag = (int)Math.Sqrt(InputImage.Size.Width * InputImage.Size.Width + InputImage.Size.Height * InputImage.Size.Height);
            float[,] accArray = new float[(int)Math.Ceiling(Math.PI / thetaSize), diag];

            for (int x = 0; x < InputImage.Size.Width; x++) {
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    if (Image[x, y].R != 255)
                        for (double i = 0; i < (Math.PI * 100); i += (thetaSize * 100))
                        {                           
                            double r = x * Math.Cos((i / 100)) + y * Math.Sin((i / 100));
                            double rest = r % rMax;
                            if (rest < 0.5)
                                accArray[(int)(i / (thetaSize * 100)), Math.Abs((int)(r - rest))]++;
                            else
                                accArray[(int)(i / (thetaSize * 100)), Math.Abs((int)(r + (1 - rest)))]++;
                        }
                }
            }

            Color[,] houghImage = new Color[accArray.GetLength(0), accArray.GetLength(1)];
            OutputImage = new Bitmap(accArray.GetLength(0), accArray.GetLength(1));

            for (int x = 0; x < houghImage.GetLength(0); x++)
            {
                for (int y = 0; y < houghImage.GetLength(1); y++)
                {
                    int value = (int)(accArray[x, y]) * 10;
                    if (value > 255)
                        value = 255;
                    houghImage[x, y] = Color.FromArgb(value, value, value);
                    OutputImage.SetPixel(x, y, houghImage[x, y]);
                }
            }

            pictureBox3.Image = (Image)OutputImage;

            OutputImage = new Bitmap(InputImage.Size.Width, InputImage.Size.Height);
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
            if (OutputImage == null) return;                                // Get out if no output image
            pictureBox1.Image = pictureBox2.Image;
            InputImage = new Bitmap(pictureBox2.Image);
        }
    }
}