using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Security.Cryptography;

//16:5 image aspect ratio
//fix up scaling!!
namespace osutostep
{
    public enum TextLocation
    {
        TopLeft, TopCenter, TopRight,
        CenterLeft, Center, CenterRight,
        BottomLeft, BottomCenter, BottomRight,
    }

    public class Banner
    {
        private Bitmap bannerImage;

        public bool Loaded { get; private set; }
        public Bitmap BannerSource { get; private set; }
        public string BannerTitle { get; set; }
        public string BannerArtist { get; set; }

        public string BannerFont { get; set; }
        public Color FontColor { get; set; }

        public static readonly string DefaultFont = "";

        private Graphics bannerGraphics;

        public Banner(string path = "")
        {
            LoadFromImage(path);
        }

        public Banner(int width, int height)
        {
            try
            {
                BannerSource = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                int scaleW = BannerSource.Size.Width;
                int scaleH = (int)(((double)BannerSource.Size.Width) / 3.2);
                bannerImage = new Bitmap(scaleW, scaleH, PixelFormat.Format32bppArgb);
                bannerGraphics = Graphics.FromImage(bannerImage);
                Loaded = true;
                FontColor = Color.Black;
            }
            catch { }
        }

        public void LoadFromImage(string path)
        {
            Loaded = false;
            if (!string.IsNullOrWhiteSpace(path))
            {
                try
                {
                    BannerSource = new Bitmap(path);
                    Loaded = true;
                    FontColor = Color.Black;
                }
                catch { }
            }
        }

        private int hashThatShitLOL(Bitmap bitmap)
        {
            byte[] someshit;
            using (MemoryStream memStream = new MemoryStream())
            {
                bitmap.Save(memStream, ImageFormat.Bmp);
                using (MD5 checksum = MD5.Create())
                {
                    someshit = checksum.ComputeHash(memStream);
                }
            }
            int total = 0;
            foreach (byte b in someshit)
            {
                total ^= b;
            }
            return total % 9;
        }

        public Bitmap GenerateFinalImage()
        {
            TextLocation textLoc = TextLocation.BottomRight;
            //same location for same background
            switch (hashThatShitLOL(BannerSource))
            {
                case 0:
                    textLoc = TextLocation.TopLeft;
                    break;
                case 1:
                    textLoc = TextLocation.TopCenter;
                    break;
                case 2:
                    textLoc = TextLocation.TopRight;
                    break;
                case 3:
                    textLoc = TextLocation.CenterLeft;
                    break;
                case 4:
                    textLoc = TextLocation.Center;
                    break;
                case 5:
                    textLoc = TextLocation.CenterRight;
                    break;
                case 6:
                    textLoc = TextLocation.BottomLeft;
                    break;
                case 7:
                    textLoc = TextLocation.BottomCenter;
                    break;
                case 8:
                    textLoc = TextLocation.BottomRight;
                    break;
            }
            return GenerateFinalImage(0, (BannerSource.Height / 2) - (int)(((BannerSource.Size.Width) / 3.2) / 2.0), 1, textLoc);
        }

        private void generateBannerOutput(double scale)
        {
            int scaleW = (int)((double)BannerSource.Size.Width * scale);
            int scaleH = (int)((((double)BannerSource.Size.Width) / 3.2) * scale);
            bannerImage = new Bitmap(scaleW, scaleH, PixelFormat.Format32bppArgb);
            bannerGraphics = Graphics.FromImage(bannerImage);
        }

        public Bitmap GenerateFinalImage(int x, int y, double scale, TextLocation location)
        {
            if (!Loaded)
            {
                return null;
            }

            generateBannerOutput(scale);

            PrivateFontCollection pfc = new PrivateFontCollection();
            bool usingCustFont = true;
            try
            {
                pfc.AddFontFile(BannerFont);
            }
            catch { usingCustFont = false; }
            Font largeBold;
            Font smallRegular;


            float n = GetFontSize(bannerImage.Height);

            if (usingCustFont)
            {
                largeBold = new Font(pfc.Families[0], n, FontStyle.Bold);
                smallRegular = new Font(pfc.Families[0], n / 3.6f);
            }
            else
            {
                largeBold = new Font(DefaultFont, n, FontStyle.Bold);
                smallRegular = new Font(DefaultFont, n / 3.6f);
            }

            SizeF mainSize = bannerGraphics.MeasureString(BannerTitle, largeBold);
            SizeF subSize = bannerGraphics.MeasureString($"Artist : {BannerArtist}", smallRegular);

            PointF mainLoc = getMainTextPoint(mainSize, subSize, new SizeF(bannerImage.Width, bannerImage.Height), location);
            PointF subLoc = getSubTextPoint(mainSize, subSize, new SizeF(bannerImage.Width, bannerImage.Height), location);

            SolidBrush norm = new SolidBrush(FontColor);
            Pen pen = new Pen(Color.White, 2);

            bannerGraphics.DrawImage(BannerSource, -x, -y);

            int totalHeight, totalWidth;
            totalWidth = (int)Math.Max(mainSize.Width, subSize.Width);
            totalHeight = (int)((subLoc.Y + subSize.Height) - mainLoc.Y);
            Rectangle surrounding = new Rectangle(Math.Min((int)mainLoc.X, (int)subLoc.X), Math.Min((int)mainLoc.Y, (int)subLoc.Y), totalWidth, totalHeight);
            Blur(bannerImage, surrounding, 4);
            //bannerGraphics.ddddd:D         
            bannerGraphics.DrawRectangle(pen, surrounding);
            bannerGraphics.DrawString(BannerTitle, largeBold, norm, mainLoc);
            bannerGraphics.DrawString($"Artist : {BannerArtist}", smallRegular, norm, subLoc);

            norm.Dispose();
            pen.Dispose();

            return bannerImage;
        }

