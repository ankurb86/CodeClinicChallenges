using System;
using System.IO;
using System.Net;

namespace ImageFaceDetection
{
    class Program
    {
        private static string messageForAPIFailure = "Please provide a valid API Key as the first argument to the program";
        private static string messageForImageLoadFailure = "Image not found. Please provide a valid image file with path as the second argument to the program";
        static void Main(string[] args)
        {
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
