using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.UI;
using PipeCrossBeam;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CommunityToolkit.Mvvm.ComponentModel;
using PipeCrossBeam.Common;

namespace PipeCrossBeam
{

    /// <summary>
    /// 判断管道穿梁是否符合规范，并返回程序状态
    /// </summary>
    public class PipeCrossBeamRule : ObservableObject
    {
        // 对话框标题
        string dialogTitle = "管道穿梁检测";

        /// <summary>
        /// 预留的类型
        /// </summary>
        public enum ReservationType
        {
            bushing,
            hole
        }

        /// <summary>
        /// 图中所有的管道的集合（用来测试每个管道穿梁是否符合规范）
        /// </summary>
        private ICollection<ElementId> pipes = new Collection<ElementId>();
        public ICollection<ElementId> Pipes { get => pipes; set => SetProperty(ref pipes, value); }

        /// <summary>
        /// 图中所有梁的集合
        /// </summary>
        private ICollection<ElementId> beams = new Collection<ElementId>();
        public ICollection<ElementId> Beams { get => beams; set => beams = value; }

        private Dictionary<ICollection<ElementId>, Document> beamAndDocs = new Dictionary<ICollection<ElementId>, Document>();
        public Dictionary<ICollection<ElementId>, Document> BeamAndDocs { get => beamAndDocs; set => beamAndDocs = value; }


        /// <summary>
        /// 管道穿梁处的信息的集合
        /// </summary>
        private List<PipeCrossBeamOutput> pipeCrossBeamOutputs = new List<PipeCrossBeamOutput>();

        public List<PipeCrossBeamOutput> PipeCrossBeamOutputs { get => pipeCrossBeamOutputs; set => SetProperty(ref pipeCrossBeamOutputs, value); }

