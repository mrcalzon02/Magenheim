using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>One-shot presentation state at 70% and 35%; persisted on the King's ZDO for reconstruction safety.</summary>
internal sealed class NowhereKingPhaseTransition : MonoBehaviour
{
    private const string Transition70="magenheim.nowhereking.transition70",Transition35="magenheim.nowhereking.transition35";
    private Character _king; private ZNetView _view;
    private void Awake(){_king=GetComponent<Character>();_view=GetComponent<ZNetView>();}
    private void Update(){if(_king==null||_king.IsDead()||_view==null||!_view.IsValid()||!_view.IsOwner())return;var hp=_king.GetHealthPercentage();var zdo=_view.GetZDO();if(hp<=.70f&&!zdo.GetBool(Transition70,false)){zdo.Set(Transition70,true);Pulse(new Color(.16f,.04f,.22f),3.8f,1.5f);}if(hp<=.35f&&!zdo.GetBool(Transition35,false)){zdo.Set(Transition35,true);Pulse(new Color(.34f,.03f,.45f),5.5f,2.2f);}}
    private void Pulse(Color color,float range,float intensity){var marker=new GameObject("NowhereKing_PhasePulse");marker.transform.SetParent(transform,false);marker.transform.localPosition=Vector3.up*1.7f;var light=marker.AddComponent<Light>();light.color=color;light.range=range;light.intensity=intensity;Destroy(marker,2.5f);}
}
