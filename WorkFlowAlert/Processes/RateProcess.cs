// using Newtonsoft.Json;
// using System.Text;
//
// namespace WorkFlowAlert.Processes;
//
// public class RateProcess
// {
//     public async Task<List<ApiResponseModel>> GetApiResponseList()
//     {
//             
//         List<ApiResponseModel> responseList = new List<ApiResponseModel>();
//
//         using (HttpClient _httpClient = new HttpClient())
//         {
//             string apiUrl = "https://api.nla.com.gh/FastCredit/api/LoadCredit";
//
//             // Create a custom HttpRequestMessage
//             HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
//
//             // Add parameters in the headers
//             request.Headers.Add("SP", "SIMNET");
//
//
//             // Add Basic Authorization header
//             string username = "SIMNET";
//             string password = "ec57163b-6344-43a2-9acc-eafd759e929c";
//             string base64Auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
//             request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64Auth);
//
//
//             string jsonPayload = "{\"SP\": \"SIMNET\"}"; // I included a body to the request
//             request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
//
//             // Send the request
//             HttpResponseMessage response = _httpClient.Send(request);
//
//             try
//             {
//                 if (response.IsSuccessStatusCode)
//                 {
//                     // Read the response content
//                     string responseBody = await response.Content.ReadAsStringAsync();
//
//                     // Deserialize the JSON response into a list of objects
//                     responseList = JsonConvert.DeserializeObject<List<ApiResponseModel>>(responseBody);
//                 }
//
//             }
//             catch (Exception ex)
//             {
//                 Console.WriteLine($"Exception: {ex.Message}");
//             }
//
//         }
//
//         return responseList;
//
//     }
// }