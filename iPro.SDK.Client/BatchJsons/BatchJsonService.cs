using iPro.SDK.Client.Helpers;
using iPro.SDK.Client.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace iPro.SDK.Client.BatchJsons
{
    public class BatchJsonService
    {
        private readonly FileLogger _logger;
        private readonly BatchJsonState _state;
        private readonly BatchJsonStore<BatchJsonState> _store;
        private readonly Func<string, byte[], string, Task<ParsedResult>> _postContent;

        public BatchJsonService(
            Func<string, byte[], string, Task<ParsedResult>> postContent)
        {
            _logger = new FileLogger();
            _state = new BatchJsonState();
            _store = new BatchJsonStore<BatchJsonState>(_state);
            _postContent = postContent;
        }

        public BatchJsonStore<BatchJsonState> Store => _store;

        public void ClearFiles()
        {
            _store.Dispatch(state =>
            {
                state.SelectedFiles = null;
                state.ScannedCount = 0;
                state.ScannedError = null;
            });
        }

        public Task SelectFiles(IEnumerable<string> fileNames)
        {
            _store.Dispatch(state =>
            {
                state.IsScanning = true;
                state.SelectedFiles = fileNames;
            });

            return Task.Run(() =>
            {
                var scanned = 0;
                Exception scannedError = null;

                if (fileNames?.Any() == true)
                {
                    try { scanned = BigJsonHelper.CountInFiles(fileNames); }
                    catch (Exception ex) { scannedError = ex; }
                }

                _store.Dispatch(state =>
                {
                    state.IsScanning = false;
                    state.ScannedCount = scanned;
                    state.ScannedError = scannedError;
                });
            });
        }

        public Task SetPayload(string payload)
        {
            _store.Dispatch(state => state.PayloadText = payload);

            if (_state.HasSelectedFiles)
            {
                return Task.FromResult(false);
            }

            _store.Dispatch(state => state.IsScanning = true);

            return Task.Run(() =>
            {
                var scanned = 0;
                Exception scannedError = null;

                if (!string.IsNullOrEmpty(payload))
                {
                    try { scanned = BigJsonHelper.CountInJson(payload); }
                    catch (Exception ex) { scannedError = ex; }
                }

                _store.Dispatch(state =>
                {
                    state.IsScanning = false;
                    state.ScannedCount = scanned;
                    state.ScannedError = scannedError;
                });
            });
        }

        public Task Start(string endpoint)
        {
            // validate
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new Exception("Endpoint is required");
            }

            if (string.IsNullOrWhiteSpace(_state.PayloadText))
            {
                throw new Exception("Payload is required");
            }

            if (_state.ScannedError != null)
            {
                throw new Exception("Scanned Failed");
            }

            // start state
            _store.Dispatch(state =>
            {
                state.LogFilePath = _logger.FilePath;
                state.ApiEndpoint = endpoint;
                state.IsPushing = true;
                state.StartsAt = DateTime.Now;
                state.EndsAt = null;
                state.TotalCount = state.ScannedCount;
                state.SuccessCount = 0;
                state.FailedCount = 0;
            });

            // process
            return Task.Run(async () =>
            {
                try
                {
                    if (_state.HasSelectedFiles)
                    {
                        await BigJsonAsyncHelper.LoadFilesAsync(_state.SelectedFiles, PushItem);
                    }
                    else
                    {
                        await BigJsonAsyncHelper.LoadJsonAsync(_state.PayloadText, PushItem);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error("Process Failed", ex.ToString());
                    throw;
                }
                finally
                {
                    _store.Dispatch(state =>
                    {
                        state.IsPushing = false;
                        state.EndsAt = DateTime.Now;
                    });
                }
            });
        }

        private async Task PushItem(JObject item)
        {
            var itemJson = JsonConvert.SerializeObject(item);
            var content = new StringContent(itemJson);

            var result = await _postContent(
                _state.ApiEndpoint,
                content.ReadAsByteArrayAsync().Result,
                "application/json");

            var logTitle = $"Index: {_state.SuccessCount + _state.FailedCount}";

            if (result.Success)
            {
                _logger.Info(logTitle, result.Message);
                _store.Dispatch(x => x.SuccessCount += 1);
            }
            else
            {
                _logger.Warn(logTitle, result.Message);
                _store.Dispatch(x => x.FailedCount += 1);
            }

            Thread.Sleep(100);
        }
    }
}