        public void PipeCrossBeamRuleXP(UIDocument uidoc, Document doc, ReservationType reservationType)
        {
            Selection selection = uidoc.Selection;
            Pipes = CollectPipe.CollectPipes(doc);     //收集到文档中所有的管道集合

            // 当用户选择预留套管是，外径+50
            // 当用户选择预留洞时，外径+100
            // 因为计算时用的是半径计算，故50和100减半
            Double pipeReservationType;
            if (reservationType == ReservationType.bushing)
            {
                pipeReservationType = 25 / 304.8;
            }
            else
            {
                pipeReservationType = 50 / 304.8;
            }



            if (Pipes.Count == 0)
            {
                MessageBox.Show(dialogTitle, "本文件中无管道的信息！");

            }
            else
            {
                BeamAndDocs = GetInformaFromABeam.CollectBeams(doc);     //收集文档中所有的梁的集合
                if (BeamAndDocs.Keys.Count == 0)
                {
                    MessageBox.Show(dialogTitle, "本文件中无梁的信息！");
                }
                else
                {
                    foreach (ElementId pipe in Pipes)
                    {

                        // 设置管道的留洞尺寸信息
                        Element pipeEle = doc.GetElement(pipe);
                        Pipe pipeIn = pipeEle as Pipe;
                        PipeInformation pipeInformation = new PipeInformation();
                        pipeInformation.PipeDe = pipeIn.get_Parameter(BuiltInParameter.RBS_PIPE_OUTER_DIAMETER).AsDouble();     // 管道外径

                        pipeInformation.Bushing = pipeReservationType;


                        // 设置管道的绝热层厚度
                        pipeInformation.Insulation = pipeIn.get_Parameter(BuiltInParameter.RBS_REFERENCE_INSULATION_THICKNESS).AsDouble();

                        Curve pipeCurve;    //管道的位置信息的曲线类型

                        //获取管道的位置信息，并转换为曲线类型
                        try
                        {
                            Location location = doc.GetElement(pipe).Location;
                            LocationCurve locationCurve = location as LocationCurve;
                            pipeCurve = locationCurve.Curve;
                        }
                        catch
                        {
                            continue;
                        }

                        foreach (ICollection<ElementId> beams in BeamAndDocs.Keys)
                        {
                            // 管道穿越的梁的集合
                            ICollection<ElementId> pipeCrossBeams = GetInformaFromABeam.CollectPipeCrossBeams(BeamAndDocs[beams], beams, pipeEle);

                            foreach (ElementId pipeCrossBeam in pipeCrossBeams)
                            {
                                Element pipeCrossBeamEle = BeamAndDocs[beams].GetElement(pipeCrossBeam);
                                FaceArray beamFaces = GetInformaFromABeam.GetFaceFromABeam(pipeCrossBeamEle);   //获取梁的面数组
                                IntersectionResultArray intersectionResultArray;    //新建一个线与面的交点数组
                                bool havePipeCrossBeamXYZ = false;
                                List<int> Irregularities = new List<int>();    // 管道不符合规范内容

                                foreach (Face face in beamFaces)
                                {
                                    XYZ[] faceXYZs;
                                    try
                                    {
                                        faceXYZs = GetInformaFromFace.GetInforma(face);   //获取面的四个边点数组
                                    }
                                    catch
                                    {
                                        continue;
                                    }

                                    SetComparisonResult crossResult = face.Intersect(pipeCurve, out intersectionResultArray);   //面与线相交并产生结果
                                    if (crossResult == SetComparisonResult.Overlap)      //如果相交
                                    {
                                        havePipeCrossBeamXYZ = true;    // 穿梁处管中心和梁有交点
                                        foreach (IntersectionResult intersectionResult in intersectionResultArray)
                                        {
                                            XYZ intersectionXYZ = intersectionResult.XYZPoint;

                                            //判断管道穿梁是否符合规范，如果不符合，此梁添加进不符合规范的元素集合
                                            if (!CollisionResults.CollisionResult(intersectionXYZ, faceXYZs, pipeInformation.PipeDe,
                                                pipeInformation.Bushing, pipeInformation.Insulation, out Irregularities))
                                            {
                                                PipeCrossBeamOutput output = new PipeCrossBeamOutput();
                                                output.PipeId = pipe;
                                                output.BeamId = pipeCrossBeam;
                                                output.Diameter = pipeIn.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble() * 304.8;

                                                Double bushingSizeA = Math.Round(pipeInformation.PipeDe * 304.8 + pipeInformation.Insulation * 304.8 * 2 + pipeInformation.Bushing * 304.8 * 2, 0);
                                                Double HoleSizeB = BushingSize.BushingSizeOut(bushingSizeA);     // 调整留洞尺寸使之更符合常规
                                                output.HoleSize = BushingSize.BushingSizeNominal(HoleSizeB);

                                                output.Irregularities = CrossBeamRule.OutRules(Irregularities);     // 不符规范的原因

                                                PipeCrossBeamOutputs.Add(output);
                                            }
                                        }
                                    }


                                }

                                // 与梁相交管中心却不在梁上的情况
                                if (!havePipeCrossBeamXYZ)
                                {
                                    PipeCrossBeamOutput output_04 = new PipeCrossBeamOutput();
                                    output_04.PipeId = pipe;
                                    output_04.BeamId = pipeCrossBeam;
                                    output_04.Diameter = pipeIn.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble() * 304.8;
                                    Double bushingSizeA = Math.Round(pipeInformation.PipeDe * 304.8 + pipeInformation.Insulation * 304.8 * 2 + pipeInformation.Bushing * 304.8 * 2, 0);
                                    Double bushingSizeB = BushingSize.BushingSizeOut(bushingSizeA);     // 调整留洞尺寸使之更符合常规
                                    output_04.HoleSize = BushingSize.BushingSizeNominal(bushingSizeB);

                                    output_04.Irregularities = CrossBeamRule.OutRules(new List<int>() { 3 });

                                    PipeCrossBeamOutputs.Add(output_04);
                                }

                            }

                        }
                    }
                    if (PipeCrossBeamOutputs.Count == 0)
                    {
                        MessageBox.Show(dialogTitle, "所有管道穿梁均符合规范要求。\n" +
                            "注意：本规则只检测《高层建筑混凝土结构技术规程》JGJ 3-2010 6.3.7条！");
                    }
                    else
                    {
                        PipeCrossBeamOutputs = PipeCrossBeamOutputs.Distinct().ToList();
                    }
                }
            }
        }
    }

}


