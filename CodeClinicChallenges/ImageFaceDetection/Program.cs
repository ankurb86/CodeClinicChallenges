using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections.Generic;
using System.Collections;
using SixLabors.ImageSharp.Drawing.Processing;
using System.Diagnostics;

namespace ImageFaceDetection
{
    class Program
    {
        private static string messageForAPIFailure = "Please provide a valid API Key as the first argument to the program";
        private static string messageForImageLoadFailure = "Image not found. Please provide a valid image file with path as the second argument to the program";
        static void Main(string[] args)
        {
            // Remeber to pass the apiKey and Image location separately as arguments to the application
            var apiKey = !string.IsNullOrWhiteSpace(args[0]) ? args[0] : throw new ArgumentException(messageForAPIFailure, args[0]);
            var imageFile = File.Exists(args[1]) ? args[1] : throw new FileNotFoundException(messageForImageLoadFailure, args[1]);

            // API Documentation: https://westcentralus.dev.cognitive.microsoft.com/docs/services/563879b61984550e40cbbe8d/operations/563879b61984550f3039523a/console
            // My API Endpoint : https://westcentralus.api.cognitive.microsoft.com/face

            // POST https://westcentralus.api.cognitive.microsoft.com/face/v1.0/verify HTTP/1.1
            // Host: westcentralus.api.cognitive.microsoft.com
            // Content - Type: application / json
            // Ocp - Apim - Subscription - Key: ••••••••••••••••••••••••••••••••

            var location = "westcentralus";
            var faceURL = new Uri($"https://{location}.api.cognitive.microsoft.com/face/v1.0/detect/?subscription-key={apiKey}");
            var httpPost = CreateHttpRequest(faceURL, "POST", "application/octet-stream");


            using (var filestream = File.OpenRead(imageFile))
            {
                filestream.CopyTo(httpPost.GetRequestStream());
            }

            //Send the Image File to the HTTP Endpoint

            string data = GetResponse(httpPost);

            //Getting the JSON Data

            var rectangles = GetRectangles(data);

            var faceImage = Image.Load(imageFile);
            var colorCode = new Rgba32(30,30,255);
            var imageCopyName = "";
            var faceCount = 0;

            foreach (var rectangle in rectangles)
            {
                faceImage.Mutate(x => x.DrawPolygon(colorCode, 30, rectangle));
                faceCount++;
            }

            if(faceCount == 1)
            {
                Console.WriteLine("We found 1 face in the image");
            }
            else
            {
                Console.WriteLine($"We found {faceCount} faces in the image");
            }

            if (!Directory.Exists($"{Environment.CurrentDirectory}\\images"))
                Directory.CreateDirectory($"{Environment.CurrentDirectory}\\images");
            imageCopyName = $"{Environment.CurrentDirectory}\\images\\FaceDetectedImage.jpeg";

            SaveImage(faceImage, imageCopyName);
            OpenWithDefaultApp(imageCopyName);
        }

        private static void OpenWithDefaultApp(string imageCopyName)
        {
            var imageAppToOpen = new ProcessStartInfo()
            {
                FileName = "explorer.exe",
                Arguments = imageCopyName,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            Process.Start(imageAppToOpen);
        }

        private static void SaveImage(Image faceImage, string imageCopyName)
        {
            using (var fileStream = File.Create(imageCopyName))
            {
                faceImage.SaveAsJpeg(fileStream);
            }
        }

        private static IEnumerable<PointF[]> GetRectangles(string data)
        {
            var allFaces = JArray.Parse(data);

            foreach (var face in allFaces)
            {
                var faceId = face["faceId"].ToString();
                var top = (int)face["faceRectangle"]["top"];
                var left = (int)face["faceRectangle"]["left"];
                var width = (int)face["faceRectangle"]["width"];
                var height = (int)face["faceRectangle"]["height"];

                var rectangle = new PointF[]
                {
                    new PointF(left, top),
                    new PointF(left + width, top),
                    new PointF(left + width, top + height),
                    new PointF(left, top + height)
                };

                yield return rectangle;
            }
        }

        private static string GetResponse(HttpWebRequest httpPost)
        {
            try
            {
                using (var response = httpPost.GetResponse())
                using (var streamReader = new StreamReader(response.GetResponseStream()))
                {
                    return streamReader.ReadToEnd();
                }
            }
            catch (WebException e)
            {
                using (WebResponse response = e.Response)
                {
                    HttpWebResponse httpResponse = (HttpWebResponse)response;
                    Console.WriteLine("Error code: {0}", httpResponse.StatusCode);
                    using (var streamReader = new StreamReader(response.GetResponseStream()))
                        return streamReader.ReadToEnd();
                }
            }
            
            
        }

        private static HttpWebRequest CreateHttpRequest(Uri faceURL, string httpMethod, string contentType)
        {
            var request = WebRequest.CreateHttp(faceURL);
            request.Method = httpMethod;
            request.ContentType = contentType;

            return request;
        }
    }
}
