using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PipeCrossBeam
{
    /// <summary>
    /// 符合常规的预留尺寸
    /// </summary>
    public class BushingSize
    {
        // 钢管公称直径和外径的对应字典
        static Dictionary<Double, Double> bushingSize = new Dictionary<Double, Double>()
        {
            {10,17.2},{15,21.3},{20,26.9},{25,33.7},{32,42.4},{40,48.3},{50,60.3},
            {65,76.1},{80,88.9},{100,114.3},{125,139.7},{150,168.3},{200,219.1},{250,273.1},{300,323.9},
            {350,355.6},{400,406.4},{450,457},{500,508},{600,610},{700,711},{800,813},{900,914},{1000,1016}
        };

        /// <summary>
        /// 返回符合常规的外径
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static Double BushingSizeOut(Double size)
        {
            Double de = 0;
            foreach (Double vale in bushingSize.Values)
            {
                if (size <= vale)
                {
                    de = vale;
                    break;
                }
            }

            return de;
        }

        /// <summary>
        /// 返回该外径的公称直径
        /// </summary>
        /// <param name="de"></param>
        /// <returns></returns>
        public static Double BushingSizeNominal(Double de)
        {
            Double size = 0;
            foreach (Double key in bushingSize.Keys)
            {
                if (de == bushingSize[key])
                {
                    size = key;
                    break;
                }
            }

            return size;
        }
    }
}
