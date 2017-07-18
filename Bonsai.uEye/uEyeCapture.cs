using Bonsai;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using uEye;
using uEye.Defines;
using uEye.Types;
using OpenCV.Net;
using System.Drawing;
using System.ComponentModel;
using System.Drawing.Design;
using System.Globalization;

namespace Bonsai.uEye
{
    [Description("Produces a sequence of images acquired from a uEye camera.")]
    public class uEyeCapture : Source<uEyeDataFrame>
    {
        IObservable<uEyeDataFrame> source;

        public uEyeCapture()
        {
            source = Observable.Create<uEyeDataFrame>(observer =>
            {
                var deviceId = DeviceId;
                var camera = new Camera();
                try
                {
                    var statusRet = deviceId.HasValue ? camera.Init(deviceId.Value | (int)DeviceEnumeration.UseDeviceID) : camera.Init();
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

                        ImageInfo imageInfo;
                        camera.Information.GetImageInfo(activeMemID, out  imageInfo);

                        using (var output = new IplImage(frameSize, depth, channels, imageBuffer))
                        {
                            observer.OnNext(new uEyeDataFrame(output.Clone(), imageInfo));
                        }
                    };

                    statusRet = camera.Acquisition.Capture();
                    HandleResult(statusRet);
                }
                catch
                {
                    camera.Exit();
                    throw;
                }

                return () =>
                {
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

        [TypeConverter(typeof(DeviceIdConverter))]
        [Description("The id of the device to open.")]
        public int? DeviceId { get; set; }

        [FileNameFilter("Ini-File (*.ini)|*.ini")]
        [Editor("Bonsai.Design.OpenFileNameEditor, Bonsai.Design", typeof(UITypeEditor))]
        [Description("The name of the file containing the camera configuration parameters.")]
        public string ConfigFile { get; set; }

        public override IObservable<uEyeDataFrame> Generate()
        {
            return source;
        }

        class DeviceIdConverter : Int32Converter
        {
            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                var text = value as string;
                if (!string.IsNullOrEmpty(text))
                {
                    return int.Parse(text.Split(' ')[0], culture);
                }

                return null;
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                if (destinationType == typeof(string))
                {
                    var deviceId = (int?)value;
                    if (deviceId.HasValue)
                    {
                        CameraInformation[] cameras;
                        global::uEye.Info.Camera.GetCameraList(out cameras);
                        var cameraIndex = Array.FindIndex(cameras, camera => camera.DeviceID == deviceId);
                        if (cameraIndex >= 0)
                        {
                            return string.Format("{0} ({1})", deviceId, cameras[cameraIndex].Model);
                        }
                    }
                }
                return base.ConvertTo(context, culture, value, destinationType);
            }

            public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
            {
                return true;
            }

            public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
            {
                CameraInformation[] cameras;
                global::uEye.Info.Camera.GetCameraList(out cameras);
                return new StandardValuesCollection(Array.ConvertAll(cameras, camera => (int)camera.DeviceID));
            }
        }
    }
}