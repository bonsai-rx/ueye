using OpenCV.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using uEye;
using uEye.Defines;
using uEye.Types;
using System.Drawing;

namespace Bonsai.uEye
{
    public class uEyeDataFrame
    {
        public uEyeDataFrame(IplImage image, ImageInfo imageInfo)
        {
            Image = image;
            ImageInfo = imageInfo;
        }

        public IplImage Image { get; private set; }

        public ImageInfo ImageInfo { get; private set; }
    }
}
