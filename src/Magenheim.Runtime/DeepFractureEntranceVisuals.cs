using System;
using System.Collections.Generic;
using System.Linq;
using Magenheim.Core;
using Magenheim.Core.DeepFractures;
using UnityEngine;
namespace Magenheim.Runtime;
internal static class DeepFractureEntranceVisuals {
    internal const string InteriorAnchorName="Magenheim_DeepFracture_InteriorAnchor";
    internal static void Build(GameObject locationContainer){ModelAssets.Load(locationContainer,"deep-fracture-entrance",hideOriginal:false);var anchor=new GameObject(InteriorAnchorName);anchor.transform.SetParent(locationContainer.transform,false);anchor.transform.localPosition=new Vector3(0,1.15f,-1.2f);}
}
