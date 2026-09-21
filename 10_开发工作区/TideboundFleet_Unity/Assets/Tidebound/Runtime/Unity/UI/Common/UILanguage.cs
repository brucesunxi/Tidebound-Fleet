using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Tidebound.Unity.UI
{
    public enum LanguagePreference { Auto, English, Chinese }
    public enum DeviceRegion { Unknown, China, Other }

    public static class UILanguage
    {
        private const string PreferenceKey="Tidebound.Language";
        private static bool initialized;
        private static LanguagePreference preference;
        private static DeviceRegion region;
        private static readonly Dictionary<string,string> exact=new Dictionary<string,string>(StringComparer.Ordinal);
        private static readonly List<KeyValuePair<Regex,string>> templates=new List<KeyValuePair<Regex,string>>();
        public static event Action Changed;
        public static LanguagePreference Preference {get{Ensure();return preference;}}
        public static DeviceRegion Region {get{Ensure();return region;}}
        public static bool IsChinese {get{Ensure();return Resolve(preference,region)==LanguagePreference.Chinese;}}
        public static bool Pseudo {get;private set;}
        public static LanguagePreference Resolve(LanguagePreference choice,DeviceRegion detected) =>
            choice==LanguagePreference.Chinese || choice==LanguagePreference.Auto && detected==DeviceRegion.China ? LanguagePreference.Chinese : LanguagePreference.English;
        public static DeviceRegion ReadRegion(Func<string> read)
        {
            try {var country=read()?.Trim().ToUpperInvariant();return country=="CN"?DeviceRegion.China:
                !string.IsNullOrEmpty(country) && Regex.IsMatch(country,"^[A-Z]{2}$") ? DeviceRegion.Other:DeviceRegion.Unknown;}
            catch {return DeviceRegion.Unknown;}
        }
        private static string PlatformCountry()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using(var player=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using(var activity=player.GetStatic<AndroidJavaObject>("currentActivity"))
            using(var telephony=activity.Call<AndroidJavaObject>("getSystemService","phone"))
                return telephony?.Call<string>("getNetworkCountryIso");
#else
            // iOS/desktop have no verified offline current-country provider here.
            // A language, time zone or user-configured region is not geolocation.
            return null;
#endif
        }
        private static void Ensure()
        {
            if(initialized)return;initialized=true;
            var saved=PlayerPrefs.GetInt(PreferenceKey,0);preference=saved>=0 && saved<=2?(LanguagePreference)saved:LanguagePreference.Auto;
            region=ReadRegion(PlatformCountry);
            var asset=Resources.Load<TextAsset>("TideboundUI/Locale");
            if(asset==null)return;
            foreach(var line in asset.text.Split('\n'))
            {
                var parts=line.TrimEnd('\r').Split('\t');if(parts.Length!=2)continue;
                var key=parts[0].Replace("\\n","\n");var value=parts[1].Replace("\\n","\n");
                exact[key]=value;
                if(!Regex.IsMatch(key,@"\{\d+\}"))continue;
                var expression=Regex.Escape(key);
                expression=Regex.Replace(expression,@"\\\{(\d+)\}",m=>"(?<p"+m.Groups[1].Value+">.*?)");
                templates.Add(new KeyValuePair<Regex,string>(new Regex("^"+expression+"$",RegexOptions.Singleline|RegexOptions.CultureInvariant),value));
            }
        }
        public static void SetPreference(LanguagePreference value,bool persist=true)
        {
            Ensure();if(!Enum.IsDefined(typeof(LanguagePreference),value))throw new ArgumentOutOfRangeException(nameof(value));
            preference=value;
            if(persist){PlayerPrefs.SetInt(PreferenceKey,(int)value);PlayerPrefs.Save();}
            Changed?.Invoke();
        }
        public static void SetPseudoForReview(bool value){Pseudo=value;Changed?.Invoke();}
        public static void ReloadPreference(){initialized=false;exact.Clear();templates.Clear();Ensure();Changed?.Invoke();}
        public static string Translate(string source)
        {
            if(string.IsNullOrEmpty(source))return source??"";Ensure();
            if(Pseudo && Regex.IsMatch(source,@"[A-Za-z]"))return "["+source+new string('·',Math.Max(1,source.Length/4))+"]";
            return IsChinese?TranslateChinese(source,0):source;
        }
        private static string TranslateChinese(string source,int depth)
        {
            if(depth>6 || string.IsNullOrWhiteSpace(source))return source;
            source=source.Trim();
            if(exact.TryGetValue(source,out var found))return found;
            foreach(var pair in templates)
            {
                var match=pair.Key.Match(source);if(!match.Success)continue;
                return Regex.Replace(pair.Value,@"\{(\d+)\}",m=>TranslateChinese(match.Groups["p"+m.Groups[1].Value].Value,depth+1));
            }
            foreach(var delimiter in new[]{"\n","  |  "," | ","  /  "," / ","  "})
                if(source.Contains(delimiter))return string.Join(delimiter,source.Split(new[]{delimiter},StringSplitOptions.None).Select(s=>TranslateChinese(s,depth+1)));
            return source;
        }
    }
}
