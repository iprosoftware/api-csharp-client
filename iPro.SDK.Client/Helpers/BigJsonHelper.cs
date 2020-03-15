using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace iPro.SDK.Client.Helpers
{
    public class BigJsonHelper
    {
        public static void LoadFiles(IEnumerable<string> jsonFiles, Action<JObject> itemAction)
        {
            foreach (var jsonFile in jsonFiles)
            {
                LoadFile(jsonFile, itemAction);
            }
        }

        public static void LoadFile(string jsonFile, Action<JObject> itemAction)
        {
            if (!File.Exists(jsonFile))
            {
                throw new FileNotFoundException();
            }

            using (var reader = new StreamReader(jsonFile))
            {
                LoadJsonStream(reader, itemAction);
            }
        }

        public static void LoadJson(string json, Action<JObject> itemAction)
        {
            if (string.IsNullOrEmpty(json))
            {
                throw new ArgumentNullException(nameof(json));
            }

            using (var reader = new StringReader(json))
            {
                LoadJsonStream(reader, itemAction);
            }
        }

        public static int CountInFiles(IEnumerable<string> jsonFiles)
        {
            var count = 0;

            foreach (var jsonFile in jsonFiles)
            {
                count += CountInFile(jsonFile);
            }

            return count;
        }

        public static int CountInFile(string jsonFile)
        {
            var count = 0;

            LoadFile(jsonFile, (item) =>
            {
                count += 1;
            });

            return count;
        }

        public static int CountInJson(string json)
        {
            var count = 0;

            LoadJson(json, (item) =>
            {
                count += 1;
            });

            return count;
        }

        private static void LoadJsonStream(TextReader textReader, Action<JObject> itemAction)
        {
            using (var reader = new JsonTextReader(textReader))
            {
                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.StartObject)
                    {
                        var item = JObject.Load(reader);
                        itemAction(item);
                    }
                }
            }
        }
    }

    public class BigJsonAsyncHelper
    {
        public async static Task LoadFilesAsync(IEnumerable<string> jsonFiles, Func<JObject, Task> itemAction)
        {
            foreach (var jsonFile in jsonFiles)
            {
                await LoadFileAsync(jsonFile, itemAction);
            }
        }

        public async static Task LoadFileAsync(string jsonFile, Func<JObject, Task> itemAction)
        {
            if (!File.Exists(jsonFile))
            {
                throw new FileNotFoundException();
            }

            using (var reader = new StreamReader(jsonFile))
            {
                await LoadJsonStreamAsync(reader, itemAction);
            }
        }

        public async static Task LoadJsonAsync(string json, Func<JObject, Task> itemAction)
        {
            if (string.IsNullOrEmpty(json))
            {
                throw new ArgumentNullException(nameof(json));
            }

            using (var reader = new StringReader(json))
            {
                await LoadJsonStreamAsync(reader, itemAction);
            }
        }

        private async static Task LoadJsonStreamAsync(TextReader textReader, Func<JObject, Task> itemAction)
        {
            using (var reader = new JsonTextReader(textReader))
            {
                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.StartObject)
                    {
                        var item = JObject.Load(reader);
                        await itemAction(item);
                    }
                }
            }
        }
    }
}
