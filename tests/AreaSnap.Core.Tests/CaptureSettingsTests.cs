using AreaSnap.Core;
using AreaSnap.Capture;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AreaSnap.Core.Tests;

[TestClass]
public class CaptureSettingsTests
{
    [TestMethod]
    public void DefaultFileNameTemplate_IsValid()
    {
        Assert.AreEqual("AreaSnap-{yyyyMMdd-HHmmss}", CaptureSettings.DefaultFileNameTemplate);
    }

    [TestMethod]
    public void GenerateFileName_Png()
    {
        var name = ScreenCapture.GenerateFileName("AreaSnap-{yyyyMMdd-HHmmss}", OutputFormat.Png);
        Assert.IsTrue(name.EndsWith(".png"));
        Assert.IsTrue(name.StartsWith("AreaSnap-"));
    }

    [TestMethod]
    public void GenerateFileName_Jpg()
    {
        var name = ScreenCapture.GenerateFileName("AreaSnap-{yyyyMMdd-HHmmss}", OutputFormat.Jpg);
        Assert.IsTrue(name.EndsWith(".jpg"));
    }

    [TestMethod]
    public void OutputFormat_Values()
    {
        Assert.AreEqual(2, Enum.GetNames<OutputFormat>().Length);
        Assert.IsTrue(Enum.IsDefined(OutputFormat.Png));
        Assert.IsTrue(Enum.IsDefined(OutputFormat.Jpg));
    }

    [TestMethod]
    public void CaptureMode_Values()
    {
        Assert.AreEqual(3, Enum.GetNames<CaptureMode>().Length);
        Assert.IsTrue(Enum.IsDefined(CaptureMode.FullScreen));
        Assert.IsTrue(Enum.IsDefined(CaptureMode.Region));
        Assert.IsTrue(Enum.IsDefined(CaptureMode.Window));
    }
}