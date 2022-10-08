using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PipeCrossBeam.Common
{
    /// <summary>
    /// 管道穿梁处的信息
    /// </summary>
    public class PipeCrossBeamOutput : ObservableObject,IEquatable<PipeCrossBeamOutput>
    {
        /// <summary>
        /// 预留洞口或套管序号
        /// </summary>
        int holeNO;
        public int HoleNO { get => holeNO; set => SetProperty(ref holeNO, value); }

        /// <summary>
        /// 管道ID
        /// </summary>
        ElementId pipeId;
        public ElementId PipeId { get => pipeId; set => SetProperty(ref pipeId, value); }

        /// <summary>
        /// 穿管的梁的ID
        /// </summary>
        ElementId beamId;
        public ElementId BeamId { get => beamId; set => SetProperty(ref beamId, value); }

        /// <summary>
        /// 管径
        /// </summary>
        Double diameter;
        public Double Diameter { get => diameter; set => SetProperty(ref diameter, value); }


        /// <summary>
        /// 留洞尺寸
        /// </summary>
        Double holeSize;
        public Double HoleSize { get => holeSize; set => SetProperty(ref holeSize, value); }

        /// <summary>
        /// 不符合规范的原因
        /// </summary>
        private string irregularities;
        public string Irregularities { get => irregularities; set => SetProperty(ref irregularities, value); }

        /// <summary>
        /// 继承比较接口
        /// 当两个类的管道ID和梁ID相同时，两个数据就相同
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(PipeCrossBeamOutput other)
        {
            if (this.BeamId == other.BeamId && this.PipeId == other.PipeId) return true;
            else { return false; }

        }
        public override int GetHashCode()
        {
            int hashBeamId = this.BeamId.GetHashCode();
            int hashPipeId = this.PipeId.GetHashCode();
            return hashBeamId ^ hashPipeId;
        }
    }
}
