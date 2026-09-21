using UnityEngine.UI;

namespace Tidebound.Unity.UI
{
    /// <summary>Retains the source message key, including while inactive, so locale changes never translate a translation.</summary>
    public sealed class HarborText : Text
    {
        private string source="";
        public string Source => source;
        public override string text
        {
            get => base.text;
            set {var next=value??"";if(source==next)return;source=next;base.text=UILanguage.Translate(source);}
        }
        protected override void OnEnable(){base.OnEnable();UILanguage.Changed+=Refresh;Refresh();}
        protected override void OnDisable(){UILanguage.Changed-=Refresh;base.OnDisable();}
        private void Refresh(){base.text=UILanguage.Translate(source);font=HarborUI.Font;}
    }
}
