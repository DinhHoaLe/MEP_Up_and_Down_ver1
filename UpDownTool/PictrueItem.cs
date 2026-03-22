using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace MEP_Up_and_Down.UpDownTool
{
    public class PictrueItem
    {
        public BitmapImage Img { get; set; }

        public string Option { get; set; }  

        public PictrueItem(BitmapImage bitmapaImg, string option) {
            Img = bitmapaImg;
            Option = option;
        }
    }
}
