using Autodesk.Revit.DB;
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
using Autodesk.Revit.UI;
using System.Collections;
using System.Collections.ObjectModel;
using static PipeCrossBeam.PipeCrossBeamRule;
using CommunityToolkit.Mvvm.ComponentModel;
using PipeCrossBeam.Common;
using Microsoft.Xaml.Behaviors;

namespace PipeCrossBeam
{
    /// <summary>
    /// UserControl1.xaml 的交互逻辑
    /// </summary>
    public partial class PipeCrossBeamView : Window
    {
        /// <summary>
        /// 主窗口
        /// </summary>
        /// <param name="elementIds"></param>
        /// <param name="uidoc"></param>
        public PipeCrossBeamView(UIDocument uidoc, Document doc)
        {
            InitializeComponent();

            PipeCrossBeamViewModel pipeCrossBeamViewModel = new PipeCrossBeamViewModel(uidoc, doc);

            this.DataContext = pipeCrossBeamViewModel;
            pipeNum.Text = pipeCrossBeamViewModel.PipeCrossBeamOutputs.Count.ToString();
            
        }
    }
}
