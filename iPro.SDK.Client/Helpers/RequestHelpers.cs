using iPro.SDK.Client.Models;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace iPro.SDK.Client.Helpers
{
    public class RequestHelpers
    {
        public static string HandleWebException(WebException ex)
        {
            var sb = new StringBuilder();

            using (var response = ex.Response)
            {
                try
                {
                    var httpResponse = (HttpWebResponse)response;

                    sb.AppendLine("-------------Message-------------");
                    sb.AppendLine($"StatusCode: {httpResponse.StatusCode}");
                    sb.AppendLine($"Status: {httpResponse.StatusDescription}");

                    using (var data = response.GetResponseStream())
                    {
                        var text = new StreamReader(data).ReadToEnd();
                        sb.AppendLine(text);
                    }
                }
                catch
                {
                }
            }

            sb.AppendLine(string.Empty);
            sb.AppendLine("-------------Error-------------");
            sb.AppendLine(ex.ToString());
            sb.AppendLine(string.Empty);

            return sb.ToString();
        }

        public static async Task<ParsedResult> ParseResponse(HttpWebRequest httpRequest)
        {
            var response = await httpRequest.GetResponseAsync();
            var message = string.Format("Status Code: {0}" + Environment.NewLine, (int)((HttpWebResponse)response).StatusCode);
            var success = false;

            var contentDispositionHeader = response.Headers["Content-Disposition"];
            if (!string.IsNullOrWhiteSpace(contentDispositionHeader))
            {
                success = true;
                message += contentDispositionHeader;

                var contentDisposition = new ContentDisposition(contentDispositionHeader);
                var extName = response.ContentType == MediaTypeNames.Application.Pdf ? ".pdf" : Path.GetExtension(contentDisposition.FileName);

                using (var dialog = new SaveFileDialog())
                {
                    dialog.Filter = "All files (*.*)|*.*";
                    dialog.FilterIndex = 1;
                    dialog.RestoreDirectory = true;
                    dialog.DefaultExt = extName;
                    dialog.FileName = contentDisposition.FileName;

                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        using (var stream = dialog.OpenFile())
                        {
                            var res = response.GetResponseStream();
                            res.CopyTo(stream);
                        }
                    }
                }
            }
            else
            {
                var reader = new StreamReader(response.GetResponseStream());
                var responseText = reader.ReadToEnd();

                if (response.ContentType == "application/json")
                {
                    var responseObj = JsonConvert.DeserializeObject<JsonResponse>(responseText);
                    success = responseObj.Success;

                    var parsedObj = JsonConvert.DeserializeObject<dynamic>(responseText);
                    var formattedJson = JsonConvert.SerializeObject(parsedObj, Formatting.Indented);
                    message += formattedJson;
                }
                else
                {
                    success = true;
                    message += responseText;
                }
            }

            return new ParsedResult
            {
                Success = success,
                Message = message,
            };
        }

        public static async Task<ParsedResult> HandleRequestState(Func<Task<ParsedResult>> func)
        {
            ParsedResult result = null;

            try
            {
                result = await func();
            }
            catch (WebException ex)
            {
                result = new ParsedResult
                {
                    Success = false,
                    Message = HandleWebException(ex),
                };
            }
            catch (Exception ex)
            {
                if (ex.InnerException is WebException)
                {
                    var webException = (WebException)ex.InnerException;
                    result = new ParsedResult
                    {
                        Success = false,
                        Message = HandleWebException(webException),
                    };
                }
                else
                {
                    result = new ParsedResult
                    {
                        Success = false,
                        Message = (ex.InnerException ?? ex).ToString(),
                    };
                }
            }

            return result;
        }
    }
}