        public PointF getSubTextPoint(SizeF textSize, SizeF subTextSize, SizeF bannerSize, TextLocation location)
        {
            float yLoc = 0, xLoc = 0;
            //anchor offset (20, 20)

            if (location.ToString().Substring(0, 6).Equals("Bottom"))
            {
                yLoc = bannerSize.Height - 20 - subTextSize.Height;
            }
            else if (location.ToString().Substring(0, 6).Equals("Center"))
            {
                yLoc = (bannerSize.Height / 2);
            }
            else
            {
                yLoc = 20 + textSize.Height;
            }

            if (location.ToString().EndsWith("Right"))
            {
                xLoc = bannerSize.Width - 20 - subTextSize.Width;
            }
            else if (location.ToString().EndsWith("Center"))
            {
                xLoc = (bannerSize.Width / 2) - (subTextSize.Width / 2);
            }
            else
            {
                xLoc = 20;
            }
            return new PointF(xLoc, yLoc);
        }

        public PointF getMainTextPoint(SizeF textSize, SizeF subTextSize, SizeF bannerSize, TextLocation location)
        {
            float yLoc = 0, xLoc = 0;
            //anchor offset (20, 20)

            if (location.ToString().Substring(0, 6).Equals("Bottom"))
            {
                yLoc = bannerSize.Height - 20 - textSize.Height - subTextSize.Height;
            }
            else if (location.ToString().Substring(0, 6).Equals("Center"))
            {
                yLoc = (bannerSize.Height / 2) - (textSize.Height);
            }
            else
            {
                yLoc = 20;
            }

            if (location.ToString().EndsWith("Right"))
            {
                xLoc = bannerSize.Width - 20 - textSize.Width;
            }
            else if (location.ToString().EndsWith("Center"))
            {
                xLoc = (bannerSize.Width / 2) - (textSize.Width / 2);
            }
            else
            {
                xLoc = 20;
            }
            return new PointF(xLoc, yLoc);
        }

        public Bitmap GenerateFinalImage(int y, TextLocation location)
        {
            return GenerateFinalImage(0, y, 1, location);
        }
        public float GetFontSize(int w)
        {
            //10 is an arbitrary proportion to the image, this can be fine tuned until it looks right
            float scalingFactor = 10.0f;
            int finalPixel = (int)(((float)w / scalingFactor) + 0.5f);
            return finalPixel * (7 / 5);
        }

        //ripped from the net
        private static void Blur(Bitmap image, Rectangle rectangle, int blurSize)
        {
            // look at every pixel in the blur rectangle
            for (int xx = rectangle.X; xx < rectangle.X + rectangle.Width; xx++)
            {
                for (int yy = rectangle.Y; yy < rectangle.Y + rectangle.Height; yy++)
                {
                    int avgR = 0, avgG = 0, avgB = 0;
                    int blurPixelCount = 0;

                    // average the color of the red, green and blue for each pixel in the
                    // blur size while making sure you don't go outside the image bounds
                    for (int x = xx; (x < xx + blurSize && x < image.Width); x++)
                    {
                        for (int y = yy; (y < yy + blurSize && y < image.Height); y++)
                        {
                            Color pixel = image.GetPixel(x, y);

                            avgR += pixel.R;
                            avgG += pixel.G;
                            avgB += pixel.B;

                            blurPixelCount++;
                        }
                    }

                    avgR = avgR / blurPixelCount;
                    avgG = avgG / blurPixelCount;
                    avgB = avgB / blurPixelCount;

                    // now that we know the average for the blur size, set each pixel to that color
                    for (int x = xx; x < xx + blurSize && x < image.Width && x < rectangle.X + rectangle.Width; x++)
                        for (int y = yy; y < yy + blurSize && y < image.Height && y < rectangle.Y + rectangle.Height; y++)
                            image.SetPixel(x, y, Color.FromArgb(avgR, avgG, avgB));
                }
            }
        }
    }
}