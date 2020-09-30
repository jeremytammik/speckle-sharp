﻿using ConnectorGrashopper.Extras;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ConnectorGrashopper.Objects
{
  public class ExpandSpeckleObject : GH_Component, IGH_VariableParameterComponent
  {
    public override Guid ComponentGuid { get => new Guid("cfa4e9b4-3ae4-4bb9-90d8-801c34e9a37e"); }

    protected override System.Drawing.Bitmap Icon { get => null; }

    private ISpeckleConverter Converter;

    private ISpeckleKit Kit;

    public ExpandSpeckleObject()
      : base("Create Speckle Object", "CSO",
          "Allows you to create a Speckle object by setting its keys and values.",
          "Speckle 2", "Object Management")
    {
      Kit = KitManager.GetDefaultKit();
      try
      {
        Converter = Kit.LoadConverter(Applications.Rhino);
        Message = $"Using the \n{Kit.Name}\n Kit Converter";
      }
      catch
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No default kit found on this machine.");
      }
    }

    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      Menu_AppendSeparator(menu);
      Menu_AppendItem(menu, "Select the converter you want to use:");

      var kits = KitManager.GetKitsWithConvertersForApp(Applications.Rhino);

      foreach (var kit in kits)
      {
        Menu_AppendItem(menu, $"{kit.Name} ({kit.Description})", (s, e) => { SetConverterFromKit(kit.Name); }, true, kit.Name == Kit.Name);
      }

      Menu_AppendSeparator(menu);
    }

    private void SetConverterFromKit(string kitName)
    {
      if (kitName == Kit.Name) return;

      Kit = KitManager.Kits.FirstOrDefault(k => k.Name == kitName);
      Converter = Kit.LoadConverter(Applications.Rhino);

      Message = $"Using the {Kit.Name} Converter";
      ExpireSolution(true);
    }

    public override bool Read(GH_IReader reader)
    {
      // TODO: Read kit name and instantiate converter
      return base.Read(reader);
    }

    public override bool Write(GH_IWriter writer)
    {
      // TODO: Write kit name to disk
      return base.Write(writer);
    }

    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    {
    }

    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("Debug", "d", "debug output, please ignore", GH_ParamAccess.list);
      pManager.AddGenericParameter("Object", "O", "The created object", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      // TODO
    }

    private object TryConvertItem(object value)
    {
      object result = null;

      if (value is Grasshopper.Kernel.Types.IGH_Goo)
      {
        value = value.GetType().GetProperty("Value").GetValue(value);
      }

      if (value is Base || Speckle.Core.Models.Utilities.IsSimpleType(value.GetType()))
      {
        return value;
      }

      if (Converter.CanConvertToSpeckle(value))
      {
        return Converter.ConvertToSpeckle(value);
      }

      return result;
    }

    public bool CanInsertParameter(GH_ParameterSide side, int index) => false;

    public bool CanRemoveParameter(GH_ParameterSide side, int index) => false;

    public IGH_Param CreateParameter(GH_ParameterSide side, int index)
    {
      var myParam = new Param_GenericAccess
      {
        Name = GH_ComponentParamServer.InventUniqueNickname("ABCD", Params.Input),
        MutableNickName = true,
        Optional = true
      };

      myParam.NickName = myParam.Name;

      myParam.AttributesChanged += (sender, e) => { Debug.WriteLine($"Attributes Changes."); };
      myParam.ObjectChanged += (sender, e) => { Debug.WriteLine($"Object Changes."); };

      return myParam;
    }

    public bool DestroyParameter(GH_ParameterSide side, int index)
    {
      return true;
    }

    public void VariableParameterMaintenance()
    {
    }

  }
}
