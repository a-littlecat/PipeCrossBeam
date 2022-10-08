using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using XPTools.Converter;

namespace XPTools
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]

    /// <summary>
    /// 创建插件的面板
    /// </summary>
    public class ExternalPanel : IExternalApplication
    {
        public Result OnShutdown(UIControlledApplication application)
        {    

            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {
            string toolsName = "XP工具箱";  // 面板的名称

            // 提供Revit命令的图标
            MyImageConverter converter = new MyImageConverter();
            ImageSource testImage = converter.ImageSourceFromBitmap(ImagesResource.test);

            // 此类库运行的完整地址
            string bath = Assembly.GetExecutingAssembly().Location;


            // 调试修改版的地址
            string bathbase = bath.Substring(0, bath.Length - @"ClassLibrary1\bin\Debug\XPTools.dll".Length);
            string pipeCrossBeamRuleAddress = bathbase + @"PipeCrossBeam\bin\Debug\PipeCrossBeam.dll";

            // 打包exe版的地址
            //string bathbase = bath.Substring(0, bath.Length - @"XPTools.dll".Length);
            //string pipeCrossBeamRuleAddress = bathbase + @"PipeCrossBeam\PipeCrossBeam.dll";

            // 创建面板
            RibbonPanel ribbonPanel = application.CreateRibbonPanel(toolsName);

            //创建功能按钮的信息
            PushButtonData pipeCrossBeamRule = new PushButtonData("pipeButton", "管道穿梁检测", pipeCrossBeamRuleAddress, "PipeCrossBeam.PipeCrossBeamRuleXP");
            pipeCrossBeamRule.LargeImage = testImage;

            // 面板添加功能按钮
            ribbonPanel.AddItem(pipeCrossBeamRule);

            return Result.Succeeded;
        }
    }
}