/// <summary>
/// 不符合规范的原因
/// </summary>
public class CrossBeamRule
{
    private string[] rules = new string[4];
    public CrossBeamRule()
    {
        rules[0] = "洞口位置不在梁跨1/3区域";
        rules[1] = "洞口高度大于梁高的40%";
        rules[2] = "洞口顶部距梁顶或洞口底部距梁底小于200mm";
        rules[3] = "洞口中心在梁截面范围外";
    }
    public string this[int index]
    {
        get
        {
            return rules[index];
        }
    }

    public static string OutRules(List<int> ruleIndex)
    {
        string result = null;
        foreach (int i in ruleIndex)
        {
            CrossBeamRule crossBeamRule = new CrossBeamRule();
            result = result + crossBeamRule[i] + '\n';
        }
        result = result.TrimEnd('\n');

        return result;
    }
}




/// <summary>
/// 收集管道信息
/// </summary>
public class CollectPipe
{
    /// <summary>
    /// 收集文档内的所有管道的集合
    /// </summary>
    /// <param name="commandData"></param>
    /// <returns></returns>
    public static ICollection<ElementId> CollectPipes(Document doc)
    {
        ICollection<ElementId> result = new Collection<ElementId>();

        FilteredElementCollector collector = new FilteredElementCollector(doc);
        ElementClassFilter filter = new ElementClassFilter(typeof(Pipe));

        result = collector.WherePasses(filter).ToElementIds();
        return result;
    }
}


/// <summary>
/// 穿管的信息
/// </summary>
public class PipeInformation
{
    Double pipeDe;
    public Double PipeDe
    {
        get { return pipeDe; }
        set { pipeDe = value; }
    }

    Double bushing;
    public Double Bushing
    {
        get { return bushing; }
        set { bushing = value; }
    }
    Double insulation;
    public Double Insulation
    {
        get { return insulation; }
        set { insulation = value; }
    }

}



/// <summary>
/// 收集梁的信息
/// </summary>
public class GetInformaFromABeam
{
    /// <summary>
    /// 收集梁的集合信息
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    public static FaceArray GetFaceFromABeam(Element element)
    {
        //设置几何选项
        Options options = new Options();
        options.IncludeNonVisibleObjects = true;
        options.ComputeReferences = true;
        options.DetailLevel = ViewDetailLevel.Fine;

        FaceArray faceArray = new FaceArray();

        GeometryElement geometryElement = element.get_Geometry(options);    //获取元素的几何数据
        foreach (GeometryObject geomobj in geometryElement)
        {
            if (geomobj is GeometryInstance)
            {
                GeometryInstance geometryInstance = (GeometryInstance)geomobj;
                GeometryElement geometry = geometryInstance.GetInstanceGeometry();
                foreach (GeometryObject geometryObject in geometry)
                {
                    Solid solid = geometryObject as Solid;
                    if (solid != null)
                    {
                        foreach (Face face in solid.Faces)
                        {
                            if (face != null)
                            {
                                faceArray.Append(face);
                            }
                            else
                            {
                                TaskDialog.Show("^-^", "实体的面为空" + "\n" + face.ToString());
                            }
                        }
                    }
                }
            }
            else if (geomobj is Solid)
            {
                Solid solid = geomobj as Solid;
                if (solid != null)
                {
                    foreach (Face face in solid.Faces)
                    {
                        if (face != null)
                        {
                            faceArray.Append(face);
                        }
                        else
                        {
                            TaskDialog.Show("^-^", "实体的面为空" + "\n" + face.ToString());
                        }
                    }
                }
            }
        }
        return faceArray;
    }


