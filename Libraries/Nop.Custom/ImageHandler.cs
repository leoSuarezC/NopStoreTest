using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Custom
{
    public static class CustomMiddlewareExtensions
    {
        public static IApplicationBuilder UseCustomHanlderMiddleware
                                      (this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ImageHandler>();
        }
    }

    public class ImageHandler
    {

        private RequestDelegate _next;

        public ImageHandler(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                string imagePath = context.Request.Path;
                string fullPath = @"C:\Anil\Chrris\NopCommerce4.0\Presentation\Nop.Web\wwwroot" + imagePath.Replace("/", "\\");
                string croppedPath = @"C:\Anil\Chrris\NopCommerce4.0\Presentation\Nop.Web\wwwroot" + imagePath.Replace("/", "\\").Replace("logo", "logo3");
                ImageMagick.MagickImage imgLarge = new ImageMagick.MagickImage(fullPath);
                imgLarge.Crop(100, 100);
                imgLarge.ToBitmap(System.Drawing.Imaging.ImageFormat.Png).Save(croppedPath);
                MemoryStream ms = new MemoryStream();
                using (FileStream file = new FileStream(croppedPath, FileMode.Open, FileAccess.Read))
                {
                    byte[] bytes = new byte[file.Length];
                    file.Read(bytes, 0, (int)file.Length);
                    ms.Write(bytes, 0, (int)file.Length);
                    ms.Position = 0;
                    context.Response.ContentType = "image/png";
                    context.Response.ContentLength = bytes.Length;                    
                    await ms.CopyToAsync(context.Response.Body);                    
                }
            }
            catch (Exception e) {
                //return Task.FromResult(0);
            }
            //await context.Response.WriteAsync
            //("<h2>This handler is written for processing files with .report extension<h2>");

            //context.Request.QueryString

            //more logic follows...
        }
    }
}
