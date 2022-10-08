using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using PipeCrossBeam.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using static PipeCrossBeam.PipeCrossBeamRule;

namespace PipeCrossBeam
{
    public class PipeCrossBeamViewModel : ObservableObject
    {
        private InformationOut informationOutXP;
        public InformationOut InformationOutXP { get => informationOutXP; set => informationOutXP = value; }

        private List<PipeCrossBeamOutput> pipeCrossBeamOutputs;
        public List<PipeCrossBeamOutput> PipeCrossBeamOutputs { get => pipeCrossBeamOutputs; set => SetProperty( ref pipeCrossBeamOutputs ,value); }
        public PipeCrossBeamViewModel(UIDocument uidoc, Document doc)
        {
            InformationOutXP = new InformationOut();
            PipeCrossBeamOutputs = new List<PipeCrossBeamOutput>();

            InformationOutXP.Uidoc = uidoc;
            InformationOutXP.Doc = doc;
            InformationOutXP.ReservationTypeX = ReservationType.bushing;

            RunningCalculations(InformationOutXP);         

            ReserveType = new RelayCommand<string>(SelectionReserveType);
            PipeAndBeam = new RelayCommand<object>(SelectionPipeAndBeam);
        }

        public ICommand PipeAndBeam { get; set; }
        private void SelectionPipeAndBeam(object obj)
        {
            ICollection<ElementId> Selected = new Collection<ElementId>();
            ICollection<ElementId> SelectedPipes = new Collection<ElementId>();
            IList selectPipes = obj as IList;

            foreach (PipeCrossBeamOutput pipeCrossBeamOutput in selectPipes)
            {
                Selected.Add(pipeCrossBeamOutput.PipeId);
                Selected.Add(pipeCrossBeamOutput.BeamId);

                SelectedPipes.Add(pipeCrossBeamOutput.PipeId);
            }
            try
            {
                InformationOutXP.Uidoc.Selection.SetElementIds(Selected);   // 选择管道
                InformationOutXP.Uidoc.ShowElements(SelectedPipes);  // 显示管道
                InformationOutXP.Uidoc.RefreshActiveView();  // 刷新视图
            }
            catch
            {

            }
        }

        /// <summary>
        /// 选择预留类型
        /// </summary>
        public ICommand ReserveType { get; set; }
        private void SelectionReserveType(string obj)
        {
            if (obj == "bushingReserve" || obj == null)
            {
                InformationOutXP.ReservationTypeX = ReservationType.bushing;

                RunningCalculations(InformationOutXP);
            }
            else if(obj == "holeReserve")
            {
                InformationOutXP.ReservationTypeX = ReservationType.hole;

                RunningCalculations(InformationOutXP);
            }
        
        }

        private void RunningCalculations(InformationOut information)
        {
            PipeCrossBeamRule pipeCrossBeamRule1 = new PipeCrossBeamRule();
            pipeCrossBeamRule1.PipeCrossBeamRuleXP(information.Uidoc, information.Doc, information.ReservationTypeX);
            PipeCrossBeamOutputs = pipeCrossBeamRule1.PipeCrossBeamOutputs;
            for (int i = 0; i < PipeCrossBeamOutputs.Count; i++)
            {
                PipeCrossBeamOutputs[i].HoleNO = i + 1;
            }
        }
    }
}