    /// <summary>
    /// 收集文档中所有梁的集合
    /// </summary>
    /// <param name="doc"></param>
    /// <returns></returns>
    public static Dictionary<ICollection<ElementId>, Document> CollectBeams(Document doc)
    {
        Dictionary<ICollection<ElementId>, Document> result = new Dictionary<ICollection<ElementId>, Document>();

        // 收集此文档中直接暴露的梁
        FilteredElementCollector collector = new FilteredElementCollector(doc);
        ElementCategoryFilter filter = new ElementCategoryFilter(BuiltInCategory.OST_StructuralFraming);
        ICollection<ElementId> docBeams = collector.WherePasses(filter).ToElementIds();
        if (docBeams.Count > 0)
        {
            result.Add(docBeams, doc);
        }

        // 收集链接文件中的梁
        FilteredElementCollector collector2 = new FilteredElementCollector(doc);
        ElementClassFilter filterLink = new ElementClassFilter(typeof(RevitLinkInstance));
        ICollection<ElementId> docLinks = collector2.WherePasses(filterLink).ToElementIds();
        if (docLinks.Count > 0)
        {
            foreach (ElementId elementId in docLinks)
            {
                RevitLinkInstance linkInstance = doc.GetElement(elementId) as RevitLinkInstance;
                Document linkdoc = linkInstance.GetLinkDocument();
                FilteredElementCollector linkCollector = new FilteredElementCollector(linkdoc);
                ICollection<ElementId> linkBeam = linkCollector.WherePasses(filter).ToElementIds();
                if (linkBeam.Count > 0)
                {
                    result.Add(linkBeam, linkdoc);
                }
            }
        }
        return result;
    }

    /// <summary>
    /// 收集管道与梁有交集的梁ID的集合
    /// </summary>
    /// <param name="doc">文档</param>
    /// <param name="beams">文档中梁的集合</param>
    /// <param name="elementId">管道ID</param>
    /// <returns></returns>
    public static ICollection<ElementId> CollectPipeCrossBeams(Document doc,
        ICollection<ElementId> beams, Element element)
    {
        ICollection<ElementId> result = new Collection<ElementId>();

        FilteredElementCollector collector = new FilteredElementCollector(doc, beams);
        ElementIntersectsElementFilter filter = new ElementIntersectsElementFilter(element);    //过滤与管道元素相交的过滤器
        result = collector.WherePasses(filter).ToElementIds();

        return result;
    }
}



/// <summary>
/// 获取面的中的信息
/// </summary>
public class GetInformaFromFace
{

    /// <summary>
    /// 整合本类的方法。
    /// 提供面的四个角点。
    /// </summary>
    /// <param name="face"></param>
    /// <returns></returns>
    public static XYZ[] GetInforma(Face face)
    {
        XYZ[] result = new XYZ[4];

        CurveArray curveArray = GetCureFromFace(face);
        result = GetXYZFromCurve(curveArray);

        return result;

    }

    //获取面的中的线
    static CurveArray GetCureFromFace(Face face)
    {
        CurveArray curveArray = new CurveArray();
        EdgeArrayArray edgeArrayArray = face.EdgeLoops; //得到面的边界线数组的数组
        foreach (EdgeArray edgeArray in edgeArrayArray)
        {
            foreach (Edge edge in edgeArray)
            {
                curveArray.Append(edge.AsCurve());
            }
        }
        return curveArray;
    }



    //得到线数组的点
    //排序方法为：xyz[4]=[左上，右上，左下，右下]
    static XYZ[] GetXYZFromCurve(CurveArray curveArray)
    {
        XYZ[] faceXYZ = new XYZ[curveArray.Size];
        int sortRule = 2;   //排序规则：0代表以X轴排序，1代表以Y轴排序，2代表Z轴

        faceXYZ = DuplicateRemoval(curveArray);

        XYZ[] result = FourXYZ(faceXYZ);

        MaoPaoMax(result, sortRule);

        ZMaoPaoMax(result);

        return result;
    }



