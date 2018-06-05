using System;
using System.IO;
using System.Linq;
using System.Management.Instrumentation;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;

namespace RatingsApi
{
    public static class CreateRating
    {
        [FunctionName("CreateRating")]
        public static HttpResponseMessage Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]HttpRequestMessage req,
            [DocumentDB(
                databaseName: "ratingsapi",
                collectionName: "ratings",
                ConnectionStringSetting = "CosmosDb")]out dynamic document,
            TraceWriter log)
        {
            //Validation
            HttpResponseMessage response = null;
            document = null;
            Rating temp = new Rating();
            //document = temp;
            try
            {
                temp = req.Content.ReadAsAsync<Rating>().Result;
                temp.id = Guid.NewGuid();
            }
            catch (Exception ex)
            {
                response = req.CreateResponse(HttpStatusCode.InternalServerError, "Invalid Request body");
            }

            try
            {
                GetObject("https://serverlessohlondonuser.azurewebsites.net/api/GetUser?userId=",temp.userId.ToString());

            }
            catch (Exception ex)
            {
                return req.CreateResponse(HttpStatusCode.NotFound, $"No user found with id: {temp.userId}");

            }
            try
            {
                GetObject("https://serverlessohlondonproduct.azurewebsites.net/api/GetProduct?productId=", temp.productId.ToString());
            }
            catch (Exception ex)
            {
                 return req.CreateResponse(HttpStatusCode.NotFound, $"No product found with id: {temp.productId}");
            }


            //Write to Cosmos
            document = temp;
            response = req.CreateResponse(HttpStatusCode.OK, temp);
            return response;
        }

        private static void GetObject(string url, string id)
        {
            url = url + id;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            try
            {
                WebResponse userResponse = request.GetResponse();
                using (Stream responseStream = userResponse.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
                    var result = JsonConvert.DeserializeObject(reader.ReadToEnd());
                    if (result == null)
                    {
                        throw new InstanceNotFoundException();
                    }
                }
            }
            catch (WebException ex)
            {
                WebResponse errorResponse = ex.Response;
                using (Stream responseStream = errorResponse.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8"));
                    String errorText = reader.ReadToEnd();
                }
                throw;
            }
        }
    }
}