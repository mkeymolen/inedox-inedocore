﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.ExecutionEngine;
#if Otter
using Inedo.Otter.Documentation;
using Inedo.Otter.Extensibility;
using Inedo.Otter.Extensibility.Operations;
using Inedo.Otter.Extensions;
#elif BuildMaster
using Inedo.BuildMaster.Web;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Operations;
#endif

namespace Inedo.Extensions.Operations.HTTP
{
    public abstract class HttpOperationBase : ExecuteOperation
    {
        protected HttpOperationBase()
        {
        }

        [Required]
        [ScriptAlias("Url")]
        [DisplayName("URL")]
        public string Url { get; set; }
        [Category("Options")]
        [ScriptAlias("LogResponseBody")]
        [DisplayName("Log response body")]
        public bool LogResponseBody { get; set; }
        [Category("Options")]
        [PlaceholderText("400:599")]
        [ScriptAlias("ErrorStatusCodes")]
        [DisplayName("Error status codes")]
        [Description("Comma-separated status codes (or ranges in the form of start:end) that should indicate this action has failed. "
                    + "For example, a value of \"401,500:599\" will fail on all server errors and also when \"HTTP Unauthorized\" is returned. "
                    + "The default is 400:599.")]
        public string ErrorStatusCodes { get; set; }
        [Category("Options")]
        [Output]
        [ScriptAlias("ResponseBody")]
        [DisplayName("Store response as")]
        [PlaceholderText("Do not store response body as variable")]
        public string ResponseBodyVariable { get; set; }
        [Category("Options")]
        [ScriptAlias("RequestHeaders")]
        [DisplayName("Request headers")]
        [FieldEditMode(FieldEditMode.Multiline)]
        public IDictionary<string, RuntimeValue> RequestHeaders { get; set; }
        [Category("Options")]
        [DisplayName("Max response length")]
        [DefaultValue(1000)]
        public int MaxResponseLength { get; set; } = 1000;

        protected HttpClient CreateClient()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Clear();
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(typeof(Operation).Assembly.GetCustomAttribute<AssemblyProductAttribute>().Product, typeof(Operation).Assembly.GetName().Version.ToString()));
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("InedoCore", typeof(HttpOperationBase).Assembly.GetName().Version.ToString()));

            if (this.RequestHeaders != null)
            {
                foreach (var header in this.RequestHeaders)
                {
                    this.LogDebug($"Adding request header {header.Key}={header.Value.AsString()}...");
                    client.DefaultRequestHeaders.Add(header.Key, header.Value.AsString() ?? string.Empty);
                }
            }

            return client;
        }
        protected async Task ProcessResponseAsync(HttpResponseMessage response)
        {
            var message = $"Server responded with status code {(int)response.StatusCode} - {response.ReasonPhrase}";
            if (!string.IsNullOrWhiteSpace(this.ErrorStatusCodes))
            {
                var errorCodeRanges = StatusCodeRangeList.Parse(this.ErrorStatusCodes);
                if (errorCodeRanges.IsInAnyRange((int)response.StatusCode))
                    this.LogError(message);
                else
                    this.LogInformation(message);
            }
            else
            {
                if (response.IsSuccessStatusCode)
                    this.LogInformation(message);
                else
                    this.LogError(message);
            }

            using (var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
            {
                var text = await this.GetTruncatedResponseAsync(responseStream).ConfigureAwait(false);
                if (string.IsNullOrEmpty(text))
                {
                    this.LogDebug("The Content Length of the response was 0.");
                }
                else
                {
                    this.ResponseBodyVariable = text;
                    if (this.LogResponseBody)
                    {
                        if (text.Length >= this.MaxResponseLength)
                            this.LogDebug($"The following response Content Body is truncated to {this.MaxResponseLength} characters.");

                        this.LogInformation("Response Content Body: " + text);
                    }
                }
            }
        }

        private async Task<string> GetTruncatedResponseAsync(Stream stream)
        {
            using (var reader = new StreamReader(stream, InedoLib.UTF8Encoding))
            {
                var text = new StringBuilder();
                var buffer = new char[1024];
                int remaining = this.MaxResponseLength;

                int read = await reader.ReadAsync(buffer, 0, Math.Min(remaining, 1024)).ConfigureAwait(false);
                while (read > 0 && remaining > 0)
                {
                    text.Append(buffer, 0, read);
                    remaining -= read;
                    if (remaining > 0)
                        read = await reader.ReadAsync(buffer, 0, Math.Min(remaining, 1024)).ConfigureAwait(false);
                }

                return text.ToString();
            }
        }
    }
}
