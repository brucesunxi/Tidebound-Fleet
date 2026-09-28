using Tidebound.Collection;
using System.Linq;
using Tidebound.Save;
using Tidebound.Unity.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.LevelDesign
{
    public sealed partial class CollectionPanel
    {
        private RectTransform showcaseGrid;
        private readonly Button[] showcaseCards=new Button[ShowcaseCatalog.All.Count];
        private readonly RectTransform[] showcaseHeadings=new RectTransform[ShowcaseCatalog.Groups.Count];
        private void BuildShowcases()
        {
            showcaseGrid=HarborUI.Rect("ShowcaseGrid",categoryPanel);
            for(var i=0;i<ShowcaseCatalog.All.Count;i++)
            {
                var definition=ShowcaseCatalog.All[i];var card=HarborUI.Control("Showcase_"+definition.Id,showcaseGrid,definition.Name,()=>ChooseShowcase(definition.Id),true);
                showcaseCards[i]=card;
                var label=card.GetComponentInChildren<Text>();label.fontSize=16;label.alignment=TextAnchor.MiddleCenter;
                var art=HarborUI.Rect("Preview",card.transform);var preview=art.gameObject.AddComponent<CollectionShipPreview>();preview.Initialize(true,256);preview.PresentShowcase(definition.Id);preview.AllowMotion=()=>false;
                preview.SetWaterVisible(false);
                HarborUI.Label("Ownership",card.transform,"",13);
            }
            for(var i=0;i<showcaseHeadings.Length;i++)
            {
                var heading=HarborUI.Rect("Source_"+ShowcaseCatalog.Groups[i].Source,showcaseGrid);showcaseHeadings[i]=heading;
                var rule=heading.gameObject.AddComponent<ShowcaseSectionRule>();rule.color=new Color(.44f,.23f,.10f);rule.raycastTarget=false;
                var caption=HarborUI.Label("Heading",heading,"",20);caption.color=rule.color;caption.alignment=TextAnchor.MiddleCenter;
            }
        }
        public ShowcaseSelectionStatus ChooseShowcase(string id)
        {
            if(!gameObject.activeInHierarchy||HasConfirmation||category!=2||service==null)return ShowcaseSelectionStatus.Busy;
            var status=service.SelectShowcase(id);
            message.text=status==ShowcaseSelectionStatus.Saved||status==ShowcaseSelectionStatus.AlreadySelected?
                "Display ship saved.":status==ShowcaseSelectionStatus.Locked?"This display ship is not owned.":"Unable to save ship. Please retry.";
            PresentShowcases();return status;
        }
        private void PresentShowcases()
        {
            if(showcaseGrid==null)return;
            for(var i=0;i<showcaseCards.Length;i++)
            {
                var item=ShowcaseCatalog.All[i];var card=showcaseCards[i];var owned=service?.OwnsShowcase(item.Id)==true;
                var selected=service?.SelectedShowcaseId==item.Id;card.interactable=owned;
                HarborUI.SetSelected(card,selected);
                card.GetComponentInChildren<Text>().text=UILanguage.IsChinese?item.ChineseName:item.Name;
                card.transform.Find("Ownership").GetComponent<Text>().text=selected?"In use":owned?"Owned":
                    item.Acquisition==ShowcaseAcquisition.Level?(UILanguage.IsChinese?"通关 "+item.ClearLevel+" 关":"Level "+item.ClearLevel):(UILanguage.IsChinese?"未解锁":"Locked");
                card.transform.Find("Preview").GetComponent<CollectionShipPreview>().SetShowcaseOwned(owned);
            }
            if(category==2)categoryInfo.text=UILanguage.IsChinese?"选择已拥有的主页形象":"Choose an owned display ship.";
        }
        private void LayoutShowcases(float width,float height)
        {
            Place(showcaseGrid,new Rect(0,0,width,height));var cardW=(width-26)/4;var top=0f;
            for(var group=0;group<showcaseHeadings.Length;group++)
            {
                var definition=ShowcaseCatalog.Groups[group];var heading=showcaseHeadings[group];
                Place(heading,new Rect(0,height-top-32,width,32));
                var label=heading.GetComponentInChildren<Text>();label.text=UILanguage.IsChinese?definition.ChineseTitle:definition.Title;label.fontSize=UILanguage.IsChinese?20:18;
                var textWidth=UILanguage.IsChinese?100:126;Place(label.rectTransform,new Rect((width-textWidth)/2,0,textWidth,32));
                var rule=heading.GetComponent<ShowcaseSectionRule>();rule.CaptionWidth=textWidth;rule.SetAllDirty();
                top+=38;
                for(var i=0;i<definition.CatalogIndices.Count;i++)
                    Place((RectTransform)showcaseCards[definition.CatalogIndices[i]].transform,new Rect(4+i%4*(cardW+6),height-top-144-i/4*151,cardW,144));
                top+=((definition.CatalogIndices.Count+3)/4)*151+12;
            }
            for(var i=0;i<showcaseCards.Length;i++)
            {
                var card=showcaseCards[i];
                Place((RectTransform)card.transform.Find("Preview"),new Rect(-cardW*.15f,38,cardW*1.3f,100));
                var label=card.GetComponentInChildren<Text>();label.fontSize=UILanguage.IsChinese?13:11;Place(label.rectTransform,new Rect(3,26,cardW-6,30));
                var state=card.transform.Find("Ownership").GetComponent<Text>();state.fontSize=11;Place(state.rectTransform,new Rect(3,3,cardW-6,23));
            }
        }
        private static float ShowcaseListHeight()=>ShowcaseCatalog.Groups.Sum(g=>38+((g.CatalogIndices.Count+3)/4)*151+12)-7;
    }
}
