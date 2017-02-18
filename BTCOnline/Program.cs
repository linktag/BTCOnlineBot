using System;
using System.Collections.Generic;
using AForge.Imaging.Filters;
using System.Linq;
using System.Threading.Tasks;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Tesseract;
using System.Diagnostics;
using System.Net;
using OpenQA.Selenium.Support.UI;
using System.Threading;

namespace BTCOnline
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("#################################################");
            Console.WriteLine("#	BTCBot, by LinkTag17 on Nulled		#");
            Console.WriteLine("#################################################\n\n");
            string idt = "0";
            string pass = "0";
            string key = "0";

            if (args.Count() == 0)
            {
                Console.WriteLine("Please enter arguments. Use -h for help");
            }
            else
            {
                foreach (string arg in args)
                {
                    if (arg == "-h")
                    {
                        Console.WriteLine("Use the following commands : ");
                        Console.WriteLine("-h Display   help message");
                        Console.WriteLine("-idt=<IDT>   Insert your email/identifiant for BTCOnline");
                        Console.WriteLine("-pass=<PASSWORD>     Insert your password for BTCOnline");
                        Console.WriteLine("-DBC=<KEY>   Insert your Death By Captcha key for ReCaptcha");
                    }
                    else if (arg.Contains("-idt="))
                    {
                        idt = arg.Split('=')[1];
                    }
                    else if (arg.Contains("-pass="))
                    {
                        pass = arg.Split('=')[1];
                    }
                    else if (arg.Contains("-DBC="))
                    {
                        key = arg.Split('=')[1];
                    }
                    else
                    {
                        Console.WriteLine("Error : Invalid argument " + arg);
                    }
                }
            }

            DemarrerBot(idt, pass);
            //Console.WriteLine(ResoudreCaptcha(new Bitmap("transfo1\\27.png")));
            //SupprimerTraits(new Bitmap("finale_image.png")).Save("sans_traits.png", System.Drawing.Imaging.ImageFormat.Png);
            //Console.WriteLine(OCR(new Bitmap("transformee.png")));
            //Console.WriteLine(OCRItalien(new Bitmap("transfo2\\11.png")));

            Wait(10);
        }

        static void Wait(int s)
        {
            System.Threading.Thread.Sleep(s*1000);
        }

        static void DemarrerBot(string id, string mdp)
        {
            //Explications
            Console.WriteLine("Account creditentials : \nIdt = {0}\npassword = {1}\n", id, mdp);

            //On ouvre le driver
            Console.WriteLine("Opening WebBrowser");
            IWebDriver driver = new FirefoxDriver();
            driver.Navigate().GoToUrl("https://btcclicks.com/login");

            //Connexion
            Console.WriteLine("Loging");
            driver.FindElement(By.Id("inputUsername")).SendKeys(id);
            driver.FindElement(By.Id("inputPassword")).SendKeys(mdp + Keys.Enter);

            Console.WriteLine("Success");
            Wait(5);

            //Direction page des pubs
            Console.WriteLine("Naviguate to Ads webpage");
            driver.Navigate().GoToUrl("http://btcclicks.com/ads");

            //On charge les annonces
            var annonces = driver.FindElements(By.ClassName("viewBox"));
            Console.WriteLine("{0} ads loaded !\n\n", annonces.Count.ToString());

            //On s'occupe de chaque annonce
            for (int k = 0; k < annonces.Count; k++)
            {
                //Explication de la pub
                string link = "/html/body/div[@id='wrapper']/div[@id='contentWrapper']/section/div[@class='container']/div[@class='col-xs-12']/div[" + (k + 2).ToString() + "]";
                string reward = driver.FindElement(By.XPath(link+"/div[@class='viewReward']")).Text;
                string name = driver.FindElement(By.XPath(link+"/div[1]/a[1]")).Text;
                Console.WriteLine("# Ad n°" + (k+1).ToString() + ", Reward : " + reward.Split('→')[0] + ", time : " + reward.Split('→')[1]);
                Console.WriteLine("'" + name + "'");

                //On vérifie si on ne l'a pas deja fait
                if (driver.FindElements(By.XPath(link+"/div[@class='viewTitle']")).Count != 0)
                {
                    string mainTab = driver.CurrentWindowHandle;
                    //On ouvre le lien
                    driver.FindElement(By.XPath(link+"/div[@class='viewTitle']/a[1]")).Click();

                    Wait(3);
                    //Changement d'onglet
                    List<string> tableau = new List<string>(driver.WindowHandles);
                    tableau.Remove(mainTab);
                    string newTab = tableau[0];
                    driver.SwitchTo().Window(newTab);
                    int essaie = 0;

                    while (essaie < 3)
                    {
                        //On cherche pendant trois minutes ou un reCaptcha ou un Captcha classique
                        Console.WriteLine("Waiting for captcha");
                        int typeCaptcha = TypeCaptcha(driver);

                        //On fait ce que l'on a a faire
                        if (typeCaptcha == -1) //Rien a été trouvé : erreur
                        {
                            Console.WriteLine("Can't detect captcha. Cancel the view.");
                            essaie += 3;
                        }
                        else //Sinon, cela a réussi
                        {
                            if (typeCaptcha == 1)//Si c'est un recaptcha
                            {
                                Console.WriteLine("reCaptcha detect. It need to be solve manually. Press any key when captcha is complete");
                                Console.ReadLine();

                                //On clique sur le bouton ok
                                driver.FindElement(By.XPath("//div[@class='modal-footer']/button[1]")).Click();
                            }
                            else //C'est un captcha normale
                            {
                                Console.WriteLine("Classic captcha detected");
                                //On telecharge l'image du captcha
                                Bitmap captcha = ExtraireCaptcha(driver);
                                Console.WriteLine("Captcha downloaded");

                                //On le resout :
                                string solutionCaptcha = ResoudreCaptcha(captcha);

                                //On l'ajoute
                                driver.FindElement(By.XPath("//div[@id='captcha']/input[1]")).SendKeys(solutionCaptcha + Keys.Enter);
                            }

                            //Result :
                            Wait(2);
                            string sortie = driver.FindElement(By.XPath("//div[@id='overlay']")).Text;
                            Console.WriteLine("Result : '{0}'", sortie);

                            if (sortie.Contains("You have been paid"))
                            {
                                //C'est une réussite
                                essaie = 5;
                            }
                            else //On reccomence
                            {
                                Console.WriteLine("Retry ! ({0})", essaie.ToString());
                                essaie += 1;
                            }


                        }

                        driver.Navigate().Refresh();
                    }

                    //On ferme l'onglet et on revient au principal
                    driver.Close();
                    driver.SwitchTo().Window(mainTab);
                    Wait(2);
                    Console.WriteLine("\n");

                }
                else
                {
                    Console.WriteLine("Ad Already viewed\n\n");
                }

                
            }
        }

        private static int TypeCaptcha(IWebDriver driver)
        {
            DateTime end = DateTime.Now.Add(new TimeSpan(0, 0, 60+30));
            while (end > DateTime.Now)
            {
                if (driver.FindElements(By.XPath("//div[@id='recaptcha-elem']/div[1]")).Count != 0)
                    return 1;
                else if (driver.FindElements(By.XPath("//div[@id='captcha']/img[1]")).Count != 0)
                    return 2;
                else
                    System.Threading.Thread.Sleep(2500);
            }
            return -1;
    }

        private static Bitmap ExtraireCaptcha(IWebDriver driver)
        {
            ITakesScreenshot ssdriver = driver as ITakesScreenshot;
            Screenshot screenshot = ssdriver.GetScreenshot();

            Screenshot tempImage = screenshot;

            tempImage.SaveAsFile("full.png", ScreenshotImageFormat.Png);
            Wait(2);

            //replace with the XPath of the image element
            IWebElement my_image = driver.FindElement(By.XPath("//div[@id='captcha']/img[1]"));

            Point point = my_image.Location;
            int width = my_image.Size.Width;
            int height = my_image.Size.Height;

            Rectangle section = new Rectangle(point, new Size(width, height));
            Bitmap source = new Bitmap("full.png");
            source = SupprimerTraits(source);
            Bitmap final_image = CropImage(source, section);

            return final_image;
        }

        private static Bitmap CropImage(Bitmap source, Rectangle section)
        {
            Bitmap bmp = new Bitmap(section.Width, section.Height);
            Graphics g = Graphics.FromImage(bmp);
            g.DrawImage(source, 0, 0, section, GraphicsUnit.Pixel);
            return bmp;
        }

        private static Bitmap CaptchaOrangeEtCroix(Bitmap captcha)
        {
            //On convertir l'image en un tableau
            Rectangle rect = new Rectangle(0, 0, captcha.Width, captcha.Height);
            BitmapData bmpData = captcha.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            int numBytes = bmpData.Stride * captcha.Height;
            byte[] rgbValues = new byte[numBytes];

            // Copy the RGB values into the array.
            Marshal.Copy(ptr, rgbValues, 0, numBytes);

            //On recompile l'image
            // Copy the RGB values back to the bitmap
            Marshal.Copy(rgbValues, 0, ptr, numBytes);

            //throw new NotImplementedException();

            // Unlock the bits.
            captcha.UnlockBits(bmpData);


            //Console.WriteLine("({0}, {1}, {2})", GetPixel(rgbValues, captcha.Width, captcha.Width - 1, captcha.Height - 1)[0].ToString(), GetPixel(rgbValues, captcha.Width, captcha.Width - 1, captcha.Height - 1)[1].ToString(), GetPixel(rgbValues, captcha.Width, captcha.Width - 1, captcha.Height - 1)[2].ToString());
            byte[] detectionOrange = new byte[3] { 50, 121, 230 };

            if (GetPixel(rgbValues, captcha.Width, captcha.Width-1, captcha.Height-1)[0] == detectionOrange[0] && GetPixel(rgbValues, captcha.Width, captcha.Width - 1, captcha.Height - 1)[1] == detectionOrange[1] && GetPixel(rgbValues, captcha.Width, captcha.Width - 1, captcha.Height - 1)[2] == detectionOrange[2])
            {
                Console.WriteLine("Orange Captcha detected. Deleting orange");
                captcha = SupprimerFond(captcha, GetPixel(rgbValues, captcha.Width, captcha.Width-1, captcha.Height/2), 15);
            }

            return captcha;
        }



        public static Bitmap CropImage(Bitmap image, int x, int y, int width, int height)
        {

            Bitmap croppedImage;
            // Here we capture the resource - image file.
            using (var originalImage = image)
            {
                Rectangle crop = new Rectangle(x, y, width, height);

                // Here we capture another resource.
                croppedImage = originalImage.Clone(crop, originalImage.PixelFormat);

            } // Here we release the original resource - bitmap in memory and file on disk.

            // At this point the file on disk already free - you can record to the same path.
            //croppedImage.Save("cropped.png", System.Drawing.Imaging.ImageFormat.Png);

            // It is desirable release this resource too.
            return croppedImage;
        }



        private static string ResoudreCaptcha(Bitmap captcha)
        {
            //On commence par Crop l'image au bonnes dimensions
            Console.WriteLine("Resising captcha");
            captcha = CropImage(captcha, 15, 1, 90-15-1, 40-1-1);
            //captcha = SupprimerTraits(captcha);

            //On supprime les traits sur l'image
            Console.WriteLine("Editing Captcha to make it easier to read");

            //On effectue la suppression du fond
            captcha = SupprimerFond(captcha, 5);

            //On l'améliore encore
            captcha = CaptchaOrangeEtCroix(captcha);
            captcha = AmeliorationsSupplementaires(captcha);


            //Utilisation de l'OCR
            Console.WriteLine("Use OCR to read captcha");
            string captchaText = OCRItalien(captcha);
            Console.WriteLine("Captcha : {0}", captchaText);


            //On enregistre le captcha modifie
            captcha.Save("amelioree.png", System.Drawing.Imaging.ImageFormat.Png);

            //Reconnaiissance
            Console.WriteLine("Understanding captcha");
            string solution = InterpreterResultat(captchaText);
            return solution;
        } 

        private static string ResoudreItalien(Bitmap captcha)
        {
            Bitmap imagem = captcha;
            imagem = imagem.Clone(new Rectangle(0, 0, captcha.Width, captcha.Height), System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            Erosion erosion = new Erosion();
            Dilatation dilatation = new Dilatation();
            Invert inverter = new Invert();
            ColorFiltering cor = new ColorFiltering();
            cor.Blue = new AForge.IntRange(200, 255);
            cor.Red = new AForge.IntRange(200, 255);
            cor.Green = new AForge.IntRange(200, 255);
            Opening open = new Opening();
            BlobsFiltering bc = new BlobsFiltering();
            Closing close = new Closing();
            GaussianSharpen gs = new GaussianSharpen();
            ContrastCorrection cc = new ContrastCorrection();
            bc.MinHeight = 10;
            FiltersSequence seq = new FiltersSequence(gs, inverter, open, inverter, bc, inverter, open, cc, cor, bc, inverter);
            string reconhecido = OCRItalien((Bitmap)seq.Apply(imagem));
            return reconhecido;
        }

        private static string OCRItalien(Bitmap b)
        {
            string res = "";
            using (var engine = new TesseractEngine(null, "eng", EngineMode.Default))
            {
                engine.SetVariable("tessedit_char_whitelist", "1234567890+-");
                engine.SetVariable("tessedit_unrej_any_wd", true);

                using (var page = engine.Process(b, PageSegMode.SingleLine))
                    res = page.GetText();
            }
            return res;
        }

        private static Bitmap AmeliorationsSupplementaires(Bitmap captcha)
        {
            //Augmentation de la taille de l'image sur un fond blanc
            Bitmap fond = DrawFilledRectangle(80 * 3, 40 * 3);
            using (Graphics grd = Graphics.FromImage(fond))
            {
                grd.DrawImage(captcha, new PointF((80 * 3 - captcha.Width) / 2, (40 * 3 - captcha.Height) / 2));
            }

            captcha = fond;

            Segmenter segmenter = new Segmenter();
            segmenter.Image = captcha;
            //segmenter.Resize(90 * 2, 40 * 3);
            //segmenter.ColorFillBlobs(30, Color.White, 58);
            segmenter.BlackAndWhite();
            segmenter.RemoveSmallBlobs(5, 5, 5, Color.White, 5);
            segmenter.MeanShiftFilter(1,1,1);
            return segmenter.Image;
        }

        private static  Bitmap DrawFilledRectangle(int x, int y)
        {
            Bitmap bmp = new Bitmap(x, y);
            using (Graphics graph = Graphics.FromImage(bmp))
            {
                Rectangle ImageSize = new Rectangle(0, 0, x, y);
                graph.FillRectangle(Brushes.White, ImageSize);
            }
            return bmp;
        }

        public static void CopyRegionIntoImage(Bitmap srcBitmap, Rectangle srcRegion, ref Bitmap destBitmap, Rectangle destRegion)
        {
            using (Graphics grD = Graphics.FromImage(destBitmap))
            {
                grD.DrawImage(srcBitmap, destRegion, srcRegion, GraphicsUnit.Pixel);
            }
        }

        private static string OCR(Bitmap captcha)
        {
            string text;
            using (var engine = new TesseractEngine(null, "eng", EngineMode.Default))
            {
                engine.SetVariable("tessedit_char_whitelist", "123456789+-");
                using (var page = engine.Process(captcha))
                {
                    text = page.GetText();
                    Console.WriteLine("Mean confidence: {0}", page.GetMeanConfidence());
                    text.Replace(System.Environment.NewLine, " ");
                    Console.WriteLine("Captcha : {0}", text);
                }
            }
            return text;
        }

        private static string InterpreterResultat(string text)
        {
            string solution = "19";
            // On fait le calcul :
            if (text.Contains("+"))
            {
                int a = 0;
                int b = 0;
                Int32.TryParse(text.Split('+')[0], out a);
                Int32.TryParse(text.Split('+')[1], out b);
                solution = (a + b).ToString();
            }
            else if (text.Contains("-"))
            {
                int a = 0;
                int b = 0;
                Int32.TryParse(text.Split('-')[0], out a);
                Int32.TryParse(text.Split('-')[1], out b);
                solution = (a - b).ToString();
            }
            else if (text.Contains("—"))
            {
                int a = 0;
                int b = 0;
                Int32.TryParse(text.Split('—')[0], out a);
                Int32.TryParse(text.Split('—')[1], out b);
                solution = (a - b).ToString();
            }
            else
            {
                Console.WriteLine("Error : Can't find + or - . Captcha resolution is incorrect");
            }

            Console.WriteLine("Answer : " + solution);
            return solution;
        }

        private static Bitmap SupprimerTraits(Bitmap captcha)
        {
            //On convertir l'image en un tableau
            Rectangle rect = new Rectangle(0, 0, captcha.Width, captcha.Height);
            BitmapData bmpData = captcha.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            int numBytes = bmpData.Stride * captcha.Height;
            byte[] rgbValues = new byte[numBytes];

            // Copy the RGB values into the array.
            Marshal.Copy(ptr, rgbValues, 0, numBytes);

            // Manipulate the bitmap, such as changing the
            // blue value for every other pixel in the the bitmap.
           
            for (int j = 0; j < captcha.Height; j += 1)
            {
                for (int i = 0; i < captcha.Width; i+=1)
                {
                    //Si le pixel est gris
                    if (rgbValues[4*(captcha.Width*j +i) ] == 64  && rgbValues[4*(captcha.Width * j +i) + 1] == 64  && rgbValues[4*(captcha.Width * j +i) + 2] == 64)
                    {
                        //Si on est a la premiere ligne
                        if (j == 0)
                        {
                            rgbValues[4*(captcha.Width*j +i)] = 255;
                            rgbValues[4*(captcha.Width*j +i) + 1] = 255;
                            rgbValues[4*(captcha.Width*j +i) + 2] = 255;
                        }
                        else
                        {
                            //On lui donne la couleur du pixel d'au dessus
                            rgbValues[4*(captcha.Width*j +i)] = rgbValues[4*(captcha.Width*(j-1) +i)];
                            rgbValues[4*(captcha.Width*j +i) + 1] = rgbValues[4*(captcha.Width*(j-1) +i) + 1];
                            rgbValues[4*(captcha.Width*j +i) + 2] = rgbValues[4*(captcha.Width*(j-1) +i) + 2];
                        }
                    }

                }
            }
            // Copy the RGB values back to the bitmap
            Marshal.Copy(rgbValues, 0, ptr, numBytes);

            //throw new NotImplementedException();

            // Unlock the bits.
            captcha.UnlockBits(bmpData);

            return captcha;
        }

        private static Bitmap SupprimerFond(Bitmap captcha, byte[] PixelRef, int N)
        {
            //On convertir l'image en un tableau
            Rectangle rect = new Rectangle(0, 0, captcha.Width, captcha.Height);
            BitmapData bmpData = captcha.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            int numBytes = bmpData.Stride * captcha.Height;
            byte[] rgbValues = new byte[numBytes];

            // Copy the RGB values into the array.
            Marshal.Copy(ptr, rgbValues, 0, numBytes);

            // Manipulate the bitmap, such as changing the
            // blue value for every other pixel in the the bitmap.

            //On creer un tableau du meme genre
            byte[] tableau = new byte[captcha.Width * captcha.Height];
            for (int k = 0; k < tableau.Count(); k++)
                tableau[k] = 0;
            tableau[(int)(captcha.Width / 2)] = 1;

            int n = N;
            byte[] pixelRef = PixelRef;
            int[] alpha = new int[3] { n, n, n };
            int[] beta = new int[3] { n, n, n };

            for (int j = 0; j < captcha.Height; j++)
            {
                for (int i = (int)(captcha.Width / 2); i < captcha.Width; i++)
                {
                    if (Compare(pixelRef, GetPixel(rgbValues, captcha.Width, i, j), alpha, beta))
                    {
                        tableau[j * captcha.Width + i] = 1;
                        //On adapte alpha et beta
                        for (int k = 0; k < 3; k++)
                        {
                            int diff = pixelRef[k] - GetPixel(rgbValues, captcha.Width, i, j)[k];

                            if (diff > 0)
                            {
                                if (diff + n > alpha[k])
                                    alpha[k] = diff + n;
                            }
                            else
                            {
                                if (n - diff > beta[k])
                                    beta[k] = n - diff;
                            }
                        }

                    }
                }
            }


            for (int j = 0; j < captcha.Height; j++)
            {
                for (int i = (int)(captcha.Width / 2) - 1; i >= 0; i--)
                {
                    if (Compare(pixelRef, GetPixel(rgbValues, captcha.Width, i, j), alpha, beta))
                    {
                        tableau[j * captcha.Width + i] = 1;
                        //On adapte alpha et beta
                        for (int k = 0; k < 3; k++)
                        {
                            int diff = pixelRef[k] - GetPixel(rgbValues, captcha.Width, i, j)[k];

                            if (diff > 0)
                            {
                                if (diff + n > alpha[k])
                                    alpha[k] = diff + n;
                            }
                            else
                            {
                                if (n - diff > beta[k])
                                    beta[k] = n - diff;
                            }
                        }
                    }
                }
            }

            for (int j = 0; j < captcha.Height; j++)
            {
                for (int i = (int)(captcha.Width / 2); i < captcha.Width; i++)
                {
                    if (Compare(pixelRef, GetPixel(rgbValues, captcha.Width, i, j), alpha, beta))
                    {
                        tableau[j * captcha.Width + i] = 1;
                        //On adapte alpha et beta
                        for (int k = 0; k < 3; k++)
                        {
                            int diff = pixelRef[k] - GetPixel(rgbValues, captcha.Width, i, j)[k];

                            if (diff > 0)
                            {
                                if (diff + n > alpha[k])
                                    alpha[k] = diff + n;
                            }
                            else
                            {
                                if (n - diff > beta[k])
                                    beta[k] = n - diff;
                            }
                        }

                    }
                }
            }


            //On peut alors remplir l'image de blanc la ou necessaire
            for (int j = 0; j < captcha.Height; j += 1)
            {
                for (int i = 0; i < captcha.Width; i += 1)
                {
                    if (tableau[j * captcha.Width + i] == 1)
                    {
                        rgbValues[4 * (captcha.Width * j + i)] = 255;
                        rgbValues[4 * (captcha.Width * j + i) + 1] = 255;
                        rgbValues[4 * (captcha.Width * j + i) + 2] = 255;
                    }
                    else if (rgbValues[4 * (captcha.Width * j + i)] > 180 && rgbValues[4 * (captcha.Width * j + i) + 1] > 180 && rgbValues[4 * (captcha.Width * j + i) + 2] > 180)
                    {
                        rgbValues[4 * (captcha.Width * j + i)] = 255;
                        rgbValues[4 * (captcha.Width * j + i) + 1] = 255;
                        rgbValues[4 * (captcha.Width * j + i) + 2] = 255;
                    }
                }
            }

            // Copy the RGB values back to the bitmap
            Marshal.Copy(rgbValues, 0, ptr, numBytes);

            //throw new NotImplementedException();

            // Unlock the bits.
            captcha.UnlockBits(bmpData);

            return captcha;
        }


        private static Bitmap SupprimerFond(Bitmap captcha, int N)
        {
            //On convertir l'image en un tableau
            Rectangle rect = new Rectangle(0, 0, captcha.Width, captcha.Height);
            BitmapData bmpData = captcha.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            int numBytes = bmpData.Stride * captcha.Height;
            byte[] rgbValues = new byte[numBytes];

            // Copy the RGB values into the array.
            Marshal.Copy(ptr, rgbValues, 0, numBytes);

            // Manipulate the bitmap, such as changing the
            // blue value for every other pixel in the the bitmap.

            //On creer un tableau du meme genre
            byte[] tableau = new byte[captcha.Width*captcha.Height];
            for (int k = 0; k < tableau.Count(); k++)
                tableau[k] = 0;
            tableau[(int)(captcha.Width/ 2)] = 1;

            int n = N;
            byte[] pixelRef = GetPixel(rgbValues, captcha.Width, 40, 1);
            int[] alpha = new int[3] { n, n, n };
            int[] beta = new int[3] { n, n, n };

            for (int j = 0; j < captcha.Height; j++)
            {
                for (int i = (int)(captcha.Width / 2); i < captcha.Width; i++)
                {
                    if (Compare(pixelRef, GetPixel(rgbValues, captcha.Width, i, j), alpha, beta))
                    {
                        tableau[j * captcha.Width + i] = 1;
                        //On adapte alpha et beta
                        for (int k = 0; k < 3; k++)
                        {
                            int diff = pixelRef[k] - GetPixel(rgbValues, captcha.Width, i, j)[k];

                            if (diff > 0)
                            {
                                if (diff + n > alpha[k])
                                    alpha[k] = diff + n;
                            }
                            else
                            {
                                if (n - diff > beta[k])
                                    beta[k] = n - diff;
                            }
                        }

                    }
                }
            }


            for (int j = 0; j < captcha.Height; j++)
            {
                for (int i = (int)(captcha.Width / 2) - 1; i >= 0; i--)
                {
                    if (Compare(pixelRef, GetPixel(rgbValues, captcha.Width, i, j), alpha, beta))
                    {
                        tableau[j * captcha.Width + i] = 1;
                        //On adapte alpha et beta
                        for (int k = 0; k < 3; k++)
                        {
                            int diff = pixelRef[k] - GetPixel(rgbValues, captcha.Width, i, j)[k];

                            if (diff > 0)
                            {
                                if (diff + n > alpha[k])
                                    alpha[k] = diff + n;
                            }
                            else
                            {
                                if (n - diff > beta[k])
                                    beta[k] = n - diff;
                            }
                        }
                    }
                }
            }

            for (int j = 0; j < captcha.Height; j++)
            {
                for (int i = (int)(captcha.Width / 2); i < captcha.Width; i++)
                {
                    if (Compare(pixelRef, GetPixel(rgbValues, captcha.Width, i, j), alpha, beta))
                    {
                        tableau[j * captcha.Width + i] = 1;
                        //On adapte alpha et beta
                        for (int k = 0; k < 3; k++)
                        {
                            int diff = pixelRef[k] - GetPixel(rgbValues, captcha.Width, i, j)[k];

                            if (diff > 0)
                            {
                                if (diff + n > alpha[k])
                                    alpha[k] = diff + n;
                            }
                            else
                            {
                                if (n - diff > beta[k])
                                    beta[k] = n - diff;
                            }
                        }

                    }
                }
            }
            

            //On peut alors remplir l'image de blanc la ou necessaire
            for (int j = 0; j < captcha.Height; j += 1)
            {
                for (int i = 0; i < captcha.Width; i += 1)
                {
                    if (tableau[j * captcha.Width + i] == 1)
                    {
                        rgbValues[4 * (captcha.Width * j + i)] = 255;
                        rgbValues[4 * (captcha.Width * j + i) + 1] = 255;
                        rgbValues[4 * (captcha.Width * j + i) + 2] = 255;
                    }
                    else if (rgbValues[4 * (captcha.Width * j + i)] > 180 && rgbValues[4 * (captcha.Width * j + i)+1] > 180 && rgbValues[4 * (captcha.Width * j + i)+2] > 180)
                    {
                        rgbValues[4 * (captcha.Width * j + i)] = 255;
                        rgbValues[4 * (captcha.Width * j + i) + 1] = 255;
                        rgbValues[4 * (captcha.Width * j + i) + 2] = 255;
                    }
                }
            }

            // Copy the RGB values back to the bitmap
            Marshal.Copy(rgbValues, 0, ptr, numBytes);

            //throw new NotImplementedException();

            // Unlock the bits.
            captcha.UnlockBits(bmpData);

            return captcha;
        }

        private static bool Compare(byte[] compareur, byte[] comparee, int[] alpha, int[] beta)
        {
            bool result = true;
            for (int k = 0; k < 3; k++)
                if (comparee[k] < (compareur[k] - alpha[k]) || comparee[k] > (compareur[k] + beta[k]))
                    result = false;
            return result;
        }

        private static byte[] GetPixel(byte[] rgbValues, int width,  int i, int j)
        {
            byte[] result = new byte[3];
            result[0] = rgbValues[4 * (width * j + i)];
            result[1] = rgbValues[4 * (width * j + i) + 1];
            result[2] = rgbValues[4 * (width * j + i) + 2];
            return result;
        }

        private static bool Compare(int compareur1, int compareur2, int compareur3, int comparee1, int comparee2, int comparee3, int n)
        {
            bool result = true;
            if (comparee1 < compareur1 - n)
                result = false;
            else if (comparee1 > compareur1 + n)
                result = false;
            else if (comparee2 < compareur2 - n)
                result = false;
            else if (comparee2 > compareur2 + n)
                result = false;
            else if (comparee3 < compareur3 - n)
                result = false;
            else if (comparee3 > compareur3 + n)
                result = false;
            return result;
        }
    }
}

