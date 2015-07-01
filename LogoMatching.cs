using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MMS.LogoMatching.Core.Entities;
using OpenSURFcs;

namespace MMS.LogoMatching
{
    public class LogoMatch
    {
        public LogoMatch()
        {
            this.IsMatch = false;
            Channels = new List<Channel>();
        }
        public bool IsMatch { get; set; }
        public double Ratio { get; set; }
        public int? ChannelId { get; set; }
        public int ChannelPointsCount { get; set; }

        public float MatchX1 { get; set; }
        public float MatchY1 { get; set; }
        public float MatchX2 { get; set; }
        public float MatchY2 { get; set; }

        public List<Channel> Channels { get; set; }
    }

    public class LogoMatching
    {
        private const float FLT_MAX = 3.402823466e+38F;

        public double Match(Bitmap bmp1, Bitmap bmp2)
        {
            List<IPoint> iPoints1 = GetImagePoints(bmp1);
            return Match(iPoints1, bmp2);
        }

        public double Match(List<IPoint> iPoints1, Bitmap bmp2)
        {
            List<IPoint> iPoints2 = GetImagePoints(bmp2);
            return Match(iPoints1, iPoints2);
       
        }

        public List<IPoint>[] GetDistinctMatches(List<IPoint>[] matches)
        {
            for (var i = 0; i < matches.Length; i++)
            {
                matches[i] = GetDistinctMatch(matches[i]);
            }
            return matches;
        }

          public List<IPoint> GetDistinctMatch(List<IPoint> points)
          {
              List<IPoint> distinctMatches = new List<IPoint>();
              foreach (var point in points)
              {
                  if (!distinctMatches.Contains(point))
                      distinctMatches.Add(point);
              }
              return distinctMatches;

          }
        public double Match(List<IPoint> iPoints1, List<IPoint> iPoints2)
        {
            double minLength = (double)Math.Min(iPoints1.Count, iPoints2.Count);
            double maxLength = (double)Math.Max(iPoints1.Count, iPoints2.Count);
            if ((minLength / maxLength) > 0.6)
            {
                var matches = GetMatches(iPoints1, iPoints2);
                double maxMatchLength = matches[0].Count > matches[1].Count ? matches[0].Count : matches[1].Count;
                return maxMatchLength / minLength;
            }
            else
            {
                return 0;
            }

        }

        public double MatchLogo(List<IPoint> logoPoints, List<IPoint> iPoints2)
        {
            double minLength = (double)Math.Min(logoPoints.Count, iPoints2.Count);
            double maxLength = (double)Math.Max(logoPoints.Count, iPoints2.Count);
            if ((logoPoints.Count > iPoints2.Count && (minLength / maxLength) > 0.6) || logoPoints.Count >= 8)
            {
                var matches = GetMatches(logoPoints, iPoints2);
                double maxMatchLength = matches[0].Count > matches[1].Count ? matches[0].Count : matches[1].Count;
                return maxMatchLength / minLength;
            }
            else
            {
                return 0;
            }

        }

        public bool Match(Bitmap bmp1, Bitmap bmp2, double threshold)
        {
            double ratio = Match(bmp1, bmp2);
            if (ratio >= threshold)
                return true;
            return false;
      
        }

        public List<IPoint> GetImagePoints(Bitmap bmp, float thresh = 0.0002f)
        {
            IntegralImage iimg = IntegralImage.FromImage(bmp);

            // Extract the interest points
            List<IPoint> ipts = FastHessian.getIpoints(thresh, 5, 2, iimg);
            // Describe the interest points
            SurfDescriptor.DecribeInterestPoints(ipts, false, false, iimg);
            return ipts;
        }

        public List<VideoPoint> GetVideoImagePoints(Bitmap bmp, float thresh = 0.0002f)
        {
            var points = GetImagePoints(bmp, thresh);
            List<VideoPoint> videoPoints = new List<VideoPoint>();
            foreach (var point in points)
            {
                VideoPoint videoPoint = new VideoPoint();
                videoPoint.CopyFrom(point);
                videoPoints.Add(videoPoint);
            
            }
            return videoPoints;
        }

