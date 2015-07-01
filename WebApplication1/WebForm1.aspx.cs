using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Windows.Forms;
using System.Web.UI;
using System.IO;
using System.Web.UI.WebControls;
using OpenSURFcs;
using System.Drawing;
using MMS.LogoMatching;

namespace WebApplication1
{
    public partial class WebForm1 : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Image1.ImageUrl = "no_image_available.jpeg";
            Image2.ImageUrl = "no_image_available.jpeg";
        }

        //protected void Button1_Click(object sender, EventArgs e)
        //{
        //    // open file dialog 
        //    OpenFileDialog open = new OpenFileDialog();
        //    // image filters
        //    open.Filter = "Image Files(*.jpg; *.jpeg; *.gif; *.bmp)|*.jpg; *.jpeg; *.gif; *.bmp";
        //    open.ShowDialog();
        //}

        protected void Button1_Click(object sender, EventArgs e) {
               StartUpLoad();
          }

         private void StartUpLoad() {
             //get the file name of the posted image
             if (FileUpload1.PostedFiles.Count == 2)
             {
                 var file = FileUpload1.PostedFiles[0];
                 var file2 = FileUpload1.PostedFiles[1];
                 //validates the posted file before saving
                 if (file != null && file2 != null && file.FileName != "" && file2.FileName != "")
                 {
                     // 10240 KB means 10MB, You can change the value based on your requirement
                     if (file.ContentLength > 502400)
                     {
                         Page.ClientScript.RegisterClientScriptBlock(typeof(Page), "Alert", "alert('File is too big for this demo.')", true);
                     }
                     else
                     {
                         var num = Directory.GetFiles(Server.MapPath("/ImageStorage/"));
                         int filname = num.Length + 1;
                         if (num.Length > 100)
                         {
                             foreach (var f in num)
                             {
                                 File.Delete(f);
                             }
                             filname = 1;
                         }
                       
                         string imgPath = "ImageStorage/" + filname + Path.GetExtension(file.FileName);
                         file.SaveAs(Server.MapPath(imgPath));
                         Image1.ImageUrl = "~/" + imgPath;
                         
                         filname += 1;

                         imgPath = "ImageStorage/" + filname + Path.GetExtension(file2.FileName);
                         file2.SaveAs(Server.MapPath(imgPath));
                         Image2.ImageUrl = "~/" + imgPath;

                         //MatchImages(Image1.ImageUrl, Image2.ImageUrl);
                         var ans = MatchImages(file, file2);
                         Label1.Text = ans.ToString() + "%";
                         //Page.ClientScript.RegisterClientScriptBlock(typeof(Page), "Alert", "alert('Matching Ratio :')", true);
                     }
                 }
             }
            
          }

         private float MatchImages(HttpPostedFile file, HttpPostedFile file2)
         {
             System.Drawing.Image image = System.Drawing.Image.FromStream(file.InputStream);
             Bitmap bmp = new Bitmap(image);

             System.Drawing.Image image2 = System.Drawing.Image.FromStream(file2.InputStream);
             Bitmap bmp2 = new Bitmap(image2);

             LogoMatching lg = new LogoMatching();
             List<IPoint> iPoints1 = lg.GetImagePoints(bmp);
             List<IPoint> iPoints2 = lg.GetImagePoints(bmp2);

             int min = iPoints1.Count > iPoints2.Count ? iPoints2.Count : iPoints1.Count;

             var matches = lg.GetMatchesOptimized(iPoints1, iPoints2, 20);
             int i1 = matches[0].Count;
             int i2 = matches[1].Count;
             if (i1 > i2)
                 return ((float)i1 / (float)min) * 100;
             else
                 return ((float)i2 / (float)min) * 100;
         }

         private void MatchImages(string p1, string p2)
         {
            // Bitmap newBitmap = new Bitmap()
         }

    }
}