﻿using ConnectorGrasshopper.Extras;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using GrasshopperAsyncComponent;
using Speckle.Core.Api;
using Speckle.Core.Logging;
using System;
using System.Linq;

namespace ConnectorGrasshopper.Conversion
{
  public class DeserializeObject : GH_AsyncComponent
  {
    public override Guid ComponentGuid { get => new Guid("CC6E8983-C6E9-47ED-8F63-8DB7D677B997"); }

    protected override System.Drawing.Bitmap Icon => Properties.Resources.Deserialize;

    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public DeserializeObject() : base("Deserialize", "Deserialize", "Deserializes a JSON string to a Speckle object.", "Speckle 2 Dev", "Conversion")
    {
      BaseWorker = new DeserializeWorker(this);
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddTextParameter("O", "O", "Speckle objects you want to serialize.", GH_ParamAccess.tree);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("S", "S", "Serialized objects.", GH_ParamAccess.tree);
    }

    protected override void BeforeSolveInstance()
    {
      Tracker.TrackPageview("serialization", "deserialize");
      base.BeforeSolveInstance();
    }
  }

  public class DeserializeWorker : WorkerInstance
  {
    GH_Structure<GH_String> Objects;
    GH_Structure<GH_SpeckleBase> ConvertedObjects;

    public DeserializeWorker(GH_Component parent) : base(parent)
    {
      Objects = new GH_Structure<GH_String>();
      ConvertedObjects = new GH_Structure<GH_SpeckleBase>();
    }

    public override void DoWork(Action<string, double> ReportProgress, Action Done)
    {
      if (CancellationToken.IsCancellationRequested) return;

      int branchIndex = 0, completed = 0;
      foreach (var list in Objects.Branches)
      {
        var path = Objects.Paths[branchIndex];
        foreach (var item in list)
        {
          if (CancellationToken.IsCancellationRequested) return;

          try
          {
            var deserialised = Operations.Deserialize(item.Value);
            ConvertedObjects.Append(new GH_SpeckleBase() { Value = deserialised }, Objects.Paths[branchIndex]);
          }
          catch (Exception e)
          {
            ConvertedObjects.Append(new GH_SpeckleBase() { Value = null }, Objects.Paths[branchIndex]);
            Parent.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Object at {Objects.Paths[branchIndex]} is not a Speckle object. Exception: {e.Message}.");
          }

          ReportProgress(Id, ((completed++ + 1) / (double)Objects.Count()));
        }

        branchIndex++;
      }

      Done();
    }

    public override WorkerInstance Duplicate() => new DeserializeWorker(Parent);

    public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
    {
      if (CancellationToken.IsCancellationRequested) return;

      GH_Structure<GH_String> _objects;
      DA.GetDataTree(0, out _objects);

      int branchIndex = 0;
      foreach (var list in _objects.Branches)
      {
        var path = _objects.Paths[branchIndex];
        foreach (var item in list)
        {
          Objects.Append(item, _objects.Paths[branchIndex]);
        }
        branchIndex++;
      }
    }

    public override void SetData(IGH_DataAccess DA)
    {
      if (CancellationToken.IsCancellationRequested) return;
      DA.SetDataTree(0, ConvertedObjects);
    }
  }
}