        public List<IPoint>[] GetMatchesOptimized2(List<IPoint> ipts1, List<IPoint> ipts2)
        {
            int arrayCount = 64;
            float sumX = 90;
            float sumY = 72;
            float padding = 20;
            List<IPoint>[] leftPoints = new List<IPoint>[arrayCount];
            List<IPoint>[] rightPoints = new List<IPoint>[arrayCount];
            float x = 0;
            float y = 0;
            for (var i = 0; i < arrayCount; i++)
            {
                if (x > 720)
                {
                    y += sumY;
                    x = 0;
                }
                float x2 = x + sumX;
                float y2 = y + sumY;
                leftPoints[i] = ipts1.Where(u => u.x >= (x - padding) && u.x <= (x2 + padding) && u.y >= (y - padding) && u.y <= (y2 + padding)).ToList();
                rightPoints[i] = ipts2.Where(u => u.x >= (x - padding) && u.x <= (x2 + padding) && u.y >= (y - padding) && u.y <= (y2+padding)).ToList();
                x += sumX;
            }
            int count1 = rightPoints.Sum(w => w.Count);
            int count2 = leftPoints.Sum(w => w.Count);
            List<IPoint>[] matches = new List<IPoint>[2];
            matches[0] = new List<IPoint>();
            matches[1] = new List<IPoint>();
            for (var i = 0; i < arrayCount; i++)
            {
                var mches = GetMatches(leftPoints[i], rightPoints[i]);
                matches[0].AddRange(mches[0]);
                matches[1].AddRange(mches[1]);
            }
            return matches;
        }

        public List<IPoint>[] GetMatchesOptimized(List<IPoint> ipts1, List<IPoint> ipts2,int padding=20)
        {
            bool different = false;
            if (ipts1.Count < ipts2.Count)
            {
                List<IPoint> temp = new List<IPoint>();
                temp.AddRange(ipts1);
                ipts1 = ipts2;
                ipts2 = temp;
                different = true;
            }
            double dist;
            double d1, d2;
            IPoint match = new IPoint();

            List<IPoint>[] matches = new List<IPoint>[2];
            matches[0] = new List<IPoint>();
            matches[1] = new List<IPoint>();
            for (int i = 0; i < ipts1.Count; i++)
            {
                d1 = d2 = FLT_MAX;
                bool flag = false;
                for (int j = 0; j < ipts2.Count; j++)
                {
                    if ((ipts1[i].x - padding) < ipts2[j].x && (ipts1[i].x + padding) > ipts2[j].x && (ipts1[i].y - padding) < ipts2[j].y && (ipts1[i].y + padding) > ipts2[j].y)
                    {
                        dist = GetDistance(ipts1[i], ipts2[j]);
                       // Console.WriteLine(ipts2[j].x);
                        if (dist < d1) // if this feature matches better than current best  
                        {
                            d2 = d1;
                            d1 = dist;
                            match = ipts2[j];
                            flag = true;
                        }
                        else if (dist < d2) // this feature matches better than second best  
                        {
                            d2 = dist;
                            flag = true;
                        }
                    }
                    //else d1 = d2 = FLT_MAX;
                }
                // If match has a d1:d2 ratio < 0.65 ipoints are a match  
                if (d1 / d2 < 0.77 && flag) //Match  
                {
                    matches[0].Add(ipts1[i]);
                    matches[1].Add(match);
                }
            }

            matches = GetDistinctMatches(matches);
            matches = GetMatches(matches[0], matches[1]);

            if (different)
            {
                List<IPoint> temp = new List<IPoint>();
                temp.AddRange(matches[0]);
                matches[0] = matches[1];
                matches[1] = temp;
            }

            return matches;
        }


