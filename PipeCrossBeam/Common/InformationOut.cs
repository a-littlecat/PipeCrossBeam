using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PipeCrossBeam.PipeCrossBeamRule;

namespace PipeCrossBeam.Common
{
    /// <summary>
    /// 界面绑定的信息
    /// </summary>
    public class InformationOut : ObservableObject
    {
        /// <summary>
        /// 模型文件的UI文档
        /// </summary>
        private UIDocument uidoc;
        public UIDocument Uidoc { get => uidoc; set => SetProperty(ref uidoc, value); }


        /// <summary>
        /// 模型文件的文档
        /// </summary>
        private Document doc;
        public Document Doc { get => doc; set => SetProperty(ref doc, value); }


        /// <summary>
        /// 预留类型（套管或洞）
        /// </summary>
        ReservationType reservationTypeX;
        public ReservationType ReservationTypeX { get => reservationTypeX; set => SetProperty(ref reservationTypeX, value); }

    }
}
