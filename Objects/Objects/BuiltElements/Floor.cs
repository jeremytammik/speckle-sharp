﻿using System.Collections.Generic;

namespace Objects.BuiltElements
{
  public class Floor : Element, IFloor
  {
    public ICurve outline { get; set; }
    public List<ICurve> voids { get; set; } = new List<ICurve>();

    public Floor()
    {
    }
  }
}