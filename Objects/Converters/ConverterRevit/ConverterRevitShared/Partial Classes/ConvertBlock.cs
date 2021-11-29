﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Autodesk.Revit.DB;
using Objects.Geometry;
using Speckle.Core.Logging;
using DB = Autodesk.Revit.DB;
using Mesh = Objects.Geometry.Mesh;
using BlockInstance = Objects.Other.BlockInstance;
using Transform = Objects.Other.Transform;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public Group BlockInstanceToNative(BlockInstance instance, Transform transform = null)
    {
      // need to combine the two transforms, but i'm stupid and did it wrong so leaving like this for now
      if ( transform != null )
        transform *= new Transform(instance.transform);
      else
        transform = new Transform(instance.transform);
      var applyTransform = transform.isScaled;

      // convert definition geometry to native
      var breps = new List<Brep>();
      var meshes = new List<Mesh>();
      var curves = new List<DB.Curve>();
      var blocks = new List<BlockInstance>();
      foreach ( var geometry in instance.blockDefinition.geometry )
      {
        switch ( geometry )
        {
          case Brep brep:
            try
            {
              breps.Add(brep);
            }
            catch ( Exception e )
            {
              Report.LogConversionError(new SpeckleException(
                $"Could not convert block {instance.id} brep to native, falling back to mesh representation.", e));
              meshes.Add(brep.displayMesh);
            }

            break;
          case Mesh mesh:
            if ( applyTransform )
              mesh = mesh.Transform(transform);
            meshes.Add(mesh);
            break;
          case ICurve curve:
            try
            {
              var modelCurves = CurveToNative(curve);
              curves.AddRange(modelCurves.Cast<DB.Curve>());
            }
            catch ( Exception e )
            {
              Report.LogConversionError(
                new SpeckleException($"Could not convert block {instance.id} curve to native.", e));
            }

            break;
          case BlockInstance blk:
            blocks.Add(blk);
            break;
        }
      }

      var ids = new List<ElementId>();
      breps.ForEach(o => { ids.Add(( DirectShapeToNative(o).NativeObject as DB.DirectShape )?.Id); });
      meshes.ForEach(o => { ids.Add(( DirectShapeToNative(o).NativeObject as DB.DirectShape )?.Id); });
      curves.ForEach(o => { ids.Add(Doc.Create.NewModelCurve(o, NewSketchPlaneFromCurve(o, Doc)).Id); });
      blocks.ForEach(o => { ids.Add(BlockInstanceToNative(o, transform).Id); });

      var group = Doc.Create.NewGroup(ids);
      group.GroupType.Name = $"SpeckleBlock_{instance.blockDefinition.name}_{instance.applicationId ?? instance.id}";
      return group;
    }


    private bool MatrixDecompose(double[ ] m, out double rotation)
    {
      var matrix = new Matrix4x4(
        ( float ) m[ 0 ], ( float ) m[ 1 ], ( float ) m[ 2 ], ( float ) m[ 3 ],
        ( float ) m[ 4 ], ( float ) m[ 5 ], ( float ) m[ 6 ], ( float ) m[ 7 ],
        ( float ) m[ 8 ], ( float ) m[ 9 ], ( float ) m[ 10 ], ( float ) m[ 11 ],
        ( float ) m[ 12 ], ( float ) m[ 13 ], ( float ) m[ 14 ], ( float ) m[ 15 ]);

      if ( Matrix4x4.Decompose(matrix, out Vector3 _scale, out Quaternion _rotation, out Vector3 _translation) )
      {
        rotation = Math.Acos(_rotation.W) * 2;
        return true;
      }
      else
      {
        rotation = 0;
        return false;
      }
    }
  }
}