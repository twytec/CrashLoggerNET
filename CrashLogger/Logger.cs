using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CrashLogger
{
    /// <summary>
    /// Static CrashLogger
    /// </summary>
    public static class Logger
    {
        //private const string WebApiUrl = "http://localhost:5000/api/newcrash/create";
        private const string WebApiUrl = "https://crashlogger.com/api/newcrash/create";
        private static string _appGuidId;
        private static System.Collections.Concurrent.BlockingCollection<NewCrashItem> _list;
        private static HttpClient _client;
        private static bool _isSend = false;
        private const int _maxLength = 10485760;

        #region Public

        /// <summary>
        /// Init
        /// </summary>
        /// <param name="appGuidId">AppGuidId</param>
        public static void Init(string appGuidId)
        {
            _appGuidId = appGuidId;
            _list = new System.Collections.Concurrent.BlockingCollection<NewCrashItem>();
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            SendFromFileSystem();
        }

        #region Add

        /// <summary>
        /// Add Text to Log
        /// </summary>
        /// <param name="name">Any Name. Max. size 1024 chars</param>
        /// <param name="value">Any Value. Max. size 10MB</param>
        public static void Add(string name, string value)
        {
            CheckInit();

            if (CheckSize(name, value) == false)
                return;

            _list.Add(new NewCrashItem() { DataType = CrashDataType.Text, Name = name, Value = value });
        }

        /// <summary>
        /// Add File to Log
        /// </summary>
        /// <param name="filename">Filename with extension. Max. size 1024 chars</param>
        /// <param name="file">Max. 10MB</param>
        public static void Add(string filename, System.IO.Stream file)
        {
            CheckInit();
            
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                file.CopyTo(ms);
                Add(filename, ms.ToArray());
            }
        }

        /// <summary>
        /// Add File to Log
        /// </summary>
        /// <param name="filename">Filename with extension. Max. size 1024 chars</param>
        /// <param name="file">Max. 10MB</param>
        public static void Add(string filename, byte[] file)
        {
            CheckInit();

            if (CheckSize(filename, file: file) == false)
                return;

            var v = Convert.ToBase64String(file);
            _list.Add(new NewCrashItem() { DataType = CrashDataType.File, Name = filename, Value = v });
        }

        #endregion

        #region Clear

        /// <summary>
        /// Removes all elements
        /// </summary>
        public static void Clear()
        {
            CheckInit();
            
            if (_list.Count == 0)
                return;

            for (int i = 0; i < _list.Count; i++)
            {
                _list.Take();
            }
        }

        #endregion

        #region Send

        /// <summary>
        /// Send to Api
        /// </summary>
        /// <param name="name">Any Name</param>
        public static void Send(string name)
        {
            SendAsync(name).Wait();
        }

        /// <summary>
        /// Send to Api async
        /// </summary>
        /// <param name="name">Any Name</param>
        /// <returns></returns>
        public static async Task SendAsync(string name)
        {
            CheckInit();

            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            if (_list.Count == 0)
            {
                return;
            }

            var n = new NewCrash()
            {
                AppGuidId = _appGuidId,
                Date = DateTime.Now.ToUniversalTime(),
                Items = new List<NewCrashItem>(_list),
                Name = name
            };

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(n);

            if (await SendToApi(json) == false)
            {
                SaveToFileSystem(json);
            }
        }

        /// <summary>
        /// Send to Api without internal Logs
        /// </summary>
        /// <param name="name">Any Name</param>
        /// <param name="items">Log list</param>
        public static void Send(string name, Dictionary<string, string> items)
        {
            SendAsync(name, items).Wait();
        }

        /// <summary>
        /// Send to Api without internal Logs
        /// </summary>
        /// <param name="name">Any Name</param>
        /// <param name="items">Log list</param>
        public static async Task SendAsync(string name, Dictionary<string, string> items)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            if (items == null || items.Count == 0)
            {
                return;
            }

            var n = new NewCrash()
            {
                AppGuidId = _appGuidId,
                Date = DateTime.Now.ToUniversalTime(),
                Items = new List<NewCrashItem>(),
                Name = name
            };

            foreach (var item in items)
            {
                n.Items.Add(new NewCrashItem() {
                    DataType = CrashDataType.Text,
                    Name = item.Key,
                    Value = item.Value
                });
            }

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(n);

            if (await SendToApi(json) == false)
            {
                SaveToFileSystem(json);
            }
        }
        
        #endregion

        #endregion

        #region Private

        private static void CheckInit()
        {
            if (_list == null)
            {
                throw new Exception("CrashLogger ist not Initializes. Use Init()");
            }
        }

        private static bool CheckSize(string name, string value = null, byte[] file = null)
        {
            if (name.Length > 1024)
            {
                return false;
            }

            if (value != null)
            {
                if (Encoding.UTF8.GetBytes(value).Length > _maxLength)
                {
                    return false;
                }
            }

            if (file != null)
            {
                if (file.Length > _maxLength)
                {
                    return false;
                }
            }

            return true;
        }

        private static async Task<bool> SendToApi(string json)
        {
            try
            {
                while (_isSend)
                {
                    await Task.Delay(100);
                }

                _isSend = true;
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var resp = await _client.PostAsync(WebApiUrl, content);
                if (resp.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    _isSend = false;
                    return true;
                }
                else
                {
                    var error = await resp.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                var la = ex;
            }

            _isSend = false;
            return false;
        }

        private static void SaveToFileSystem(string json)
        {
            if (GetTempPath() is string path)
            {
                var file = System.IO.Path.Combine(path, $"{Guid.NewGuid().ToString()}.json");
                System.IO.File.WriteAllText(file, json);
            }
        }

        private static async void SendFromFileSystem()
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

        private static string GetTempPath()
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
        
        private class NewCrash
        {
            public string AppGuidId { get; set; }
            public string Name { get; set; }
            public DateTime Date { get; set; }
            public List<NewCrashItem> Items { get; set; }
        }

        private class NewCrashItem
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
