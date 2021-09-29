using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using HtmlAgilityPack;
using RestSharp;

namespace SilkierQuartz.Example.GameProgram
{
    public class AutoGet
    {
        private string RootPath;

        public AutoGet(string rootPath)
        {
            RootPath = rootPath;
        }

        public bool RunUrl(string url,string[] args,out Process process)
        {
            var applicationUri = new Uri(url);
            var str = applicationUri.Segments[^1];
            if (!str.EndsWith(".application"))
            {
                //不是application
                string htmlUri = url;
                if (!str.EndsWith(".htm"))
                {
                    //不是htm
                    htmlUri = url.EndsWith("/") ? $"{url}publish.htm" : $"{url}/publish.htm";
                }
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(Get(htmlUri));
                var node =doc.DocumentNode.SelectSingleNode("//*[@class=\"JustThisApp\"]/a");
                if (node == null)
                {
                    process = null;
                    return false;
                }
                string application = node.Attributes["href"].Value;
                applicationUri = new Uri(new Uri(htmlUri), application);
            }
            process = RunWithTempFolder(applicationUri, args);

            return true;
        }


        public string Get(string url)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(url)) return String.Empty;
                var client = new RestClient(url);
                var request = new RestRequest(Method.GET);
                var res = client.Execute(request);
                return res.Content;
            }
            catch (Exception)
            {
                return String.Empty;
            }

        }
        Process RunWithTempFolder(Uri url, string[] args)
        {
            var localPath = CreateTempFolder(url.ToString());
            try
            {
                return RunFromDirectory(localPath.FullName, url, args);
            }
            catch
            {
                localPath.Delete(true);
            }
            return null;
        }

         Process RunFromDirectory(string localPath, Uri applicationUri, string[] args)
        {
            var co = new ClickOnceDownloader(localPath, applicationUri);
            co.Download();
           return co.ApplicationManifest.Start(args);
        }

         DirectoryInfo CreateTempFolder(string url)
        {
            string path = $"{RootPath}\\{GetMD5(url)}";
            return Directory.CreateDirectory(path);
        }


          static string GetMD5(string myString)
          {
              using MD5 mi = MD5.Create();
              byte[] buffer = Encoding.UTF8.GetBytes(myString);
              byte[] newBuffer = mi.ComputeHash(buffer);
              StringBuilder sb = new StringBuilder();
              foreach (byte t in newBuffer)
              {
                  sb.Append(t.ToString("x2"));
              }
              return sb.ToString();
          }
    }

    public static class ManifestParser
    {
        public static ClickOnceManifest DownloadAndParseManifest(Uri uri)
        {
            return Parse(
                XElement.Load(uri.ToString()),
                GetUriWithoutFile(uri.ToString()));
        }

        public static ClickOnceManifest Parse(XElement xml, Uri applicationBaseUri = null)
        {
            ClickOnceManifest manifest = new ClickOnceManifest { ApplicationBaseUri = applicationBaseUri };
            XElement xelement =
                xml.Elements(XName.Get("entryPoint", "urn:schemas-microsoft-com:asm.v2")).First().
                    Elements(XName.Get("commandLine", "urn:schemas-microsoft-com:asm.v2")).First();
            manifest.CommandLine = xelement.Attribute(XName.Get("file")).Value;
            manifest.Parameters = xelement.Attribute(XName.Get("parameters")).Value;
            manifest.Files =
                xml.Elements(XName.Get("dependency", "urn:schemas-microsoft-com:asm.v2"))
                    .SelectMany((Func<XElement, IEnumerable<XElement>>)
                        (dependency =>
                            dependency.Elements(XName.Get("dependentAssembly", "urn:schemas-microsoft-com:asm.v2"))))
                    .Select((Func<XElement, XAttribute>)(depAssem => depAssem.Attribute(XName.Get("codebase"))))
                    .Where(attr => attr != null)
                    .Select((Func<XAttribute, string>)(attr => attr.Value))
                    .Concat(
                        xml.Elements(XName.Get("file", "urn:schemas-microsoft-com:asm.v2"))
                            .Select(file => file.Attribute(XName.Get("name")).Value)
                    )
                    .ToArray();
            return manifest;
        }

        internal static Uri GetUriWithoutFile(string uri)
        {
            return new Uri(Regex.Replace(uri, "(?<=/)[^/]*$", ""));
        }
    }


    public class ClickOnceDownloader
    {
        readonly string localRootPath;

        public ClickOnceDownloader(string folder, Uri applicationUri)
        {
            localRootPath = folder;
            ApplicationUrl = applicationUri.ToString();
        }
        public string ApplicationUrl { get; private set; }
        public ClickOnceManifest ApplicationManifest { get; private set; }

        public void Download()
        {
            var applicationManifestXml = XElement.Load(ApplicationUrl);
            var absoluteCodebase =
               new Uri(ManifestParser.GetUriWithoutFile(ApplicationUrl),
                  ParseCodebase(applicationManifestXml));
            ApplicationManifest = LoadManifest(applicationManifestXml, absoluteCodebase);
            string fileName = Path.Combine(ApplicationManifest.LocalPath, ApplicationManifest.CommandLine);
            if (File.Exists(fileName))
            {


                return;
            }
            else
            {

                DownloadFiles(ApplicationManifest);
            }

        }

        ClickOnceManifest LoadManifest(XElement applicationManifestXml, Uri absoluteCodebase)
        {
            ClickOnceManifest manifest = ManifestParser.DownloadAndParseManifest(absoluteCodebase);
            manifest.Version = ParseVersion(applicationManifestXml);
            manifest.LocalPath = GetVersionPath(manifest.Version);




            return manifest;
        }

        string GetVersionPath(Version version)
        {
            return Path.Combine(localRootPath, version.ToString());
        }

        static Version ParseVersion(XElement applicationManifest)
        {
            return new Version(
               applicationManifest.Elements(XName.Get("assemblyIdentity", "urn:schemas-microsoft-com:asm.v1")).First()
                  .Attribute(XName.Get("version")).Value);
        }

        static string ParseCodebase(XElement applicationManifest)
        {
            return applicationManifest.Elements(
               XName.Get("dependency", "urn:schemas-microsoft-com:asm.v2")).First()
               .Elements(XName.Get("dependentAssembly", "urn:schemas-microsoft-com:asm.v2")).First().Attribute(XName.Get("codebase")).Value;
        }

        static void DownloadFiles(ClickOnceManifest manifest)
        {
            if (!Directory.Exists(manifest.LocalPath))
                Directory.CreateDirectory(manifest.LocalPath);
            WebClient webClient = new WebClient();
            foreach (string path2 in manifest.Files)
            {
                Uri address = new Uri(manifest.ApplicationBaseUri, path2.Replace('\\', '/') + ".deploy");
                string str = Path.Combine(manifest.LocalPath, path2);
                string path = Regex.Replace(str, "(?<!:)\\\\[^\\\\]+$", "");
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                if (!File.Exists(str))
                    webClient.DownloadFile(address, str);
            }
        }
    }

    public class ClickOnceManifest
    {
        public Uri ApplicationBaseUri { get; set; }

        public string[] Files { get; set; }

        public Version Version { get; set; }

        public string CommandLine { get; set; }

        public string Parameters { get; set; }

        public string LocalPath { get; set; }

        public Process Start(string[] args)
        {
            string fileName = Path.Combine(this.LocalPath, CommandLine);
            string arguments = string.Join(" ", args.Select(s => '"' + s + '"').ToArray());
            Process process =
                new Process()
                {
                    StartInfo = CreateProcessStartInfo(fileName, arguments, LocalPath),
                    EnableRaisingEvents = true
                };
            //process.Start();
            return process;
        }

        static ProcessStartInfo CreateProcessStartInfo(string fileName, string arguments, string workingDirectory)
        {


            return
                new ProcessStartInfo(fileName, arguments)
                {
                    WorkingDirectory = workingDirectory
                };
        }
    }


}
