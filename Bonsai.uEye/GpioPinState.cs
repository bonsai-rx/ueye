using OpenCV.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bonsai.uEye
{
    public class GpioPinState : Transform<uEyeDataFrame, Scalar>
    {
        public override IObservable<Scalar> Process(IObservable<uEyeDataFrame> source)
        {
            return source.Select(input =>
            {
                var gpio = input.ImageInfo.IoStatus;
                var gpio0 = ((gpio) & 0x1);
                var gpio1 = ((gpio >> 1) & 0x1);
                var gpio2 = ((gpio >> 2) & 0x1);
                var gpio3 = 0x0;
                return new Scalar(gpio0, gpio1, gpio2, gpio3);
            });
        }
    }
}