    /// <summary>
    /// 点数组根据坐标轴的冒泡算法
    /// </summary>
    /// <param name="xyzArray">点数组</param>
    /// <param name="k">坐标轴</param>
    static void MaoPaoMax(XYZ[] xyzArray, int k)
    {
        XYZ temp = new XYZ();
        for (int i = 0; i < xyzArray.Length; i++)
        {
            for (int j = 0; j < xyzArray.Length - 1 - i; j++)
            {
                if (xyzArray[j][k] > xyzArray[j + 1][k])
                {
                    temp = xyzArray[j];
                    xyzArray[j] = xyzArray[j + 1];
                    xyzArray[j + 1] = temp;
                }
            }
        }
    }

    /// <summary>
    /// 实测一个面会出现超过4个点的情况，提取面的四个边点
    /// </summary>
    /// <param name="xYZs"></param>
    /// <returns></returns>
    static XYZ[] FourXYZ(XYZ[] xYZs)
    {
        XYZ[] result = new XYZ[4];

        MaoPaoMax(xYZs, 0);
        result[0] = xYZs[0];
        result[1] = xYZs[1];
        result[2] = xYZs[xYZs.Length - 1];
        result[3] = xYZs[xYZs.Length - 2];

        return result;
    }

    /// <summary>
    /// 在按Z轴排序的基础上，把Z轴相等的点按X轴排序
    /// </summary>
    /// <param name="xYZs"></param>
    static void ZMaoPaoMax(XYZ[] xYZs)
    {
        XYZ[] xYZs1 = new XYZ[2];
        XYZ[] xYZs2 = new XYZ[2];

        //Z轴高点在上，低点在下
        xYZs1[0] = xYZs[2];
        xYZs1[1] = xYZs[3];
        xYZs2[0] = xYZs[0];
        xYZs2[1] = xYZs[1];
        MaoPaoMax(xYZs1, 0);
        MaoPaoMax(xYZs2, 0);

        xYZs[0] = xYZs1[0];
        xYZs[1] = xYZs1[1];
        xYZs[2] = xYZs2[0];
        xYZs[3] = xYZs2[1];
    }


    /// <summary>
    /// 获取线数组的边点数组并去重
    /// </summary>
    /// <param name="curveArray">线数组</param>
    /// <returns></returns>
    static XYZ[] DuplicateRemoval(CurveArray curveArray)
    {
        XYZ[] curve = new XYZ[curveArray.Size * 2];
        XYZ[] result = new XYZ[curveArray.Size];
        int num = 0;
        int numre = 0;

        //获取边点数组
        foreach (Curve curve1 in curveArray)
        {
            curve.SetValue(curve1.GetEndPoint(0), num);
            num++;
            curve.SetValue(curve1.GetEndPoint(1), num);
            num++;
        }

        //去重
        for (int i = 0; i < curve.Length; i++)
        {
            bool Dup = true;
            for (int j = i + 1; j < curve.Length; j++)
            {
                if (Compare(curve[i], curve[j]))
                {
                    Dup = false;
                }
            }
            if (Dup)
            {
                result.SetValue(curve[i], numre);
                numre++;
            }
        }

        return result;
    }


    /// <summary>
    /// 比较两个XYZ点是否相等（获取的四边的8个点其中有些本应相等的点坐标却因为位数不同而不相等，故重新设计一个比较方法）
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    static bool Compare(XYZ a, XYZ b)
    {
        bool result;

        if (Math.Round(a[0], 8).Equals(Math.Round(b[0], 8)) &&
            Math.Round(a[1], 8).Equals(Math.Round(b[1], 8)) &&
            Math.Round(a[2], 8).Equals(Math.Round(b[2], 8)))
        {
            result = true;
        }
        else
        {
            result = false;
        }

        return result;
    }
}