        public List<IPoint>[] GetMatches(List<IPoint> ipts1, List<IPoint> ipts2)
        {
            double dist;
            double d1, d2;
            IPoint match = new IPoint();

            List<IPoint>[] matches = new List<IPoint>[2];
            matches[0] = new List<IPoint>();
            matches[1] = new List<IPoint>();
            for (int i = 0; i < ipts1.Count; i++)
            {
                d1 = d2 = FLT_MAX;

                for (int j = 0; j < ipts2.Count; j++)
                {
                    dist = GetDistance(ipts1[i], ipts2[j]);

                    if (dist < d1) // if this feature matches better than current best  
                    {
                        d2 = d1;
                        d1 = dist;
                        match = ipts2[j];
                    }
                    else if (dist < d2) // this feature matches better than second best  
                    {
                        d2 = dist;
                    }
                }
                // If match has a d1:d2 ratio < 0.65 ipoints are a match  
                if (d1 / d2 < 0.77) //Match  
                {
                    matches[0].Add(ipts1[i]);
                    matches[1].Add(match);
                }
            }
            return matches;
        }

        public List<IPoint>[] GetMatchesTaskParallal(List<IPoint> ipts1, List<IPoint> ipts2)
        {
            IPoint match = new IPoint();
            List<IPoint>[] matches = new List<IPoint>[2];
            matches[0] = new List<IPoint>();
            matches[1] = new List<IPoint>();
            int pageSize = (ipts1.Count * ipts2.Count) / 60000;
            if (pageSize == 0) pageSize = 1;
            int pages = (int)Math.Ceiling((float)ipts1.Count / (float)pageSize);
            Parallel.For(0, pages, x =>
            {
                for (int i = x * pageSize; i < ((x * pageSize) + pageSize) && i < ipts1.Count; i++)
                {
                    double dist;
                    double d1, d2;
                    d1 = d2 = FLT_MAX;
                    for (int j = 0; j < ipts2.Count; j++)
                    {
                        dist = GetDistance(ipts1[i], ipts2[j]);
                        if (dist < 0.001)
                            d1 = d2 = FLT_MAX;
                        if (dist < d1) // if this feature matches better than current best  
                        {
                            d2 = d1;
                            d1 = dist;
                            match = ipts2[j];
                        }
                        else if (dist < d2) // this feature matches better than second best  
                        {
                            d2 = dist;
                        }
                    }
                    // If match has a d1:d2 ratio < 0.65 ipoints are a match  
                    if (d1 / d2 < 0.77) //Match  
                    {
                        lock (matches)
                        {
                            matches[0].Add(ipts1[i]);
                            matches[1].Add(match);
                        }
                    }
                }
            });
            return matches;
        }

        public List<VideoPoint>[] GetMatches(List<VideoPoint> ipts1, List<VideoPoint> ipts2,bool multiThread=false)
        {
            double dist;
            double d1, d2;
            VideoPoint match = new VideoPoint();

            List<VideoPoint>[] matches = new List<VideoPoint>[2];
            matches[0] = new List<VideoPoint>();
            matches[1] = new List<VideoPoint>();

            if (multiThread)
            {
                var output=Parallel.For(0,ipts1.Count,i=>{
                
                    d1 = d2 = FLT_MAX;

                    for (int j = 0; j < ipts2.Count; j++)
                    {
                        dist = GetDistance(ipts1[i], ipts2[j]);

                        if (dist < d1) // if this feature matches better than current best  
                        {
                            d2 = d1;
                            d1 = dist;
                            match = ipts2[j];
                        }
                        else if (dist < d2) // this feature matches better than second best  
                        {
                            d2 = dist;
                        }
                    }
                    // If match has a d1:d2 ratio < 0.65 ipoints are a match  
                    if (d1 / d2 < 0.77) //Match  
                    {
                        matches[0].Add(ipts1[i]);
                        matches[1].Add(match);
                    }
                });

            }
            else
            {

                for (int i = 0; i < ipts1.Count; i++)
                {
                    d1 = d2 = FLT_MAX;

                    for (int j = 0; j < ipts2.Count; j++)
                    {
                        dist = GetDistance(ipts1[i], ipts2[j]);

                        if (dist < d1) // if this feature matches better than current best  
                        {
                            d2 = d1;
                            d1 = dist;
                            match = ipts2[j];
                        }
                        else if (dist < d2) // this feature matches better than second best  
                        {
                            d2 = dist;
                        }
                    }
                    // If match has a d1:d2 ratio < 0.65 ipoints are a match  
                    if (d1 / d2 < 0.77) //Match  
                    {
                        matches[0].Add(ipts1[i]);
                        matches[1].Add(match);
                    }
                }
            }
            return matches;
        }

