using Autodesk.Revit.UI;
using MEP_Up_and_Down.Lib;
using MEP_Up_and_Down.UpDownTool.DuctCutEvent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MEP_Up_and_Down.UpDownTool
{
    /// <summary>
    /// Interaction logic for UpAndDown.xaml
    /// </summary>
    public partial class UpAndDown : Window
    {
        private ExternalEvent DuctCutDownEvent;
        private ExternalEvent PipeCutDownEvent;
        private ExternalEvent CTCutDownEvent;

        public UpAndDown()
        {
            InitializeComponent();

            var listOption = new List<string>() {"Cut Up", "Cut Down", "Move Up", "Move Down" };
            cbbOption.ItemsSource = listOption;
            cbbOption.SelectedIndex = 0;

            var listOptionAngle = new List<string>() { "45°", "90°" };
            cbbAngle.ItemsSource = listOptionAngle;
            cbbAngle.SelectedIndex = 0;

            tbOffset.Text = "1000";

            //Event 
            MEPDuctCutEvent ductCutEvent = new MEPDuctCutEvent() { window = this };
            DuctCutDownEvent = ExternalEvent.Create(ductCutEvent);
        }




        private void BindingImage(string option, string angle)
        {
            string findOption = $"{option}-{angle}";

            List<BitmapImage> listBitmap = new List<BitmapImage>()
            {
                MEPLib.Convert(Properties.Resources._460822136_122121114440442990_5047330998855420735_n),
                MEPLib.Convert(Properties.Resources._460822136_122121114440442990_5047330998855420735_n),
                MEPLib.Convert(Properties.Resources._464112140_122128355468442990_2366169051644275698_n),
                MEPLib.Convert(Properties.Resources._461043174_122121367370442990_3659341725448325718_n),
                MEPLib.Convert(Properties.Resources._460822136_122121114440442990_5047330998855420735_n),
                MEPLib.Convert(Properties.Resources._460822136_122121114440442990_5047330998855420735_n),
                MEPLib.Convert(Properties.Resources._464112140_122128355468442990_2366169051644275698_n),
                MEPLib.Convert(Properties.Resources._461043174_122121367370442990_3659341725448325718_n),
            };

            List<PictrueItem> pictrueItemsList = new List<PictrueItem>();

            int i = 0; 
            foreach(string optionItem in cbbOption.Items)
            {
                foreach(string goc in cbbAngle.Items)
                {
                    string name = $"{optionItem}-{goc}";
                    PictrueItem pictrueItem = new PictrueItem(listBitmap[i],name);
                    pictrueItemsList.Add(pictrueItem);
                    i++;
                }
            }

            PictrueItem item = pictrueItemsList.Find(x => x.Option == findOption);
            img.Source = item.Img;
        }

        private void cbbOption_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                string option = cbbOption.SelectedValue.ToString();
                string angle = cbbAngle.SelectedValue.ToString();
                BindingImage(option, angle);
            }
            catch{ }
        }

        private void cbbAngle_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                string option = cbbOption.SelectedValue.ToString();
                string angle = cbbAngle.SelectedValue.ToString();
                BindingImage(option, angle);
            }
            catch { }
        }


        public string Option;

        public string Angle;

        public double Offset;

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            Option = cbbOption.SelectedValue.ToString() ;
            Angle = cbbAngle.SelectedValue.ToString() ;

            bool isNumber = double.TryParse(tbOffset.Text , out double number);

            if (!isNumber)
            {
                MessageBox.Show("Enter a number");
                tbOffset.Text = "1000";
            }
            else
            {
                Offset = number;

                TabItem tabItem = tabControl.SelectedItem as TabItem;
                if (tabItem.Header.ToString() == "Duct")
                {
                    DuctCutDownEvent.Raise();
                }
                else if (tabItem.Header.ToString() == "Pipe")
                {
                    PipeCutDownEvent.Raise();
                }
                else if (tabItem.Header.ToString() == "Cable Tray")
                {
                    CTCutDownEvent.Raise();
                }
            }






        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }


}