/// <summary>
/// 查看管道位置是否符合规范
/// </summary>
public class CollisionResults
{
    /// <summary>
    /// 判断管道穿梁位置是否符合规范，返回一个bool值，true为符合规范，false为不符合规范
    /// </summary>
    /// <param name="xYZ">穿洞位置</param>
    /// <param name="xYZs">穿洞位置所处的梁的截面</param>
    /// <param name="pipeDe">管道的外径</param>
    /// <param name="bushing">管道的套管所占厚度</param>
    /// <param name="insulation">管道的绝热层厚度</param>
    /// <returns></returns>
    public static bool CollisionResult(XYZ xYZ, XYZ[] xYZs, Double pipeDe, Double bushing, Double insulation, out List<int> X)
    {
        bool result = true;
        XYZ[] xyzsTransform = new XYZ[4];
        XYZ xyzTransform = new XYZ();
        int i = 0;
        XYZ[] xyzPipeWidth = new XYZ[4];
        X = new List<int>();

        XYZ axis = XYZ.BasisZ;
        Double radian = Math.PI * 2 - DeflectionRadian(xYZs);
        Transform transform = Transform.CreateRotationAtPoint(axis, radian, xYZs[2]);

        foreach (XYZ xyz in xYZs)
        {
            xyzsTransform.SetValue(transform.OfPoint(xyz), i);
            i++;
        }

        xyzTransform = transform.OfPoint(xYZ);
        xyzPipeWidth = PipeHole(xyzTransform, pipeDe, bushing, insulation);

        Double holeHight = xyzPipeWidth[1][2] - xyzPipeWidth[3][2];
        Double beamHight = xyzsTransform[0][2] - xyzsTransform[2][2];

        Double xLendth = xyzsTransform[1][0] - xyzsTransform[0][0];
        Double zLendth = xyzsTransform[1][2] - xyzsTransform[3][2];
        XYZ x1_3 = xyzsTransform[0] + new XYZ(xLendth / 3, 0, 0);
        XYZ x2_3 = xyzsTransform[1] + new XYZ(-xLendth / 3, 0, 0);
        XYZ z1_3 = xyzsTransform[1] + new XYZ(0, 0, -200 / 304.8);
        XYZ z2_3 = xyzsTransform[3] + new XYZ(0, 0, 200 / 304.8);

        // 判断不符合哪天规范
        if (xyzPipeWidth[0][0] < x1_3[0] || xyzPipeWidth[2][0] > x2_3[0])
        {
            result = false;
            X.Add(0);
        }
        if (holeHight > beamHight * 0.4)
        {
            result = false;
            X.Add(1);
        }
        if (xyzPipeWidth[1][2] > z1_3[2] || xyzPipeWidth[3][2] < z2_3[2])
        {
            result = false;
            X.Add(2);
        }

        return result;

    }



    /// <summary>
    /// 需要把梁的面旋转为与x轴平行方便计算
    /// 此方法计算需要旋转的角度
    /// </summary>
    /// <param name="xYZs">梁的平面的四个点的数组</param>
    /// <returns></returns>
    public static Double DeflectionRadian(XYZ[] xYZs)
    {
        Double result = 0;

        Double xa = xYZs[1][0];
        Double xb = xYZs[0][0];

        Double ya = xYZs[1][1];
        Double yb = xYZs[0][1];

        Double x = xa - xb;
        Double y = ya - yb;


        result = Math.Atan2(y, x);

        return result;
    }


    /// <summary>
    /// 计算梁的面中穿梁管道外边的四个点（梁的面需与X轴平行）
    /// 返回数组方向为[左，上，右，下]
    /// </summary>
    /// <param name="xYZ">穿梁处的点</param>
    /// <param name="pipeDe">管道外径</param>
    /// <param name="bushing">套管厚度</param>
    /// <param name="insulation">绝热层厚度</param>
    /// <returns></returns>
    public static XYZ[] PipeHole(XYZ xYZ, Double pipeDe, Double bushing, Double insulation)
    {
        XYZ[] result = new XYZ[4];

        Double widthA = (pipeDe + bushing * 2 + insulation * 2) * 304.8;
        Double width = (BushingSize.BushingSizeOut(widthA) / 304.8) / 2;
        result[0] = xYZ + new XYZ(-width, 0, 0);
        result[1] = xYZ + new XYZ(0, 0, width);
        result[2] = xYZ + new XYZ(width, 0, 0);
        result[3] = xYZ + new XYZ(0, 0, -width);

        return result;
    }
}
    

