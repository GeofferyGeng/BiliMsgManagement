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
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BiliMsgManagement
{
    /// <summary>
    /// MsgDetail.xaml 的交互逻辑
    /// </summary>
    public partial class MsgDetail : Window, IComponentConnector
    {
        public MsgDetail()
        {
            InitializeComponent();
        }

        public MsgDetail(string content)
        {
            InitializeComponent();
            msgtxt.Text = content;
        }
    }
}
