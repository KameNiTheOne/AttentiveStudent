using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.Diagnostics;
using ConfigChangeReactor;

namespace CaptureService.Services
{
    public class AudioCapture : Configurable
    {
        long audiofileDuration;
        public string CaptureDeviceName;
        MMDevice device;
        public AudioCapture(Dictionary<string, string> cnfg)
        {
            audiofileDuration = long.Parse(cnfg["audiofileDuration"]);
            CaptureDeviceName = cnfg["captureDeviceName"];

            var enumerator = new MMDeviceEnumerator();
            foreach (var wasapi in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
            {
                if (wasapi.DeviceFriendlyName == CaptureDeviceName)
                {
                    Console.WriteLine($"Capturing on: {wasapi.DeviceFriendlyName}");
                    device = wasapi;
                    break;
                }
            }

            ReactorDomain.Subscribe(ChangeHandler);
        }
        public async Task<int> Capture(string outputTempPath, CancellationToken ct)
        {
            long afd = audiofileDuration;
            var capture = new WasapiLoopbackCapture(device);
            // optionally we can set the capture waveformat here: e.g. capture.WaveFormat = new WaveFormat(44100, 16,2);
            var writer = new WaveFileWriter(outputTempPath, capture.WaveFormat);
            int elapsedTime = 0;

            capture.DataAvailable += (s, a) =>
            {
                writer.Write(a.Buffer, 0, a.BytesRecorded);
                long audioLengthInSeconds = capture.WaveFormat.AverageBytesPerSecond * afd;
                if (writer.Position > audioLengthInSeconds)
                {
                    capture.StopRecording();
                }
            };

            capture.RecordingStopped += (s, a) =>
            {
                elapsedTime = (int)Math.Ceiling((double)writer.Position / capture.WaveFormat.AverageBytesPerSecond);
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
            return elapsedTime;
        }
        public override void ChangeHandler(Dictionary<string, string> cnfg)
        {
            audiofileDuration = long.Parse(cnfg["audiofileDuration"]);
            if (CaptureDeviceName != cnfg["captureDeviceName"])
            {
                CaptureDeviceName = cnfg["captureDeviceName"];

                var enumerator = new MMDeviceEnumerator();
                foreach (var wasapi in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
                {
                    if (wasapi.DeviceFriendlyName == CaptureDeviceName)
                    {
                        device = wasapi;
                        break;
                    }
                }
            }
        }
    }
}
