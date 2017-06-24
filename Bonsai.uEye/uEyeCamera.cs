using OpenCV.Net;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using uEye;
using uEye.Defines;
using uEye.Types;

namespace Bonsai.uEye
{
    public class uEyeCamera : Source<IplImage>
    {
        IObservable<IplImage> source;

        public uEyeCamera()
        {
            source = Observable.Create<IplImage>(observer =>
            {
                var camera = new Camera();
                var statusRet = camera.Init();
                HandleResult(statusRet);

                if (!string.IsNullOrEmpty(ConfigFile))
                {
                    statusRet = camera.Parameter.Load(ConfigFile);
                    HandleResult(statusRet);
                }

                statusRet = camera.Memory.Allocate();
                HandleResult(statusRet);

                Int32 s32MemID;
                statusRet = camera.Memory.GetActive(out s32MemID);
                HandleResult(statusRet);

                int frameWidth;
                statusRet = camera.Memory.GetWidth(s32MemID, out frameWidth);
                HandleResult(statusRet);

                int frameHeight;
                statusRet = camera.Memory.GetHeight(s32MemID, out frameHeight);
                HandleResult(statusRet);

                int s32Bpp;
                statusRet = camera.Memory.GetBitsPerPixel(s32MemID, out s32Bpp);
                HandleResult(statusRet);

                ColorMode colorMode;
                statusRet = camera.PixelFormat.Get(out colorMode);
                HandleResult(statusRet);

                Rectangle rect;
                HandleResult(camera.Size.AOI.Get(out rect));
                HandleResult(camera.Size.AOI.Set(rect));
                statusRet = camera.Memory.Allocate();
                HandleResult(statusRet);

                var frameSize = new OpenCV.Net.Size(frameWidth, frameHeight);
                var depth = GetImageDepth(colorMode);
                var channels = s32Bpp / (int)depth;

                camera.EventFrame += (sender, e) =>
                {
                    Int32 activeMemID;
                    camera.Memory.GetActive(out activeMemID);

                    IntPtr imageBuffer;
                    camera.Memory.ToIntPtr(activeMemID, out imageBuffer);
                    using (var output = new IplImage(frameSize, depth, channels, imageBuffer))
                    {
                        observer.OnNext(output.Clone());
                    }
                };

                statusRet = camera.Acquisition.Capture();
                HandleResult(statusRet);

                return () =>
                {
                    CaptureStatus status;
                    HandleResult(camera.Information.GetCaptureStatus(out status));
                    Console.WriteLine(status.Total);
                    camera.Acquisition.Stop();
                    camera.Exit();
                };
            })
            .PublishReconnectable()
            .RefCount();
        }

        IplDepth GetImageDepth(ColorMode colorMode)
        {
            return IplDepth.U8;
        }

        void HandleResult(Status statusRet)
        {
            if (statusRet != Status.SUCCESS)
            {
                throw new InvalidOperationException(string.Format("uEye operation failed: {0}", statusRet));
            }
        }

        public int Index { get; set; }

        public string ConfigFile { get; set; }

        public override IObservable<IplImage> Generate()
        {
            return source;
        }
    }
}
