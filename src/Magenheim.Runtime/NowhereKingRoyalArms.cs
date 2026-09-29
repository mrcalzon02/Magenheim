using System;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Binds the authored Last Argument pair to the King's actual humanoid hands. At 35% health each
/// client locally leaves the visible blades driven into the dais while gameplay remains server-owned.
/// </summary>
internal sealed class NowhereKingRoyalArms : MonoBehaviour
{
    private Character _king=null!;
    private Transform? _leftHand,_rightHand,_leftForearm,_rightForearm;
    private GameObject? _firmament,_nullGate;
    private bool _built,_abandoned;

    private void Awake()=>_king=GetComponent<Character>();

    private void Start()
    {
        Build();
        UpdatePresentation();
    }

    private void Update()
    {
        if(!_built||_king==null||_king.IsDead())return;
        UpdatePresentation();
    }

    internal void ResetPresentation()
    {
        if(!_built)Build();
        if(!_built)return;
        AttachToHands();
        _abandoned=false;
    }

    private void Build()
    {
        if(_built)return;
        var skeleton=HumanoidSegmentBinder.DonorSkeleton.Resolve(gameObject);
        _leftHand=skeleton["LeftHand"];
        _rightHand=skeleton["RightHand"];
        _leftForearm=skeleton["LeftForeArm"];
        _rightForearm=skeleton["RightForeArm"];
        if(_leftHand==null||_rightHand==null||_leftForearm==null||_rightForearm==null)
            throw new InvalidOperationException("Nowhere King humanoid donor lacks the hand/forearm roles required for twin swords.");

        _firmament=ModelAssets.Load(
            gameObject,NowhereKingRewardRegistrar.FirmamentModelId,
            item:false,hideOriginal:false,parent:_rightHand);
        _nullGate=ModelAssets.Load(
            gameObject,NowhereKingRewardRegistrar.NullGateModelId,
            item:false,hideOriginal:false,parent:_leftHand);

        Orient(_firmament.transform,_rightHand,_rightForearm);
        Orient(_nullGate.transform,_leftHand,_leftForearm);
        _built=true;
    }

    private void UpdatePresentation()
    {
        var shouldAbandon=_king.GetHealthPercentage()<=.35f;
        if(shouldAbandon&&!_abandoned)Abandon();
        else if(!shouldAbandon&&_abandoned)ResetPresentation();
    }

    private void AttachToHands()
    {
        if(_firmament!=null&&_rightHand!=null&&_rightForearm!=null)
        {
            _firmament.transform.SetParent(_rightHand,false);
            Orient(_firmament.transform,_rightHand,_rightForearm);
            _firmament.SetActive(true);
        }
        if(_nullGate!=null&&_leftHand!=null&&_leftForearm!=null)
        {
            _nullGate.transform.SetParent(_leftHand,false);
            Orient(_nullGate.transform,_leftHand,_leftForearm);
            _nullGate.SetActive(true);
        }
    }

    private void Abandon()
    {
        if(_firmament==null||_nullGate==null)return;
        var basePoint=transform.position;
        DriveIntoDais(_firmament.transform,basePoint-transform.right*1.35f);
        DriveIntoDais(_nullGate.transform,basePoint+transform.right*1.35f);
        _abandoned=true;
    }

    private void DriveIntoDais(Transform sword,Vector3 point)
    {
        sword.SetParent(null,true);
        sword.position=point+Vector3.up*1.05f;
        sword.rotation=Quaternion.LookRotation(Vector3.down,transform.forward);
    }

    private static void Orient(Transform sword,Transform hand,Transform forearm)
    {
        var outward=hand.position-forearm.position;
        if(outward.sqrMagnitude<1e-5f)outward=hand.forward;
        outward.Normalize();
        var up=Vector3.up-Vector3.Dot(Vector3.up,outward)*outward;
        if(up.sqrMagnitude<1e-5f)up=hand.up;
        var world=Quaternion.LookRotation(outward,up.normalized);
        sword.localPosition=Vector3.zero;
        sword.localRotation=Quaternion.Inverse(hand.rotation)*world;
        sword.localScale=Vector3.one;
    }

    private void OnDestroy()
    {
        if(_firmament!=null)Destroy(_firmament);
        if(_nullGate!=null)Destroy(_nullGate);
    }
}
