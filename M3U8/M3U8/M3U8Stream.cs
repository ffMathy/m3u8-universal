using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace M3U8
{
    public class M3U8Stream : IRandomAccessStream
    {
        private readonly string _url;

        private readonly Dictionary<string, byte[]> _internalBuffer;

        private bool _stop;

        public M3U8Stream(string url)
        {
            _internalBuffer = new Dictionary<string, byte[]>();

            _stop = false;
            _url = url;

            StartDownloading(url);
        }

        private async void StartDownloading(string url)
        {
            using (var client = new HttpClient())
            {
                while (!_stop)
                {
                    var data = await client.GetStringAsync(url);

                    var lines = data.Split('\n');
                    if (lines.Any())
                    {
                        const int defaultTargetDuration = 100;
                        var targetDuration = defaultTargetDuration;

                        var firstLine = lines[0];
                        if (firstLine != "#EXTM3U")
                        {
                            throw new InvalidOperationException(
                                "The provided URL does not link to a well-formed M3U8 playlist.");
                        }

                        for (var i = 1; i < lines.Length; i++)
                        {
                            var line = lines[i];
                            if (line.StartsWith("#"))
                            {
                                var lineData = line.Substring(1);

                                var split = lineData.Split(':');

                                var name = split[0];
                                var value = split[1];

                                switch (name)
                                {
                                    case "EXT-X-TARGETDURATION":
                                        if (targetDuration == defaultTargetDuration)
                                        {
                                            targetDuration = int.Parse(value);
                                        }
                                        break;

                                    //oh, how sweet. a header for us to entirely ignore. we'll always use cache.
                                    case "EXT-X-ALLOW-CACHE":
                                        break;

                                    case "EXT-X-VERSION":
                                        break;

                                    case "EXT-X-MEDIA-SEQUENCE":
                                        break;

                                    case "EXTINF":
                                        var nextLine = lines[i + 1];
                                        if (!_internalBuffer.ContainsKey(nextLine) && !_stop)
                                        {
                                            var bytes = await client.GetByteArrayAsync(nextLine);
                                            _internalBuffer.Add(nextLine, bytes);
                                        }
                                        break;
                                }
                            }
                        }

                        //wait for a new part of the stream to appear if we're lucky.
                        await Task.Delay(targetDuration * 1000 / 2);
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            "The provided URL does not contain any data.");
                    }
                }
            }
        }

        public void Dispose()
        {
            _stop = true;
        }

        public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
        {
            return AsyncInfo.Run<IBuffer, uint>((token, progress) =>
                Task.Run(async delegate()
                {
                    var bytesRead = 0u;

                    //keep going until we've read all data.
                    while (bytesRead < count && !token.IsCancellationRequested)
                    {

                        var firstKey = string.Empty;
                        while (string.IsNullOrEmpty(firstKey) && !token.IsCancellationRequested)
                        {
                            //while we don't have data in the trunk, wait for it.
                            firstKey = _internalBuffer.Keys.FirstOrDefault();
                            await Task.Delay(100, token);
                        }

                        //did we cancel? exit out.
                        if (token.IsCancellationRequested)
                        {
                            return buffer;
                        }

                        //copy the data over.
                        var bufferData = _internalBuffer[firstKey];

                        var amount = Math.Min(bufferData.Length, count - bytesRead);
                        bufferData.CopyTo(0, buffer, bytesRead, (int)amount);

                        //increment bytes read.
                        bytesRead += (uint)amount;

                        //report the progress.
                        progress.Report(bytesRead);

                    }

                    return buffer;

                }, token));

        }

        public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer)
        {
            throw new InvalidOperationException();
        }

        public IAsyncOperation<bool> FlushAsync()
        {
            throw new InvalidOperationException();
        }

        public IInputStream GetInputStreamAt(ulong position)
        {
            throw new InvalidOperationException();
        }

        public IOutputStream GetOutputStreamAt(ulong position)
        {
            throw new InvalidOperationException();
        }

        public void Seek(ulong position)
        {
            Position = position;
        }

        public IRandomAccessStream CloneStream()
        {
            return new M3U8Stream(_url);
        }

        public bool CanRead { get; private set; }
        public bool CanWrite { get; private set; }
        public ulong Position { get; private set; }
        public ulong Size { get; set; }
    }
}
