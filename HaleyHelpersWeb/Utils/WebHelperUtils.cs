﻿using Azure;
using Azure.Core;
using Haley.Abstractions;
using Haley.Enums;
using Haley.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace Haley.Utils {
    public static class WebHelperUtils {
        public static async Task SetMessage(this HttpResponse target,HttpStatusCode status, string message = null) {
            if (target == null) return;
            await target.SetBytes(status, string.IsNullOrWhiteSpace(message) ? Array.Empty<byte>() : Encoding.UTF8.GetBytes(message));
        }

        public static async Task SetContent(this HttpResponse target, HttpStatusCode status, HttpContent content = null) {
            if (target == null) return;
            target.StatusCode = (int)status;
            if (content != null) {
                await content.CopyToAsync(target.Body);
            }
        }

        public static async Task SetBytes(this HttpResponse target, HttpStatusCode status, byte[] message) {
            if (target == null) return;
            target.StatusCode = (int)status;
            if (message != null && message.Length > 0) {
                await target.Body.WriteAsync(message);
            }
        }

        public static async Task SendProxyRequest(this HttpResponse target, HttpRequest source, HttpClient client, string relativeURI, HttpMethod method, Dictionary<string, string>? customHeaders = null,CancellationToken cancellation_token =default) {
            try {
                #region 1. CREATE PROXY REQUEST 
                //Note that, if the relativeURI starts with a slash, then it will be treated as absolute path on the HOST.. and relative to already existing base url..

                // For example, if our base url is 'https://test.one.com/pod/razor/api/ and we try to add '/va/file' as the relative url, then because of the '/' infront ofthe /va/file it will be treated as absolute to the host. So, it will become 'https://test.one.com/va/file' to avoid this, remove the '/' in the relative url.

                relativeURI = relativeURI.TrimStart('/');
                var proxyRequest = new HttpRequestMessage {
                    Method = method,
                    RequestUri = new Uri(relativeURI, UriKind.Relative),
                    Version = HttpVersion.Version20,
                    VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
                };

                // Stream request body if present
                //HTTP Get cannot have request Body
                if (!HttpMethods.IsGet(source.Method) && (source.ContentLength > 0 || source.Headers.ContainsKey("Transfer-Encoding"))) {

                    proxyRequest.Content = new StreamContent(source.Body, bufferSize: 1024 * 128); //IMPORTANT: NOTE THAT, HERE WE ARE STREAMING THE REQUEST BODY. Which means, even the downstream fails, this will continue to read the data. So, we need to ensure that it doesn't happen. So, when downstream fails, we need to stop this.

                    //SETTING THE CONTENT TYPE HERE WILL ENDUP IN ERROR. LET US SKIP SETTING HERE AND INSTEAD, SETUP IN THE HEADERS ITSELF.
                    //// Try parse content type safely (let us handle it in headers)
                }

                #endregion

                #region 2. COPY SOURCE REQUEST HEADERS TO PROXY REQUEST HEADERS
                // COPY SOURCE REQUEST HEADERS TO THE PROXY REQUEST
                foreach (var header in source.Headers) {

                    //We are facing issues, when trying to stream upload. Where, Sent 0 request bytes, but Content-length promised NNNN
                    // Let HttpClient compute the content length. Let us not set it by ourselves.
                    if (header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase)) continue;

                    if (!proxyRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray())) {
                        proxyRequest.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                    }
                }

                // CUSTOM HEADERS
                if (customHeaders != null) {
                    foreach (var kvp in customHeaders) {
                        // Avoid overwriting reserved headers accidentally
                        if (string.Equals(kvp.Key, "Content-Length", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(kvp.Key, "Host", StringComparison.OrdinalIgnoreCase))
                            continue;

                        // Try to add to request-level headers first
                        if (!proxyRequest.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value)) {
                            proxyRequest.Content?.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
                        }
                    }
                }
                #endregion

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellation_token);
                var linkedToken = cts.Token;

                // Send request and stream response
                var response = await client.SendAsync(proxyRequest, HttpCompletionOption.ResponseHeadersRead, linkedToken);
                if (!response.IsSuccessStatusCode) {
                    target.StatusCode = (int)response.StatusCode;
                    string errorBody = string.Empty;
                    try {
                        errorBody = await response.Content.ReadAsStringAsync(CancellationToken.None);
                    } catch {
                        // ignore 
                    }

                    // Construct a descriptive message
                    var message = $"Downstream API failed: {(int)response.StatusCode} {response.ReasonPhrase}";
                    if (!string.IsNullOrWhiteSpace(errorBody)) message += $" | Body: {errorBody.Trim()}";

                    cts.CancelAfter(15); //abort upload 
                    throw new HttpRequestException(message);
                }

                target.StatusCode = (int)response.StatusCode;

                //See, here, we are streaming the request..

                #region COPY RESPONSE HEADERS TO THE SOURCE RESPONSE HEADERS 
                //We should not forward the transfer-encoding headers

                //COPY THE RESPONSE HEADERS
                foreach (var header in response.Headers) {
                    if (string.Equals(header.Key, "Transfer-Encoding", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(header.Key, "Content-Length", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(header.Key, "Connection", StringComparison.OrdinalIgnoreCase))
                        continue;
                    target.Headers[header.Key] = header.Value.ToArray();
                }

                //COPY THE RESPONSE CONTENT HEADERS
                foreach (var header in response.Content.Headers) {
                    if (string.Equals(header.Key, "Transfer-Encoding", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(header.Key, "Content-Length", StringComparison.OrdinalIgnoreCase))
                        continue;

                    target.Headers[header.Key] = header.Value.ToArray();
                }
                #endregion

                // Copy the body manually
                await response.Content.CopyToAsync(target.Body, linkedToken);
            } catch (Exception ex) {
                await target.WriteAsync($"{ex.Message}");
            }
        }

        public static object ConvertDBAResult(this object input, ResultFilter filter = ResultFilter.FirstDictionaryValue) {
            //If we send error
            if (input is FeedbackError dbaerr) {
                return new BadRequestObjectResult(dbaerr.ToString());
            }
            if (input is Feedback dbres) {
                return new OkObjectResult(dbres.ToString());
            }
            //If we send direct action result
            if (typeof(IActionResult).IsAssignableFrom(input.GetType())) return input;
            return input;
        }

        public static string GetClientIP(this HttpContext context) {
            try {
                return context.Connection.RemoteIpAddress?.ToString();
            } catch (Exception) {
                return string.Empty;
            }
        }

        public static string GetEncryptedIPCookie(this HttpContext context, string encryptKey, string encryptSalt = null, Dictionary<string, string> payload = null) {
            var ip = context.GetClientIP();
            if (string.IsNullOrWhiteSpace(ip)) throw new ArgumentException("Unable to verify the ip address of the request.");
            if (string.IsNullOrWhiteSpace(encryptKey)) throw new ArgumentException("Encryption Key cannot be empty or null");
            if (string.IsNullOrWhiteSpace(encryptSalt)) encryptSalt = encryptKey; //assign the key as the salt as well

            var cookieDic = new Dictionary<string, string> {
                ["ip"] = ip
            };
            if (payload != null) {
                foreach (var item in payload) {
                    if (!cookieDic.ContainsKey(item.Key)) {
                        cookieDic.Add(item.Key, item.Value);
                    }
                }
            }

            return EncryptionUtils.Encrypt(cookieDic.ToJson(), encryptKey, encryptSalt).value;
        }

        public static void AppendEncryptIPCookie(this HttpContext context, string cookieName, CookieOptions options, string encryptKey, string encryptSalt = null, Dictionary<string,string> payload = null) {
            try {
                if (context == null) throw new ArgumentNullException("HttpContext");
                if (string.IsNullOrWhiteSpace(cookieName)) throw new ArgumentNullException("CookieName is required for creation");
                var cookie = GetEncryptedIPCookie(context, encryptKey, encryptSalt, payload);
                context.Response.Cookies.Append(cookieName, cookie, options);
            } catch (Exception) {
                context.Response.Body = new MemoryStream(Encoding.UTF8.GetBytes("Error while trying to add cookie"));
            }
        }

        public static Dictionary<string,string> DecryptIPCookie (this string cookie, string encryptKey, string encryptSalt = null) {
            if (string.IsNullOrWhiteSpace(cookie)) throw new ArgumentNullException("Cookie value cannot be null for decryption.");
            if (string.IsNullOrWhiteSpace(encryptKey)) throw new ArgumentException("Encryption Key cannot be empty or null");
            if (string.IsNullOrWhiteSpace(encryptSalt)) encryptSalt = encryptKey; //assign the key as the salt as well
            var decrypted = EncryptionUtils.Decrypt(cookie,encryptKey, encryptSalt);
            return decrypted.FromJson<Dictionary<string, string>>();
        }

        public static void CreateAuthPolicy(this AuthorizationOptions options, string scheme_name, string policy_name, Action<AuthorizationPolicyBuilder> processor = null) {
            var policyBuilder = new AuthorizationPolicyBuilder(scheme_name) //Api Key
                            .RequireAuthenticatedUser();

            processor?.Invoke(policyBuilder);
            options.AddPolicy(policy_name, policyBuilder.Build());
        }
    }
}
