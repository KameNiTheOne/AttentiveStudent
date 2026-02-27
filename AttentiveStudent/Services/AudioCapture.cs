using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.Diagnostics;

namespace CaptureService.Services
{
    public class AudioCapture : IDisposable
    {
        long audiofileDuration;
        string outputFolder;
        MMDevice device;
        public AudioCapture(string _outputFolder, string deviceName, long _audiofileDuration)
        {
            audiofileDuration = _audiofileDuration;

            outputFolder = _outputFolder;
            Directory.CreateDirectory(outputFolder);

            bool deviceSetup = false;
            var enumerator = new MMDeviceEnumerator();
            foreach (var wasapi in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
            {
                if (wasapi.DeviceFriendlyName == deviceName)
                {
                    Console.WriteLine($"Capturing on: {wasapi.DeviceFriendlyName}");
                    device = wasapi;
                    break;
                }
            }

        }
        public async Task<int> Capture(string outputTempPath, CancellationToken ct)
        {
            var capture = new WasapiLoopbackCapture(device);
            // optionally we can set the capture waveformat here: e.g. capture.WaveFormat = new WaveFormat(44100, 16,2);
            var writer = new WaveFileWriter(outputTempPath, capture.WaveFormat);

            capture.DataAvailable += (s, a) =>
            {
                writer.Write(a.Buffer, 0, a.BytesRecorded);
                long audioLengthInSeconds = capture.WaveFormat.AverageBytesPerSecond * audiofileDuration;
                if (writer.Position > audioLengthInSeconds)
                {
                    capture.StopRecording();
                }
            };

            capture.RecordingStopped += (s, a) =>
            {
                writer.Dispose();
                writer = null;
                capture.Dispose();
            };
            //Record a file number i
            var watch = Stopwatch.StartNew();
            capture.StartRecording();
            while (capture.CaptureState != CaptureState.Stopped)
            {
                if (ct.IsCancellationRequested)
                {
                    capture.StopRecording();
                }
                await Task.Delay(50);
            }
            return (int)Math.Ceiling((double)watch.ElapsedMilliseconds/1000);
        }
        public void Dispose()
        {
            Directory.Delete(outputFolder, true);
        }
    }
}
