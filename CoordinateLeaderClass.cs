using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
namespace CoordinateLeader
{
    public class CoordinateLeaderClass
    {
        //<<<-----------default formats----------->>>
        public static readonly string DEFAULT_IDL_FORMAT = "E=<easting>\nN=<northing>";
        public static readonly string DEFAULT_IDML_FORMAT = "E=<easting>\nN=<northing>";
        //<<<-----------current formats----------->>>
        public static string IDLFormat = DEFAULT_IDL_FORMAT;
        public static string IDMLFormat = DEFAULT_IDML_FORMAT;
        //<<<-----------validate format----------->>>
        public static bool IsValidFormat(string fmt)
        {
            if (string.IsNullOrWhiteSpace(fmt)) return false;
            return fmt.Contains("<easting>") && fmt.Contains("<northing>");
        }
        //<<<-----------format coordinate----------->>>
        public static string FormatCoordinate(string fmt, double x, double y, string defaultFmt)
        {
            if (!IsValidFormat(fmt))
            {
                Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog("Invalid format found.\nUsing default format.");
                fmt = defaultFmt;
            }
            return fmt.Replace("<easting>", x.ToString("0.000")).Replace("<northing>", y.ToString("0.000"));
        }
        //<<<-----------IDL----------->>>
        [CommandMethod("IDL")]
        public void IDL()
        {
            CreateLeader(IDLFormat, DEFAULT_IDL_FORMAT, false);
        }
        //<<<-----------IDML----------->>>
        [CommandMethod("IDML")]
        public void IDML()
        {
            CreateLeader(IDMLFormat, DEFAULT_IDML_FORMAT, true);
        }
        //<<<-----------common leader creator----------->>>
        public void CreateLeader(string format, string defaultFormat, bool landing)
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            PromptPointResult ppr = ed.GetPoint("\nSelect point to annotate: ");
            if (ppr.Status != PromptStatus.OK) return;
            Point3d pt = ppr.Value;
            PromptPointOptions ppo = new PromptPointOptions("\nSpecify text location: ");
            ppo.BasePoint = pt;
            ppo.UseBasePoint = true;
            PromptPointResult ppr2 = ed.GetPoint(ppo);
            if (ppr2.Status != PromptStatus.OK) return;
            Point3d txtPt = ppr2.Value;
            string txt = FormatCoordinate(format, pt.X, pt.Y, defaultFormat);
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                MLeader ml = new MLeader();
                ml.SetDatabaseDefaults();
                ml.ContentType = ContentType.MTextContent;
                int leaderIdx = ml.AddLeader();
                int lineIdx = ml.AddLeaderLine(leaderIdx);
                ml.AddFirstVertex(lineIdx, pt);
                ml.AddLastVertex(lineIdx, txtPt);
                ml.EnableLanding = landing;
                ml.EnableDogleg = landing;
                MText mt = new MText();
                mt.SetDatabaseDefaults();
                mt.Location = txtPt;
                mt.TextHeight = db.Textsize > 0 ? db.Textsize : 2.5;
                mt.Contents = txt.Replace("\n", "\\P");
                ml.MText = mt;
                btr.AppendEntity(ml);
                tr.AddNewlyCreatedDBObject(ml, true);
                tr.Commit();
            }
        }
        //<<<-----------FormatIDLConfig----------->>>
        [CommandMethod("FormatIDLConfig")]
        public void FormatIDLConfig()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            PromptStringOptions pso = new PromptStringOptions("\nEnter IDL format.\nUse \\n for new line.\nExample:\nE=<easting>\\nN=<northing>\n\nFormat: ");
            pso.AllowSpaces = true;
            PromptResult pr = ed.GetString(pso);
            if (pr.Status != PromptStatus.OK) return;
            string fmt = pr.StringResult.Replace("\\n", "\n");
            if (!IsValidFormat(fmt))
            {
                Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog("Invalid format.\n\nFormat must contain:\n<easting>\n<northing>\n\nRestoring default.");
                IDLFormat = DEFAULT_IDL_FORMAT;
                return;
            }
            IDLFormat = fmt;
            ed.WriteMessage("\nIDL format updated.\n");
        }
        //<<<-----------FormatIDMLConfig----------->>>
        [CommandMethod("FormatIDMLConfig")]
        public void FormatIDMLConfig()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            PromptStringOptions pso = new PromptStringOptions("\nEnter IDML format.\nUse \\n for new line.\nExample:\nE=<easting>\\nN=<northing>\n\nFormat: ");
            pso.AllowSpaces = true;
            PromptResult pr = ed.GetString(pso);
            if (pr.Status != PromptStatus.OK) return;
            string fmt = pr.StringResult.Replace("\\n", "\n");
            if (!IsValidFormat(fmt))
            {
                Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog("Invalid format.\n\nFormat must contain:\n<easting>\n<northing>\n\nRestoring default.");
                IDMLFormat = DEFAULT_IDML_FORMAT;
                return;
            }
            IDMLFormat = fmt;
            ed.WriteMessage("\nIDML format updated.\n");
        }
        //<<<-----------ResetIDLConfig----------->>>
        [CommandMethod("ResetIDLConfig")]
        public void ResetIDLConfig()
        {
            IDLFormat = DEFAULT_IDL_FORMAT;
            Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog("IDL format reset.\n\n" + IDLFormat);
        }
        //<<<-----------ResetIDMLConfig----------->>>
        [CommandMethod("ResetIDMLConfig")]
        public void ResetIDMLConfig()
        {
            IDMLFormat = DEFAULT_IDML_FORMAT;
            Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog("IDML format reset.\n\n" + IDMLFormat);
        }
    }
}