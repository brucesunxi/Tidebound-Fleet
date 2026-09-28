using System;
using Tidebound.Unity.LevelDesign;
using Tidebound.Unity.UI;
using UnityEngine;

namespace Tidebound.Unity.Ship
{
    /// <summary>Records actual stern motion in the board plane. Never moves a ship or influences occupancy.</summary>
    public sealed class ShipWakePresentation : MonoBehaviour
    {
        private Transform ship;
        private ShipMovementView movement;
        private PlanarShipLaneView lane;
        private Func<bool> pause;
        private Vector2 previous;
        private bool initialized;
        public HarborWakeRibbon Ribbon {get;private set;}
        public void Initialize(Transform target,string style,Func<bool> isPaused)
        {
            ship=target;movement=target.GetComponent<ShipMovementView>();lane=target.GetComponent<PlanarShipLaneView>();pause=isPaused;
            Ribbon=gameObject.AddComponent<HarborWakeRibbon>();Ribbon.raycastTarget=false;
            Ribbon.Length=2.4f;Ribbon.Lifetime=.68f;Ribbon.SampleSpacing=.025f;Ribbon.color=new Color(1,1,1,.64f);
            var art=HarborAppearanceArt.Trail(style);Ribbon.SetStyle(Resources.Load<Texture2D>(art.resourcePath),HarborAppearanceArt.UV(art.uv),art.aspect);
            Ribbon.Width=Mathf.Min(.84f,Ribbon.Width);
        }
        private void LateUpdate()
        {
            if(!Ribbon||!ship)return;
            if(pause?.Invoke()==true||movement.IsPaused)return;
            var point=(Vector2)transform.InverseTransformPoint(ship.TransformPoint(new Vector3(0,-.4f,0)));
            var delta=initialized?Vector2.Distance(previous,point):0;
            var travel=ship.gameObject.activeInHierarchy&&(movement.IsTravelling||lane.HasEnteredLane)&&delta>.0001f;
            if(delta>Ribbon.Length*2)Ribbon.ClearHistory();
            Ribbon.Advance(Time.unscaledDeltaTime,point,travel&&delta<=Ribbon.Length*2);
            previous=point;initialized=true;
        }
    }
}
