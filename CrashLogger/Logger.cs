using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CrashLogger
{
    /// <summary>
    /// CrashLogger
    /// </summary>
    public class Logger
    {
        private const string WebApiUrl = "https://crashlogger.com/api/newcrash/create";
        private string _appGuidId;
        private System.Collections.Concurrent.BlockingCollection<CrashItem> _list;

        private static HttpClient _client;
        
        /// <summary>
        /// The max. size of CrashItem Value (File/Text)
        /// </summary>
        public const int MaxValueSize = 10485760;

        /// <summary>
        /// Max. length of CrashItem Name
        /// </summary>
        public const int MaxNameLength = 1024;

        #region Public

        /// <summary>
        /// Create new instanz of Logger
        /// </summary>
        /// <param name="appGuidId">AppGuidId</param>
        public Logger(string appGuidId)
        {
            _appGuidId = appGuidId;
            _list = new System.Collections.Concurrent.BlockingCollection<CrashItem>
            {
                new CrashItem() { DataType = CrashDataType.Text, Name = "OSDescription", Value = RuntimeInformation.OSDescription },
                new CrashItem() { DataType = CrashDataType.Text, Name = "FrameworkDescription", Value = RuntimeInformation.FrameworkDescription },
                new CrashItem() { DataType = CrashDataType.Text, Name = "OSArchitecture", Value = RuntimeInformation.OSArchitecture.ToString("g") },
                new CrashItem() { DataType = CrashDataType.Text, Name = "ProcessArchitecture", Value = RuntimeInformation.ProcessArchitecture.ToString("g") },
            };
            
            if (_client == null)
            {
                _client = new HttpClient();
                _client.DefaultRequestHeaders.Accept.Clear();
                _client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                SendFromFileSystem();
            }
        }

        #region Add

        /// <summary>
        /// Add Text to Log
        /// </summary>
        /// <param name="name">Any Name. Max. size 1024 chars</param>
        /// <param name="value">Any Value. Max. size 10MB</param>
        public void Add(string name, string value)
        {
            try
            {
                if (CheckSize(name, value) == false)
                    return;

                _list.Add(new CrashItem() { DataType = CrashDataType.Text, Name = name, Value = value });
            }
            catch (Exception)
            {
                
            }
        }

        /// <summary>
        /// Add File to Log
        /// </summary>
        /// <param name="filename">Filename with extension. Max. size 1024 chars</param>
        /// <param name="file">Max. 10MB</param>
        public void Add(string filename, System.IO.Stream file)
        {
            try
            {
                using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
                {
                    file.CopyTo(ms);
                    Add(filename, ms.ToArray());
                }
            }
            catch (Exception)
            {
                
            }
        }

        /// <summary>
        /// Add File to Log
        /// </summary>
        /// <param name="filename">Filename with extension. Max. size 1024 chars</param>
        /// <param name="file">Max. 10MB</param>
        public void Add(string filename, byte[] file)
        {
            try
            {
                if (CheckSize(filename, file: file) == false)
                    return;

                var v = Convert.ToBase64String(file);
                _list.Add(new CrashItem() { DataType = CrashDataType.File, Name = filename, Value = v });
            }
            catch (Exception)
            {
                
            }
        }

        #endregion

        #region Clear

        /// <summary>
        /// Removes all elements
        /// </summary>
        public void Clear()
        {
            try
            {
                if (_list.Count == 0)
                    return;

                for (int i = 0; i < _list.Count; i++)
                {
                    _list.Take();
                }
            }
            catch (Exception)
            {
                
            }
        }

        #endregion

        #region Send

        /// <summary>
        /// Send to Api
        /// </summary>
        /// <param name="name">Any Name</param>
        public void Send(string name)
        {
            SendAsync(name).Wait();
        }

        /// <summary>
        /// Send to Api async
        /// </summary>
        /// <param name="name">Any Name</param>
        /// <returns></returns>
        public async Task SendAsync(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return;
                }

                if (_list.Count == 0)
                {
                    return;
                }

                var n = new Crash()
                {
                    AppGuidId = _appGuidId,
                    Date = DateTime.Now.ToUniversalTime(),
                    Items = new List<CrashItem>(_list),
                    Name = name
                };

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(n);

                if (await SendToApi(json) == false)
                {
                    SaveToFileSystem(json);
                }
            }
            catch (Exception)
            {
                
            }
        }

        #endregion

        #endregion

        #region Private

        private bool CheckSize(string name, string value = null, byte[] file = null)
        {
            if (name.Length > MaxNameLength)
            {
                return false;
            }

            if (value != null)
            {
                if (Encoding.UTF8.GetBytes(value).Length > MaxValueSize)
                {
                    return false;
                }
            }

            if (file != null)
            {
                if (file.Length > MaxValueSize)
                {
                    return false;
                }
            }

            return true;
        }

        private async Task<bool> SendToApi(string json)
        {
            try
            {
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var resp = await _client.PostAsync(WebApiUrl, content);
                if (resp.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return true;
                }
                else
                {
#if DEBUG
                    var error = await resp.Content.ReadAsStringAsync();
#endif
                }
            }
            catch (Exception)
            {
            }
            
            return false;
        }

        private void SaveToFileSystem(string json)
        {
            if (GetTempPath() is string path)
            {
                var file = System.IO.Path.Combine(path, $"{Guid.NewGuid().ToString()}.json");
                System.IO.File.WriteAllText(file, json);
            }
        }

        private async void SendFromFileSystem()
        {
            if (GetTempPath() is string path)
            {
                var files = System.IO.Directory.GetFiles(path);
                if (files.Length > 0)
                {
                    for (int i = 0; i < files.Length; i++)
                    {
                        var file = files[i];
                        try
                        {
                            if (System.IO.Path.GetExtension(file) == ".json")
                            {
                                var json = System.IO.File.ReadAllText(file);
                                if (await SendToApi(json) == false)
                                {
                                    break;
                                }

                                System.IO.File.Delete(file);
                            }
                        }
                        catch (Exception)
                        {
                            try
                            {
                                System.IO.File.Delete(file);
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                }
            }
        }

        private string GetTempPath()
        {
            try
            {
                var temp = System.IO.Path.GetTempPath();
                var folder = System.IO.Path.Combine(temp, "CrashLogger");

                if (System.IO.Directory.Exists(folder) == false)
                    System.IO.Directory.CreateDirectory(folder);

                folder = System.IO.Path.Combine(folder, _appGuidId);

                if (System.IO.Directory.Exists(folder) == false)
                    System.IO.Directory.CreateDirectory(folder);

                return folder;
            }
            catch (Exception)
            {

            }

            return null;
        }

        #endregion
        
        private class Crash
        {
            public string AppGuidId { get; set; }
            public string Name { get; set; }
            public DateTime Date { get; set; }
            public List<CrashItem> Items { get; set; }
        }

        private class CrashItem
        {
            public string Name { get; set; }
            public string Value { get; set; }
            public CrashDataType DataType { get; set; }
        }

        private enum CrashDataType
        {
            Text = 0,
            File = 1
        }
    }

    
}