        private static double GetDistance(IPoint ip1, IPoint ip2)
        {
            float sum = 0.0f;
            for (int i = 0; i < 64; ++i)
                sum += (ip1.descriptor[i] - ip2.descriptor[i]) * (ip1.descriptor[i] - ip2.descriptor[i]);
            return Math.Sqrt(sum);
        }


     
        //public LogoMatch MatchLogo(List<IPoint> interestPoints,double width,double height,List<int> channelIds=null)
        //{
        //    LogoMatch match = new LogoMatch();
        //    var sourceLogos = CacheManager.Logos.Where(x => x.SourceImageWidth == width && x.SourceImageHeight == height).OrderByDescending(x => x.MatchCount).ToList();
        //    if (sourceLogos != null && channelIds != null)
        //    {
        //        sourceLogos = sourceLogos.Where(x => channelIds.Contains(x.ChannelId)).ToList();
        //    }
        //    if (sourceLogos != null)
        //    {
        //        foreach (var logo in sourceLogos)
        //        {
        //            float padding = 2;
        //            var points = interestPoints.Where(x => x.x >= (logo.Minx - padding) && x.y >= (logo.Miny - padding) && x.x <= (logo.Maxx + padding) && x.y <= (logo.Maxy + padding)).ToList();
        //            //Bitmap croppedImage=Logo.CloneBitmap(bmp,logo.X1,logo.Y1,logo.X2,logo.Y2);
        //            //List<IPoint> points = GetImagePoints(croppedImage);
        //            if (points.Count > 20 && points.Count >= logo.InterestPointsList.Count)
        //            {
        //                double ratio = Match(logo.InterestPointsList, points);
        //                if (ratio > 0.7)
        //                {
        //                    match.Ratio = ratio;
        //                    match.Logo = logo;
        //                    match.IsMatch = true;
        //                    break;
        //                }
        //                else
        //                {
        //                    if (match.Ratio < ratio)
        //                    {
        //                        match.Logo = logo;
        //                        match.Ratio = ratio;
        //                    }
        //                }
        //            }

        //        }
        //        if (match.Ratio >= 0.20)
        //        {
        //            match.IsMatch = true;
        //        }
        //    }
        //    return match;
        
        //}





        public Bitmap ExtractLogo(Bitmap bmp,Logo logo)
        {
                Bitmap croppedImage = Logo.CloneBitmap(bmp, logo.X1, logo.Y1, logo.X2, logo.Y2);
                      return croppedImage;
        }


        public bool MatchFrames(List<IPoint> imagePoints1, List<IPoint> imagePoints2)
        {
            double ratio = Match(imagePoints1, imagePoints2);
            if (ratio < AppConstants.FrameMatchRatio)
                return false;
            else
                return true;
        }


        public Bitmap MakeGrayscale3(Bitmap original)
        {
            //create a blank bitmap the same size as original
            Bitmap newBitmap = new Bitmap(original.Width, original.Height);

            //get a graphics object from the new image
            Graphics g = Graphics.FromImage(newBitmap);

            //create the grayscale ColorMatrix
            ColorMatrix colorMatrix = new ColorMatrix(
               new float[][] 
      {
         new float[] {.3f, .3f, .3f, 0, 0},
         new float[] {.59f, .59f, .59f, 0, 0},
         new float[] {.11f, .11f, .11f, 0, 0},
         new float[] {0, 0, 0, 1, 0},
         new float[] {0, 0, 0, 0, 1}
      });

            //create some image attributes
            ImageAttributes attributes = new ImageAttributes();

            //set the color matrix attribute
            attributes.SetColorMatrix(colorMatrix);

            //draw the original image on the new image
            //using the grayscale color matrix
            g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
               0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);

            //dispose the Graphics object
            g.Dispose();
            return newBitmap;
        }


        public List<Bitmap> GetFaces(Bitmap bmp, List<IPoint> interestPoints=null)
        {
            return null;
        }

    }
}
