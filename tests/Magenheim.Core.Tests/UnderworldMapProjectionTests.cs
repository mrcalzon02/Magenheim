using System;
using Magenheim.Core.Underworld;

internal static class UnderworldMapProjectionTests
{
    public static int Run()
    {
        var n=0; void A(bool ok,string m){n++;if(!ok)throw new InvalidOperationException("Underworld map projection assertion "+n+" failed: "+m);}
        var d=UnderworldSpatialDomain.CreateDefault();
        A(UnderworldMapProjection.LayerAtWorldColumn(d,0,0)==MagenheimMapLayer.Surface,"origin is surface");
        A(UnderworldMapProjection.LayerAtWorldColumn(d,d.HostCenterX,d.HostCenterZ)==MagenheimMapLayer.Underworld,"host centre is Underworld");
        var local=UnderworldMapProjection.WorldToLayer(d,MagenheimMapLayer.Underworld,d.HostCenterX+123,d.HostCenterZ-456);
        A(Math.Abs(local.X-123)<0.001&&Math.Abs(local.Z+456)<0.001,"host offset must disappear from map projection");
        var host=UnderworldMapProjection.LayerToWorld(d,MagenheimMapLayer.Underworld,local.X,local.Z);
        A(Math.Abs(host.X-(d.HostCenterX+123))<0.001&&Math.Abs(host.Z-(d.HostCenterZ-456))<0.001,"projection round trips");
        A(UnderworldMapProjection.UnderworldBiomeAt(d,12345,0,0)==UnderworldTerrainBiome.FungalForest,"logical centre uses canonical Fungal biome");
        A(UnderworldMapProjection.CellIndex(8,8,3,2)==19,"exploration cells have deterministic row-major identity");
        return n;
    }
}
